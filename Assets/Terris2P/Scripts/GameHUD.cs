using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Terris2P
{
    /// <summary>
    /// Hệ thống Canvas uGUI hiển thị HUD và Menu cho Terris 2P.
    /// Quản lý Menu chính, InGame HUD (Tim P1/P2, vai trò, khối kế tiếp),
    /// nút và hộp thoại tạm dừng (Pause Box), đếm ngược, và bảng kết thúc trận.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }

        [Header("Canvas & Scaler")]
        public Canvas mainCanvas;
        public CanvasScaler canvasScaler;

        [Header("Các Panel giao diện")]
        public GameObject mainMenuPanel;
        public GameObject inGameHudPanel;
        public GameObject gameOverPanel;
        public GameObject countdownPanel;
        public GameObject pausePanel;

        [Header("Thành phần HUD P1")]
        public Text p1NameText;
        public Text p1RoleText;
        public Text p1HeartsText;
        public Image p1NextPieceImage;
        public Text p1NextLabel;

        [Header("Thành phần HUD P2")]
        public Text p2NameText;
        public Text p2RoleText;
        public Text p2HeartsText;
        public Image p2NextPieceImage;
        public Text p2NextLabel;

        [Header("Thông báo & Đếm ngược")]
        public Text countdownText;
        public Text bannerText;
        private Coroutine bannerCoroutine;

        [Header("Bảng kết thúc trận")]
        public Text gameOverWinnerText;
        public Text gameOverReasonText;
        public Button rematchBtn;
        public Button returnMenuBtn;
        public Text rematchBtnText;
        public Text returnMenuBtnText;

        [Header("Hộp thoại tạm dừng")]
        public Button pauseBtn;
        public Button resumeBtn;
        public Button pauseReturnMenuBtn;
        public Text pauseBtnText;
        public Text pauseTitleText;
        public Text pauseSubtitleText;
        public Text resumeBtnText;
        public Text pauseReturnMenuBtnText;

        [Header("Nút & Chuỗi Menu chính")]
        public Button startGameBtn;
        public Button modeToggleBtn;
        public Button difficultyToggleBtn;
        public Button languageToggleBtn;
        public Text menuSubtitleText;
        public Text startGameBtnText;
        public Text modeToggleBtnText;
        public Text difficultyToggleBtnText;
        public Text languageToggleBtnText;
        public Text tipText;
        public Text rulesHeaderText;
        public Text rulesContentText;
        public Text controlsHeaderText;
        public Text p1CtrlText;
        public Text p2CtrlText;

        [Header("Font chữ UI")]
        public Font uiFont;

        public void EnsureFont()
        {
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (uiFont == null)
                {
                    string[] fonts = Font.GetOSInstalledFontNames();
                    if (fonts != null && fonts.Length > 0)
                        uiFont = Font.CreateDynamicFontFromOSFont(fonts[0], 16);
                }
            }
        }

        private void Awake()
        {
            Instance = this;

            EnsureFont();
            EnsureEventSystem();

            if (inGameHudPanel == null && mainMenuPanel == null)
            {
                BuildCanvasUI();
            }
            else
            {
                EnsureMenuButtons();
                HookSceneUIButtons();
            }

            UpdateAllLocalizedTexts();
        }

        public void EnsureMenuButtons()
        {
            if (mainMenuPanel == null) return;
            Transform leftPanel = mainMenuPanel.transform.Find("LeftNavPanel");
            if (leftPanel == null) return;

            if (modeToggleBtn == null)
            {
                Transform existing = leftPanel.Find("ModeToggleBtn");
                if (existing != null)
                {
                    modeToggleBtn = existing.GetComponent<Button>();
                    modeToggleBtnText = existing.GetComponentInChildren<Text>();
                }
                else
                {
                    GameObject btnGo = CreateButton("ModeToggleBtn", leftPanel, LocalizationManager.ModePvEBtn,
                        new Vector2(0.5f, 0.49f), new Vector2(440, 50), new Color(0.18f, 0.5f, 0.35f), () =>
                        {
                            var nextMode = GameManager.SelectedMode == GameMode.PlayerVsAI
                                ? GameMode.PlayerVsPlayer
                                : GameMode.PlayerVsAI;
                            GameManager.SelectedMode = nextMode;
                            UpdateAllLocalizedTexts();
                        });
                    modeToggleBtn = btnGo.GetComponent<Button>();
                    modeToggleBtnText = btnGo.GetComponentInChildren<Text>();
                }
            }

            if (difficultyToggleBtn == null)
            {
                Transform existing = leftPanel.Find("DifficultyToggleBtn");
                if (existing != null)
                {
                    difficultyToggleBtn = existing.GetComponent<Button>();
                    difficultyToggleBtnText = existing.GetComponentInChildren<Text>();
                }
                else
                {
                    GameObject btnGo = CreateButton("DifficultyToggleBtn", leftPanel, "",
                        new Vector2(0.5f, 0.39f), new Vector2(440, 48), new Color(0.55f, 0.32f, 0.15f), () =>
                        {
                            var nextDiff = (AIDifficulty)(((int)GameManager.SelectedDifficulty + 1) % 4);
                            GameManager.SelectedDifficulty = nextDiff;
                            UpdateAllLocalizedTexts();
                        });
                    difficultyToggleBtn = btnGo.GetComponent<Button>();
                    difficultyToggleBtnText = btnGo.GetComponentInChildren<Text>();
                }
            }

            if (startGameBtn != null)
            {
                RectTransform rt = startGameBtn.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.59f);
                rt.anchorMax = new Vector2(0.5f, 0.59f);
                rt.sizeDelta = new Vector2(440, 56);
            }
            if (languageToggleBtn != null)
            {
                RectTransform rt = languageToggleBtn.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.29f);
                rt.anchorMax = new Vector2(0.5f, 0.29f);
                rt.sizeDelta = new Vector2(440, 46);
            }
            Transform tipBox = leftPanel.Find("TipBox");
            if (tipBox != null)
            {
                RectTransform rt = tipBox.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.06f, 0.03f);
                rt.anchorMax = new Vector2(0.94f, 0.22f);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.transform.SetParent(transform);
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        public void HookSceneUIButtons()
        {
            if (startGameBtn != null)
            {
                startGameBtn.onClick.RemoveAllListeners();
                startGameBtn.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.StartGame();
                    else
                        UnityEngine.SceneManagement.SceneManager.LoadScene("GameplayScene");
                });
            }

            if (modeToggleBtn != null)
            {
                modeToggleBtn.onClick.RemoveAllListeners();
                modeToggleBtn.onClick.AddListener(() =>
                {
                    var nextMode = GameManager.SelectedMode == GameMode.PlayerVsAI
                        ? GameMode.PlayerVsPlayer
                        : GameMode.PlayerVsAI;
                    GameManager.SelectedMode = nextMode;
                    UpdateAllLocalizedTexts();
                });
            }

            if (difficultyToggleBtn != null)
            {
                difficultyToggleBtn.onClick.RemoveAllListeners();
                difficultyToggleBtn.onClick.AddListener(() =>
                {
                    var nextDiff = (AIDifficulty)(((int)GameManager.SelectedDifficulty + 1) % 4);
                    GameManager.SelectedDifficulty = nextDiff;
                    UpdateAllLocalizedTexts();
                });
            }

            if (languageToggleBtn != null)
            {
                languageToggleBtn.onClick.RemoveAllListeners();
                languageToggleBtn.onClick.AddListener(() =>
                {
                    LocalizationManager.ToggleLanguage();
                    UpdateAllLocalizedTexts();
                });
            }

            if (pauseBtn != null)
            {
                pauseBtn.onClick.RemoveAllListeners();
                pauseBtn.onClick.AddListener(() => GameManager.Instance?.PauseGame());
            }

            if (resumeBtn != null)
            {
                resumeBtn.onClick.RemoveAllListeners();
                resumeBtn.onClick.AddListener(() => GameManager.Instance?.ResumeGame());
            }

            if (pauseReturnMenuBtn != null)
            {
                pauseReturnMenuBtn.onClick.RemoveAllListeners();
                pauseReturnMenuBtn.onClick.AddListener(() => GameManager.Instance?.ReturnToMenu());
            }

            if (rematchBtn != null)
            {
                rematchBtn.onClick.RemoveAllListeners();
                rematchBtn.onClick.AddListener(() => GameManager.Instance?.RestartMatch());
            }

            if (returnMenuBtn != null)
            {
                returnMenuBtn.onClick.RemoveAllListeners();
                returnMenuBtn.onClick.AddListener(() => GameManager.Instance?.ReturnToMenu());
            }
        }

        public void UpdateAllLocalizedTexts()
        {
            if (menuSubtitleText != null) menuSubtitleText.text = LocalizationManager.MenuSubtitle;
            if (startGameBtnText != null) startGameBtnText.text = LocalizationManager.StartGameBtn;

            GameMode curMode = GameManager.SelectedMode;
            AIDifficulty curDiff = GameManager.SelectedDifficulty;

            if (modeToggleBtnText != null)
                modeToggleBtnText.text = curMode == GameMode.PlayerVsAI ? LocalizationManager.ModePvEBtn : LocalizationManager.ModePvPBtn;

            if (difficultyToggleBtnText != null)
            {
                if (curMode == GameMode.PlayerVsAI)
                    difficultyToggleBtnText.text = $"{LocalizationManager.DifficultyPrefix}{LocalizationManager.GetDifficultyName(curDiff)}";
                else
                    difficultyToggleBtnText.text = LocalizationManager.CurrentLanguage == GameLanguage.Vietnamese
                        ? "--- CHẾ ĐỘ 2 NGƯỜI (PvP) ---"
                        : "--- 2 PLAYERS (PvP) ---";
            }

            if (languageToggleBtnText != null) languageToggleBtnText.text = LocalizationManager.LanguageToggleBtn;
            if (tipText != null) tipText.text = LocalizationManager.TipText;
            if (rulesHeaderText != null) rulesHeaderText.text = LocalizationManager.RulesHeader;
            if (rulesContentText != null) rulesContentText.text = LocalizationManager.RulesContent;
            if (controlsHeaderText != null) controlsHeaderText.text = LocalizationManager.ControlsHeader;
            if (p1CtrlText != null) p1CtrlText.text = LocalizationManager.P1CtrlText;
            if (p2CtrlText != null) p2CtrlText.text = LocalizationManager.P2CtrlText;

            if (pauseBtnText != null) pauseBtnText.text = LocalizationManager.PauseBtn;
            if (pauseTitleText != null) pauseTitleText.text = LocalizationManager.PauseTitle;
            if (pauseSubtitleText != null) pauseSubtitleText.text = LocalizationManager.PauseSubtitle;
            if (resumeBtnText != null) resumeBtnText.text = LocalizationManager.ResumeBtn;
            if (pauseReturnMenuBtnText != null) pauseReturnMenuBtnText.text = LocalizationManager.PauseReturnMenuBtn;
            if (rematchBtnText != null) rematchBtnText.text = LocalizationManager.RematchBtn;
            if (returnMenuBtnText != null) returnMenuBtnText.text = LocalizationManager.ReturnMenuBtn;

            if (p1NameText != null) p1NameText.text = LocalizationManager.P1Name;
            if (p1RoleText != null) p1RoleText.text = LocalizationManager.P1Role;
            if (p2NameText != null) p2NameText.text = LocalizationManager.GetP2DisplayName(curMode, curDiff);
            if (p2RoleText != null) p2RoleText.text = LocalizationManager.GetP2Role(curMode, curDiff);
        }

        public void SetPlayerName(PlayerSide side, string name)
        {
            if (side == PlayerSide.Top && p1NameText != null) p1NameText.text = name;
            if (side == PlayerSide.Bottom && p2NameText != null) p2NameText.text = name;
        }

        public void BuildCanvasUI()
        {
            EnsureFont();

            if (mainCanvas == null)
                mainCanvas = GetComponent<Canvas>();

            if (mainCanvas == null)
            {
                GameObject canvasGo = new GameObject("Terris2P_Canvas");
                canvasGo.transform.SetParent(transform);
                mainCanvas = canvasGo.AddComponent<Canvas>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                mainCanvas.sortingOrder = 100;

                canvasScaler = canvasGo.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1920, 1080);
                canvasScaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
            }

            Transform parent = mainCanvas.transform;

            // 1. Menu chính (nếu có)
            BuildMainMenu(parent);

            // 2. HUD trong trận đấu
            BuildInGameHUD(parent);

            // 3. Panel đếm ngược và thông báo
            BuildBanners(parent);

            // 4. Hộp thoại tạm dừng (Pause Modal)
            BuildPauseModal(parent);

            // 5. Bảng thông báo kết thúc trận
            BuildGameOverModal(parent);

            HookSceneUIButtons();
        }

        public void BuildMainMenu(Transform parent)
        {
            mainMenuPanel = CreatePanel("MainMenuPanel", parent, new Color(0.05f, 0.07f, 0.11f, 0.96f));

            // ========================================================
            // CỘT BÊN TRÁI: TIÊU ĐỀ & NÚT ĐIỀU HƯỚNG
            // ========================================================
            GameObject leftPanel = CreatePanel("LeftNavPanel", mainMenuPanel.transform, new Color(0.08f, 0.12f, 0.2f, 0.92f));
            RectTransform lrt = leftPanel.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.04f, 0.08f);
            lrt.anchorMax = new Vector2(0.39f, 0.92f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            // Tiêu đề game bên trái
            CreateText("MenuTitle", leftPanel.transform, LocalizationManager.MenuTitle, 56, FontStyle.Bold, Color.cyan,
                new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(500, 70));

            menuSubtitleText = CreateText("MenuSubtitle", leftPanel.transform, LocalizationManager.MenuSubtitle, 20, FontStyle.Normal, new Color(0.8f, 0.9f, 1f),
                new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(500, 50));

            // Đường kẻ trang trí phân cách
            GameObject leftDivider = CreatePanel("LeftDivider", leftPanel.transform, new Color(0.2f, 0.4f, 0.7f, 0.5f));
            RectTransform ldRt = leftDivider.GetComponent<RectTransform>();
            ldRt.anchorMin = new Vector2(0.08f, 0.69f);
            ldRt.anchorMax = new Vector2(0.92f, 0.693f);
            ldRt.offsetMin = Vector2.zero;
            ldRt.offsetMax = Vector2.zero;

            // 1. Nút Bắt đầu chơi (Nổi bật, kích thước lớn)
            GameObject startBtnGo = CreateButton("StartGameBtn", leftPanel.transform, LocalizationManager.StartGameBtn,
                new Vector2(0.5f, 0.58f), new Vector2(440, 58), new Color(0.12f, 0.55f, 0.9f), () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.StartGame();
                    else
                        UnityEngine.SceneManagement.SceneManager.LoadScene("GameplayScene");
                });
            startGameBtn = startBtnGo.GetComponent<Button>();
            startGameBtnText = startBtnGo.GetComponentInChildren<Text>();

            // 2. Nút Chọn chế độ chơi (PvP / PvE)
            GameMode initMode = GameManager.Instance != null ? GameManager.Instance.currentGameMode : GameMode.PlayerVsAI;
            string initModeText = initMode == GameMode.PlayerVsAI ? LocalizationManager.ModePvEBtn : LocalizationManager.ModePvPBtn;
            GameObject modeBtnGo = CreateButton("ModeToggleBtn", leftPanel.transform, initModeText,
                new Vector2(0.5f, 0.48f), new Vector2(440, 50), new Color(0.18f, 0.5f, 0.35f), () =>
                {
                    if (GameManager.Instance != null)
                    {
                        var nextMode = GameManager.Instance.currentGameMode == GameMode.PlayerVsAI
                            ? GameMode.PlayerVsPlayer
                            : GameMode.PlayerVsAI;
                        GameManager.Instance.SetGameMode(nextMode);
                        UpdateAllLocalizedTexts();
                    }
                });
            modeToggleBtn = modeBtnGo.GetComponent<Button>();
            modeToggleBtnText = modeBtnGo.GetComponentInChildren<Text>();

            // 3. Nút Chọn độ khó AI
            AIDifficulty initDiff = GameManager.Instance != null ? GameManager.Instance.currentAIDifficulty : AIDifficulty.Medium;
            string initDiffText = $"{LocalizationManager.DifficultyPrefix}{LocalizationManager.GetDifficultyName(initDiff)}";
            GameObject diffBtnGo = CreateButton("DifficultyToggleBtn", leftPanel.transform, initDiffText,
                new Vector2(0.5f, 0.38f), new Vector2(440, 48), new Color(0.55f, 0.32f, 0.15f), () =>
                {
                    if (GameManager.Instance != null)
                    {
                        var nextDiff = (AIDifficulty)(((int)GameManager.Instance.currentAIDifficulty + 1) % 4);
                        GameManager.Instance.SetAIDifficulty(nextDiff);
                        UpdateAllLocalizedTexts();
                    }
                });
            difficultyToggleBtn = diffBtnGo.GetComponent<Button>();
            difficultyToggleBtnText = diffBtnGo.GetComponentInChildren<Text>();

            // 4. Nút Chọn ngôn ngữ (Tiếng Anh / Tiếng Việt)
            GameObject langBtnGo = CreateButton("LanguageToggleBtn", leftPanel.transform, LocalizationManager.LanguageToggleBtn,
                new Vector2(0.5f, 0.28f), new Vector2(440, 46), new Color(0.28f, 0.22f, 0.48f), () =>
                {
                    LocalizationManager.ToggleLanguage();
                    UpdateAllLocalizedTexts();
                });
            languageToggleBtn = langBtnGo.GetComponent<Button>();
            languageToggleBtnText = langBtnGo.GetComponentInChildren<Text>();

            // 5. Khung ghi chú / mẹo nhanh phía dưới cột trái
            GameObject tipBox = CreatePanel("TipBox", leftPanel.transform, new Color(0.05f, 0.08f, 0.14f, 0.75f));
            RectTransform tbRt = tipBox.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0.06f, 0.02f);
            tbRt.anchorMax = new Vector2(0.94f, 0.21f);
            tbRt.offsetMin = Vector2.zero;
            tbRt.offsetMax = Vector2.zero;

            tipText = CreateText("TipText", tipBox.transform, LocalizationManager.TipText, 14, FontStyle.Normal, new Color(0.75f, 0.85f, 0.95f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 130), TextAnchor.MiddleLeft);

            // ========================================================
            // CỘT BÊN PHẢI: HƯỚNG DẪN LUẬT CHƠI & ĐIỀU KHIỂN
            // ========================================================
            GameObject rightPanel = CreatePanel("RightInfoPanel", mainMenuPanel.transform, new Color(0.08f, 0.12f, 0.2f, 0.92f));
            RectTransform rrt = rightPanel.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.42f, 0.08f);
            rrt.anchorMax = new Vector2(0.96f, 0.92f);
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;

            // 1. Khung Luật chơi (Nửa trên bên phải)
            GameObject rulesBox = CreatePanel("RulesBox", rightPanel.transform, new Color(0.06f, 0.09f, 0.16f, 0.85f));
            RectTransform rbRt = rulesBox.GetComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(0.03f, 0.49f);
            rbRt.anchorMax = new Vector2(0.97f, 0.96f);
            rbRt.offsetMin = Vector2.zero;
            rbRt.offsetMax = Vector2.zero;

            rulesHeaderText = CreateText("RulesHeader", rulesBox.transform, LocalizationManager.RulesHeader, 22, FontStyle.Bold, Color.yellow,
                new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.90f), Vector2.zero, new Vector2(0, 32), TextAnchor.MiddleLeft);

            rulesContentText = CreateText("RulesContent", rulesBox.transform, LocalizationManager.RulesContent, 15, FontStyle.Normal, Color.white,
                new Vector2(0.04f, 0.44f), new Vector2(0.96f, 0.44f), Vector2.zero, new Vector2(0, 310), TextAnchor.UpperLeft);

            // 2. Khung Hướng dẫn điều khiển (Nửa dưới bên phải)
            GameObject controlsBox = CreatePanel("ControlsBox", rightPanel.transform, new Color(0.06f, 0.09f, 0.16f, 0.85f));
            RectTransform cbRt = controlsBox.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.03f, 0.04f);
            cbRt.anchorMax = new Vector2(0.97f, 0.46f);
            cbRt.offsetMin = Vector2.zero;
            cbRt.offsetMax = Vector2.zero;

            controlsHeaderText = CreateText("ControlsHeader", controlsBox.transform, LocalizationManager.ControlsHeader, 22, FontStyle.Bold, Color.yellow,
                new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.89f), Vector2.zero, new Vector2(0, 32), TextAnchor.MiddleLeft);

            // Thẻ Người chơi 1 (Phía trên / Tấn công)
            GameObject p1CtrlBox = CreatePanel("P1_CtrlBox", controlsBox.transform, new Color(0.08f, 0.14f, 0.22f, 0.95f));
            RectTransform p1cRt = p1CtrlBox.GetComponent<RectTransform>();
            p1cRt.anchorMin = new Vector2(0.03f, 0.08f);
            p1cRt.anchorMax = new Vector2(0.485f, 0.77f);
            p1cRt.offsetMin = Vector2.zero;
            p1cRt.offsetMax = Vector2.zero;

            p1CtrlText = CreateText("P1_CtrlText", p1CtrlBox.transform, LocalizationManager.P1CtrlText, 15, FontStyle.Normal, Color.white,
                new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), Vector2.zero, new Vector2(0, 180), TextAnchor.MiddleLeft);

            // Thẻ Người chơi 2 (Phía dưới / Phòng thủ)
            GameObject p2CtrlBox = CreatePanel("P2_CtrlBox", controlsBox.transform, new Color(0.22f, 0.12f, 0.08f, 0.95f));
            RectTransform p2cRt = p2CtrlBox.GetComponent<RectTransform>();
            p2cRt.anchorMin = new Vector2(0.515f, 0.08f);
            p2cRt.anchorMax = new Vector2(0.97f, 0.77f);
            p2cRt.offsetMin = Vector2.zero;
            p2cRt.offsetMax = Vector2.zero;

            p2CtrlText = CreateText("P2_CtrlText", p2CtrlBox.transform, LocalizationManager.P2CtrlText, 15, FontStyle.Normal, Color.white,
                new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), Vector2.zero, new Vector2(0, 180), TextAnchor.MiddleLeft);
        }

        public void BuildInGameHUD(Transform parent)
        {
            inGameHudPanel = CreatePanel("InGameHUDPanel", parent, Color.clear);

            // ================= Nút Tạm Dừng Ở Góc Trên Giữa =================
            GameObject pauseBtnGo = CreateButton("PauseBtn", inGameHudPanel.transform, LocalizationManager.PauseBtn,
                new Vector2(0.5f, 0.94f), new Vector2(180, 46), new Color(0.2f, 0.3f, 0.45f, 0.9f), () =>
                {
                    GameManager.Instance?.PauseGame();
                });
            pauseBtn = pauseBtnGo.GetComponent<Button>();
            pauseBtnText = pauseBtnGo.GetComponentInChildren<Text>();

            // ================= HUD Người chơi 1 (Góc trên bên trái) =================
            GameObject p1Hud = CreatePanel("P1_HUD", inGameHudPanel.transform, new Color(0.08f, 0.12f, 0.2f, 0.75f));
            RectTransform p1Rt = p1Hud.GetComponent<RectTransform>();
            p1Rt.anchorMin = new Vector2(0.02f, 0.78f);
            p1Rt.anchorMax = new Vector2(0.22f, 0.98f);
            p1Rt.offsetMin = Vector2.zero;
            p1Rt.offsetMax = Vector2.zero;

            p1NameText = CreateText("P1_Name", p1Hud.transform, LocalizationManager.P1Name, 24, FontStyle.Bold, Color.cyan,
                new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(250, 35));

            p1RoleText = CreateText("P1_Role", p1Hud.transform, LocalizationManager.P1Role, 18, FontStyle.Italic, Color.white,
                new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(250, 30));

            p1HeartsText = CreateText("P1_Hearts", p1Hud.transform, $"{LocalizationManager.HeartsPrefix}<color=#ff3344>♥♥♥</color>", 22, FontStyle.Bold, Color.white,
                new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(250, 35));

            // Khung hiển thị khối kế tiếp của P1 (Góc trên bên phải)
            GameObject p1NextBox = CreatePanel("P1_NextBox", inGameHudPanel.transform, new Color(0.08f, 0.12f, 0.2f, 0.75f));
            RectTransform p1NbRt = p1NextBox.GetComponent<RectTransform>();
            p1NbRt.anchorMin = new Vector2(0.78f, 0.78f);
            p1NbRt.anchorMax = new Vector2(0.98f, 0.98f);
            p1NbRt.offsetMin = Vector2.zero;
            p1NbRt.offsetMax = Vector2.zero;

            p1NextLabel = CreateText("P1_NextLabel", p1NextBox.transform, $"{LocalizationManager.NextLabelPrefix}T", 20, FontStyle.Bold, Color.cyan,
                new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(200, 30));

            p1NextPieceImage = CreateImage("P1_NextImg", p1NextBox.transform,
                new Vector2(0.5f, 0.35f), new Vector2(60, 60), Color.white);

            // ================= HUD Người chơi 2 (Góc dưới bên trái) =================
            GameObject p2Hud = CreatePanel("P2_HUD", inGameHudPanel.transform, new Color(0.2f, 0.1f, 0.08f, 0.75f));
            RectTransform p2Rt = p2Hud.GetComponent<RectTransform>();
            p2Rt.anchorMin = new Vector2(0.02f, 0.02f);
            p2Rt.anchorMax = new Vector2(0.22f, 0.22f);
            p2Rt.offsetMin = Vector2.zero;
            p2Rt.offsetMax = Vector2.zero;

            p2NameText = CreateText("P2_Name", p2Hud.transform, LocalizationManager.P2Name, 24, FontStyle.Bold, new Color(1f, 0.6f, 0.2f),
                new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(250, 35));

            p2RoleText = CreateText("P2_Role", p2Hud.transform, LocalizationManager.P2Role, 18, FontStyle.Italic, Color.white,
                new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(250, 30));

            p2HeartsText = CreateText("P2_Hearts", p2Hud.transform, $"{LocalizationManager.HeartsPrefix}<color=#ff3344>♥♥♥</color>", 22, FontStyle.Bold, Color.white,
                new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(250, 35));

            // Khung hiển thị khối kế tiếp của P2 (Góc dưới bên phải)
            GameObject p2NextBox = CreatePanel("P2_NextBox", inGameHudPanel.transform, new Color(0.2f, 0.1f, 0.08f, 0.75f));
            RectTransform p2NbRt = p2NextBox.GetComponent<RectTransform>();
            p2NbRt.anchorMin = new Vector2(0.78f, 0.02f);
            p2NbRt.anchorMax = new Vector2(0.98f, 0.22f);
            p2NbRt.offsetMin = Vector2.zero;
            p2NbRt.offsetMax = Vector2.zero;

            p2NextLabel = CreateText("P2_NextLabel", p2NextBox.transform, $"{LocalizationManager.NextLabelPrefix}I", 20, FontStyle.Bold, new Color(1f, 0.6f, 0.2f),
                new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(200, 30));

            p2NextPieceImage = CreateImage("P2_NextImg", p2NextBox.transform,
                new Vector2(0.5f, 0.35f), new Vector2(60, 60), Color.white);
        }

        public void BuildBanners(Transform parent)
        {
            countdownPanel = CreatePanel("CountdownPanel", parent, Color.clear);

            countdownText = CreateText("CountdownText", countdownPanel.transform, "", 80, FontStyle.Bold, Color.yellow,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 150));

            bannerText = CreateText("BannerText", countdownPanel.transform, "", 28, FontStyle.Bold, Color.white,
                new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(800, 60));
            bannerText.gameObject.SetActive(false);
        }

        public void BuildPauseModal(Transform parent)
        {
            pausePanel = CreatePanel("PausePanel", parent, new Color(0.04f, 0.06f, 0.1f, 0.94f));

            // Tiêu đề
            pauseTitleText = CreateText("PauseTitle", pausePanel.transform, LocalizationManager.PauseTitle, 54, FontStyle.Bold, Color.yellow,
                new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(600, 70));

            pauseSubtitleText = CreateText("PauseSubtitle", pausePanel.transform, LocalizationManager.PauseSubtitle, 20, FontStyle.Normal, Color.white,
                new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(600, 40));

            // Nút Tiếp tục
            GameObject resumeGo = CreateButton("ResumeBtn", pausePanel.transform, LocalizationManager.ResumeBtn,
                new Vector2(0.5f, 0.44f), new Vector2(380, 65), new Color(0.15f, 0.6f, 0.35f), () =>
                {
                    GameManager.Instance?.ResumeGame();
                });
            resumeBtn = resumeGo.GetComponent<Button>();
            resumeBtnText = resumeGo.GetComponentInChildren<Text>();

            // Nút Quay lại Menu
            GameObject returnMenuGo = CreateButton("PauseReturnMenuBtn", pausePanel.transform, LocalizationManager.PauseReturnMenuBtn,
                new Vector2(0.5f, 0.31f), new Vector2(380, 65), new Color(0.55f, 0.25f, 0.25f), () =>
                {
                    GameManager.Instance?.ReturnToMenu();
                });
            pauseReturnMenuBtn = returnMenuGo.GetComponent<Button>();
            pauseReturnMenuBtnText = returnMenuGo.GetComponentInChildren<Text>();

            pausePanel.SetActive(false);
        }

        public void BuildGameOverModal(Transform parent)
        {
            gameOverPanel = CreatePanel("GameOverPanel", parent, new Color(0.05f, 0.05f, 0.08f, 0.95f));

            CreateText("GameOverTitle", gameOverPanel.transform, "KẾT THÚC TRẬN ĐẤU", 56, FontStyle.Bold, Color.yellow,
                new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(800, 80));

            gameOverWinnerText = CreateText("WinnerLabel", gameOverPanel.transform, LocalizationManager.P1Wins, 44, FontStyle.Bold, Color.cyan,
                new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(800, 70));

            gameOverReasonText = CreateText("ReasonLabel", gameOverPanel.transform, LocalizationManager.DefaultReason, 24, FontStyle.Italic, Color.white,
                new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(800, 50));

            GameObject rematchGo = CreateButton("RematchBtn", gameOverPanel.transform, LocalizationManager.RematchBtn,
                new Vector2(0.5f, 0.40f), new Vector2(360, 60), new Color(0.2f, 0.7f, 0.3f), () =>
                {
                    GameManager.Instance?.RestartMatch();
                });
            rematchBtn = rematchGo.GetComponent<Button>();
            rematchBtnText = rematchGo.GetComponentInChildren<Text>();

            GameObject returnMenuGo = CreateButton("ReturnMenuBtn", gameOverPanel.transform, LocalizationManager.ReturnMenuBtn,
                new Vector2(0.5f, 0.28f), new Vector2(360, 60), new Color(0.4f, 0.4f, 0.5f), () =>
                {
                    GameManager.Instance?.ReturnToMenu();
                });
            returnMenuBtn = returnMenuGo.GetComponent<Button>();
            returnMenuBtnText = returnMenuGo.GetComponentInChildren<Text>();

            gameOverPanel.SetActive(false);
        }

        // ================= API Công Khai =================

        public void ShowInGameHUD()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (inGameHudPanel != null) inGameHudPanel.SetActive(true);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (countdownPanel != null) countdownPanel.SetActive(true);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        public void ShowPauseMenu()
        {
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        public void HidePauseMenu()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        public void SetRoleText(PlayerSide side, string role)
        {
            if (side == PlayerSide.Top && p1RoleText != null) p1RoleText.text = role;
            if (side == PlayerSide.Bottom && p2RoleText != null) p2RoleText.text = role;
        }

        public void UpdateHearts(PlayerSide side, int hearts)
        {
            string heartString = $"{LocalizationManager.HeartsPrefix}<color=#ff3344>";
            for (int i = 0; i < hearts; i++) heartString += "♥";
            heartString += "</color>";

            if (hearts < 3)
            {
                heartString += "<color=#555555>";
                for (int i = hearts; i < 3; i++) heartString += "♥";
                heartString += "</color>";
            }

            if (side == PlayerSide.Top && p1HeartsText != null)
            {
                p1HeartsText.gameObject.SetActive(true);
                p1HeartsText.text = heartString;
            }
            if (side == PlayerSide.Bottom && p2HeartsText != null)
            {
                p2HeartsText.gameObject.SetActive(true);
                p2HeartsText.text = heartString;
            }
        }

        public void UpdateNextPiece(PlayerSide side, TetrominoType type)
        {
            Color color = TetrominoData.GetColor(type);
            if (side == PlayerSide.Top)
            {
                if (p1NextLabel != null) p1NextLabel.text = $"{LocalizationManager.NextLabelPrefix}{type}";
                if (p1NextPieceImage != null) p1NextPieceImage.color = color;
            }
            else
            {
                if (p2NextLabel != null) p2NextLabel.text = $"{LocalizationManager.NextLabelPrefix}{type}";
                if (p2NextPieceImage != null) p2NextPieceImage.color = color;
            }
        }

        public void ShowCountdown(Action onDone)
        {
            StartCoroutine(CountdownRoutine(onDone));
        }

        private IEnumerator CountdownRoutine(Action onDone)
        {
            if (countdownText != null) countdownText.gameObject.SetActive(true);
            for (int i = 3; i > 0; i--)
            {
                if (countdownText != null) countdownText.text = i.ToString();
                SoundManager.Instance?.PlayRotate();
                yield return new WaitForSeconds(0.7f);
            }
            if (countdownText != null) countdownText.text = LocalizationManager.CountdownStart;
            SoundManager.Instance?.PlayHardDrop();
            yield return new WaitForSeconds(0.6f);
            if (countdownText != null)
            {
                countdownText.text = "";
                countdownText.gameObject.SetActive(false);
            }
            onDone?.Invoke();
        }

        public void ShowBanner(string message, float duration = 1.0f)
        {
            if (bannerCoroutine != null) StopCoroutine(bannerCoroutine);
            bannerCoroutine = StartCoroutine(BannerRoutine(message, duration));
        }

        private IEnumerator BannerRoutine(string message, float duration)
        {
            if (bannerText == null) yield break;
            bannerText.text = message;
            bannerText.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            bannerText.gameObject.SetActive(false);
        }

        public void ShowGameOver(PlayerSide winner, string reason = null)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                gameOverWinnerText.text = winner == PlayerSide.Top ? LocalizationManager.P1Wins : LocalizationManager.P2Wins;
                gameOverWinnerText.color = winner == PlayerSide.Top ? Color.cyan : new Color(1f, 0.6f, 0.2f);
                gameOverReasonText.text = string.IsNullOrEmpty(reason) ? LocalizationManager.DefaultReason : reason;
            }
        }

        // ================= Phương thức hỗ trợ tạo UI =================

        private GameObject CreatePanel(string name, Transform parent, Color bgColor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = bgColor;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private Text CreateText(string name, Transform parent, string content, int size, FontStyle style, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 delta, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            EnsureFont();

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text txt = go.AddComponent<Text>();
            txt.font = uiFont;
            txt.text = content;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.supportRichText = true;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = delta;
            return txt;
        }

        private Image CreateImage(string name, Transform parent, Vector2 anchor, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = GridManager.GetDefaultBlockSprite();
            img.color = color;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            return img;
        }

        private GameObject CreateButton(string name, Transform parent, string text, Vector2 anchor, Vector2 size, Color btnColor, Action onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = btnColor;

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = btnColor * 1.25f;
            cb.pressedColor = btnColor * 0.75f;
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick?.Invoke());

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            int btnFontSize = text.Contains("\n") ? 16 : 20;
            CreateText("BtnText", go.transform, text, btnFontSize, FontStyle.Bold, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);

            return go;
        }
    }
}
