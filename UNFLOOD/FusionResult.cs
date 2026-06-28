namespace UNFLOOD
{
    public class FusionResult
    {
        public string Lokasi { get; set; } = "Tidak Diketahui";
        public int KetinggianAir { get; set; } = 0; // dalam satuan cm
        public int DebitArus { get; set; } = 0;      // dalam satuan m³/s
        public string Catatan { get; set; } = "-";
    }
}