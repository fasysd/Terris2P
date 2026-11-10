using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Terris2P
{
    /// <summary>
    /// Các chế độ chơi trong Terris 2P.
    /// </summary>
    public enum GameMode
    {
        PlayerVsPlayer,
        PlayerVsAI
    }

    /// <summary>
    /// Các cấp độ khó của AI.
    /// </summary>
    public enum AIDifficulty
    {
        Easy,
        Medium,
        Hard,
        Master
    }

    /// <summary>
    /// Các trạng thái chính của vòng lặp trò chơi.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Countdown,
        Playing,
        RoundReset,
        Paused,
        GameOver
    }

    /// <summary>
    /// Quản lý trung tâm điều phối trạng thái game,
    /// chuyển tiếp vòng đấu, tạm dừng, máu (tim) và điều kiện thắng/thua.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        [Header("Chế độ chơi & Độ khó AI")]
        public const string GameModePrefKey = "Terris2P_GameMode";
        public const string DifficultyPrefKey = "Terris2P_AIDifficulty";

        public static GameMode SelectedMode
        {
            get
            {
                string saved = PlayerPrefs.GetString(GameModePrefKey, GameMode.PlayerVsAI.ToString());
                if (Enum.TryParse<GameMode>(saved, out var mode)) return mode;
                return GameMode.PlayerVsAI;
            }
            set
            {
                PlayerPrefs.SetString(GameModePrefKey, value.ToString());
                PlayerPrefs.Save();
                if (Instance != null) Instance.currentGameMode = value;
            }
        }

        public static AIDifficulty SelectedDifficulty
        {
            get
            {
                string saved = PlayerPrefs.GetString(DifficultyPrefKey, AIDifficulty.Medium.ToString());
                if (Enum.TryParse<AIDifficulty>(saved, out var diff)) return diff;
                return AIDifficulty.Medium;
            }
            set
            {
                PlayerPrefs.SetString(DifficultyPrefKey, value.ToString());
                PlayerPrefs.Save();
                if (Instance != null) Instance.currentAIDifficulty = value;
            }
        }

        public GameMode currentGameMode = GameMode.PlayerVsAI;
        public AIDifficulty currentAIDifficulty = AIDifficulty.Medium;
        private AI.TetrisAIController aiController;

        [Header("Tự động bắt đầu trận")]
        public bool autoStart = true;

        [Header("Tham chiếu đối tượng")]
        public GridManager gridManager;
        public PlayerController topPlayer;
        public PlayerController bottomPlayer;
        public GameHUD HUD => GameHUD.Instance;

        public int P1Hearts { get; private set; } = 3;
        public int P2Hearts { get; private set; } = 3;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            currentGameMode = SelectedMode;
            currentAIDifficulty = SelectedDifficulty;

            if (gridManager == null) gridManager = GridManager.Instance ?? FindFirstObjectByType<GridManager>();

            if (topPlayer == null)
            {
                foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                    if (p.side == PlayerSide.Top) { topPlayer = p; break; }
            }

            if (bottomPlayer == null)
            {
                foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                    if (p.side == PlayerSide.Bottom) { bottomPlayer = p; break; }
            }

            // Đăng ký nhận sự kiện từ GridManager
            if (gridManager != null)
            {
                gridManager.OnLinesCleared += HandleLinesCleared;
            }

            // Đăng ký nhận sự kiện từ hai người chơi
            if (topPlayer != null)
            {
                topPlayer.OnPieceLocked += HandlePieceLocked;
                topPlayer.OnToppedOut += HandlePlayerToppedOut;
                topPlayer.OnNextPieceChanged += HandleNextPieceChanged;
            }

            if (bottomPlayer != null)
            {
                bottomPlayer.OnPieceLocked += HandlePieceLocked;
                bottomPlayer.OnToppedOut += HandlePlayerToppedOut;
                bottomPlayer.OnNextPieceChanged += HandleNextPieceChanged;
            }

            if (autoStart)
            {
                StartGame();
            }
            else
            {
                topPlayer?.StopRound();
                bottomPlayer?.StopRound();
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;

            if (gridManager != null)
                gridManager.OnLinesCleared -= HandleLinesCleared;

            if (topPlayer != null)
            {
                topPlayer.OnPieceLocked -= HandlePieceLocked;
                topPlayer.OnToppedOut -= HandlePlayerToppedOut;
                topPlayer.OnNextPieceChanged -= HandleNextPieceChanged;
            }

            if (bottomPlayer != null)
            {
                bottomPlayer.OnPieceLocked -= HandlePieceLocked;
                bottomPlayer.OnToppedOut -= HandlePlayerToppedOut;
                bottomPlayer.OnNextPieceChanged -= HandleNextPieceChanged;
            }

            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            // Kiểm tra phím tạm dừng (Escape hoặc P)
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)
                {
                    if (CurrentState == GameState.Playing)
                    {
                        PauseGame();
                    }
                    else if (CurrentState == GameState.Paused)
                    {
                        ResumeGame();
                    }
                }
            }
        }

        public void StartGame()
        {
            Time.timeScale = 1f;
            P1Hearts = 3;
            P2Hearts = 3;

            if (gridManager != null)
            {
                gridManager.ClearBoard();
                gridManager.SpawnCenterBarrier(2);
            }

            ConfigureGameMode();

            HUD?.ShowInGameHUD();
            HUD?.UpdateHearts(PlayerSide.Top, P1Hearts);
            HUD?.UpdateHearts(PlayerSide.Bottom, P2Hearts);
            HUD?.SetRoleText(PlayerSide.Top, LocalizationManager.P1Role);
            HUD?.SetRoleText(PlayerSide.Bottom, LocalizationManager.GetP2Role(currentGameMode, currentAIDifficulty));
            HUD?.SetPlayerName(PlayerSide.Bottom, LocalizationManager.GetP2DisplayName(currentGameMode, currentAIDifficulty));

            CurrentState = GameState.Countdown;

            if (HUD != null)
            {
                HUD.ShowCountdown(() =>
                {
                    CurrentState = GameState.Playing;
                    topPlayer?.StartRound();
                    bottomPlayer?.StartRound();
                });
            }
            else
            {
                CurrentState = GameState.Playing;
                topPlayer?.StartRound();
                bottomPlayer?.StartRound();
            }
        }

        public void SetGameMode(GameMode mode)
        {
            currentGameMode = mode;
            SelectedMode = mode;
        }

        public void SetAIDifficulty(AIDifficulty diff)
        {
            currentAIDifficulty = diff;
            SelectedDifficulty = diff;
            if (aiController != null)
                aiController.SetDifficulty(diff);
        }

        private void ConfigureGameMode()
        {
            if (bottomPlayer == null) return;

            if (currentGameMode == GameMode.PlayerVsAI)
            {
                bottomPlayer.isAIControlled = true;
                aiController = bottomPlayer.GetComponent<AI.TetrisAIController>();
                if (aiController == null)
                    aiController = bottomPlayer.gameObject.AddComponent<AI.TetrisAIController>();

                aiController.playerController = bottomPlayer;
                aiController.gridManager = gridManager;
                aiController.SetDifficulty(currentAIDifficulty);
                if (aiController.thinkingConfig == null)
                    aiController.thinkingConfig = AI.AIThinkingConfigSO.GetDefaultInstance();
                aiController.enabled = true;
                aiController.ResetPlan();
            }
            else
            {
                bottomPlayer.isAIControlled = false;
                if (aiController != null)
                {
                    aiController.ResetPlan();
                    aiController.enabled = false;
                }
            }
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            HUD?.ShowPauseMenu();
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            Time.timeScale = 1f;
            CurrentState = GameState.Playing;
            HUD?.HidePauseMenu();
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MenuScene");
        }

        public void RestartMatch()
        {
            Time.timeScale = 1f;
            StartGame();
        }

        public bool CheckEdgeLoss()
        {
            if (CurrentState != GameState.Playing || gridManager == null) return false;

            // Kiểm tra khối gạch chạm mép trên (biên của P1: hàng height - 1)
            bool p1EdgeHit = gridManager.HasBlockAtRow(gridManager.Height - 1);
            // Kiểm tra khối gạch chạm mép dưới (biên của P2: hàng 0)
            bool p2EdgeHit = gridManager.HasBlockAtRow(0);

            if (p1EdgeHit)
            {
                string reason = LocalizationManager.CurrentLanguage == GameLanguage.Vietnamese
                    ? "Người chơi 1 chạm mép trên!"
                    : "Player 1 hit the top edge!";
                TriggerHealthLoss(PlayerSide.Top, reason);
                return true;
            }
            if (p2EdgeHit)
            {
                string reason = LocalizationManager.CurrentLanguage == GameLanguage.Vietnamese
                    ? "Người chơi 2 chạm mép dưới!"
                    : "Player 2 hit the bottom edge!";
                TriggerHealthLoss(PlayerSide.Bottom, reason);
                return true;
            }

            return false;
        }

        public void TriggerHealthLoss(PlayerSide loserSide, string reason)
        {
            SoundManager.Instance?.PlayHeartLost();

            if (loserSide == PlayerSide.Top)
            {
                P1Hearts = Mathf.Max(0, P1Hearts - 1);
                HUD?.UpdateHearts(PlayerSide.Top, P1Hearts);
            }
            else
            {
                P2Hearts = Mathf.Max(0, P2Hearts - 1);
                HUD?.UpdateHearts(PlayerSide.Bottom, P2Hearts);
            }

            if (P1Hearts <= 0)
            {
                string overReason = LocalizationManager.CurrentLanguage == GameLanguage.Vietnamese
                    ? "Người chơi 1 chạm mép trên (hết tim)!"
                    : "Player 1 hit top edge (out of hearts)!";
                TriggerGameOver(PlayerSide.Bottom, overReason);
            }
            else if (P2Hearts <= 0)
            {
                string overReason = LocalizationManager.CurrentLanguage == GameLanguage.Vietnamese
                    ? "Người chơi 2 chạm mép dưới (hết tim)!"
                    : "Player 2 hit bottom edge (out of hearts)!";
                TriggerGameOver(PlayerSide.Top, overReason);
            }
            else
            {
                TriggerRoundReset(loserSide, () =>
                {
                    gridManager?.ExecuteHeartLossReset(loserSide);
                    topPlayer?.StartRound();
                    bottomPlayer?.StartRound();
                });
            }
        }

        public void TriggerRoundReset(PlayerSide toppedOutSide, Action onResume)
        {
            if (CurrentState != GameState.Playing) return;

            StartCoroutine(RoundResetRoutine(toppedOutSide, onResume));
        }

        private IEnumerator RoundResetRoutine(PlayerSide toppedOutSide, Action onResume)
        {
            CurrentState = GameState.RoundReset;
            topPlayer?.StopRound();
            bottomPlayer?.StopRound();
            aiController?.ResetPlan();

            string bannerMsg = LocalizationManager.GetRoundResetBanner(toppedOutSide);
            HUD?.ShowBanner(bannerMsg, 1.6f);

            yield return new WaitForSeconds(1.6f);

            onResume?.Invoke();
            CurrentState = GameState.Playing;
        }

        public void TriggerGameOver(PlayerSide winner, string reason)
        {
            if (CurrentState == GameState.GameOver) return;

            CurrentState = GameState.GameOver;
            topPlayer?.StopRound();
            bottomPlayer?.StopRound();
            aiController?.ResetPlan();

            SoundManager.Instance?.PlayVictory();
            HUD?.ShowGameOver(winner, reason);
        }

        private void HandlePieceLocked(PlayerSide side)
        {
            if (CurrentState != GameState.Playing) return;
            if (CheckEdgeLoss()) return;

            PlayerController pc = side == PlayerSide.Top ? topPlayer : bottomPlayer;
            pc?.SpawnPiece();
        }

        private void HandleLinesCleared(int count, PlayerSide side, List<int> rows)
        {
            if (CurrentState != GameState.Playing) return;

            PlayerSide opponent = side == PlayerSide.Top ? PlayerSide.Bottom : PlayerSide.Top;
            string sideText = side == PlayerSide.Top ? LocalizationManager.P1Name : LocalizationManager.P2Name;
            string oppText = opponent == PlayerSide.Top ? LocalizationManager.P1Name : LocalizationManager.P2Name;
            string pushWord = LocalizationManager.CurrentLanguage == GameLanguage.Vietnamese ? "ĐẨY" : "PUSHES";
            string linesWord = LocalizationManager.CurrentLanguage == GameLanguage.Vietnamese ? "Hàng" : "Lines";
            HUD?.ShowBanner($"{sideText} {pushWord} {oppText}! (+{count} {linesWord})", 1.2f);
            CheckEdgeLoss();
        }

        private void HandlePlayerToppedOut(PlayerSide side)
        {
            if (CurrentState != GameState.Playing) return;

            string sideName = side == PlayerSide.Top ? LocalizationManager.P1Name : LocalizationManager.P2Name;
            string reason = LocalizationManager.CurrentLanguage == GameLanguage.Vietnamese
                ? $"{sideName} bị chặn vị trí xuất hiện khối!"
                : $"{sideName} spawn position is blocked!";
            TriggerHealthLoss(side, reason);
        }

        private void HandleNextPieceChanged(PlayerSide side, TetrominoType type)
        {
            HUD?.UpdateNextPiece(side, type);
        }
    }
}
