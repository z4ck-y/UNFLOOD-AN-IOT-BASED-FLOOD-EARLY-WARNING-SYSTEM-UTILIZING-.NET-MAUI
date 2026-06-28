using System;
using SQLite;

namespace UNFLOOD.Models
{
    [Table("RiwayatSensor")]
    public class RiwayatSensor
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string Lokasi { get; set; } = string.Empty;

        public double TMA { get; set; }

        public double FlowRate { get; set; }

        // Menyimpan status hasil interpolasi Hybrid Neural-Fuzzy (AMAN / WASPADA / BAHAYA)
        public string StatusFuzzy { get; set; } = "AMAN";

        public string Catatan { get; set; } = string.Empty;

        public DateTime WaktuPencatatan { get; set; } = DateTime.Now;
    }
}