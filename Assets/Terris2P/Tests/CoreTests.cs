using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Terris2P.Tests
{
    /// <summary>
    /// Bộ kiểm thử tự động (Unit Tests) cho các tính năng cốt lõi của Terris 2P.
    /// </summary>
    public class CoreTests
    {
        [Test]
        public void TetrisBag_GeneratesAllSevenPiecesPerCycle()
        {
            TetrisBag bag = new TetrisBag();
            HashSet<TetrominoType> types = new HashSet<TetrominoType>();

            for (int i = 0; i < 7; i++)
            {
                types.Add(bag.Next());
            }

            Assert.AreEqual(7, types.Count, "7-Bag phải sinh đủ cả 7 loại khối Tetromino khác nhau trong mỗi chu kỳ.");
        }

        [Test]
        public void GridManager_CorrectlyValidatesBoundsOn15x30()
        {
            GameObject go = new GameObject("TestGrid");
            GridManager grid = go.AddComponent<GridManager>();
            grid.InitializeGrid(15, 30);

            Assert.IsTrue(grid.IsInside(new Vector2Int(0, 0)));
            Assert.IsTrue(grid.IsInside(new Vector2Int(14, 29)));
            Assert.IsFalse(grid.IsInside(new Vector2Int(-1, 0)));
            Assert.IsFalse(grid.IsInside(new Vector2Int(15, 10)));
            Assert.IsFalse(grid.IsInside(new Vector2Int(5, 30)));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TetrominoData_HasValidCellCoordinates()
        {
            for (int i = 0; i < 7; i++)
            {
                TetrominoType type = (TetrominoType)i;
                Vector2Int[] cells = TetrominoData.GetCells(type);
                Assert.AreEqual(4, cells.Length, $"{type} phải có chính xác 4 ô tọa độ.");
            }
        }

        [Test]
        public void LocalizationManager_ToggleLanguage_SwitchesBetweenEnglishAndVietnamese()
        {
            // Đặt về mặc định tiếng Anh
            LocalizationManager.CurrentLanguage = GameLanguage.English;
            Assert.AreEqual(GameLanguage.English, LocalizationManager.CurrentLanguage);
            Assert.AreEqual("▶  PLAY GAME", LocalizationManager.StartGameBtn);
            Assert.AreEqual("PAUSED", LocalizationManager.PauseTitle);

            // Chuyển sang tiếng Việt
            LocalizationManager.ToggleLanguage();
            Assert.AreEqual(GameLanguage.Vietnamese, LocalizationManager.CurrentLanguage);
            Assert.AreEqual("▶  BẮT ĐẦU CHƠI", LocalizationManager.StartGameBtn);
            Assert.AreEqual("TẠM DỪNG", LocalizationManager.PauseTitle);

            // Chuyển lại về tiếng Anh
            LocalizationManager.ToggleLanguage();
            Assert.AreEqual(GameLanguage.English, LocalizationManager.CurrentLanguage);
            Assert.AreEqual("▶  PLAY GAME", LocalizationManager.StartGameBtn);
        }

        [Test]
        public void GameManager_CheckEdgeLoss_DecrementsHeartWhenRowHit()
        {
            GameObject gmGo = new GameObject("Test_GM");
            GameManager gm = gmGo.AddComponent<GameManager>();
            gm.autoStart = false;

            GameObject gridGo = new GameObject("Test_Grid");
            GridManager grid = gridGo.AddComponent<GridManager>();
            grid.InitializeGrid(15, 30);
            gm.gridManager = grid;

            // Bắt đầu game
            gm.StartGame();
            Assert.AreEqual(3, gm.P1Hearts);
            Assert.AreEqual(3, gm.P2Hearts);

            // Đặt 1 khối ở mép dưới cùng (row 0 - P2 biên)
            grid.LockCells(new[] { new Vector2Int(5, 0) }, Color.yellow, PlayerSide.Bottom);

            // Kiểm tra mất máu
            bool hit = gm.CheckEdgeLoss();
            Assert.IsTrue(hit, "CheckEdgeLoss phải phát hiện khối ở row 0.");
            Assert.AreEqual(2, gm.P2Hearts, "P2 phải bị mất 1 tim khi chạm mép dưới.");
            Assert.AreEqual(3, gm.P1Hearts, "P1 phải giữ nguyên 3 tim.");

            Object.DestroyImmediate(gridGo);
            Object.DestroyImmediate(gmGo);
        }

        [Test]
        public void Players_HaveIndependentInputHandlers()
        {
            GameObject p1Go = new GameObject("Test_P1");
            PlayerController p1 = p1Go.AddComponent<PlayerController>();
            p1.SetSide(PlayerSide.Top);

            GameObject p2Go = new GameObject("Test_P2");
            PlayerController p2 = p2Go.AddComponent<PlayerController>();
            p2.SetSide(PlayerSide.Bottom);

            PlayerInputHandler p1Input = p1.GetComponent<PlayerInputHandler>();
            PlayerInputHandler p2Input = p2.GetComponent<PlayerInputHandler>();

            Assert.AreEqual(PlayerSide.Top, p1.side, "Phe của P1 phải là Top.");
            Assert.AreEqual(PlayerSide.Top, p1Input.side, "Phe của bộ xử lý input P1 phải là Top.");

            Assert.AreEqual(PlayerSide.Bottom, p2.side, "Phe của P2 phải là Bottom.");
            Assert.AreEqual(PlayerSide.Bottom, p2Input.side, "Phe của bộ xử lý input P2 phải là Bottom.");

            Assert.AreNotEqual(p1Input.side, p2Input.side, "Bộ xử lý input của P1 và P2 phải thuộc hai phe khác nhau.");

            Object.DestroyImmediate(p1Go);
            Object.DestroyImmediate(p2Go);
        }

        [Test]
        public void GridManager_CenterBarrier_IsPermanentAndIndestructible()
        {
            GameObject go = new GameObject("TestGrid_Barrier");
            GridManager grid = go.AddComponent<GridManager>();
            grid.InitializeGrid(15, 30);

            grid.SpawnCenterBarrier(2);

            // Các hàng trung tâm trên bảng 15x30 là hàng 14 và hàng 15
            for (int y = 14; y <= 15; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    Assert.IsTrue(grid.IsLocked(new Vector2Int(x, y)), $"Ô rào chắn ({x}, {y}) phải bị khóa.");
                    Assert.IsTrue(grid.IsIndestructible(new Vector2Int(x, y)), $"Ô rào chắn ({x}, {y}) phải không thể phá hủy.");
                }
            }

            // Khóa khối thông thường ở vị trí khác để kích hoạt hàm ClearFullRows
            grid.LockCells(new[] { new Vector2Int(0, 20) }, Color.red, PlayerSide.Top);

            // Kiểm tra các hàng rào chắn vẫn nguyên vẹn
            for (int y = 14; y <= 15; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    Assert.IsTrue(grid.IsLocked(new Vector2Int(x, y)), $"Ô rào chắn ({x}, {y}) không được bị xóa khi kiểm tra xóa hàng.");
                }
            }

            Object.DestroyImmediate(go);
        }

        [Test]
        public void GridManager_CenterBarrier_PushesTowardOpponentWhenRowCleared()
        {
            GameObject go = new GameObject("TestGrid_BarrierPush");
            GridManager grid = go.AddComponent<GridManager>();
            grid.InitializeGrid(15, 30);

            grid.SpawnCenterBarrier(2); // Hàng 14 và 15

            // P1 (Phía trên) xóa hàng 20:
            // Mọi khối phải bị đẩy dồn XUỐNG phía P2 để thu hẹp sân của P2!
            Vector2Int[] row20 = new Vector2Int[grid.width];
            for (int x = 0; x < grid.width; x++) row20[x] = new Vector2Int(x, 20);
            grid.LockCells(row20, Color.cyan, PlayerSide.Top);

            // Các hàng rào chắn (14 và 15) phải dịch chuyển XUỐNG 1 hàng thành hàng 13 và 14!
            for (int y = 13; y <= 14; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    Assert.IsTrue(grid.IsLocked(new Vector2Int(x, y)), $"Ô rào chắn phải dịch chuyển xuống ({x}, {y}).");
                    Assert.IsTrue(grid.IsIndestructible(new Vector2Int(x, y)), $"Ô rào chắn đã dịch chuyển ({x}, {y}) phải giữ thuộc tính không thể phá hủy.");
                }
            }

            // Hàng 15 cũ nay phải trở thành rỗng (đã dời xuống hàng 14)
            Assert.IsFalse(grid.IsLocked(new Vector2Int(0, 15)), "Hàng rào chắn cũ 15 phải trống.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void GridManager_HeartLossReset_WipesLoserAndKeepsHalfSurvivorAdjacentToBarrier()
        {
            GameObject go = new GameObject("TestGrid_HeartReset");
            GridManager grid = go.AddComponent<GridManager>();
            grid.InitializeGrid(15, 30);

            grid.SpawnCenterBarrier(2); // Hàng 14 và 15

            // P1 có 4 hàng gạch (chừa lại 1 ô x=14 để không bị xóa hàng ngay lúc khóa): hàng 16, 17, 18, 19
            for (int y = 16; y <= 19; y++)
            {
                for (int x = 0; x < grid.width - 1; x++)
                {
                    grid.LockCells(new[] { new Vector2Int(x, y) }, Color.cyan, PlayerSide.Top);
                }
            }

            // P2 có 2 hàng gạch (chừa ô x=14): hàng 12, 13
            for (int y = 12; y <= 13; y++)
            {
                for (int x = 0; x < grid.width - 1; x++)
                {
                    grid.LockCells(new[] { new Vector2Int(x, y) }, Color.yellow, PlayerSide.Bottom);
                }
            }

            // Giả lập P2 bị mất máu (khối chạm đáy biên dưới)
            grid.ExecuteHeartLossReset(PlayerSide.Bottom);

            // 1. Rào chắn phải được đưa về vị trí chính giữa (hàng 14 và 15)
            for (int y = 14; y <= 15; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    Assert.IsTrue(grid.IsIndestructible(new Vector2Int(x, y)), $"Rào chắn trung tâm tại ({x}, {y}) phải còn nguyên.");
                }
            }

            // 2. Phần sân của bên thua (P2) (hàng 0 đến 13) phải bị xóa sạch hoàn toàn (0 khối)!
            for (int y = 0; y <= 13; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    Assert.IsFalse(grid.IsLocked(new Vector2Int(x, y)), $"Ô ({x}, {y}) của P2 phải bị xóa sạch.");
                }
            }

            // 3. Bên thắng (P1) có 4 hàng (16, 17, 18, 19).
            // Nửa số hàng giáp rào chắn (2 hàng: 16 và 17) bị xóa!
            // 2 hàng còn lại (18 và 19) được đẩy dồn sát vào rào chắn cố định (nay ở hàng 16 và 17)!
            for (int y = 16; y <= 17; y++)
            {
                for (int x = 0; x < grid.width - 1; x++)
                {
                    Assert.IsTrue(grid.IsLocked(new Vector2Int(x, y)), $"Hàng sống sót phải được dồn về hàng {y}.");
                }
            }

            // Các hàng từ 18 đến 29 phải hoàn toàn trống!
            for (int y = 18; y < grid.height; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    Assert.IsFalse(grid.IsLocked(new Vector2Int(x, y)), $"Hàng {y} phải trống.");
                }
            }

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Player_CanDisableSpawning_KeepsActivePiece()
        {
            GameObject gridGo = new GameObject("TestGrid_SpawnCtrl");
            GridManager grid = gridGo.AddComponent<GridManager>();
            grid.InitializeGrid(15, 30);

            GameObject playerGo = new GameObject("TestPlayer_SpawnCtrl");
            PlayerController pc = playerGo.AddComponent<PlayerController>();
            pc.gridManager = grid;
            pc.SetSide(PlayerSide.Top);

            // Ban đầu cho phép sinh khối
            pc.canSpawn = true;
            pc.StartRound();

            Assert.IsTrue(pc.HasActivePiece, "Người chơi phải có khối đang rơi khi canSpawn = true.");

            // Tắt tính năng sinh khối trong lúc khối đang rơi
            pc.CanSpawn = false;

            // Khối đang rơi hiện tại KHÔNG được bị hủy!
            Assert.IsTrue(pc.HasActivePiece, "Khối đang rơi phải tiếp tục hoạt động khi canSpawn chuyển thành false.");

            // Mô phỏng khối tiếp đất và khóa lại
            Vector2Int[] cells = pc.GetWorldCells();
            grid.LockCells(cells, Color.cyan, pc.side);

            // Thử gọi SpawnPiece (như khi chế độ chơi gọi sau khi khóa khối)
            pc.SpawnPiece();

            // Khi canSpawn là false, không được sinh khối mới
            Assert.IsFalse(pc.canSpawn, "canSpawn đang là false.");

            // Bật lại tính năng sinh khối
            pc.CanSpawn = true;
            Assert.IsTrue(pc.HasActivePiece, "Bật lại CanSpawn phải tự động sinh khối mới nếu người chơi chưa có khối.");

            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(gridGo);
        }
    }
}
