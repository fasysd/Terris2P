using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Terris2P.AI;

namespace Terris2P.Tests
{
    /// <summary>
    /// Bộ kiểm thử tự động (Unit Tests) cho hệ thống AI của Terris 2P:
    /// VirtualBoard, Heuristics, Move Generator, Beam Search, Minimax Threat, và Controller API.
    /// </summary>
    public class AITests
    {
        [Test]
        public void VirtualBoard_InitializesBarrierAtCenter()
        {
            VirtualBoard vb = VirtualBoard.CreateWithCenterBarrier();

            vb.GetBarrierBounds(out int minY, out int maxY);
            Assert.AreEqual(14, minY, "Rào chắn phía dưới phải ở hàng 14.");
            Assert.AreEqual(15, maxY, "Rào chắn phía trên phải ở hàng 15.");

            for (int y = 14; y <= 15; y++)
            {
                Assert.AreEqual(VirtualBoard.FullRowMask, vb.rows[y], $"Hàng {y} phải được lấp đầy hoàn toàn.");
                Assert.AreEqual(VirtualBoard.FullRowMask, vb.indestructible[y], $"Hàng {y} phải không thể phá hủy.");
            }
        }

        [Test]
        public void VirtualBoard_DropPieceUpward_LandsBelowBarrier()
        {
            VirtualBoard vb = VirtualBoard.CreateWithCenterBarrier(); // Hàng 14-15 bị khóa

            // P2 thả khối O từ pivot (7, 1) theo trọng lực +1 (hướng lên trên)
            bool dropped = vb.TryDropPiece(TetrominoType.O, 7, 1, 0, 1, out Vector2Int ghost);

            Assert.IsTrue(dropped, "Khối O phải thả rơi thành công.");
            // Khối O có các ô (0,0), (1,0), (0,1), (1,1).
            // Ô cao nhất là pivot.y + 1. Khối va chạm khi chạm hàng 14.
            // Do đó ghost.y + 1 = 13 => ghost.y = 12!
            Assert.AreEqual(12, ghost.y, "Khối O thả lên phải dừng lại ngay sát dưới rào chắn ở y=12.");
        }

        [Test]
        public void VirtualBoard_P2LineClear_PushesBarrierUpwardTowardP1()
        {
            VirtualBoard vb = VirtualBoard.CreateWithCenterBarrier(); // Hàng 14-15

            // Điền đầy hàng 12 (trừ rào chắn) và khóa lại
            vb.rows[12] = VirtualBoard.FullRowMask;

            // Xóa hàng do P2 tạo
            int cleared = vb.ClearFullRows(PlayerSide.Bottom);
            Assert.AreEqual(1, cleared, "Phải xóa được 1 hàng.");

            // Sau khi P2 xóa hàng 12: mọi hàng bên trên clearedRow (13..Height-2) bị đẩy LÊN 1 hàng!
            // Rào chắn (14-15) phải dịch chuyển lên thành hàng 15 và 16!
            vb.GetBarrierBounds(out int newMinY, out int newMaxY);
            Assert.AreEqual(15, newMinY, "Rào chắn phải bị đẩy lên hàng 15.");
            Assert.AreEqual(16, newMaxY, "Rào chắn phải bị đẩy lên hàng 16.");
        }

        [Test]
        public void TetrisAISearch_GeneratesValidMovesForAllPieces()
        {
            VirtualBoard vb = VirtualBoard.CreateWithCenterBarrier();
            HeuristicWeights weights = HeuristicWeights.ForDifficulty(AIDifficulty.Medium);

            for (int i = 0; i < 7; i++)
            {
                TetrominoType type = (TetrominoType)i;
                List<CandidatePlacement> moves = TetrisAISearch.GeneratePlacements(ref vb, type, PlayerSide.Bottom, weights);

                Assert.Greater(moves.Count, 0, $"Phải sinh được ít nhất 1 nước đi cho khối {type}.");
                foreach (var move in moves)
                {
                    Assert.GreaterOrEqual(move.x, 0, "Cột x phải >= 0.");
                    Assert.Less(move.x, VirtualBoard.Width, "Cột x phải < Width.");
                    Assert.IsFalse(move.resultingBoard.HasBlockAtRow(0), "Nước đi ban đầu không được đè vào mép chết 0.");
                }
            }
        }

        [Test]
        public void TetrisAIHeuristic_PenalizesDangerNearRow0()
        {
            VirtualBoard safeBoard = VirtualBoard.CreateWithCenterBarrier();
            VirtualBoard dangerousBoard = VirtualBoard.CreateWithCenterBarrier();

            // Safe board: khối nằm ở hàng 10-12 (gần rào chắn)
            safeBoard.rows[12] = 0x00FF;

            // Dangerous board: khối tràn xuống sát hàng 1 (sắp chết chạm hàng 0)
            dangerousBoard.rows[1] = 0x00FF;

            HeuristicWeights weights = HeuristicWeights.ForDifficulty(AIDifficulty.Hard);
            float safeScore = TetrisAIHeuristic.Evaluate(ref safeBoard, 0, weights);
            float dangerScore = TetrisAIHeuristic.Evaluate(ref dangerousBoard, 0, weights);

            Assert.Greater(safeScore, dangerScore, "Thế cờ an toàn phải có điểm Heuristic cao hơn thế cờ sắp chạm hàng 0.");
        }

        [Test]
        public void TetrisAISearch_FindBestMove_SelectsHighScoringMove()
        {
            VirtualBoard vb = VirtualBoard.CreateWithCenterBarrier();

            AIMoveDecision decision = TetrisAISearch.FindBestMove(
                vb,
                TetrominoType.I,
                TetrominoType.O,
                AIDifficulty.Hard
            );

            Assert.GreaterOrEqual(decision.targetX, 0, "Target X phải hợp lệ.");
            Assert.Less(decision.targetX, VirtualBoard.Width, "Target X phải trong bàn cờ.");
            Assert.IsTrue(decision.shouldHardDrop, "Chế độ Hard phải cho phép thả tức thì.");
            Assert.Greater(decision.score, -100000f, "Điểm của nước đi tốt nhất phải hợp lệ.");
        }

        [Test]
        public void AIThinkingProfile_EasyMode_IsFastAndSmart()
        {
            AIThinkingProfile easy = AIThinkingProfile.CreateDefault(AIDifficulty.Easy);

            // Mức độ Dễ nay thông minh hơn và nhanh hơn
            Assert.GreaterOrEqual(easy.beamWidth, 3, "Easy mode phải có BeamWidth >= 3 để tìm nước đi tốt.");
            Assert.IsTrue(easy.useLookahead, "Easy mode phải xét cả khối tiếp theo.");
            Assert.IsTrue(easy.allowHardDrop, "Easy mode cho phép thả khối.");
            Assert.LessOrEqual(easy.thinkingDelay, 0.2f, "Thời gian suy nghĩ của Easy phải <= 0.2s.");
            Assert.LessOrEqual(easy.actionInterval, 0.12f, "Khoảng cách thao tác của Easy phải <= 0.12s (~300+ APM).");
            Assert.LessOrEqual(easy.randomNoiseRange, 10f, "Nhiễu ngẫu nhiên của Easy phải nhỏ để không đặt gạch ngớ ngẩn.");
        }

        [Test]
        public void AIController_CommandsPlayerThroughPublicAPI()
        {
            GameObject gridGo = new GameObject("TestGrid_AI");
            GridManager grid = gridGo.AddComponent<GridManager>();
            grid.InitializeGrid(15, 30);
            grid.SpawnCenterBarrier(2);

            GameObject playerGo = new GameObject("TestP2_AI");
            PlayerController pc = playerGo.AddComponent<PlayerController>();
            pc.gridManager = grid;
            pc.SetSide(PlayerSide.Bottom);
            pc.isAIControlled = true;
            pc.StartRound();

            Assert.IsTrue(pc.IsActive, "P2 phải có khối đang rơi.");
            int startX = pc.Pivot.x;

            // AI gọi lệnh Move(1) qua API
            bool moved = pc.Move(1);
            Assert.IsTrue(moved, "Lệnh Move(1) phải thành công.");
            Assert.AreEqual(startX + 1, pc.Pivot.x, "Pivot X phải dịch sang phải 1 đơn vị.");

            // AI gọi lệnh xoay qua API
            int startRot = pc.Rotation;
            bool rotated = pc.RotatePiece();
            Assert.IsTrue(rotated, "Lệnh RotatePiece phải thành công.");
            Assert.AreEqual((startRot + 1) % 4, pc.Rotation, "Rotation phải tăng thêm 1.");

            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(gridGo);
        }

        [Test]
        public void GameManager_ConfiguresAIModeProperly()
        {
            GameObject gmGo = new GameObject("Test_GM_AI");
            GameManager gm = gmGo.AddComponent<GameManager>();
            gm.autoStart = false;

            GameObject gridGo = new GameObject("Test_Grid_AI");
            GridManager grid = gridGo.AddComponent<GridManager>();
            grid.InitializeGrid(15, 30);
            gm.gridManager = grid;

            GameObject p1Go = new GameObject("Test_P1");
            PlayerController p1 = p1Go.AddComponent<PlayerController>();
            p1.SetSide(PlayerSide.Top);
            gm.topPlayer = p1;

            GameObject p2Go = new GameObject("Test_P2");
            PlayerController p2 = p2Go.AddComponent<PlayerController>();
            p2.SetSide(PlayerSide.Bottom);
            gm.bottomPlayer = p2;

            // Đặt chế độ Đấu với máy
            gm.SetGameMode(GameMode.PlayerVsAI);
            gm.SetAIDifficulty(AIDifficulty.Hard);
            gm.StartGame();

            Assert.IsTrue(p2.isAIControlled, "P2 phải ở trạng thái được AI điều khiển.");
            Assert.IsFalse(p1.isAIControlled, "P1 phải do người chơi điều khiển.");

            TetrisAIController ai = p2.GetComponent<TetrisAIController>();
            Assert.IsNotNull(ai, "P2 phải được gắn component TetrisAIController.");
            Assert.AreEqual(AIDifficulty.Hard, ai.difficulty, "Độ khó của AI phải là Hard.");

            Object.DestroyImmediate(p1Go);
            Object.DestroyImmediate(p2Go);
            Object.DestroyImmediate(gridGo);
            Object.DestroyImmediate(gmGo);
        }

        [Test]
        public void MainMenu_ModeAndDifficulty_ToggleAndPersist()
        {
            // Thiết lập chế độ PvE và độ khó Hard
            GameManager.SelectedMode = GameMode.PlayerVsAI;
            GameManager.SelectedDifficulty = AIDifficulty.Hard;

            Assert.AreEqual(GameMode.PlayerVsAI, GameManager.SelectedMode);
            Assert.AreEqual(AIDifficulty.Hard, GameManager.SelectedDifficulty);

            // Chuyển sang chế độ PvP
            GameManager.SelectedMode = GameMode.PlayerVsPlayer;
            Assert.AreEqual(GameMode.PlayerVsPlayer, GameManager.SelectedMode);

            // Chu kỳ độ khó: Hard -> Master -> Easy -> Medium
            GameManager.SelectedDifficulty = AIDifficulty.Master;
            Assert.AreEqual(AIDifficulty.Master, GameManager.SelectedDifficulty);

            GameManager.SelectedDifficulty = AIDifficulty.Easy;
            Assert.AreEqual(AIDifficulty.Easy, GameManager.SelectedDifficulty);

            // Đặt lại mặc định
            GameManager.SelectedMode = GameMode.PlayerVsAI;
            GameManager.SelectedDifficulty = AIDifficulty.Medium;
        }

        [Test]
        public void AIThinkingConfigSO_ReturnsValidProfilesForAllDifficulties()
        {
            AIThinkingConfigSO config = ScriptableObject.CreateInstance<AIThinkingConfigSO>();

            foreach (AIDifficulty diff in System.Enum.GetValues(typeof(AIDifficulty)))
            {
                AIThinkingProfile profile = config.GetProfile(diff);
                Assert.IsNotNull(profile, $"Profile cho độ khó {diff} không được null.");
                Assert.AreEqual(diff, profile.difficulty, "Difficulty của profile phải khớp.");
                Assert.Greater(profile.beamWidth, 0, "BeamWidth phải > 0.");
                Assert.Greater(profile.actionInterval, 0f, "ActionInterval phải > 0.");
                Assert.Greater(profile.searchDepth, 0, "SearchDepth phải > 0.");
            }

            Object.DestroyImmediate(config);
        }

        [Test]
        public void AIThinkingConfigSO_CustomProfileDrivesSearchAndController()
        {
            AIThinkingConfigSO config = ScriptableObject.CreateInstance<AIThinkingConfigSO>();
            config.hardProfile.beamWidth = 12;
            config.hardProfile.thinkingDelay = 0.01f;
            config.hardProfile.actionInterval = 0.03f;

            GameObject playerGo = new GameObject("Test_AI_SO");
            PlayerController pc = playerGo.AddComponent<PlayerController>();
            TetrisAIController ai = playerGo.AddComponent<TetrisAIController>();
            ai.playerController = pc;
            ai.difficulty = AIDifficulty.Hard;
            ai.thinkingConfig = config;

            Assert.AreEqual(12, ai.ActiveProfile.beamWidth);
            Assert.AreEqual(0.01f, ai.ActiveProfile.thinkingDelay);
            Assert.AreEqual(0.03f, ai.ActiveProfile.actionInterval);

            // Chạy tìm kiếm với profile tùy chỉnh
            VirtualBoard vb = VirtualBoard.CreateWithCenterBarrier();
            AIMoveDecision decision = TetrisAISearch.FindBestMove(vb, TetrominoType.T, TetrominoType.I, ai.ActiveProfile);
            Assert.GreaterOrEqual(decision.targetX, 0);
            Assert.Less(decision.targetX, VirtualBoard.Width);

            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(config);
        }
    }
}
