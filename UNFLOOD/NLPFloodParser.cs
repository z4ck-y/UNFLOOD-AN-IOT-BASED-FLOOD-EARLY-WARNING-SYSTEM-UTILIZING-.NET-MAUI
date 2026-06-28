using System;
using System.Text.RegularExpressions;

namespace UNFLOOD
{
    public static class NlpFloodParser
    {
        public static FusionResult ExtractFloodData(string text)
        {
            var result = new FusionResult();

            if (string.IsNullOrEmpty(text))
                return result;

            // 1. Ekstraksi Lokasi
            var lokasiMatch = Regex.Match(text, @"(?:bendungan|sungai|lokasi)\s+([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
            if (lokasiMatch.Success)
            {
                result.Lokasi = lokasiMatch.Groups[1].Value;
            }

            // 2. Ekstraksi Ketinggian Air
            var tinggiMatch = Regex.Match(text, @"(?:ketinggian|tma)\s+(\d+)|(\d+)\s*cm", RegexOptions.IgnoreCase);
            if (tinggiMatch.Success)
            {
                string val = !string.IsNullOrEmpty(tinggiMatch.Groups[1].Value)
                             ? tinggiMatch.Groups[1].Value
                             : tinggiMatch.Groups[2].Value;
                result.KetinggianAir = int.Parse(val);
            }

            // 3. Ekstraksi Debit Arus
            var arusMatch = Regex.Match(text, @"(?:debit|arus)\s+(\d+)|(\d+)\s*(?:m3|meter kubik)", RegexOptions.IgnoreCase);
            if (arusMatch.Success)
            {
                string val = !string.IsNullOrEmpty(arusMatch.Groups[1].Value)
                             ? arusMatch.Groups[1].Value
                             : arusMatch.Groups[2].Value;
                result.DebitArus = int.Parse(val);
            }

            // 4. Ekstraksi Catatan Lapangan
            var catatanMatch = Regex.Match(text, @"(?:catatan|status|kondisi)\s+(.+?)$", RegexOptions.IgnoreCase);
            if (catatanMatch.Success)
            {
                result.Catatan = catatanMatch.Groups[1].Value;
            }

            return result;
        }
    }
}