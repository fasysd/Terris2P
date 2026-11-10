using System.Collections;
using UnityEngine;

namespace Terris2P.AI
{
    /// <summary>
    /// Điều khiển thực thể Máy (AI) cho Người chơi 2 (Phía dưới).
    /// Hoạt động như một người chơi bình thường: CHỈ gọi các API điều khiển công khai
    /// của PlayerController (Move, RotatePiece, TriggerHardDrop, SetSoftDrop) theo nhịp APM
    /// tương ứng với cấp độ khó; TUYỆT ĐỐI KHÔNG tác động trực tiếp vào GridManager.
    /// </summary>
    public class TetrisAIController : MonoBehaviour
    {
        [Header("Tham chiếu Đối tượng")]
        public PlayerController playerController;
        public GridManager gridManager;

        [Header("Cài đặt Độ khó")]
        public AIDifficulty difficulty = AIDifficulty.Medium;

        [Header("Cấu hình Suy nghĩ (ScriptableObject)")]
        public AIThinkingConfigSO thinkingConfig;

        public AIThinkingProfile ActiveProfile
        {
            get
            {
                if (thinkingConfig != null)
                    return thinkingConfig.GetProfile(difficulty);
                return AIThinkingConfigSO.GetDefaultInstance().GetProfile(difficulty);
            }
        }

        private bool hasPlannedMove = false;
        private AIMoveDecision currentPlan;
        private float actionTimer = 0f;
        private float thinkDelayTimer = 0f;
        private bool isThinking = false;
        private int stuckCounter = 0;
        private Vector2Int lastPivot;

        private void Awake()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();
        }

        private void Start()
        {
            if (gridManager == null)
                gridManager = GridManager.Instance ?? FindFirstObjectByType<GridManager>();
        }

        public void SetDifficulty(AIDifficulty newDifficulty)
        {
            difficulty = newDifficulty;
        }

        public void ResetPlan()
        {
            hasPlannedMove = false;
            isThinking = false;
            actionTimer = 0f;
            thinkDelayTimer = 0f;
            stuckCounter = 0;
            if (playerController != null)
            {
                playerController.SetSoftDrop(false);
            }
        }

        private float GetActionInterval()
        {
            return ActiveProfile.actionInterval;
        }

        private float GetThinkingDelay()
        {
            return ActiveProfile.thinkingDelay;
        }

        private void Update()
        {
            if (playerController == null || !playerController.isAIControlled) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            // Nếu người chơi chưa có khối đang rơi, reset kế hoạch
            if (!playerController.IsActive)
            {
                ResetPlan();
                return;
            }

            // Giai đoạn 1: Lập kế hoạch nước đi khi sinh khối mới
            if (!hasPlannedMove)
            {
                if (!isThinking)
                {
                    isThinking = true;
                    thinkDelayTimer = GetThinkingDelay();
                }

                thinkDelayTimer -= Time.deltaTime;
                if (thinkDelayTimer <= 0f)
                {
                    ComputeBestMove();
                    hasPlannedMove = true;
                    isThinking = false;
                    actionTimer = 0f;
                    stuckCounter = 0;
                    lastPivot = playerController.Pivot;
                }
                return;
            }

            // Giai đoạn 2: Thực thi nước đi bằng các thao tác người chơi theo nhịp APM
            actionTimer += Time.deltaTime;
            float interval = GetActionInterval();

            if (actionTimer >= interval)
            {
                actionTimer = 0f;
                ExecuteNextAction();
            }
        }

        /// <summary>
        /// Tạo bản sao VirtualBoard từ GridManager và giải thuật Beam Search tìm nước đi tốt nhất.
        /// </summary>
        private void ComputeBestMove()
        {
            if (gridManager == null)
                gridManager = GridManager.Instance ?? FindFirstObjectByType<GridManager>();

            VirtualBoard board = VirtualBoard.Create();
            board.CopyFromGridManager(gridManager);

            TetrominoType p2Current = playerController.CurrentType;
            TetrominoType p2Next = playerController.NextType;

            currentPlan = TetrisAISearch.FindBestMove(board, p2Current, p2Next, ActiveProfile);
        }

        /// <summary>
        /// Thực thi 1 thao tác (xoay, sang trái, sang phải hoặc thả) thông qua API của PlayerController.
        /// </summary>
        private void ExecuteNextAction()
        {
            if (!playerController.IsActive)
            {
                ResetPlan();
                return;
            }

            // Kiểm tra chống kẹt (stuck detection)
            if (playerController.Pivot == lastPivot)
            {
                stuckCounter++;
                if (stuckCounter > ActiveProfile.maxStuckCount)
                {
                    // Đã kẹt quá lâu mà không thể di chuyển tiếp -> thả khối để tiếp tục game
                    playerController.TriggerHardDrop();
                    ResetPlan();
                    return;
                }
            }
            else
            {
                stuckCounter = 0;
                lastPivot = playerController.Pivot;
            }

            // 1. Thực hiện xoay khối đến góc xoay mục tiêu
            int currentRot = playerController.Rotation;
            if (currentRot != currentPlan.targetRotation)
            {
                bool rotated = playerController.RotatePiece();
                if (!rotated)
                {
                    // Nếu xoay bị cản trở, tạm thời thử dịch chuyển ngang để né vật cản
                    int diff = currentPlan.targetX - playerController.Pivot.x;
                    if (diff != 0)
                        playerController.Move(diff > 0 ? 1 : -1);
                }
                return;
            }

            // 2. Thực hiện dịch chuyển ngang tới cột mục tiêu
            int currentX = playerController.Pivot.x;
            if (currentX < currentPlan.targetX)
            {
                playerController.Move(1);
                return;
            }
            else if (currentX > currentPlan.targetX)
            {
                playerController.Move(-1);
                return;
            }

            // 3. Đã vào đúng vị trí và góc xoay mong muốn: Thực hiện thả khối
            if (currentPlan.shouldHardDrop)
            {
                playerController.TriggerHardDrop();
                ResetPlan();
            }
            else
            {
                // Ở chế độ Dễ: Bật rơi nhanh (Soft Drop) thay vì thả tức thì
                playerController.SetSoftDrop(true);
            }
        }
    }
}
