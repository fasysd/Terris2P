using System;
using System.Collections.Generic;
using UnityEngine;

namespace Terris2P
{
    public enum PlayerSide { Top, Bottom }

    /// <summary>
    /// Quản lý bàn cờ chung 15x30.
    /// Xử lý các ô đã cố định, khóa khối, kiểm tra và xóa hàng,
    /// dồn khối khi phá hàng, tạo rào chắn trung tâm và tái cân bằng bàn cờ khi mất máu.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Kích thước Bàn cờ")]
        [Min(4)] public int width = 15;
        [Min(8)] public int height = 30;
        public Vector2 cellSize = Vector2.one;
        public Vector2 boardOrigin = new Vector2(-7.5f, -15f);

        [Header("Hình ảnh & Màu sắc")]
        public Transform cellRoot;
        public Color barrierColor = new Color(0.28f, 0.33f, 0.45f);

        private Transform[,] lockedCells;
        private bool[,] isIndestructible;
        private readonly List<PlayerController> players = new List<PlayerController>();

        // Sự kiện kích hoạt khi có hàng được phá hủy (số hàng, người chơi phá hàng, danh sách chỉ số hàng)
        public event Action<int, PlayerSide, List<int>> OnLinesCleared;

        public int Width => width;
        public int Height => height;

        private void Awake()
        {
            Instance = this;

            InitializeGrid(width, height);

            CreateBoardFrame();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void InitializeGrid(int w, int h)
        {
            width = w;
            height = h;
            lockedCells = new Transform[width, height];
            isIndestructible = new bool[width, height];
            if (cellRoot == null)
            {
                Transform existing = transform.Find("LockedCellsRoot");
                if (existing != null)
                {
                    cellRoot = existing;
                }
                else
                {
                    GameObject rootGo = new GameObject("LockedCellsRoot");
                    rootGo.transform.SetParent(transform);
                    cellRoot = rootGo.transform;
                }
            }
        }

        public bool IsIndestructible(Vector2Int cell)
        {
            if (!IsInside(cell) || isIndestructible == null) return false;
            return isIndestructible[cell.x, cell.y];
        }

        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < width &&
                   cell.y >= 0 && cell.y < height;
        }

        public bool IsLocked(Vector2Int cell)
        {
            if (lockedCells == null) return false;
            return !IsInside(cell) || lockedCells[cell.x, cell.y] != null;
        }

        public bool IsOccupied(Vector2Int cell, PlayerController requester)
        {
            if (!IsInside(cell) || lockedCells[cell.x, cell.y] != null)
                return true;

            foreach (PlayerController player in players)
            {
                if (player == null || player == requester || !player.HasActivePiece)
                    continue;

                if (player.Occupies(cell))
                    return true;
            }

            return false;
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return (Vector3)boardOrigin + new Vector3(
                (cell.x + 0.5f) * cellSize.x,
                (cell.y + 0.5f) * cellSize.y, 0f);
        }

        public void RegisterPlayer(PlayerController player)
        {
            if (player != null && !players.Contains(player))
                players.Add(player);
        }

        public void UnregisterPlayer(PlayerController player)
        {
            players.Remove(player);
        }

        public bool CanPlace(Vector2Int[] cells, PlayerController requester)
        {
            foreach (Vector2Int cell in cells)
                if (IsOccupied(cell, requester))
                    return false;

            return true;
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        public void LockCells(Vector2Int[] cells, Color color, PlayerSide side)
        {
            foreach (Vector2Int cell in cells)
            {
                if (!IsInside(cell)) continue;

                if (lockedCells[cell.x, cell.y] != null)
                {
                    SafeDestroy(lockedCells[cell.x, cell.y].gameObject);
                }

                GameObject block = CreateCellVisual("LockedCell", cell, color);
                block.transform.SetParent(cellRoot, true);
                lockedCells[cell.x, cell.y] = block.transform;
            }

            ClearFullRows(side);
        }

        private GameObject CreateCellVisual(string objectName, Vector2Int cell, Color color)
        {
            GameObject go = new GameObject(objectName);
            go.transform.position = CellToWorld(cell);
            go.transform.localScale = new Vector3(cellSize.x * 0.94f, cellSize.y * 0.94f, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultBlockSprite();
            sr.color = color;
            sr.sortingOrder = 1;
            return go;
        }

        private static Sprite defaultSprite;
        public static Sprite GetDefaultBlockSprite()
        {
            if (defaultSprite != null) return defaultSprite;
            Texture2D tex = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool isBorder = x == 0 || x == 31 || y == 0 || y == 31;
                    tex.SetPixel(x, y, isBorder ? new Color(0.15f, 0.15f, 0.2f, 0.8f) : Color.white);
                }
            }
            tex.Apply();
            defaultSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            return defaultSprite;
        }

        private void ClearFullRows(PlayerSide lockOwner)
        {
            int totalCleared = 0;
            List<int> clearedRows = new List<int>();

            bool cleared;
            do
            {
                cleared = false;

                for (int y = 0; y < height; y++)
                {
                    bool full = true;
                    bool hasIndestructible = false;

                    for (int x = 0; x < width; x++)
                    {
                        if (lockedCells[x, y] == null)
                        {
                            full = false;
                            break;
                        }
                        if (isIndestructible != null && isIndestructible[x, y])
                        {
                            hasIndestructible = true;
                        }
                    }

                    // Hàng rào chắn cố định không thể bị phá hủy!
                    if (!full || hasIndestructible) continue;

                    RemoveRow(y);
                    clearedRows.Add(y);
                    CollapseTowardOpponent(y, lockOwner);
                    totalCleared++;
                    cleared = true;
                    break;
                }
            }
            while (cleared);

            if (totalCleared > 0)
            {
                SoundManager.Instance?.PlayLineClear(totalCleared);
                OnLinesCleared?.Invoke(totalCleared, lockOwner, clearedRows);
            }
        }

        private void RemoveRow(int row)
        {
            for (int x = 0; x < width; x++)
            {
                if (lockedCells[x, row] != null)
                    SafeDestroy(lockedCells[x, row].gameObject);

                lockedCells[x, row] = null;
                if (isIndestructible != null)
                    isIndestructible[x, row] = false;
            }
        }

        private void CollapseTowardOpponent(int clearedRow, PlayerSide clearedBy)
        {
            if (clearedBy == PlayerSide.Top)
            {
                // P1 (Phía trên) xóa hàng: Đẩy dồn mọi khối xuống phía P2 (HƯỚNG XUỐNG, -Y)!
                for (int x = 0; x < width; x++)
                {
                    // Hủy bất kỳ khối nào bị đẩy vượt qua mép dưới (hàng 0 -> -1)
                    if (lockedCells[x, 0] != null)
                    {
                        SafeDestroy(lockedCells[x, 0].gameObject);
                        lockedCells[x, 0] = null;
                        if (isIndestructible != null) isIndestructible[x, 0] = false;
                    }

                    // Dịch chuyển các hàng bên dưới clearedRow (1 .. clearedRow - 1) xuống 1 hàng
                    for (int y = 1; y < clearedRow; y++)
                    {
                        lockedCells[x, y - 1] = lockedCells[x, y];
                        if (isIndestructible != null) isIndestructible[x, y - 1] = isIndestructible[x, y];
                        if (lockedCells[x, y - 1] != null)
                            lockedCells[x, y - 1].position = CellToWorld(new Vector2Int(x, y - 1));
                    }
                    lockedCells[x, clearedRow - 1] = null;
                    if (isIndestructible != null) isIndestructible[x, clearedRow - 1] = false;

                    // Dịch chuyển các hàng bên trên clearedRow (clearedRow + 1 .. height - 1) xuống 2 hàng lấp vào khoảng trống
                    for (int y = clearedRow + 1; y < height; y++)
                    {
                        lockedCells[x, y - 2] = lockedCells[x, y];
                        if (isIndestructible != null) isIndestructible[x, y - 2] = isIndestructible[x, y];
                        if (lockedCells[x, y - 2] != null)
                            lockedCells[x, y - 2].position = CellToWorld(new Vector2Int(x, y - 2));
                    }
                    lockedCells[x, height - 1] = null;
                    if (isIndestructible != null) isIndestructible[x, height - 1] = false;
                    lockedCells[x, height - 2] = null;
                    if (isIndestructible != null) isIndestructible[x, height - 2] = false;
                }
            }
            else
            {
                // P2 (Phía dưới) xóa hàng: Đẩy dồn mọi khối lên phía P1 (HƯỚNG LÊN, +Y)!
                for (int x = 0; x < width; x++)
                {
                    // Hủy bất kỳ khối nào bị đẩy vượt qua mép trên (hàng height - 1 -> height)
                    if (lockedCells[x, height - 1] != null)
                    {
                        SafeDestroy(lockedCells[x, height - 1].gameObject);
                        lockedCells[x, height - 1] = null;
                        if (isIndestructible != null) isIndestructible[x, height - 1] = false;
                    }

                    // Dịch chuyển các hàng bên trên clearedRow (clearedRow + 1 .. height - 2) lên 1 hàng
                    for (int y = height - 2; y > clearedRow; y--)
                    {
                        lockedCells[x, y + 1] = lockedCells[x, y];
                        if (isIndestructible != null) isIndestructible[x, y + 1] = isIndestructible[x, y];
                        if (lockedCells[x, y + 1] != null)
                            lockedCells[x, y + 1].position = CellToWorld(new Vector2Int(x, y + 1));
                    }
                    lockedCells[x, clearedRow + 1] = null;
                    if (isIndestructible != null) isIndestructible[x, clearedRow + 1] = false;

                    // Dịch chuyển các hàng bên dưới clearedRow (clearedRow - 1 xuống 0) lên 2 hàng lấp vào khoảng trống
                    for (int y = clearedRow - 1; y >= 0; y--)
                    {
                        lockedCells[x, y + 2] = lockedCells[x, y];
                        if (isIndestructible != null) isIndestructible[x, y + 2] = isIndestructible[x, y];
                        if (lockedCells[x, y + 2] != null)
                            lockedCells[x, y + 2].position = CellToWorld(new Vector2Int(x, y + 2));
                    }
                    lockedCells[x, 0] = null;
                    if (isIndestructible != null) isIndestructible[x, 0] = false;
                    lockedCells[x, 1] = null;
                    if (isIndestructible != null) isIndestructible[x, 1] = false;
                }
            }
        }

        /// <summary>
        /// Tạo các hàng rào chắn cố định không thể phá hủy ở giữa bàn cờ (hàng 14-15 trên bảng 15x30).
        /// Các hàng này luôn được lấp đầy hoàn toàn, không thể bị hủy, và dịch chuyển khi phá hàng
        /// nhằm ngăn người chơi đặt khối lấn sang phần sân của đối thủ.
        /// </summary>
        public void SpawnCenterBarrier(int rowCount = 2, Color? color = null)
        {
            int startY = (height / 2) - (rowCount / 2);
            Color fill = color ?? barrierColor;

            for (int y = startY; y < startY + rowCount; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (lockedCells[x, y] != null)
                        SafeDestroy(lockedCells[x, y].gameObject);

                    GameObject block = CreateCellVisual("BarrierCell", new Vector2Int(x, y), fill);
                    block.transform.SetParent(cellRoot, true);
                    lockedCells[x, y] = block.transform;
                    if (isIndestructible != null)
                        isIndestructible[x, y] = true;
                }
            }
        }

        /// <summary>
        /// Quy tắc xử lý khi một bên bị trừ máu (khối chạm vào mép biên 0 hoặc 29):
        /// - 2 hàng rào chắn cố định trở về chính giữa bàn cờ (hàng 14-15).
        /// - Bên bị trừ máu bị xóa hoàn toàn 100% số khối.
        /// - Bên sống sót bị xóa một nửa số gạch ở phía giáp rào chắn.
        /// - Số gạch còn lại của bên sống sót được đẩy dồn sát vào rào chắn cố định.
        /// </summary>
        public void ExecuteHeartLossReset(PlayerSide loserSide)
        {
            // Xác định các hàng rào chắn hiện tại
            int oldBarrierMinY = -1;
            int oldBarrierMaxY = -1;
            for (int y = 0; y < height; y++)
            {
                if (isIndestructible != null && isIndestructible[0, y])
                {
                    if (oldBarrierMinY == -1) oldBarrierMinY = y;
                    oldBarrierMaxY = y;
                }
            }
            if (oldBarrierMinY == -1)
            {
                oldBarrierMinY = (height / 2) - 1;
                oldBarrierMaxY = height / 2;
            }

            PlayerSide winnerSide = loserSide == PlayerSide.Top ? PlayerSide.Bottom : PlayerSide.Top;

            // Thu thập các hàng gạch sống sót cần giữ lại: từng hàng một
            List<Color[]> keptRows = new List<Color[]>();

            if (winnerSide == PlayerSide.Bottom)
            {
                // Bên sống sót là P2 (Phía dưới), khối nằm ở y < oldBarrierMinY.
                // Tìm tất cả các hàng có gạch của P2 theo thứ tự tăng dần (0 đến oldBarrierMinY - 1)
                List<int> occupiedRowIndices = new List<int>();
                for (int y = 0; y < oldBarrierMinY; y++)
                {
                    bool hasBlock = false;
                    for (int x = 0; x < width; x++)
                    {
                        if (lockedCells[x, y] != null && !isIndestructible[x, y])
                        {
                            hasBlock = true;
                            break;
                        }
                    }
                    if (hasBlock) occupiedRowIndices.Add(y);
                }

                int totalRows = occupiedRowIndices.Count;
                int deleteCount = Mathf.CeilToInt(totalRows / 2f);
                int keepCount = totalRows - deleteCount;

                // Các hàng giữ lại là những hàng xa rào chắn nhất (keepCount hàng đầu tiên)
                for (int i = 0; i < keepCount; i++)
                {
                    int rowY = occupiedRowIndices[i];
                    Color[] rowColors = new Color[width];
                    for (int x = 0; x < width; x++)
                    {
                        if (lockedCells[x, rowY] != null)
                        {
                            SpriteRenderer sr = lockedCells[x, rowY].GetComponent<SpriteRenderer>();
                            rowColors[x] = sr != null ? sr.color : Color.white;
                        }
                        else
                        {
                            rowColors[x] = Color.clear;
                        }
                    }
                    keptRows.Add(rowColors);
                }
            }
            else
            {
                // Bên sống sót là P1 (Phía trên), khối nằm ở y > oldBarrierMaxY.
                // Tìm tất cả các hàng có gạch của P1 theo thứ tự tăng dần (oldBarrierMaxY + 1 đến height - 1)
                List<int> occupiedRowIndices = new List<int>();
                for (int y = oldBarrierMaxY + 1; y < height; y++)
                {
                    bool hasBlock = false;
                    for (int x = 0; x < width; x++)
                    {
                        if (lockedCells[x, y] != null && !isIndestructible[x, y])
                        {
                            hasBlock = true;
                            break;
                        }
                    }
                    if (hasBlock) occupiedRowIndices.Add(y);
                }

                int totalRows = occupiedRowIndices.Count;
                int deleteCount = Mathf.CeilToInt(totalRows / 2f);
                int keepCount = totalRows - deleteCount;

                // Các hàng giữ lại là những hàng xa rào chắn nhất (keepCount hàng cuối cùng)
                for (int i = deleteCount; i < totalRows; i++)
                {
                    int rowY = occupiedRowIndices[i];
                    Color[] rowColors = new Color[width];
                    for (int x = 0; x < width; x++)
                    {
                        if (lockedCells[x, rowY] != null)
                        {
                            SpriteRenderer sr = lockedCells[x, rowY].GetComponent<SpriteRenderer>();
                            rowColors[x] = sr != null ? sr.color : Color.white;
                        }
                        else
                        {
                            rowColors[x] = Color.clear;
                        }
                    }
                    keptRows.Add(rowColors);
                }
            }

            // Xóa sạch toàn bộ bàn cờ
            ClearBoard();

            // Tái tạo lại 2 hàng rào chắn cố định ở chính giữa (hàng 14-15 trên bảng 15x30)
            int targetBarrierMinY = (height / 2) - 1;
            int targetBarrierMaxY = height / 2;
            SpawnCenterBarrier(2);

            // Đặt các hàng sống sót sát ngay cạnh hàng rào chắn cố định
            if (winnerSide == PlayerSide.Bottom)
            {
                // Các khối của P2 được đặt bắt đầu từ hàng 13 trở xuống
                for (int i = 0; i < keptRows.Count; i++)
                {
                    int targetY = targetBarrierMinY - 1 - (keptRows.Count - 1 - i);
                    if (targetY < 0) continue;

                    Color[] rowColors = keptRows[i];
                    for (int x = 0; x < width; x++)
                    {
                        if (rowColors[x] != Color.clear)
                        {
                            GameObject block = CreateCellVisual("LockedCell", new Vector2Int(x, targetY), rowColors[x]);
                            block.transform.SetParent(cellRoot, true);
                            lockedCells[x, targetY] = block.transform;
                        }
                    }
                }
            }
            else
            {
                // Các khối của P1 được đặt bắt đầu từ hàng 16 trở lên
                for (int i = 0; i < keptRows.Count; i++)
                {
                    int targetY = targetBarrierMaxY + 1 + i;
                    if (targetY >= height) continue;

                    Color[] rowColors = keptRows[i];
                    for (int x = 0; x < width; x++)
                    {
                        if (rowColors[x] != Color.clear)
                        {
                            GameObject block = CreateCellVisual("LockedCell", new Vector2Int(x, targetY), rowColors[x]);
                            block.transform.SetParent(cellRoot, true);
                            lockedCells[x, targetY] = block.transform;
                        }
                    }
                }
            }
        }

        public void ClearBoard()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (lockedCells[x, y] != null)
                        SafeDestroy(lockedCells[x, y].gameObject);
                    lockedCells[x, y] = null;
                    if (isIndestructible != null)
                        isIndestructible[x, y] = false;
                }
            }
        }

        public bool HasBlockAtRow(int row)
        {
            if (row < 0 || row >= height) return false;
            for (int x = 0; x < width; x++)
                if (lockedCells[x, row] != null) return true;
            return false;
        }

        public void CreateBoardFrame()
        {
            if (transform.Find("BoardFrame") != null) return;

            GameObject frameGo = new GameObject("BoardFrame");
            frameGo.transform.SetParent(transform);

            LineRenderer lr = frameGo.AddComponent<LineRenderer>();
            lr.positionCount = 5;
            lr.loop = true;
            lr.useWorldSpace = true;
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.3f, 0.45f, 0.7f, 0.9f);
            lr.endColor = new Color(0.3f, 0.45f, 0.7f, 0.9f);

            Vector3 bl = (Vector3)boardOrigin;
            Vector3 br = (Vector3)boardOrigin + new Vector3(width * cellSize.x, 0, 0);
            Vector3 tr = (Vector3)boardOrigin + new Vector3(width * cellSize.x, height * cellSize.y, 0);
            Vector3 tl = (Vector3)boardOrigin + new Vector3(0, height * cellSize.y, 0);

            lr.SetPosition(0, bl);
            lr.SetPosition(1, br);
            lr.SetPosition(2, tr);
            lr.SetPosition(3, tl);
            lr.SetPosition(4, bl);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.25f, 0.25f, 0.35f, 0.8f);
            for (int x = 0; x <= width; x++)
            {
                Vector3 a = (Vector3)boardOrigin + new Vector3(x * cellSize.x, 0);
                Vector3 b = a + new Vector3(0, height * cellSize.y);
                Gizmos.DrawLine(a, b);
            }
            for (int y = 0; y <= height; y++)
            {
                Vector3 a = (Vector3)boardOrigin + new Vector3(0, y * cellSize.y);
                Vector3 b = a + new Vector3(width * cellSize.x, 0);
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
