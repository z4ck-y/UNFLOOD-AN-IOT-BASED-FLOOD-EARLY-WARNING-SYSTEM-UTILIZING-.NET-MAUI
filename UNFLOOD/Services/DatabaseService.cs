using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using UNFLOOD.Models;

namespace UNFLOOD.Services
{
    public static class DatabaseService
    {
        private static SQLiteAsyncConnection? _database;

        // Ganti bagian DatabasePath menjadi ini:
        private static string DatabasePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UnfloodDatabase.db3");

        public static async Task InitializeAsync()
        {
            if (_database != null) return;

            // Membuat koneksi ke file database
            _database = new SQLiteAsyncConnection(DatabasePath);

            // Membuat tabel secara otomatis jika tabel belum ada di file database
            await _database.CreateTableAsync<RiwayatSensor>();
        }

        // Fungsi untuk menyimpan data log baru
        public static async Task<int> SimpanLogAsync(RiwayatSensor data)
        {
            await InitializeAsync();
            return await _database!.InsertAsync(data);
        }

        // Fungsi untuk mengambil semua riwayat data (diurutkan dari yang paling baru)
        public static async Task<List<RiwayatSensor>> AmbilSemuaRiwayatAsync()
        {
            await InitializeAsync();
            return await _database!.Table<RiwayatSensor>()
                                   .OrderByDescending(x => x.WaktuPencatatan)
                                   .ToListAsync();
        }

        // Fungsi untuk mengambil data riwayat berdasarkan lokasi tertentu
        public static async Task<List<RiwayatSensor>> AmbilRiwayatSesuaiLokasiAsync(string lokasi)
        {
            await InitializeAsync();
            return await _database!.Table<RiwayatSensor>()
                                   .Where(x => x.Lokasi == lokasi)
                                   .OrderByDescending(x => x.WaktuPencatatan)
                                   .ToListAsync();
        }
    }
}