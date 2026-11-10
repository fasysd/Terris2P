using System;
using UnityEngine;

namespace Terris2P.AI
{
    /// <summary>
    /// Chứa toàn bộ thông tin cấu hình suy nghĩ, đánh giá chiến thuật và nhịp độ thao tác cho một cấp độ AI.
    /// </summary>
    [Serializable]
    public class AIThinkingProfile
    {
        [Header("--- THÔNG TIN CẤP ĐỘ ---")]
        public string profileName = "Profile";
        public AIDifficulty difficulty = AIDifficulty.Medium;

        [Header("--- 1. SEARCH SPECIFICATION (TÌM KIẾM & BEAM SEARCH) ---")]
        [Tooltip("Chiều sâu tìm kiếm: 1 = chỉ khối hiện tại, 2 = khối hiện tại + khối tiếp theo")]
        [Range(1, 2)] public int searchDepth = 2;
        [Tooltip("Độ rộng chùm Beam Width (số lượng trạng thái tốt nhất được giữ lại để mở rộng tầng sau)")]
        [Range(1, 32)] public int beamWidth = 6;
        [Tooltip("Có tính toán thêm nước đi của khối kế tiếp (Next Piece) hay không")]
        public bool useLookahead = true;
        [Tooltip("Hệ số chiết khấu điểm lượng giá cho khối tiếp theo (thường từ 0.7 đến 0.95)")]
        [Range(0f, 1f)] public float lookaheadDiscount = 0.85f;

        [Header("--- 2. HEURISTIC SPECIFICATION (HÀM LƯỢNG GIÁ CHIẾN THUẬT) ---")]
        [Tooltip("Trọng số thưởng khi xóa hàng (khuyến khích tấn công đẩy dồn sân đối thủ)")]
        public float weightLineClear = 75f;
        [Tooltip("Trọng số an toàn / phạt khi khối tiến sát mép chết hàng 0")]
        public float weightDangerRow0 = 95f;
        [Tooltip("Trọng số phạt khi tạo ra lỗ hổng bị bẫy dưới gạch")]
        public float weightHoles = -50f;
        [Tooltip("Trọng số phạt độ gồ ghề của bề mặt đón gạch")]
        public float weightBumpiness = -14f;
        [Tooltip("Trọng số thưởng khi đẩy rào chắn lấn sang nửa sân P1")]
        public float weightBarrierPush = 45f;
        [Tooltip("Trọng số khai thác điểm yếu khi P1 có gạch xếp cao gần mép 29")]
        public float weightP1Vulnerability = 35f;
        [Tooltip("Biên độ nhiễu ngẫu nhiên cộng vào điểm số để mô phỏng sai lầm của con người")]
        public float randomNoiseRange = 0f;

        [Header("--- 3. PACING & EXECUTION (NHỊP ĐỘ THAO TÁC APM & TỐC ĐỘ) ---")]
        [Tooltip("Thời gian dừng suy nghĩ trước khi bắt đầu chuỗi thao tác (giây)")]
        [Min(0f)] public float thinkingDelay = 0.05f;
        [Tooltip("Khoảng thời gian nghỉ giữa mỗi lần phát lệnh bấm phím (giây)")]
        [Min(0.01f)] public float actionInterval = 0.06f;
        [Tooltip("Cho phép thả khối tức thì (Hard Drop) hay chỉ bật rơi nhanh (Soft Drop)")]
        public bool allowHardDrop = true;
        [Tooltip("Số chu kỳ thao tác tối đa trước khi cưỡng bức thả khối nếu bị kẹt")]
        public int maxStuckCount = 12;

        public HeuristicWeights ToHeuristicWeights()
        {
            return new HeuristicWeights
            {
                weightLineClear = weightLineClear,
                weightDangerRow0 = weightDangerRow0,
                weightHoles = weightHoles,
                weightBumpiness = weightBumpiness,
                weightBarrierPush = weightBarrierPush,
                weightP1Vulnerability = weightP1Vulnerability,
                randomNoiseRange = randomNoiseRange
            };
        }

        public static AIThinkingProfile CreateDefault(AIDifficulty diff)
        {
            switch (diff)
            {
                case AIDifficulty.Easy:
                    return new AIThinkingProfile
                    {
                        profileName = "Dễ (Easy)",
                        difficulty = AIDifficulty.Easy,
                        searchDepth = 2,
                        beamWidth = 4,
                        useLookahead = true,
                        lookaheadDiscount = 0.8f,
                        weightLineClear = 45f,
                        weightDangerRow0 = 60f,
                        weightHoles = -30f,
                        weightBumpiness = -8f,
                        weightBarrierPush = 25f,
                        weightP1Vulnerability = 20f,
                        randomNoiseRange = 6f, // Nhiễu nhỏ vừa phải, không đặt gạch ngớ ngẩn
                        thinkingDelay = 0.12f, // Phản xạ nhanh hơn nhiều (trước là 0.35s)
                        actionInterval = 0.09f, // Nhịp phím nhanh hơn (~330 APM, trước là 0.28s)
                        allowHardDrop = true,
                        maxStuckCount = 12
                    };
                case AIDifficulty.Medium:
                    return new AIThinkingProfile
                    {
                        profileName = "Vừa (Medium)",
                        difficulty = AIDifficulty.Medium,
                        searchDepth = 2,
                        beamWidth = 8,
                        useLookahead = true,
                        lookaheadDiscount = 0.85f,
                        weightLineClear = 75f,
                        weightDangerRow0 = 95f,
                        weightHoles = -50f,
                        weightBumpiness = -14f,
                        weightBarrierPush = 45f,
                        weightP1Vulnerability = 35f,
                        randomNoiseRange = 0f,
                        thinkingDelay = 0.05f, // Phản xạ nhanh (0.05s)
                        actionInterval = 0.055f, // ~550 APM
                        allowHardDrop = true,
                        maxStuckCount = 12
                    };
                case AIDifficulty.Hard:
                    return new AIThinkingProfile
                    {
                        profileName = "Khó (Hard)",
                        difficulty = AIDifficulty.Hard,
                        searchDepth = 2,
                        beamWidth = 14,
                        useLookahead = true,
                        lookaheadDiscount = 0.88f,
                        weightLineClear = 120f,
                        weightDangerRow0 = 150f,
                        weightHoles = -75f,
                        weightBumpiness = -20f,
                        weightBarrierPush = 75f,
                        weightP1Vulnerability = 65f,
                        randomNoiseRange = 0f,
                        thinkingDelay = 0.015f,
                        actionInterval = 0.035f, // ~850 APM
                        allowHardDrop = true,
                        maxStuckCount = 10
                    };
                default: // Master
                    return new AIThinkingProfile
                    {
                        profileName = "Tối thượng (Master)",
                        difficulty = AIDifficulty.Master,
                        searchDepth = 2,
                        beamWidth = 24,
                        useLookahead = true,
                        lookaheadDiscount = 0.92f,
                        weightLineClear = 180f,
                        weightDangerRow0 = 220f,
                        weightHoles = -100f,
                        weightBumpiness = -28f,
                        weightBarrierPush = 100f,
                        weightP1Vulnerability = 90f,
                        randomNoiseRange = 0f,
                        thinkingDelay = 0f, // Tức thì
                        actionInterval = 0.015f, // ~1300+ APM (siêu tốc)
                        allowHardDrop = true,
                        maxStuckCount = 8
                    };
            }
        }
    }

    /// <summary>
    /// ScriptableObject chứa toàn bộ thông tin cấu hình suy nghĩ của AI.
    /// Cho phép Game Designer điều chỉnh trực tiếp trên Unity Inspector mà không cần sửa code.
    /// </summary>
    [CreateAssetMenu(fileName = "AIThinkingConfig", menuName = "Terris2P/AI Thinking Config")]
    public class AIThinkingConfigSO : ScriptableObject
    {
        [Header("=== CẤU HÌNH SUY NGHĨ CHO 4 MỨC ĐỘ KHÓ ===")]
        public AIThinkingProfile easyProfile = AIThinkingProfile.CreateDefault(AIDifficulty.Easy);
        public AIThinkingProfile mediumProfile = AIThinkingProfile.CreateDefault(AIDifficulty.Medium);
        public AIThinkingProfile hardProfile = AIThinkingProfile.CreateDefault(AIDifficulty.Hard);
        public AIThinkingProfile masterProfile = AIThinkingProfile.CreateDefault(AIDifficulty.Master);

        public AIThinkingProfile GetProfile(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy: return easyProfile ?? AIThinkingProfile.CreateDefault(AIDifficulty.Easy);
                case AIDifficulty.Medium: return mediumProfile ?? AIThinkingProfile.CreateDefault(AIDifficulty.Medium);
                case AIDifficulty.Hard: return hardProfile ?? AIThinkingProfile.CreateDefault(AIDifficulty.Hard);
                case AIDifficulty.Master: return masterProfile ?? AIThinkingProfile.CreateDefault(AIDifficulty.Master);
                default: return mediumProfile ?? AIThinkingProfile.CreateDefault(AIDifficulty.Medium);
            }
        }

        private static AIThinkingConfigSO cachedDefault;
        public static AIThinkingConfigSO GetDefaultInstance()
        {
            if (cachedDefault != null) return cachedDefault;

            cachedDefault = Resources.Load<AIThinkingConfigSO>("AIThinkingConfig");
            if (cachedDefault == null)
            {
                cachedDefault = CreateInstance<AIThinkingConfigSO>();
            }
            return cachedDefault;
        }
    }
}
