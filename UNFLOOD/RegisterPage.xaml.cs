using Microsoft.Maui.Media;
using System;
using System.IO;
using Microsoft.Maui.Storage;

namespace UNFLOOD
{
    public partial class RegisterPage : ContentPage
    {
        private byte[]? _faceData;

        public RegisterPage()
        {
            InitializeComponent();
            PickerRole.SelectedIndex = 2; // Default select Warga
        }

        private void OnRoleChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton rb && e.Value)
            {
                string role = rb.Content.ToString();
                if (role == "Admin") PickerRole.SelectedIndex = 0;
                else if (role == "BMKG") PickerRole.SelectedIndex = 1;
                else if (role == "Warga") PickerRole.SelectedIndex = 2;
            }
        }

        private async void OnCaptureFaceClicked(object sender, EventArgs e)
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                using var stream = await photo.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                _faceData = memoryStream.ToArray();

                CameraPreview.Source = ImageSource.FromStream(() => new MemoryStream(_faceData));
            }
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string user = EntryUsername.Text?.Trim();
            string pass = EntryPassword.Text;
            string selectedRole = PickerRole.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(selectedRole))
            {
                await DisplayAlert("Peringatan", "Semua kolom input dan foto wajah wajib diisi!", "OK");
                return;
            }

            if (_faceData == null)
            {
                await DisplayAlert("Peringatan", "Silakan ambil foto wajah terlebih dahulu untuk otorisasi biometrik!", "OK");
                return;
            }

            Preferences.Set($"user_pass_{user}", pass);
            Preferences.Set($"user_role_{user}", selectedRole);

            string userList = Preferences.Get("daftar_semua_user", "");
            if (string.IsNullOrEmpty(userList)) userList = user;
            else if (!userList.Contains(user)) userList += "," + user;
            Preferences.Set("daftar_semua_user", userList);

            string imagePath = Path.Combine(FileSystem.AppDataDirectory, $"{user}_face.png");
            File.WriteAllBytes(imagePath, _faceData);

            await DisplayAlert("Sukses", $"User '{user}' berhasil didaftarkan sebagai {selectedRole}!", "OK");

            Application.Current.MainPage = new MainPage();
        }

        private void OnBackToLoginClicked(object sender, EventArgs e)
        {
            Application.Current.MainPage = new MainPage();
        }
    }
}