using System;
using System.Collections.Generic;
using UnityEngine;

namespace Terris2P.AI
{
    /// <summary>
    /// Mô hình bàn cờ ảo 15x30 siêu nhẹ biểu diễn bằng Bitboard (ushort[30]).
    /// Mô phỏng chính xác 100% các quy tắc vật lý của GridManager:
    /// trọng lực 2 chiều, va chạm, rào chắn trung tâm không thể phá hủy,
    /// và quy tắc đẩy dồn toàn bộ cấu trúc bàn cờ khi phá hàng.
    /// Hoàn toàn không cấp phát rác GC (Zero Memory Allocations).
    /// </summary>
    public struct VirtualBoard
    {
        public const int Width = 15;
        public const int Height = 30;
        public const ushort FullRowMask = 0x7FFF; // 15 bits đầu là 1: 0111 1111 1111 1111 (32767)

        // Mỗi phần tử là bitmask của 15 ô trong hàng (bit 0 đến 14)
        public ushort[] rows;
        public ushort[] indestructible;

        public static VirtualBoard Create()
        {
            VirtualBoard vb = new VirtualBoard
            {
                rows = new ushort[Height],
                indestructible = new ushort[Height]
            };
            return vb;
        }

        public static VirtualBoard CreateWithCenterBarrier()
        {
            VirtualBoard vb = Create();
            vb.SpawnCenterBarrier(2);
            return vb;
        }

        public VirtualBoard Clone()
        {
            VirtualBoard copy = new VirtualBoard
            {
                rows = new ushort[Height],
                indestructible = new ushort[Height]
            };
            Array.Copy(rows, copy.rows, Height);
            Array.Copy(indestructible, copy.indestructible, Height);
            return copy;
        }

        public void CopyFrom(ref VirtualBoard source)
        {
            Array.Copy(source.rows, rows, Height);
            Array.Copy(source.indestructible, indestructible, Height);
        }

        public void CopyFromGridManager(GridManager grid)
        {
            if (rows == null || rows.Length != Height) rows = new ushort[Height];
            if (indestructible == null || indestructible.Length != Height) indestructible = new ushort[Height];

            Array.Clear(rows, 0, Height);
            Array.Clear(indestructible, 0, Height);

            if (grid == null) return;

            for (int y = 0; y < Height; y++)
            {
                ushort rowMask = 0;
                ushort indestMask = 0;
                for (int x = 0; x < Width; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (grid.IsLocked(pos))
                        rowMask |= (ushort)(1 << x);
                    if (grid.IsIndestructible(pos))
                        indestMask |= (ushort)(1 << x);
                }
                rows[y] = rowMask;
                indestructible[y] = indestMask;
            }
        }

        public void SpawnCenterBarrier(int rowCount = 2)
        {
            int startY = (Height / 2) - (rowCount / 2); // hàng 14 và 15
            for (int y = startY; y < startY + rowCount; y++)
            {
                rows[y] = FullRowMask;
                indestructible[y] = FullRowMask;
            }
        }

        public bool IsCellOccupied(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return true;
            return (rows[y] & (1 << x)) != 0;
        }

        public bool IsIndestructible(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
            return (indestructible[y] & (1 << x)) != 0;
        }

        public bool CanPlace(Vector2Int[] localCells, Vector2Int pivot, int rotation)
        {
            for (int i = 0; i < localCells.Length; i++)
            {
                Vector2Int cell = pivot + Rotate(localCells[i], rotation);
                if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height)
                    return false;
                if ((rows[cell.y] & (1 << cell.x)) != 0)
                    return false;
            }
            return true;
        }

        public static Vector2Int Rotate(Vector2Int cell, int quarterTurns)
        {
            Vector2Int result = cell;
            for (int i = 0; i < quarterTurns; i++)
                result = new Vector2Int(-result.y, result.x);
            return result;
        }

        /// <summary>
        /// Tính toán vị trí tiếp đất (Ghost Pivot) của khối rơi dọc theo hướng trọng lực.
        /// </summary>
        public bool TryDropPiece(TetrominoType type, int startX, int startY, int rotation, int gravityDirection, out Vector2Int ghostPivot)
        {
            Vector2Int[] localCells = TetrominoData.GetCells(type);
            Vector2Int current = new Vector2Int(startX, startY);

            // Kiểm tra vị trí xuất phát
            if (!CanPlace(localCells, current, rotation))
            {
                ghostPivot = current;
                return false;
            }

            int safety = Height + 2;
            while (safety-- > 0)
            {
                Vector2Int next = current + new Vector2Int(0, gravityDirection);
                if (!CanPlace(localCells, next, rotation))
                    break;
                current = next;
            }

            ghostPivot = current;
            return true;
        }

        /// <summary>
        /// Khóa các ô của khối vào bàn cờ ảo và thực hiện xóa hàng nếu có.
        /// Trả về số hàng bị xóa.
        /// </summary>
        public int LockPieceAndClearLines(TetrominoType type, Vector2Int pivot, int rotation, PlayerSide side)
        {
            Vector2Int[] localCells = TetrominoData.GetCells(type);
            for (int i = 0; i < localCells.Length; i++)
            {
                Vector2Int cell = pivot + Rotate(localCells[i], rotation);
                if (cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height)
                {
                    rows[cell.y] |= (ushort)(1 << cell.x);
                }
            }

            return ClearFullRows(side);
        }

        /// <summary>
        /// Xóa các hàng đã đầy và dồn các hàng theo đúng luật đẩy về phía đối thủ.
        /// </summary>
        public int ClearFullRows(PlayerSide lockOwner)
        {
            int totalCleared = 0;
            bool cleared;
            do
            {
                cleared = false;
                for (int y = 0; y < Height; y++)
                {
                    // Hàng đầy và không chứa ô không thể phá hủy
                    if ((rows[y] & FullRowMask) == FullRowMask && indestructible[y] == 0)
                    {
                        RemoveRow(y);
                        CollapseTowardOpponent(y, lockOwner);
                        totalCleared++;
                        cleared = true;
                        break;
                    }
                }
            }
            while (cleared);

            return totalCleared;
        }

        private void RemoveRow(int row)
        {
            rows[row] = 0;
            indestructible[row] = 0;
        }

        /// <summary>
        /// Tái hiện chuẩn 100% logic CollapseTowardOpponent của GridManager.
        /// </summary>
        public void CollapseTowardOpponent(int clearedRow, PlayerSide clearedBy)
        {
            if (clearedBy == PlayerSide.Top)
            {
                // P1 xóa hàng: Đẩy dồn xuống phía P2 (HƯỚNG XUỐNG, -Y)
                // Khối ở row 0 bị đẩy vượt mép dưới (bị hủy)
                rows[0] = 0;
                indestructible[0] = 0;

                // Dịch các hàng (1..clearedRow-1) xuống 1 hàng
                for (int y = 1; y < clearedRow; y++)
                {
                    rows[y - 1] = rows[y];
                    indestructible[y - 1] = indestructible[y];
                }
                if (clearedRow > 0)
                {
                    rows[clearedRow - 1] = 0;
                    indestructible[clearedRow - 1] = 0;
                }

                // Dịch các hàng (clearedRow+1..Height-1) xuống 2 hàng lấp vào khoảng trống
                for (int y = clearedRow + 1; y < Height; y++)
                {
                    rows[y - 2] = rows[y];
                    indestructible[y - 2] = indestructible[y];
                }
                rows[Height - 1] = 0;
                indestructible[Height - 1] = 0;
                rows[Height - 2] = 0;
                indestructible[Height - 2] = 0;
            }
            else
            {
                // P2 xóa hàng: Đẩy dồn lên phía P1 (HƯỚNG LÊN, +Y)
                // Khối ở row Height - 1 bị đẩy vượt mép trên (bị hủy)
                rows[Height - 1] = 0;
                indestructible[Height - 1] = 0;

                // Dịch các hàng (Height-2 down to clearedRow+1) lên 1 hàng
                for (int y = Height - 2; y > clearedRow; y--)
                {
                    rows[y + 1] = rows[y];
                    indestructible[y + 1] = indestructible[y];
                }
                if (clearedRow < Height - 1)
                {
                    rows[clearedRow + 1] = 0;
                    indestructible[clearedRow + 1] = 0;
                }

                // Dịch các hàng (clearedRow-1 down to 0) lên 2 hàng lấp vào khoảng trống
                for (int y = clearedRow - 1; y >= 0; y--)
                {
                    rows[y + 2] = rows[y];
                    indestructible[y + 2] = indestructible[y];
                }
                rows[0] = 0;
                indestructible[0] = 0;
                rows[1] = 0;
                indestructible[1] = 0;
            }
        }

        /// <summary>
        /// Tìm phạm vi hàng rào chắn cố định hiện tại.
        /// </summary>
        public void GetBarrierBounds(out int minY, out int maxY)
        {
            minY = -1;
            maxY = -1;
            for (int y = 0; y < Height; y++)
            {
                if ((indestructible[y] & 1) != 0)
                {
                    if (minY == -1) minY = y;
                    maxY = y;
                }
            }
            if (minY == -1)
            {
                minY = (Height / 2) - 1;
                maxY = Height / 2;
            }
        }

        /// <summary>
        /// Tìm tọa độ Y thấp nhất có gạch của P2 (phía dưới rào chắn).
        /// Càng gần 0 nghĩa là càng nguy hiểm!
        /// </summary>
        public int GetP2MinY(int barrierMinY)
        {
            for (int y = 0; y < barrierMinY; y++)
            {
                if (rows[y] != 0)
                    return y;
            }
            return barrierMinY; // Chưa có khối nào, an toàn ở rào chắn
        }

        /// <summary>
        /// Tìm tọa độ Y cao nhất có gạch của P1 (phía trên rào chắn).
        /// Càng gần 29 nghĩa là P1 càng nguy hiểm!
        /// </summary>
        public int GetP1MaxY(int barrierMaxY)
        {
            for (int y = Height - 1; y > barrierMaxY; y--)
            {
                if (rows[y] != 0)
                    return y;
            }
            return barrierMaxY;
        }

        /// <summary>
        /// Đếm số lỗ hổng bên dưới các khối đã cố định của P2.
        /// Với P2, các khối rơi từ y=1 lên, nên một lỗ hổng là ô rỗng (x, y)
        /// mà có khối đã khóa ở dưới nó (y' < y, tức y' gần mép 0 hơn).
        /// </summary>
        public int CountP2Holes(int barrierMinY)
        {
            int holes = 0;
            for (int x = 0; x < Width; x++)
            {
                int mask = 1 << x;
                bool seenBlockBelow = false;
                for (int y = 0; y < barrierMinY; y++)
                {
                    if ((rows[y] & mask) != 0)
                    {
                        seenBlockBelow = true;
                    }
                    else if (seenBlockBelow)
                    {
                        // Ô rỗng nằm trên một ô đã có khối
                        holes++;
                    }
                }
            }
            return holes;
        }

        /// <summary>
        /// Tính độ gồ ghề của bề mặt đón gạch của P2.
        /// Bề mặt này quay về phía hàng 0 (y nhỏ).
        /// Với mỗi cột, surfaceY là hàng có khối nhỏ nhất (hoặc barrierMinY nếu cột trống).
        /// </summary>
        public int CalculateP2Bumpiness(int barrierMinY)
        {
            int prevSurfaceY = -1;
            int totalBumpiness = 0;

            for (int x = 0; x < Width; x++)
            {
                int mask = 1 << x;
                int surfaceY = barrierMinY;
                for (int y = 0; y < barrierMinY; y++)
                {
                    if ((rows[y] & mask) != 0)
                    {
                        surfaceY = y;
                        break;
                    }
                }

                if (prevSurfaceY != -1)
                {
                    totalBumpiness += Math.Abs(surfaceY - prevSurfaceY);
                }
                prevSurfaceY = surfaceY;
            }

            return totalBumpiness;
        }

        public bool HasBlockAtRow(int y)
        {
            if (y < 0 || y >= Height) return false;
            return rows[y] != 0;
        }
    }
}
