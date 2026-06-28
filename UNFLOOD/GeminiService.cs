using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UNFLOOD
{
    public static class GeminiService
    {
        private static readonly HttpClient _client = new HttpClient();

        private const string ApiKey = "AQ.Ab8RN6KLyf8HBMRENpTGI4-zzkiKmfuk04C1fXaFXK-R1sHELg";

        public static async Task<string> GenerateMitigasiAsync(string lokasi, double tma, string cuaca, string statusFuzzy)
        {
            try
            {
                string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent?key=" + ApiKey;

                string prompt = $"Kamu adalah 'Asisten UNFLOOD', AI konsultan siaga bencana yang ramah, luwes, dan cerdas. " +
                                $"Seseorang bertanya/menyapa kepadamu: '{statusFuzzy}'. " +
                                $"Data lapangan saat ini (sebagai informasi tambahan jika dibutuhkan) -> Tinggi Air: {tma} cm, Cuaca: {cuaca}. " +
                                $"Tugasmu: " +
                                $"1. Jika pertanyaan/laporan berkaitan dengan bencana banjir, genangan air, kondisi lingkungan, atau keselamatan, jawablah dengan penuh empati, solutif, dan gunakan data lapangan (Tinggi Air/Cuaca) sebagai referensi peringatan. " +
                                $"2. Jika pertanyaan di luar konteks bencana (misalnya: sapaan umum, pertanyaan matematika, pengetahuan umum, candaan ringan), jawablah SECARA NATURAL dan RELEVAN dengan pertanyaannya. JANGAN menyertakan informasi status banjir/cuaca/tinggi air sama sekali, kecuali jika secara eksplisit ditanyakan hubungannya. " +
                                $"Gunakan bahasa Indonesia yang santai, sopan, tidak kaku, dan mudah dipahami. Jangan bertingkah seperti robot pemerintah.";

                var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(url, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument doc = JsonDocument.Parse(responseBody);
                    return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()?.Trim() ?? "Gagal memuat.";
                }

                return $"[AI Error] Detail: {responseBody}";
            }
            catch (Exception ex)
            {
                return $"[AI Error] Kondisi: {ex.Message}";
            }
        }

        public static async Task<string> AnalyzeAsAssistantAsync(string text)
        {
            try
            {
                string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent?key=" + ApiKey;

                string prompt = $"Ekstrak data banjir ini: '{text}'. Format balasannya WAJIB persis seperti ini dipisah pipa (|): Nama Lokasi (jika tidak ada tulis Keputih) | Ketinggian Air (angka saja, jika tidak ada tulis 150) | Kondisi Cuaca (Hujan Ringan/Lebat) | Curah Hujan (angka saja, default 50) | Kesimpulan 1 kalimat singkat.";

                var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(url, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument doc = JsonDocument.Parse(responseBody);
                    return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()?.Trim() ?? "Gagal memuat.";
                }

                return $"[AI Error] Detail: {responseBody}";
            }
            catch (Exception ex)
            {
                return $"[AI Error] Kondisi: {ex.Message}";
            }
        }
    }
}
