using System;

namespace UNFLOOD.Services
{
    public class NeuralFloodPrediction
    {
        public string Status { get; set; } = "AMAN";
        public double ProbAman { get; set; }
        public double ProbWaspada { get; set; }
        public double ProbBahaya { get; set; }
        public double Confidence { get; set; }
        public string Catatan { get; set; } = "";
    }

    public static class NeuralFloodService
    {
        public static NeuralFloodPrediction Predict(
            double tma,
            double flowRate,
            double curahHujan,
            double lajuKenaikanPerMenit)
        {
            // 1. Normalisasi Input ke ruang parameter [0, 1]
            double[] input =
            {
                Normalize(tma, 0, 250),
                Normalize(flowRate, 0, 120),
                Normalize(curahHujan, 0, 100),
                Normalize(lajuKenaikanPerMenit, 0, 30)
            };

            // 2. Hidden Layer dengan 6 Neuron berbasis fungsi aktivasi Sigmoid
            double[] hidden = new double[6];
            hidden[0] = Sigmoid(-2.2 + 5.0 * input[0] + 2.0 * input[1]);
            hidden[1] = Sigmoid(-1.7 + 3.2 * input[2] + 2.5 * input[3]);
            hidden[2] = Sigmoid(3.5 - 4.0 * input[0] - 2.4 * input[1] - 1.5 * input[2] - 2.0 * input[3]);
            hidden[3] = Sigmoid(-1.0 + 3.0 * input[0] + 2.0 * input[1] + 1.0 * input[2]);
            hidden[4] = Sigmoid(-2.0 + 5.0 * input[3]);
            hidden[5] = Sigmoid(-4.5 + 7.0 * input[0] + 3.0 * input[1] + 2.0 * input[2]);

            // 3. Output Layer (Raw Logits) untuk 3 set linguistik Fuzzy
            double logitAman = 3.0 * hidden[2] - 1.2 * hidden[0] - 1.0 * hidden[1] - 2.0 * hidden[5];
            double logitWaspada = -0.3 + 1.2 * hidden[0] + 1.4 * hidden[1] + 1.8 * hidden[3] - 1.0 * hidden[5] - 0.5 * hidden[2];
            double logitBahaya = -1.8 + 3.2 * hidden[5] + 1.8 * hidden[4] + 0.8 * hidden[0] + 0.6 * hidden[1] - 1.5 * hidden[2];

            // 4. Normalisasi Softmax untuk menghasilkan bobot probabilitas Fuzzy
            double[] output = Softmax(logitAman, logitWaspada, logitBahaya);

            string status;
            double confidence;

            if (output[2] >= output[1] && output[2] >= output[0])
            {
                status = "BAHAYA";
                confidence = output[2];
            }
            else if (output[1] >= output[0])
            {
                status = "WASPADA";
                confidence = output[1];
            }
            else
            {
                status = "AMAN / NORMAL";
                confidence = output[0];
            }

            // Deterministic Safety Override untuk kondisi batas ekstrem lingkungan Keputih
            if (tma >= 160 || lajuKenaikanPerMenit >= 15)
            {
                status = "BAHAYA";
                confidence = Math.Max(confidence, 0.90);
            }

            return new NeuralFloodPrediction
            {
                Status = status,
                ProbAman = output[0],
                ProbWaspada = output[1],
                ProbBahaya = output[2],
                Confidence = confidence,
                Catatan = $"Hybrid Neural-Fuzzy membaca TMA={tma:F1} cm, Flow={flowRate:F1} L/m, Hujan={curahHujan:F1} mm, Laju={lajuKenaikanPerMenit:F1} cm/mnt."
            };
        }

        private static double Normalize(double value, double min, double max)
        {
            if (value < min) value = min;
            if (value > max) value = max;
            return (value - min) / (max - min);
        }

        private static double Sigmoid(double x)
        {
            return 1.0 / (1.0 + Math.Exp(-x));
        }

        private static double[] Softmax(double a, double b, double c)
        {
            double max = Math.Max(a, Math.Max(b, c));
            double ea = Math.Exp(a - max);
            double eb = Math.Exp(b - max);
            double ec = Math.Exp(c - max);
            double total = ea + eb + ec;
            return new[] { ea / total, eb / total, ec / total };
        }
    }
}