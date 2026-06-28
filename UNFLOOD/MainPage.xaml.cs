using Microsoft.Maui.Controls;
using System;

namespace UNFLOOD
{
    public partial class MainPage : ContentPage
    {
        // Variabel global untuk menyimpan role yang sedang dipilih (Default awal: Warga)
        private string selectedRole = "Warga";

        public MainPage()
        {
            InitializeComponent();
        }

        // Fungsi penangkap klik Radio Button otomatis untuk merubah isi variabel selectedRole
        private void OnRoleChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton rb && e.Value)
            {
                selectedRole = rb.Content.ToString();
            }
        }

        // Logika Utama Validasi Login
        // Logika Utama Validasi Login
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string user = EntryUsername.Text?.Trim();
            string pass = EntryPassword.Text;

            // 1. Validasi Input Kosong
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                await DisplayAlert("Peringatan", "Username, Password, dan Role wajib diisi!", "OK");
                return;
            }

            // 2. Cek Apakah Username Terdaftar di Preferences Lokal (DISESUAIKAN)
            if (!Preferences.ContainsKey($"user_pass_{user}"))
            {
                await DisplayAlert("Ditolak", "Username tidak ditemukan!", "OK");
                return;
            }

            // 3. Cek Kesesuaian Password (DISESUAIKAN)
            if (pass != Preferences.Get($"user_pass_{user}", ""))
            {
                await DisplayAlert("Ditolak", "Password salah!", "OK");
                return;
            }

            // 4. Validasi Kesesuaian Antara Akun Registrasi dan Role Pilihan saat Login (DISESUAIKAN)
            string registeredRole = Preferences.Get($"user_role_{user}", "Warga");
            if (registeredRole.ToLower() != selectedRole.ToLower())
            {
                await DisplayAlert("Akses Ditolak", $"Akun terdaftar sebagai {registeredRole}!", "OK");
                return;
            }

            // 5. Set Session dan Navigasi Menuju FaceVerificationPage
            Preferences.Set("session_username", user);
            Preferences.Set("session_role", selectedRole);

            await DisplayAlert("Sukses", "Data Otorisasi Valid. Menuju Verifikasi Biometrik Wajah...", "OK");
            Application.Current.MainPage = new FaceVerificationPage(user, selectedRole);
        }

        // Navigasi ke Halaman Registrasi Akun Baru
        private void OnGoToRegisterClicked(object sender, EventArgs e)
        {
            Application.Current.MainPage = new RegisterPage();
        }
    }
}