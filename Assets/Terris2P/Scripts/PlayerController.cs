using System;
using UnityEngine;

namespace Terris2P
{
    /// <summary>
    /// Điều khiển khối gạch đang rơi của người chơi, hình bóng mờ (ghost piece),
    /// bộ sinh khối 7-bag, di chuyển, xoay khối (kèm wall-kick), trọng lực và thời gian chờ khóa (lock delay).
    /// Tối ưu bộ nhớ không sinh rác rác GC bằng cách tái sử dụng mảng SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Cài đặt Người chơi")]
        public PlayerSide side = PlayerSide.Top;
        public Color playerColor = Color.cyan;
        public GridManager gridManager;

        public GridManager Grid => gridManager != null ? gridManager : GridManager.Instance;

        [Header("Kiểm soát Sinh khối")]
        [Tooltip("Nếu là false, sẽ tạm ngưng sinh khối mới cho người chơi này. Khối đang rơi nếu có vẫn tiếp tục hoạt động bình thường.")]
        public bool canSpawn = true;

        [Header("Điều khiển bằng AI")]
        [Tooltip("Nếu là true, bỏ qua đầu vào bàn phím và chỉ nhận lệnh qua API công khai.")]
        public bool isAIControlled = false;
        private bool aiSoftDropActive = false;

        public TetrominoType CurrentType => currentType;
        public TetrominoType NextType => nextType;
        public Vector2Int Pivot => pivot;
        public int Rotation => rotation;
        public bool IsActive => active;

        public bool CanSpawn
        {
            get => canSpawn;
            set
            {
                bool wasDisabled = !canSpawn;
                canSpawn = value;
                if (canSpawn && wasDisabled && !active && isRoundActive)
                {
                    SpawnPiece();
                }
            }
        }

        [Header("Tốc độ & Thời gian")]
        [Min(0.05f)] public float normalFallInterval = 0.75f;
        [Min(0.01f)] public float fastFallInterval = 0.04f;
        [Min(0.05f)] public float lockDelayDuration = 0.5f;

        private PlayerInputHandler inputHandler;
        private TetrisBag bag = new TetrisBag();

        private Vector2Int pivot;
        private Vector2Int[] localCells;
        private TetrominoType currentType;
        private TetrominoType nextType;
        private int rotation;

        private float fallTimer;
        private float lockTimer;
        private bool isTouchingSurface;
        private bool active;
        private bool isRoundActive;

        // Hình ảnh trực quan (4 ô cho khối đang rơi, 4 ô cho hình bóng mờ)
        private Transform activeVisualRoot;
        private SpriteRenderer[] activeBlockRenderers;

        private Transform ghostVisualRoot;
        private SpriteRenderer[] ghostBlockRenderers;

        public bool HasActivePiece => active;
        public int GravityDirection => side == PlayerSide.Top ? -1 : 1;

        public event Action<PlayerSide> OnPieceLocked;
        public event Action<PlayerSide> OnToppedOut;
        public event Action<PlayerSide, TetrominoType> OnNextPieceChanged;

        private void Awake()
        {
            inputHandler = GetComponent<PlayerInputHandler>();
            if (inputHandler == null)
                inputHandler = gameObject.AddComponent<PlayerInputHandler>();
            inputHandler.Initialize(side);

            CreateVisualsPool();
        }

        public void SetSide(PlayerSide newSide)
        {
            side = newSide;
            if (inputHandler == null)
                inputHandler = GetComponent<PlayerInputHandler>();
            if (inputHandler != null)
                inputHandler.Initialize(side);
        }

        private void Start()
        {
            if (inputHandler == null)
                inputHandler = GetComponent<PlayerInputHandler>();
            if (inputHandler != null)
                inputHandler.Initialize(side);

            if (Grid != null)
                Grid.RegisterPlayer(this);

            nextType = bag.Next();
            OnNextPieceChanged?.Invoke(side, nextType);
        }

        private void OnDestroy()
        {
            if (Grid != null)
                Grid.UnregisterPlayer(this);
        }

        public void StartRound()
        {
            isRoundActive = true;
            bag = new TetrisBag();
            nextType = bag.Next();
            OnNextPieceChanged?.Invoke(side, nextType);
            SpawnPiece();
        }

        public void StopRound()
        {
            isRoundActive = false;
            active = false;
            SetVisualsActive(false);
        }

        private void CreateVisualsPool()
        {
            // Các ô của khối đang rơi
            Transform existingActive = transform.Find(side + "_ActivePieceVisuals");
            if (existingActive != null)
            {
                activeVisualRoot = existingActive;
                activeBlockRenderers = activeVisualRoot.GetComponentsInChildren<SpriteRenderer>();
            }
            else
            {
                GameObject activeRootGo = new GameObject(side + "_ActivePieceVisuals");
                activeRootGo.transform.SetParent(transform);
                activeVisualRoot = activeRootGo.transform;
                activeBlockRenderers = new SpriteRenderer[4];

                for (int i = 0; i < 4; i++)
                {
                    GameObject b = new GameObject("ActiveBlock_" + i);
                    b.transform.SetParent(activeVisualRoot);
                    SpriteRenderer sr = b.AddComponent<SpriteRenderer>();
                    sr.sprite = GridManager.GetDefaultBlockSprite();
                    sr.sortingOrder = 3;
                    activeBlockRenderers[i] = sr;
                }
            }

            // Các ô của hình chiếu bóng mờ (ghost piece)
            Transform existingGhost = transform.Find(side + "_GhostPieceVisuals");
            if (existingGhost != null)
            {
                ghostVisualRoot = existingGhost;
                ghostBlockRenderers = ghostVisualRoot.GetComponentsInChildren<SpriteRenderer>();
            }
            else
            {
                GameObject ghostRootGo = new GameObject(side + "_GhostPieceVisuals");
                ghostRootGo.transform.SetParent(transform);
                ghostVisualRoot = ghostRootGo.transform;
                ghostBlockRenderers = new SpriteRenderer[4];

                for (int i = 0; i < 4; i++)
                {
                    GameObject b = new GameObject("GhostBlock_" + i);
                    b.transform.SetParent(ghostVisualRoot);
                    SpriteRenderer sr = b.AddComponent<SpriteRenderer>();
                    sr.sprite = GridManager.GetDefaultBlockSprite();
                    sr.sortingOrder = 2;
                    ghostBlockRenderers[i] = sr;
                }
            }

            SetVisualsActive(false);
        }

        private void SetVisualsActive(bool isVisible)
        {
            if (activeVisualRoot != null) activeVisualRoot.gameObject.SetActive(isVisible);
            if (ghostVisualRoot != null) ghostVisualRoot.gameObject.SetActive(isVisible);
        }

        public void SpawnPiece()
        {
            if (Grid == null) return;
            if (!canSpawn) return;

            currentType = nextType;
            nextType = bag.Next();
            OnNextPieceChanged?.Invoke(side, nextType);

            localCells = TetrominoData.GetCells(currentType);
            rotation = 0;

            int centerX = Grid.Width / 2;
            int spawnY = side == PlayerSide.Top
                ? Grid.Height - 2
                : 1;

            pivot = new Vector2Int(centerX, spawnY);
            active = true;
            fallTimer = 0f;
            lockTimer = 0f;
            isTouchingSurface = false;

            if (!Grid.CanPlace(GetWorldCells(), this))
            {
                active = false;
                SetVisualsActive(false);
                OnToppedOut?.Invoke(side);
                return;
            }

            SetVisualsActive(true);
            UpdateVisuals();
        }

        private void Update()
        {
            if (Grid == null) return;

            // Nếu tính năng sinh khối được bật lại khi người chơi chưa có khối rơi, thực hiện sinh ngay
            if (!active && isRoundActive && canSpawn)
            {
                SpawnPiece();
                return;
            }

            if (!active) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (!isAIControlled)
            {
                if (inputHandler == null)
                    inputHandler = GetComponent<PlayerInputHandler>();

                inputHandler.side = side;
                inputHandler.PollInput(Time.deltaTime);

                HandleInput();
            }

            HandleGravity(Time.deltaTime);
        }

        private void HandleInput()
        {
            if (inputHandler.MoveHorizontalDelta != 0)
            {
                TryMove(new Vector2Int(inputHandler.MoveHorizontalDelta, 0));
            }

            if (inputHandler.RotateTriggered)
            {
                TryRotate();
            }

            if (inputHandler.HardDropTriggered)
            {
                HardDrop();
            }
        }

        private void HandleGravity(float deltaTime)
        {
            bool isSoftDrop = isAIControlled ? aiSoftDropActive : (inputHandler != null && inputHandler.SoftDropActive);
            float interval = isSoftDrop ? fastFallInterval : normalFallInterval;
            fallTimer += deltaTime;

            // Kiểm tra xem khối có đang nằm trên bề mặt hay không
            Vector2Int[] testCells = GetWorldCells(pivot + Vector2Int.up * GravityDirection, rotation);
            isTouchingSurface = !Grid.CanPlace(testCells, this);

            if (isTouchingSurface)
            {
                lockTimer += deltaTime;
                if (lockTimer >= lockDelayDuration)
                {
                    LockPiece();
                    return;
                }
            }
            else
            {
                lockTimer = 0f;
            }

            if (fallTimer >= interval)
            {
                fallTimer = 0f;
                if (!isTouchingSurface)
                {
                    pivot += Vector2Int.up * GravityDirection;
                    UpdateVisuals();
                }
            }
        }

        public bool Move(int deltaX)
        {
            if (!active) return false;
            return TryMove(new Vector2Int(deltaX, 0));
        }

        public bool RotatePiece()
        {
            if (!active) return false;
            return TryRotate();
        }

        public void TriggerHardDrop()
        {
            if (!active) return;
            HardDrop();
        }

        public void SetSoftDrop(bool enabled)
        {
            aiSoftDropActive = enabled;
        }

        private bool TryMove(Vector2Int delta)
        {
            Vector2Int old = pivot;
            pivot += delta;

            if (!Grid.CanPlace(GetWorldCells(), this))
            {
                pivot = old;
                return false;
            }
            else
            {
                SoundManager.Instance?.PlayMove();
                lockTimer = 0f; // Đặt lại bộ đếm khóa khi di chuyển thành công
                UpdateVisuals();
                return true;
            }
        }

        private bool TryRotate()
        {
            int oldRotation = rotation;
            int newRotation = (rotation + 1) % 4;

            foreach (Vector2Int kick in TetrominoData.WallKickTests)
            {
                Vector2Int testPivot = pivot + kick;
                Vector2Int[] testCells = GetWorldCells(testPivot, newRotation);
                if (Grid.CanPlace(testCells, this))
                {
                    pivot = testPivot;
                    rotation = newRotation;
                    SoundManager.Instance?.PlayRotate();
                    lockTimer = 0f;
                    UpdateVisuals();
                    return true;
                }
            }
            return false;
        }

        private void HardDrop()
        {
            Vector2Int ghostPivot = CalculateGhostPivot();
            pivot = ghostPivot;
            SoundManager.Instance?.PlayHardDrop();
            LockPiece();
        }

        private void LockPiece()
        {
            if (!active) return;

            Vector2Int[] cells = GetWorldCells();
            active = false;
            SetVisualsActive(false);

            Color pieceColor = TetrominoData.GetColor(currentType);
            Grid.LockCells(cells, pieceColor, side);

            OnPieceLocked?.Invoke(side);
        }

        public Vector2Int CalculateGhostPivot()
        {
            Vector2Int ghost = pivot;
            int safety = Grid.Height + 2;

            while (safety-- > 0)
            {
                Vector2Int next = ghost + Vector2Int.up * GravityDirection;
                if (!Grid.CanPlace(GetWorldCells(next, rotation), this))
                    break;
                ghost = next;
            }

            return ghost;
        }

        public Vector2Int[] GetWorldCells()
        {
            return GetWorldCells(pivot, rotation);
        }

        public Vector2Int[] GetWorldCells(Vector2Int atPivot, int atRotation)
        {
            Vector2Int[] result = new Vector2Int[localCells.Length];
            for (int i = 0; i < localCells.Length; i++)
                result[i] = atPivot + Rotate(localCells[i], atRotation);
            return result;
        }

        public bool Occupies(Vector2Int cell)
        {
            if (!active) return false;
            foreach (Vector2Int occupied in GetWorldCells())
                if (occupied == cell) return true;
            return false;
        }

        private static Vector2Int Rotate(Vector2Int cell, int quarterTurns)
        {
            Vector2Int result = cell;
            for (int i = 0; i < quarterTurns; i++)
                result = new Vector2Int(-result.y, result.x);
            return result;
        }

        private void UpdateVisuals()
        {
            if (!active || Grid == null || activeBlockRenderers == null || ghostBlockRenderers == null) return;

            Color baseColor = TetrominoData.GetColor(currentType);
            Vector2 cellSize = Grid.cellSize;

            // Cập nhật các khối hiển thị của khối đang rơi
            Vector2Int[] currentCells = GetWorldCells();
            for (int i = 0; i < currentCells.Length && i < 4; i++)
            {
                activeBlockRenderers[i].transform.position = Grid.CellToWorld(currentCells[i]);
                activeBlockRenderers[i].transform.localScale = new Vector3(cellSize.x * 0.94f, cellSize.y * 0.94f, 1f);
                activeBlockRenderers[i].color = baseColor;
            }

            // Cập nhật các khối hiển thị của bóng mờ (ghost piece)
            Vector2Int ghostPivot = CalculateGhostPivot();
            Vector2Int[] ghostCells = GetWorldCells(ghostPivot, rotation);
            Color ghostColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);

            for (int i = 0; i < ghostCells.Length && i < 4; i++)
            {
                ghostBlockRenderers[i].transform.position = Grid.CellToWorld(ghostCells[i]);
                ghostBlockRenderers[i].transform.localScale = new Vector3(cellSize.x * 0.94f, cellSize.y * 0.94f, 1f);
                ghostBlockRenderers[i].color = ghostColor;
            }
        }
    }
}
