using System;
using UnityEngine;

namespace Terris2P
{
    /// <summary>
    /// Các ngôn ngữ được hỗ trợ trong Terris 2P.
    /// </summary>
    public enum GameLanguage
    {
        English,
        Vietnamese
    }

    /// <summary>
    /// Quản lý bản địa hóa ngôn ngữ (Tiếng Anh & Tiếng Việt),
    /// mặc định là Tiếng Anh theo yêu cầu thiết kế.
    /// </summary>
    public static class LocalizationManager
    {
        private const string PrefKey = "Terris2P_Language";
        private static GameLanguage currentLanguage = GameLanguage.English;
        private static bool isInitialized = false;

        public static event Action OnLanguageChanged;

        public static GameLanguage CurrentLanguage
        {
            get
            {
                if (!isInitialized)
                {
                    Initialize();
                }
                return currentLanguage;
            }
            set
            {
                currentLanguage = value;
                isInitialized = true;
                PlayerPrefs.SetString(PrefKey, currentLanguage.ToString());
                PlayerPrefs.Save();
                OnLanguageChanged?.Invoke();
            }
        }

        public static void Initialize()
        {
            isInitialized = true;
            string saved = PlayerPrefs.GetString(PrefKey, "English");
            if (Enum.TryParse<GameLanguage>(saved, out var lang))
            {
                currentLanguage = lang;
            }
            else
            {
                currentLanguage = GameLanguage.English;
            }
        }

        public static void ToggleLanguage()
        {
            CurrentLanguage = (CurrentLanguage == GameLanguage.English) ? GameLanguage.Vietnamese : GameLanguage.English;
        }

        // ================= Chuỗi hiển thị Menu chính =================
        public static string MenuTitle => "TERRIS 2P";

        public static string MenuSubtitle => CurrentLanguage == GameLanguage.Vietnamese
            ? "ĐỐI KHÁNG XẾP KHỐI 2 NGƯỜI\nBẢNG CHUNG 15x30"
            : "2-PLAYER BATTLE TETRIS\nSHARED 15x30 BOARD";

        public static string StartGameBtn => CurrentLanguage == GameLanguage.Vietnamese
            ? "▶  BẮT ĐẦU CHƠI"
            : "▶  PLAY GAME";

        public static string LanguageToggleBtn => CurrentLanguage == GameLanguage.Vietnamese
            ? "🌐  NGÔN NGỮ: TIẾNG VIỆT (VIETNAMESE)\n[BẤM ĐỔI SANG ENGLISH]"
            : "🌐  LANGUAGE: ENGLISH\n[CLICK TO SWITCH TO TIẾNG VIỆT]";

        public static string TipText => CurrentLanguage == GameLanguage.Vietnamese
            ? "<b>MẸO NHANH:</b>\n" +
              "• Bấm phím <b>ESC</b> hoặc <b>P</b> để tạm dừng trận đấu.\n" +
              "• Khối có 0.5s thời gian chờ (Lock Delay) trước khi cố định.\n" +
              "• Tối ưu mượt mà cho trải nghiệm trực tiếp trên WebGL."
            : "<b>QUICK TIPS:</b>\n" +
              "• Press <b>ESC</b> or <b>P</b> to pause the match.\n" +
              "• Tetrominoes have 0.5s lock delay before locking.\n" +
              "• Smoothly optimized for direct WebGL play.";

        public static string RulesHeader => CurrentLanguage == GameLanguage.Vietnamese
            ? "📜  LUẬT CHƠI ĐỐI KHÁNG"
            : "📜  BATTLE RULES";

        public static string RulesContent => CurrentLanguage == GameLanguage.Vietnamese
            ? "<b>1. Bàn cờ chung 15x30:</b> Hai người chơi thi đấu đồng thời. P1 ở trên rơi xuống (-Y), P2 ở dưới rơi lên (+Y).\n\n" +
              "<b>2. Rào chắn trung tâm:</b> Luôn có 2 hàng rào chắn cố định ở giữa (hàng 14-15), không thể bị phá hủy và ngăn không cho khối lấn sang sân đối phương.\n\n" +
              "<b>3. Đẩy khối khi phá hàng:</b> Khi một bên phá hàng, toàn bộ cấu trúc khối (kèm rào chắn) sẽ <b>bị đẩy dồn về phía đối thủ</b> để thu hẹp không gian đặt khối của họ!\n\n" +
              "<b>4. Mất tim & Tái cân bằng:</b>\n" +
              "   • Khi khối gạch chạm mép biên (P1 chạm hàng 29, P2 chạm hàng 0) ➔ <b>Bị trừ 1 tim!</b>\n" +
              "   • Rào chắn trở về giữa bàn (hàng 14-15). Bên mất máu bị <b>xóa sạch 100% gạch</b>.\n" +
              "   • Bên sống sót xóa 1/2 số gạch ở phía rào chắn, phần còn lại được dồn sát vào rào chắn.\n\n" +
              "<b>5. Chiến thắng:</b> Mỗi bên có 3 tim. Bên nào hết tim trước sẽ thua cuộc!"
            : "<b>1. Shared 15x30 Board:</b> Two players battle concurrently. P1 drops from top (-Y), P2 drops from bottom (+Y).\n\n" +
              "<b>2. Center Barrier:</b> 2 indestructible center rows (rows 14-15) always stay filled and prevent crossing.\n\n" +
              "<b>3. Push on Line Clear:</b> When a player clears lines, all blocks (with the barrier) are <b>pushed toward the opponent</b> to shrink their placement space!\n\n" +
              "<b>4. Heart Loss & Rebalance:</b>\n" +
              "   • When blocks touch edge (P1 touches row 29, P2 touches row 0) ➔ <b>Lose 1 Heart!</b>\n" +
              "   • Barrier resets to center (rows 14-15). Loser's blocks are <b>100% wiped</b>.\n" +
              "   • Survivor's blocks near barrier are halved (1/2 cleared), remaining blocks pulled tight against barrier.\n\n" +
              "<b>5. Victory:</b> Each player has 3 hearts. First to lose all hearts loses the match!";

        public static string ControlsHeader => CurrentLanguage == GameLanguage.Vietnamese
            ? "🎮  HƯỚNG DẪN ĐIỀU KHIỂN (CÙNG BÀN PHÍM)"
            : "🎮  CONTROLS (SAME KEYBOARD)";

        public static string P1CtrlText => CurrentLanguage == GameLanguage.Vietnamese
            ? "<b><color=#00ffff>NGƯỜI CHƠI 1 (TRÊN - TẤN CÔNG)</color></b>\n\n" +
              "• <b>A / D</b> : Sang trái / Sang phải\n" +
              "• <b>W</b> : Xoay khối\n" +
              "• <b>S</b> : Tăng tốc độ rơi (Rơi nhanh)\n" +
              "• <b>SPACE / F</b> : Thả khối tức thì"
            : "<b><color=#00ffff>PLAYER 1 (TOP - ATTACKER)</color></b>\n\n" +
              "• <b>A / D</b> : Move Left / Right\n" +
              "• <b>W</b> : Rotate Piece\n" +
              "• <b>S</b> : Soft Drop (Fast Fall)\n" +
              "• <b>SPACE / F</b> : Hard Drop (Instant)";

        public static string P2CtrlText => CurrentLanguage == GameLanguage.Vietnamese
            ? "<b><color=#ff9933>NGƯỜI CHƠI 2 (DƯỚI - PHÒNG THỦ)</color></b>\n\n" +
              "• <b>← / →</b> : Sang trái / Sang phải\n" +
              "• <b>↓ (Mũi tên xuống)</b> : Xoay khối\n" +
              "• <b>↑ (Mũi tên lên)</b> : Tăng tốc độ rơi (Rơi nhanh)\n" +
              "• <b>RIGHT SHIFT / ENTER</b> : Thả khối tức thì"
            : "<b><color=#ff9933>PLAYER 2 (BOTTOM - DEFENDER)</color></b>\n\n" +
              "• <b>← / →</b> : Move Left / Right\n" +
              "• <b>↓ (Down Arrow)</b> : Rotate Piece\n" +
              "• <b>↑ (Up Arrow)</b> : Soft Drop (Fast Fall)\n" +
              "• <b>RIGHT SHIFT / ENTER</b> : Hard Drop (Instant)";

        // ================= Chuỗi hiển thị Gameplay =================
        public static string PauseBtn => CurrentLanguage == GameLanguage.Vietnamese
            ? "⏸  TẠM DỪNG"
            : "⏸  PAUSE";

        public static string PauseTitle => CurrentLanguage == GameLanguage.Vietnamese
            ? "TẠM DỪNG"
            : "PAUSED";

        public static string PauseSubtitle => CurrentLanguage == GameLanguage.Vietnamese
            ? "TRÒ CHƠI ĐANG ĐƯỢC TẠM DỪNG"
            : "GAME IS CURRENTLY PAUSED";

        public static string ResumeBtn => CurrentLanguage == GameLanguage.Vietnamese
            ? "TIẾP TỤC"
            : "RESUME";

        public static string PauseReturnMenuBtn => CurrentLanguage == GameLanguage.Vietnamese
            ? "QUAY LẠI MENU"
            : "RETURN TO MENU";

        public static string P1Name => CurrentLanguage == GameLanguage.Vietnamese
            ? "NGƯỜI CHƠI 1"
            : "PLAYER 1";

        public static string P1Role => CurrentLanguage == GameLanguage.Vietnamese
            ? "TẤN CÔNG (TRÊN)"
            : "ATTACKER (TOP)";

        public static string P2Name => CurrentLanguage == GameLanguage.Vietnamese
            ? "NGƯỜI CHƠI 2"
            : "PLAYER 2";

        public static string P2Role => CurrentLanguage == GameLanguage.Vietnamese
            ? "PHÒNG THỦ (DƯỚI)"
            : "DEFENDER (BOTTOM)";

        public static string HeartsPrefix => CurrentLanguage == GameLanguage.Vietnamese
            ? "TIM: "
            : "HEARTS: ";

        public static string NextLabelPrefix => CurrentLanguage == GameLanguage.Vietnamese
            ? "TIẾP THEO: "
            : "NEXT: ";

        public static string CountdownStart => CurrentLanguage == GameLanguage.Vietnamese
            ? "BẮT ĐẦU!"
            : "START!";

        public static string P1Wins => CurrentLanguage == GameLanguage.Vietnamese
            ? "NGƯỜI CHƠI 1 CHIẾN THẮNG!"
            : "PLAYER 1 WINS!";

        public static string P2Wins => CurrentLanguage == GameLanguage.Vietnamese
            ? "NGƯỜI CHƠI 2 CHIẾN THẮNG!"
            : "PLAYER 2 WINS!";

        public static string RematchBtn => CurrentLanguage == GameLanguage.Vietnamese
            ? "CHƠI LẠI"
            : "REMATCH";

        public static string ReturnMenuBtn => CurrentLanguage == GameLanguage.Vietnamese
            ? "QUAY LẠI MENU"
            : "RETURN TO MENU";

        public static string DefaultReason => CurrentLanguage == GameLanguage.Vietnamese
            ? "Hết tim trước đối thủ!"
            : "Ran out of hearts!";

        public static string GetRoundResetBanner(PlayerSide toppedOutSide)
        {
            string player = toppedOutSide == PlayerSide.Top ? P1Name : P2Name;
            return CurrentLanguage == GameLanguage.Vietnamese
                ? $"<color=#ff4444>{player} MẤT 1 TIM!</color>\nĐANG CÂN BẰNG LẠI BÀN CỜ..."
                : $"<color=#ff4444>{player} LOST 1 HEART!</color>\nREBALANCING THE BOARD...";
        }
    }
}
