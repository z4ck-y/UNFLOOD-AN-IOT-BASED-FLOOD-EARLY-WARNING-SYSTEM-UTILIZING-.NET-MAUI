using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System;
using System.Threading.Tasks;
using UNFLOOD.Models;
using UNFLOOD.Services;

namespace UNFLOOD
{
    public partial class MainDashboardPage : ContentPage
    {
        private readonly string _userRole;
        private readonly IDispatcherTimer _telemetryTimer;
        private readonly Random _random = new Random();

        private double simTma = 45.0;
        private double simFlow = 15.0;

        private double _tmaSebelumnya = 0.0;
        private DateTime _waktuInputSebelumnya = DateTime.Now;

        private readonly FlowGaugeDrawable _gaugeDrawable;

        public MainDashboardPage(string role)
        {
            InitializeComponent();

            _userRole = role;
            TxtWelcomeRole.Text = $"Role Terverifikasi: {_userRole}";

            BtnPemicuSirine.IsVisible = false;

            _gaugeDrawable = new FlowGaugeDrawable();
            GaugeFlowView.Drawable = _gaugeDrawable;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await SegarkanTabelLogAsync();
            });

            string sapaan = _userRole == "BMKG"
                ? "Sistem Override Siaga Aktif. Silakan masukkan komando teks ekstraksi parameter untuk memaksa pembaruan data lapangan."
                : "Halo! Saya Asisten Mitigasi Cerdas UNFLOOD. Ada kondisi genangan atau banjir di sekitar Keputih yang ingin Anda tanyakan?";

            AddChatBubble(sapaan, false);

            _telemetryTimer = Dispatcher.CreateTimer();
            _telemetryTimer.Interval = TimeSpan.FromSeconds(3);
            _telemetryTimer.Tick += OnFetchTelemetryTick;
            _telemetryTimer.Start();
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            _telemetryTimer.Stop();
            if (Application.Current != null)
            {
                Application.Current.MainPage = new MainPage();
            }
        }

        private async Task SegarkanTabelLogAsync()
        {
            try
            {
                var daftarRiwayat = await DatabaseService.AmbilSemuaRiwayatAsync();
                ViewTabelLog.ItemsSource = daftarRiwayat;
            }
            catch
            {
                // Proteksi thread aman jika io database sedang dikunci
            }
        }

        private void AddChatBubble(string message, bool isUser)
        {
            var bubbleShape = new RoundRectangle
            {
                CornerRadius = isUser ? new CornerRadius(15, 15, 0, 15) : new CornerRadius(15, 15, 15, 0)
            };

            var bubbleBorder = new Border
            {
                StrokeThickness = 0,
                StrokeShape = bubbleShape,
                BackgroundColor = isUser ? Color.FromArgb("#0284C7") : Color.FromArgb("#F1F5F9"),
                Padding = new Thickness(15, 10),
                HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
                MaximumWidthRequest = 350
            };

            var label = new Label
            {
                Text = message,
                TextColor = isUser ? Colors.White : Color.FromArgb("#334155"),
                FontSize = 13,
                LineHeight = 1.3
            };

            bubbleBorder.Content = label;
            ChatContainer.Children.Add(bubbleBorder);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                await ChatScrollView.ScrollToAsync(ChatContainer, ScrollToPosition.End, true);
            });
        }

        private void OnFetchTelemetryTick(object? sender, EventArgs e)
        {
            // Simulasi fluktuasi air di hilir saluran Keputih
            simTma += _random.NextDouble() * 10 - 4;
            if (simTma < 10) simTma = 10;

            simFlow = 20 + (simTma * 0.4);

            string currentCuacaBMKG = simTma > 120 ? "Hujan Lebat" : "Hujan Ringan";
            double currentRainRate = simTma > 120 ? 45.5 : 12.0;

            LblLiveTma.Text = $"{simTma:F1} cm";
            LblLiveFlow.Text = $"{simFlow:F1} L/min";
            LblBmkgCuaca.Text = currentCuacaBMKG;
            LblBmkgHujan.Text = $"{currentRainRate:F1} mm";

            _gaugeDrawable.FlowRate = simFlow;
            GaugeFlowView.Invalidate();

            LblGaugeText.Text = $"{simFlow:F1} L/m";

            ProsesKeputusanBanjir(
                lokasi: "Keputih (Automated Sensor)",
                tma: simTma,
                flowRate: simFlow,
                curahHujan: currentRainRate,
                catatan: "Membaca telemetri dari modul hardware secara real-time."
            );
        }

        private async void OnKirimAsistenClicked(object sender, EventArgs e)
        {
            string inputText = EntryNlpText.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(inputText)) return;

            EntryNlpText.Text = "";
            AddChatBubble(inputText, true);

            if (_userRole == "Warga")
            {
                string responsKonsultasi = await GeminiService.GenerateMitigasiAsync(
                    lokasi: "Zona Hunian Warga",
                    tma: simTma,
                    cuaca: LblBmkgCuaca.Text,
                    statusFuzzy: inputText
                );
                AddChatBubble(responsKonsultasi, false);
            }
            else if (_userRole == "BMKG")
            {
                _telemetryTimer.Stop();
                AddChatBubble("Mengirim data laporan ke LLM Parser untuk ekstraksi token...", false);

                string aiResponse = await GeminiService.AnalyzeAsAssistantAsync(inputText);
                string[] extractedData = aiResponse.Split('|');

                string aiLokasi = "Keputih";
                double nilaiTma = 150.0;
                double curahHujan = 50.0;
                string aiCuaca = "Hujan Lebat";
                string aiPesan = "Mengekstrak parameter lokal.";

                if (extractedData.Length >= 5 && !aiResponse.Contains("[AI Error]"))
                {
                    aiLokasi = extractedData[0].Trim();
                    double.TryParse(extractedData[1].Trim(), out nilaiTma);
                    aiCuaca = extractedData[2].Trim();
                    double.TryParse(extractedData[3].Trim(), out curahHujan);
                    aiPesan = extractedData[4].Trim();
                }

                double estimasiFlow = 20 + (nilaiTma * 0.4);

                LblLiveTma.Text = $"{nilaiTma:F1} cm";
                LblLiveFlow.Text = $"{estimasiFlow:F1} L/min";
                LblBmkgCuaca.Text = aiCuaca;
                LblBmkgHujan.Text = $"{curahHujan:F1} mm";

                _gaugeDrawable.FlowRate = estimasiFlow;
                GaugeFlowView.Invalidate();
                LblGaugeText.Text = $"{estimasiFlow:F1} L/m";

                AddChatBubble($"✅ Perintah Override Valid: {aiPesan}", false);

                ProsesKeputusanBanjir(
                    lokasi: aiLokasi,
                    tma: nilaiTma,
                    flowRate: estimasiFlow,
                    curahHujan: curahHujan,
                    catatan: $"[Command Override Otoritas BMKG] {inputText}"
                );

                await Task.Delay(8000); // Kunci data selama 8 detik sebelum timer interupsi berjalan kembali
                _telemetryTimer.Start();
            }
        }

        private void ProsesKeputusanBanjir(
            string lokasi,
            double tma,
            double flowRate,
            double curahHujan,
            string catatan)
        {
            LblLokasi.Text = $"LOKASI MONITORING AKTIF: {lokasi.ToUpper()}";
            LblCatatan.Text = $"Catatan Lapangan Terakhir: {catatan}";

            // Skalasi visual progress bar elevasi air dinamis
            BarAktual.WidthRequest = Math.Min(tma * 1.5, 300);

            DateTime waktuSekarang = DateTime.Now;
            double selisihWaktuDetik = (waktuSekarang - _waktuInputSebelumnya).TotalSeconds;
            double lajuKenaikanPerDetik = 0.0;

            if (selisihWaktuDetik > 0.5 && _tmaSebelumnya > 0)
            {
                double selisihTma = tma - _tmaSebelumnya;
                lajuKenaikanPerDetik = selisihTma / selisihWaktuDetik;
            }

            double lajuKenaikanPerMenit = lajuKenaikanPerDetik > 0 ? lajuKenaikanPerDetik * 60 : 0;
            _tmaSebelumnya = tma;
            _waktuInputSebelumnya = waktuSekarang;

            // Memanggil Prediksi Komputasi Hibrida Neural-Fuzzy
            NeuralFloodPrediction hasilHibrida = NeuralFloodService.Predict(
                tma: tma,
                flowRate: flowRate,
                curahHujan: curahHujan,
                lajuKenaikanPerMenit: lajuKenaikanPerMenit
            );

            string statusTerpilih = hasilHibrida.Status;

            if (statusTerpilih == "BAHAYA")
            {
                string infoTambahan = lajuKenaikanPerMenit >= 15 ? $"Kenaikan Ekstrem +{lajuKenaikanPerMenit:F1} cm/mnt" : $"ANN Confidence: {hasilHibrida.Confidence:P0}";
                LblStatusBahaya.Text = $"Status: SIAGA 3 (BAHAYA) [{infoTambahan}]";
                LblStatusBahaya.TextColor = Color.FromRgb(239, 68, 68);
                LblRekomendasi.Text = "Mitigasi Evakuasi Darurat Pemukiman Aktif";
                LblRekomendasi.TextColor = Color.FromRgb(239, 68, 68);
                BtnPemicuSirine.IsVisible = _userRole == "BMKG";
            }
            else if (statusTerpilih == "WASPADA")
            {
                LblStatusBahaya.Text = $"Status: SIAGA 2 (WASPADA) [ANN Confidence: {hasilHibrida.Confidence:P0}]";
                LblStatusBahaya.TextColor = Color.FromRgb(245, 158, 11);
                LblRekomendasi.Text = "Alat Standby - Waspada Potensi Luapan Luar Bendungan";
                LblRekomendasi.TextColor = Color.FromRgb(245, 158, 11);
                BtnPemicuSirine.IsVisible = false;
            }
            else
            {
                statusTerpilih = "AMAN";
                LblStatusBahaya.Text = $"Status: AMAN / NORMAL [ANN Confidence: {hasilHibrida.Confidence:P0}]";
                LblStatusBahaya.TextColor = Color.FromRgb(16, 185, 129);
                LblRekomendasi.Text = "Sistem Normal - Aliran Fluida Lancar";
                LblRekomendasi.TextColor = Color.FromRgb(71, 85, 105);
                BtnPemicuSirine.IsVisible = false;
            }

            // Simpan log sheet otomatis ke SQLite jika status kritis atau ada intervensi manual
            if (statusTerpilih == "WASPADA" || statusTerpilih == "BAHAYA" || catatan.Contains("Override"))
            {
                SimpanLogSensor(
                    lokasi: lokasi,
                    tma: tma,
                    flowRate: flowRate,
                    status: statusTerpilih,
                    catatan: $"{catatan} | {hasilHibrida.Catatan}"
                );
            }
        }

        private void SimpanLogSensor(string lokasi, double tma, double flowRate, string status, string catatan)
        {
            var logBaru = new RiwayatSensor
            {
                Lokasi = lokasi,
                TMA = tma,
                FlowRate = flowRate,
                StatusFuzzy = status, // Menyimpan representasi linguistik dari output hibrida
                Catatan = catatan,
                WaktuPencatatan = DateTime.Now
            };

            Task.Run(async () =>
            {
                await DatabaseService.SimpanLogAsync(logBaru);
                MainThread.BeginInvokeOnMainThread(async () => await SegarkanTabelLogAsync());
            });
        }
    }

    // Canvas rendering untuk instrumentasi Flow Meter setengah lingkaran
    public class FlowGaugeDrawable : IDrawable
    {
        public double FlowRate { get; set; } = 0;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float centerX = dirtyRect.Center.X;
            float centerY = dirtyRect.Height;
            float radius = Math.Min(dirtyRect.Width / 2, dirtyRect.Height) - 8;

            canvas.StrokeColor = Color.FromArgb("#E2E8F0");
            canvas.StrokeSize = 12;
            canvas.StrokeLineCap = LineCap.Round;

            canvas.DrawArc(centerX - radius, centerY - radius, radius * 2, radius * 2, 180, 0, true, false);

            float maxFlow = 150f;
            float progress = (float)Math.Min(Math.Max(FlowRate / maxFlow, 0), 1);
            float sweepAngle = 180 * progress;
            float endAngle = 180 - sweepAngle;

            canvas.StrokeColor = progress > 0.6f ? Color.FromArgb("#EF4444") : Color.FromArgb("#0284C7");
            canvas.DrawArc(centerX - radius, centerY - radius, radius * 2, radius * 2, 180, endAngle, true, false);
        }
    }
}