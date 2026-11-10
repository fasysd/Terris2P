using System;
using System.Collections.Generic;
using UnityEngine;

namespace Terris2P.AI
{
    public struct AIMoveDecision
    {
        public int targetX;
        public int targetRotation;
        public bool shouldHardDrop;
        public float score;

        public static AIMoveDecision Default => new AIMoveDecision
        {
            targetX = 7,
            targetRotation = 0,
            shouldHardDrop = false,
            score = -999999f
        };
    }

    public struct CandidatePlacement
    {
        public int x;
        public int rotation;
        public Vector2Int ghostPivot;
        public int linesCleared;
        public float heuristicScore;
        public VirtualBoard resultingBoard;
    }

    /// <summary>
    /// Thuật toán tìm kiếm nước đi (Search Specification) cho AI P2.
    /// Kết hợp sinh nước đi hợp lệ toàn diện và Beam Search 2 tầng tối ưu tốc độ và độ chuẩn xác.
    /// </summary>
    public static class TetrisAISearch
    {
        private static readonly System.Random random = new System.Random();

        /// <summary>
        /// Số góc xoay độc nhất của từng loại khối để tối ưu hóa không gian tìm kiếm.
        /// </summary>
        public static int GetDistinctRotations(TetrominoType type)
        {
            switch (type)
            {
                case TetrominoType.O: return 1;
                case TetrominoType.I:
                case TetrominoType.S:
                case TetrominoType.Z: return 2;
                default: return 4; // T, J, L
            }
        }

        /// <summary>
        /// Sinh tất cả các nước đi khả dĩ cho một khối rơi trên bàn cờ ảo.
        /// </summary>
        public static List<CandidatePlacement> GeneratePlacements(
            ref VirtualBoard board,
            TetrominoType type,
            PlayerSide side,
            HeuristicWeights weights)
        {
            List<CandidatePlacement> candidates = new List<CandidatePlacement>(36);
            int rotations = GetDistinctRotations(type);
            Vector2Int[] localCells = TetrominoData.GetCells(type);
            int gravity = side == PlayerSide.Top ? -1 : 1;
            int startY = side == PlayerSide.Top ? VirtualBoard.Height - 2 : 1;

            for (int r = 0; r < rotations; r++)
            {
                int minXOffset = 0;
                int maxXOffset = 0;

                for (int i = 0; i < localCells.Length; i++)
                {
                    Vector2Int rotCell = VirtualBoard.Rotate(localCells[i], r);
                    if (rotCell.x < minXOffset) minXOffset = rotCell.x;
                    if (rotCell.x > maxXOffset) maxXOffset = rotCell.x;
                }

                int minValidX = 0 - minXOffset;
                int maxValidX = (VirtualBoard.Width - 1) - maxXOffset;

                for (int x = minValidX; x <= maxValidX; x++)
                {
                    if (board.TryDropPiece(type, x, startY, r, gravity, out Vector2Int ghostPivot))
                    {
                        VirtualBoard simBoard = board.Clone();
                        int cleared = simBoard.LockPieceAndClearLines(type, ghostPivot, r, side);
                        float score = TetrisAIHeuristic.Evaluate(ref simBoard, cleared, weights, random);

                        candidates.Add(new CandidatePlacement
                        {
                            x = x,
                            rotation = r,
                            ghostPivot = ghostPivot,
                            linesCleared = cleared,
                            heuristicScore = score,
                            resultingBoard = simBoard
                        });
                    }
                }
            }

            return candidates;
        }

        /// <summary>
        /// Tìm kiếm nước đi tốt nhất bằng Beam Search (sử dụng AIDifficulty).
        /// </summary>
        public static AIMoveDecision FindBestMove(
            VirtualBoard currentBoard,
            TetrominoType p2Current,
            TetrominoType p2Next,
            AIDifficulty difficulty)
        {
            AIThinkingProfile profile = AIThinkingProfile.CreateDefault(difficulty);
            return FindBestMove(currentBoard, p2Current, p2Next, profile);
        }

        /// <summary>
        /// Tìm kiếm nước đi tốt nhất bằng Beam Search theo cấu hình suy nghĩ chi tiết từ AIThinkingProfile.
        /// </summary>
        public static AIMoveDecision FindBestMove(
            VirtualBoard currentBoard,
            TetrominoType p2Current,
            TetrominoType p2Next,
            AIThinkingProfile profile)
        {
            if (profile == null)
                profile = AIThinkingProfile.CreateDefault(AIDifficulty.Medium);

            HeuristicWeights weights = profile.ToHeuristicWeights();
            int beamWidth = profile.beamWidth;
            bool useLookahead = profile.useLookahead;
            bool allowHardDrop = profile.allowHardDrop;
            float lookaheadDiscount = profile.lookaheadDiscount;

            // Tầng 1: Sinh toàn bộ các nước đi khả dĩ cho khối hiện tại của P2
            List<CandidatePlacement> level1Moves = GeneratePlacements(ref currentBoard, p2Current, PlayerSide.Bottom, weights);
            if (level1Moves.Count == 0)
            {
                return AIMoveDecision.Default;
            }

            // Sắp xếp các nước đi Tầng 1 theo điểm Heuristic giảm dần
            level1Moves.Sort((a, b) => b.heuristicScore.CompareTo(a.heuristicScore));

            int candidatesToKeep = Math.Min(beamWidth, level1Moves.Count);
            AIMoveDecision bestDecision = AIMoveDecision.Default;
            float highestScore = float.NegativeInfinity;

            for (int i = 0; i < candidatesToKeep; i++)
            {
                CandidatePlacement candidate = level1Moves[i];
                float totalCandidateScore = candidate.heuristicScore;

                // Tầng 2: Tìm kiếm khối kế tiếp (Next Piece Lookahead)
                if (useLookahead)
                {
                    VirtualBoard nextBoard = candidate.resultingBoard;
                    List<CandidatePlacement> level2Moves = GeneratePlacements(ref nextBoard, p2Next, PlayerSide.Bottom, weights);
                    if (level2Moves.Count > 0)
                    {
                        float bestLevel2 = float.NegativeInfinity;
                        for (int j = 0; j < level2Moves.Count; j++)
                        {
                            if (level2Moves[j].heuristicScore > bestLevel2)
                                bestLevel2 = level2Moves[j].heuristicScore;
                        }
                        totalCandidateScore += lookaheadDiscount * bestLevel2;
                    }
                }

                if (totalCandidateScore > highestScore)
                {
                    highestScore = totalCandidateScore;
                    bestDecision = new AIMoveDecision
                    {
                        targetX = candidate.x,
                        targetRotation = candidate.rotation,
                        shouldHardDrop = allowHardDrop,
                        score = totalCandidateScore
                    };
                }
            }

            return bestDecision;
        }

        // Overloads giữ tương thích
        public static AIMoveDecision FindBestMove(
            VirtualBoard currentBoard,
            TetrominoType p2Current,
            TetrominoType p2Next,
            TetrominoType p1Current,
            AIDifficulty difficulty)
        {
            return FindBestMove(currentBoard, p2Current, p2Next, difficulty);
        }

        public static AIMoveDecision FindBestMove(
            VirtualBoard currentBoard,
            TetrominoType p2Current,
            TetrominoType p2Next,
            TetrominoType p1Current,
            AIThinkingProfile profile)
        {
            return FindBestMove(currentBoard, p2Current, p2Next, profile);
        }
    }
}
