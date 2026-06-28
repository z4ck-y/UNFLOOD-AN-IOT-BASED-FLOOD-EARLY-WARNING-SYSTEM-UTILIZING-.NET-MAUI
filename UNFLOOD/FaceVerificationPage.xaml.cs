using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

#if WINDOWS
using OpenCvSharp;
using OpenCvSharp.Face;
#endif

namespace UNFLOOD
{
    public partial class FaceVerificationPage : ContentPage
    {
        private readonly string _username;
        private readonly string _role;
        private readonly string _dbPhotoPath;

        private CancellationTokenSource? _scanCts;
        private bool _isNavigating;

        // Semakin kecil nilai confidence LBPH, semakin ketat verifikasi keaslian wajah.
        private const double MatchThreshold = 60.0;
        private const int RequiredSuccessFrames = 4;

        public FaceVerificationPage(string username, string role)
        {
            InitializeComponent();
            _username = username;
            _role = role;

            _dbPhotoPath = Path.Combine(FileSystem.AppDataDirectory, $"{username}_face.png");
            LabelRole.Text = $"Live Face Lock | User: {username} | Hak Akses: {role}";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            StartLiveFaceScan();
        }

        protected override void OnDisappearing()
        {
            StopLiveFaceScan();
            base.OnDisappearing();
        }

        private void OnStartScanClicked(object sender, EventArgs e)
        {
            StartLiveFaceScan();
        }

        private void StartLiveFaceScan()
        {
            StopLiveFaceScan();
            _isNavigating = false;
            _scanCts = new CancellationTokenSource();

            ScanIndicator.IsVisible = true;
            ScanIndicator.IsRunning = true;

            LabelSimilarity.Text = "Status: menginisialisasi kamera laptop...";
            LabelStatus.Text = "Posisikan wajah lurus menghadap kamera untuk verifikasi biometrik.";

#if WINDOWS
            Task.Run(() => RunWindowsLiveScanAsync(_scanCts.Token));
#else
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                ScanIndicator.IsRunning = false;
                ScanIndicator.IsVisible = false;
                LabelSimilarity.Text = "Live scan biometrik memerlukan arsitektur Windows OS.";
                await DisplayAlert("Target Platform Salah", "Silakan jalankan proyek UNFLOOD ini pada mode target Windows Machine.", "OK");
            });
#endif
        }

        private void StopLiveFaceScan()
        {
            try
            {
                _scanCts?.Cancel();
                _scanCts?.Dispose();
                _scanCts = null;
            }
            catch { }
        }

#if WINDOWS
        private async Task RunWindowsLiveScanAsync(CancellationToken token)
        {
            if (!File.Exists(_dbPhotoPath))
            {
                await ShowMessageOnUiAsync("Kredensial Biometrik Hilang", "Sampel citra wajah pendaftaran tidak ditemukan. Silakan lakukan registrasi ulang akun.");
                return;
            }

            string cascadePath = await EnsureCascadeFileAsync();
            using var faceCascade = new CascadeClassifier(cascadePath);

            if (faceCascade.Empty())
            {
                await ShowMessageOnUiAsync("Kegagalan Engine", "Berkas arsitektur Haar Cascade XML tidak dapat dimuat.");
                return;
            }

            using var referenceImage = Cv2.ImRead(_dbPhotoPath, ImreadModes.Color);
            using var referenceFace = ExtractNormalizedFace(referenceImage, faceCascade);

            if (referenceFace.Empty())
            {
                await ShowMessageOnUiAsync("Kualitas Foto Rendah", "Citra pendaftaran tidak mendeteksi boks wajah yang presisi. Buat akun baru dengan cahaya terang.");
                return;
            }

            // Konfigurasi Local Binary Patterns Histograms Face Recognizer
            using var recognizer = LBPHFaceRecognizer.Create(radius: 1, neighbors: 8, gridX: 8, gridY: 8);
            using var flippedReference = new Mat();
            Cv2.Flip(referenceFace, flippedReference, FlipMode.Y);

            // Training terisolasi menggunakan sampel lokal pengguna
            recognizer.Train(new[] { referenceFace, flippedReference }, new[] { 1, 1 });

            using var capture = new VideoCapture(0);
            if (!capture.IsOpened())
            {
                await ShowMessageOnUiAsync("Hardware Error", "Kamera laptop tidak merespons. Pastikan tidak terkunci oleh aplikasi komunikasi lain.");
                return;
            }

            int successFrames = 0;
            using var frame = new Mat();
            using var gray = new Mat();

            while (!token.IsCancellationRequested && !_isNavigating)
            {
                capture.Read(frame);
                if (frame.Empty())
                {
                    await Task.Delay(80, token);
                    continue;
                }

                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.EqualizeHist(gray, gray);

                var faces = faceCascade.DetectMultiScale(
                    gray, scaleFactor: 1.1, minNeighbors: 5,
                    flags: HaarDetectionTypes.ScaleImage, minSize: new OpenCvSharp.Size(90, 90));

                string statusText = "Status: wajah tidak terdeteksi";

                if (faces.Length > 0)
                {
                    var faceRect = faces.OrderByDescending(r => r.Width * r.Height).First();
                    Cv2.Rectangle(frame, faceRect, Scalar.LimeGreen, 2); // Menggambar boks pelacak hijau

                    using var liveFace = NormalizeFaceFromGray(gray, faceRect);
                    recognizer.Predict(liveFace, out int label, out double confidence);

                    bool isMatch = label == 1 && confidence <= MatchThreshold;
                    successFrames = isMatch ? successFrames + 1 : 0;

                    double similarity = Math.Max(0, Math.Min(100, 100 - confidence));
                    statusText = isMatch
                        ? $"Wajah Sesuai {successFrames}/{RequiredSuccessFrames} | Kedekatan ± {similarity:F1}%"
                        : $"Kredensial Wajah Berbeda | Kedekatan ± {similarity:F1}%";

                    if (successFrames >= RequiredSuccessFrames)
                    {
                        _isNavigating = true;
                        await NavigateToDashboardAsync();
                        break;
                    }
                }
                else
                {
                    successFrames = 0;
                }

                await UpdatePreviewOnUiAsync(frame, statusText);
                await Task.Delay(60, token);
            }
        }

        private static Mat ExtractNormalizedFace(Mat image, CascadeClassifier faceCascade)
        {
            using var gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.EqualizeHist(gray, gray);

            var faces = faceCascade.DetectMultiScale(gray, 1.1, 5, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(90, 90));
            if (faces.Length == 0) return new Mat();

            var faceRect = faces.OrderByDescending(r => r.Width * r.Height).First();
            return NormalizeFaceFromGray(gray, faceRect);
        }

        private static Mat NormalizeFaceFromGray(Mat gray, OpenCvSharp.Rect faceRect)
        {
            var face = new Mat(gray, faceRect);
            var resized = new Mat();
            Cv2.Resize(face, resized, new OpenCvSharp.Size(160, 160));
            Cv2.EqualizeHist(resized, resized);
            return resized;
        }

        private static async Task<string> EnsureCascadeFileAsync()
        {
            string targetPath = Path.Combine(FileSystem.AppDataDirectory, "haarcascade_frontalface_default.xml");
            if (File.Exists(targetPath)) return targetPath;

            await using var input = await FileSystem.OpenAppPackageFileAsync("haarcascade_frontalface_default.xml");
            await using var output = File.Create(targetPath);
            await input.CopyToAsync(output);
            return targetPath;
        }

        private Task UpdatePreviewOnUiAsync(Mat frame, string statusText)
        {
            Cv2.ImEncode(".jpg", frame, out var imageBytes);
            return MainThread.InvokeOnMainThreadAsync(() =>
            {
                LabelSimilarity.Text = statusText;
                CameraPreview.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
            });
        }
#endif

        private Task ShowMessageOnUiAsync(string title, string message)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ScanIndicator.IsRunning = false;
                ScanIndicator.IsVisible = false;
                LabelSimilarity.Text = message;
                await DisplayAlert(title, message, "OK");
            });
        }

        private Task NavigateToDashboardAsync()
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                StopLiveFaceScan();
                ScanIndicator.IsRunning = false;
                ScanIndicator.IsVisible = false;
                LabelSimilarity.Text = "AKSES DITERIMA. Menuju Dasbor SCADA...";

                await DisplayAlert("OTORISASI BERHASIL", "Pola biometrik wajah cocok. Hak akses penuh dibuka.", "OK");

                string savedRole = Preferences.Get("session_role", "Warga");
                Application.Current!.MainPage = new MainDashboardPage(savedRole);
            });
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            StopLiveFaceScan();
            Application.Current!.MainPage = new MainPage();
        }
    }
}