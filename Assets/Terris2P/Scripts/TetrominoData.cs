using System.Collections.Generic;
using UnityEngine;

namespace Terris2P
{
    public enum TetrominoType { I, O, T, S, Z, J, L }

    /// <summary>
    /// Định nghĩa hình dạng tọa độ, màu sắc và bảng bù vị trí xoay (Wall-Kick) cho 7 loại khối Tetromino.
    /// </summary>
    public static class TetrominoData
    {
        // Tọa độ cục bộ tại trạng thái xoay ban đầu (trạng thái xuất hiện 0)
        public static Vector2Int[] GetCells(TetrominoType type)
        {
            switch (type)
            {
                case TetrominoType.I:
                    return new[] {
                        new Vector2Int(-1, 0), new Vector2Int(0, 0),
                        new Vector2Int(1, 0), new Vector2Int(2, 0)
                    };
                case TetrominoType.O:
                    return new[] {
                        new Vector2Int(0, 0), new Vector2Int(1, 0),
                        new Vector2Int(0, 1), new Vector2Int(1, 1)
                    };
                case TetrominoType.T:
                    return new[] {
                        new Vector2Int(-1, 0), new Vector2Int(0, 0),
                        new Vector2Int(1, 0), new Vector2Int(0, 1)
                    };
                case TetrominoType.S:
                    return new[] {
                        new Vector2Int(0, 0), new Vector2Int(1, 0),
                        new Vector2Int(-1, 1), new Vector2Int(0, 1)
                    };
                case TetrominoType.Z:
                    return new[] {
                        new Vector2Int(-1, 0), new Vector2Int(0, 0),
                        new Vector2Int(0, 1), new Vector2Int(1, 1)
                    };
                case TetrominoType.J:
                    return new[] {
                        new Vector2Int(-1, 1), new Vector2Int(-1, 0),
                        new Vector2Int(0, 0), new Vector2Int(1, 0)
                    };
                default: // L
                    return new[] {
                        new Vector2Int(1, 1), new Vector2Int(-1, 0),
                        new Vector2Int(0, 0), new Vector2Int(1, 0)
                    };
            }
        }

        public static Color GetColor(TetrominoType type)
        {
            switch (type)
            {
                case TetrominoType.I: return new Color(0.15f, 0.85f, 1.0f); // Xanh lơ sáng (Cyan)
                case TetrominoType.O: return new Color(1.0f, 0.88f, 0.15f); // Vàng tươi
                case TetrominoType.T: return new Color(0.75f, 0.3f, 1.0f);  // Tím
                case TetrominoType.S: return new Color(0.2f, 0.95f, 0.35f); // Xanh lá mạ
                case TetrominoType.Z: return new Color(1.0f, 0.25f, 0.3f);  // Đỏ tươi
                case TetrominoType.J: return new Color(0.25f, 0.45f, 1.0f); // Xanh lam
                default: return new Color(1.0f, 0.55f, 0.15f);              // Cam
            }
        }

        // Bảng offset kiểm tra xoay bám tường (Wall-Kick) chuẩn
        public static readonly Vector2Int[] WallKickTests = new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(2, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1)
        };
    }

    /// <summary>
    /// Bộ sinh khối ngẫu nhiên 7-Bag chuẩn Tetris, đảm bảo phân phối đồng đều cả 7 loại khối trong mỗi chu kỳ.
    /// </summary>
    public class TetrisBag
    {
        private readonly List<TetrominoType> bag = new List<TetrominoType>();

        public TetrominoType Next()
        {
            if (bag.Count == 0)
                Refill();

            TetrominoType piece = bag[bag.Count - 1];
            bag.RemoveAt(bag.Count - 1);
            return piece;
        }

        public TetrominoType Peek()
        {
            if (bag.Count == 0)
                Refill();

            return bag[bag.Count - 1];
        }

        private void Refill()
        {
            bag.Clear();
            for (int i = 0; i < 7; i++)
                bag.Add((TetrominoType)i);

            // Thuật toán xáo trộn Fisher-Yates
            for (int i = bag.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                TetrominoType tmp = bag[i];
                bag[i] = bag[r];
                bag[r] = tmp;
            }
        }
    }
}
