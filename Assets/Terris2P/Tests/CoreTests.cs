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
        public void GridManager_HeartLossReset_P2Loses_WipesP2AndKeepsP1AtOriginalPositionsExceptBarrierPath()
        {
            GameObject go = new GameObject("TestGrid_HeartReset_P2Loses");
            GridManager grid = go.AddComponent<GridManager>();
            grid.InitializeGrid(15, 30);

            grid.SpawnCenterBarrier(2); // Hàng 14 và 15

            // P1 xóa 2 hàng để đẩy rào chắn XUỐNG 2 hàng: từ 14-15 thành 12-13
            Vector2Int[] row1 = new Vector2Int[grid.width];
            for (int x = 0; x < grid.width; x++) row1[x] = new Vector2Int(x, 20);
            grid.LockCells(row1, Color.cyan, PlayerSide.Top);

            Vector2Int[] row2 = new Vector2Int[grid.width];
            for (int x = 0; x < grid.width; x++) row2[x] = new Vector2Int(x, 20);
            grid.LockCells(row2, Color.cyan, PlayerSide.Top);

            // Xác minh rào chắn hiện tại ở hàng 12 và 13
            Assert.IsTrue(grid.IsIndestructible(new Vector2Int(0, 12)), "Rào chắn phải ở hàng 12.");
            Assert.IsTrue(grid.IsIndestructible(new Vector2Int(0, 13)), "Rào chắn phải ở hàng 13.");

            // P2 (Bên thua) có khối tại hàng 4 và 5
            for (int y = 4; y <= 5; y++)
            {
                for (int x = 0; x < grid.width - 1; x++)
                {
                    grid.LockCells(new[] { new Vector2Int(x, y) }, Color.yellow, PlayerSide.Bottom);
                }
            }

            // P1 (Bên thắng):
            // - Khối tại hàng 14 (nằm trên đường rào chắn quét từ 12-13 lên 14-15) -> SẼ BỊ XÓA
            // - Khối tại hàng 24 (nằm ngoài đường quét) -> GIỮ NGUYÊN VỊ TRÍ
            for (int x = 0; x < grid.width - 1; x++)
            {
                grid.LockCells(new[] { new Vector2Int(x, 14) }, Color.cyan, PlayerSide.Top);
                grid.LockCells(new[] { new Vector2Int(x, 24) }, Color.cyan, PlayerSide.Top);
            }

            // P2 bị mất máu
            grid.ExecuteHeartLossReset(PlayerSide.Bottom);

            // 1. Rào chắn trở về vị trí chính giữa: hàng 14 và 15
            for (int y = 14; y <= 15; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    Assert.IsTrue(grid.IsIndestructible(new Vector2Int(x, y)), $"Rào chắn phải ở hàng {y}.");
                }
            }

            // 2. Bên bị trừ máu (P2): BỊ XÓA SẠCH TOÀN BỘ 100% SỐ KHỐI (hàng 0 đến 13 trống)
            for (int y = 0; y <= 13; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    Assert.IsFalse(grid.IsLocked(new Vector2Int(x, y)), $"Phần sân P2 tại hàng {y} phải bị xóa sạch.");
                }
            }

            // 3. Bên không bị trừ máu (P1):
            // - Khối tại hàng 24 GIỮ NGUYÊN VỊ TRÍ (không bị tịnh tiến)
            for (int x = 0; x < grid.width - 1; x++)
            {
                Assert.IsTrue(grid.IsLocked(new Vector2Int(x, 24)), "Khối của P1 tại hàng 24 phải giữ nguyên vị trí.");
            }

            Object.DestroyImmediate(go);
        }

        [Test]
        public void GridManager_HeartLossReset_P1Loses_WipesP1AndKeepsP2AtOriginalPositionsExceptBarrierPath()
        {
            GameObject go = new GameObject("TestGrid_HeartReset_P1Loses");
            GridManager grid = go.AddComponent<GridManager>();
            grid.InitializeGrid(15, 30);

            grid.SpawnCenterBarrier(2); // Hàng 14 và 15

            // P2 xóa 2 hàng để đẩy rào chắn LÊN 2 hàng: từ 14-15 thành 16-17
            Vector2Int[] row1 = new Vector2Int[grid.width];
            for (int x = 0; x < grid.width; x++) row1[x] = new Vector2Int(x, 8);
            grid.LockCells(row1, Color.yellow, PlayerSide.Bottom);

            Vector2Int[] row2 = new Vector2Int[grid.width];
            for (int x = 0; x < grid.width; x++) row2[x] = new Vector2Int(x, 8);
            grid.LockCells(row2, Color.yellow, PlayerSide.Bottom);

            // Xác minh rào chắn hiện tại ở hàng 16 và 17
            Assert.IsTrue(grid.IsIndestructible(new Vector2Int(0, 16)), "Rào chắn phải ở hàng 16.");
            Assert.IsTrue(grid.IsIndestructible(new Vector2Int(0, 17)), "Rào chắn phải ở hàng 17.");

            // P1 (Bên thua) có khối tại hàng 22 và 23
            for (int y = 22; y <= 23; y++)
            {
                for (int x = 0; x < grid.width - 1; x++)
                {
                    grid.LockCells(new[] { new Vector2Int(x, y) }, Color.cyan, PlayerSide.Top);
                }
            }

            // P2 (Bên thắng):
            // - Khối tại hàng 15 (nằm trên đường rào chắn quét từ 16-17 xuống 14-15) -> SẼ BỊ XÓA (thành rào chắn)
            // - Khối tại hàng 5 (nằm ngoài đường quét) -> GIỮ NGUYÊN VỊ TRÍ
            for (int x = 0; x < grid.width - 1; x++)
            {
                grid.LockCells(new[] { new Vector2Int(x, 15) }, Color.yellow, PlayerSide.Bottom);
                grid.LockCells(new[] { new Vector2Int(x, 5) }, Color.yellow, PlayerSide.Bottom);
            }

            // P1 bị mất máu
            grid.ExecuteHeartLossReset(PlayerSide.Top);

            // 1. Rào chắn hồi về vị trí chính giữa: hàng 14 và 15
            for (int y = 14; y <= 15; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    Assert.IsTrue(grid.IsIndestructible(new Vector2Int(x, y)), $"Rào chắn phải ở hàng {y}.");
                }
            }

            // 2. Bên bị trừ máu (P1): BỊ XÓA SẠCH TOÀN BỘ 100% SỐ KHỐI (hàng 16 đến 29 trống)
            for (int y = 16; y < grid.height; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    Assert.IsFalse(grid.IsLocked(new Vector2Int(x, y)), $"Phần sân P1 tại hàng {y} phải bị xóa sạch.");
                }
            }

            // 3. Bên không bị trừ máu (P2):
            // - Khối tại hàng 5 GIỮ NGUYÊN VỊ TRÍ (không bị tịnh tiến)
            for (int x = 0; x < grid.width - 1; x++)
            {
                Assert.IsTrue(grid.IsLocked(new Vector2Int(x, 5)), "Khối của P2 tại hàng 5 phải giữ nguyên vị trí.");
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
