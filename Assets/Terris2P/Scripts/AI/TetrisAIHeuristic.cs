using System;
using UnityEngine;

namespace Terris2P.AI
{
    [Serializable]
    public struct HeuristicWeights
    {
        public float weightLineClear;
        public float weightDangerRow0;
        public float weightHoles;
        public float weightBumpiness;
        public float weightBarrierPush;
        public float weightP1Vulnerability;
        public float randomNoiseRange;

        public static HeuristicWeights ForDifficulty(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return new HeuristicWeights
                    {
                        weightLineClear = 20f,
                        weightDangerRow0 = 30f,
                        weightHoles = -15f,
                        weightBumpiness = -4f,
                        weightBarrierPush = 10f,
                        weightP1Vulnerability = 10f,
                        randomNoiseRange = 35f // Nhiễu cao, dễ mắc lỗi
                    };
                case AIDifficulty.Medium:
                    return new HeuristicWeights
                    {
                        weightLineClear = 50f,
                        weightDangerRow0 = 70f,
                        weightHoles = -35f,
                        weightBumpiness = -10f,
                        weightBarrierPush = 30f,
                        weightP1Vulnerability = 25f,
                        randomNoiseRange = 10f
                    };
                case AIDifficulty.Hard:
                    return new HeuristicWeights
                    {
                        weightLineClear = 100f,
                        weightDangerRow0 = 130f,
                        weightHoles = -60f,
                        weightBumpiness = -18f,
                        weightBarrierPush = 60f,
                        weightP1Vulnerability = 50f,
                        randomNoiseRange = 0f // Tính toán chuẩn xác
                    };
                default: // Master
                    return new HeuristicWeights
                    {
                        weightLineClear = 160f,
                        weightDangerRow0 = 200f,
                        weightHoles = -90f,
                        weightBumpiness = -25f,
                        weightBarrierPush = 90f,
                        weightP1Vulnerability = 80f,
                        randomNoiseRange = 0f // Tối ưu tuyệt đối
                    };
            }
        }
    }

    /// <summary>
    /// Hàm lượng giá (Heuristic Specification) đánh giá thế cờ của P2 (Người chơi dưới).
    /// </summary>
    public static class TetrisAIHeuristic
    {
        public static float Evaluate(ref VirtualBoard board, int linesCleared, HeuristicWeights weights, System.Random rng = null)
        {
            board.GetBarrierBounds(out int barrierMinY, out int barrierMaxY);

            // 1. Kiểm tra điều kiện thua cuộc tức thì
            // Nếu có khối ở hàng 0 -> P2 chạm đáy biên dưới, thua trận!
            if (board.HasBlockAtRow(0))
            {
                return -1000000f;
            }

            int p2MinY = board.GetP2MinY(barrierMinY);

            // 2. Điểm số từ việc xóa hàng (Lines Cleared Bonus)
            float linesScore = 0f;
            if (linesCleared > 0)
            {
                // Thưởng lũy tiến theo số hàng: 1 hàng = 1x, 2 hàng = 2.5x, 3 hàng = 5x, 4 hàng = 10x
                float multiplier = linesCleared == 1 ? 1f :
                                   linesCleared == 2 ? 2.5f :
                                   linesCleared == 3 ? 5f : 10f;
                linesScore = weights.weightLineClear * multiplier;
            }

            // 3. Đánh giá khoảng cách sinh tử tới hàng 0 (Danger Row 0 Penalty)
            float dangerScore = 0f;
            if (p2MinY <= 1)
            {
                dangerScore = -weights.weightDangerRow0 * 250f; // Nguy hiểm cận kề
            }
            else if (p2MinY <= 3)
            {
                dangerScore = -weights.weightDangerRow0 * 50f * (4 - p2MinY);
            }
            else
            {
                // Càng xa mép 0 (gần rào chắn) càng an toàn
                dangerScore = weights.weightDangerRow0 * (p2MinY - 3) * 2f;
            }

            // 4. Phạt lỗ hổng (Holes Penalty)
            int holes = board.CountP2Holes(barrierMinY);
            float holesScore = weights.weightHoles * holes;

            // 5. Phạt độ gồ ghề (Bumpiness Penalty)
            int bumpiness = board.CalculateP2Bumpiness(barrierMinY);
            float bumpinessScore = weights.weightBumpiness * bumpiness;

            // 6. Thưởng lấn sân rào chắn (Barrier Advantage)
            // Rào chắn ban đầu ở hàng 14-15. Nếu rào chắn bị đẩy lên > 14, P2 chiếm thêm không gian
            float barrierScore = weights.weightBarrierPush * (barrierMinY - 14) * 5f;

            // 7. Tận dụng thế yếu của đối thủ (P1 Vulnerability)
            // Nếu P1 có khối chạm gần hàng 29, áp lực dồn lên P1 càng lớn
            int p1MaxY = board.GetP1MaxY(barrierMaxY);
            float p1ThreatScore = 0f;
            if (p1MaxY >= 26)
            {
                p1ThreatScore = weights.weightP1Vulnerability * (p1MaxY - 25) * 5f;
            }

            // Tổng điểm
            float total = linesScore + dangerScore + holesScore + bumpinessScore + barrierScore + p1ThreatScore;

            // Cộng nhiễu ngẫu nhiên (nếu có, để tạo sai số theo độ khó)
            if (weights.randomNoiseRange > 0.001f && rng != null)
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * weights.randomNoiseRange;
                total += noise;
            }

            return total;
        }
    }
}
