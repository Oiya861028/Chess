using System;
using System.Collections.Generic;
using UnityEngine;

public class StockFridge
{
    private FindMoves findMoves;
    private Evaluation evaluation;
    private Bitboard bitboard;
    private const int INFINITY = 9999999;
    private const int MATE_SCORE = 9000000; 
    
    private const int MAX_QUIESCENCE_DEPTH = 8;
    private const int DELTA_MARGIN = 120;
    
    // Move ordering constants
    private readonly int[] PieceValues = { 0, 100, 320, 330, 500, 900, 20000 }; // None, Pawn, Knight, Bishop, Rook, Queen, King
    
    // Transposition table and related fields
    private TranspositionTable transpositionTable;
    private readonly ulong[,,] zobristKeys; // [piece][color][square]
    private ulong sideToMoveKey;
    
    // Killer moves for move ordering - store two killer moves per ply
    private const int MAX_DEPTH = 64;
    private int[,] killerMoves = new int[2, MAX_DEPTH];
    
    // History heuristic table for quiet move ordering
    private int[,] historyTable = new int[2, 64 * 64]; // [color, from*64+to]
    
    // Game position history for detecting repetitions
    private Dictionary<ulong, int> gamePositionHistory = new Dictionary<ulong, int>();
    // Search position history (copy of game history used during search)
    private Dictionary<ulong, int> positionHistory = new Dictionary<ulong, int>();
    
    // Node statistics for debugging/tuning
    private int nodesSearched = 0;
    private int qNodesSearched = 0;
    
    public StockFridge(FindMoves findMoves, Bitboard bitboard)
    {
        this.findMoves = findMoves;
        this.bitboard = bitboard;
        this.evaluation = new Evaluation();
        
        // Initialize the transposition table
        this.transpositionTable = new TranspositionTable();
        
        // Initialize Zobrist keys for hashing
        System.Random rand = new System.Random(123456); 
        zobristKeys = new ulong[7,2,64]; 
        
        // Generate random numbers for each piece/square combination
        for (int piece = 0; piece < 7; piece++)
            for (int color = 0; color < 2; color++)
                for (int square = 0; square < 64; square++)
                    zobristKeys[piece,color,square] = ((ulong)rand.Next() << 32) | (ulong)rand.Next();
        
        // Generate key for side to move
        sideToMoveKey = ((ulong)rand.Next() << 32) | (ulong)rand.Next();
        
        // Initialize position history with current position
        ResetGameHistory();
        
        Debug.Log($"StockFridge initialized with transposition table. Memory usage: {transpositionTable.GetMemoryUsage() / (1024 * 1024)} MB");
    }
    
    // Reset game history for a new game
    public void ResetGameHistory()
    {
        gamePositionHistory.Clear();
        
        // Add the current position to the history
        ulong currentHash = ComputeHash(true); // Starting with white's position
        gamePositionHistory[currentHash] = 1;
        
        Debug.Log("Game position history reset");
    }
    
    // Compute Zobrist hash for the current board position
    private ulong ComputeHash(bool isWhite)
    {
        ulong hash = 0;
        
        // Process white pieces
        ProcessPieceBitboard(bitboard.WhitePawn, 1, 0, ref hash);
        ProcessPieceBitboard(bitboard.WhiteKnight, 2, 0, ref hash);
        ProcessPieceBitboard(bitboard.WhiteBishop, 3, 0, ref hash);
        ProcessPieceBitboard(bitboard.WhiteRook, 4, 0, ref hash);
        ProcessPieceBitboard(bitboard.WhiteQueen, 5, 0, ref hash);
        ProcessPieceBitboard(bitboard.WhiteKing, 6, 0, ref hash);
        
        // Process black pieces
        ProcessPieceBitboard(bitboard.BlackPawn, 1, 1, ref hash);
        ProcessPieceBitboard(bitboard.BlackKnight, 2, 1, ref hash);
        ProcessPieceBitboard(bitboard.BlackBishop, 3, 1, ref hash);
        ProcessPieceBitboard(bitboard.BlackRook, 4, 1, ref hash);
        ProcessPieceBitboard(bitboard.BlackQueen, 5, 1, ref hash);
        ProcessPieceBitboard(bitboard.BlackKing, 6, 1, ref hash);
        
        // Hash in the side to move
        if (!isWhite)
            hash ^= sideToMoveKey;
        
        // Add castling rights to hash
        if (bitboard.whiteKingMoved) hash ^= 1UL;
        if (bitboard.blackKingMoved) hash ^= 2UL;
        if (bitboard.whiteKingsideRookMoved) hash ^= 4UL;
        if (bitboard.whiteQueensideRookMoved) hash ^= 8UL;
        if (bitboard.blackKingsideRookMoved) hash ^= 16UL;
        if (bitboard.blackQueensideRookMoved) hash ^= 32UL;
        
        return hash;
    }
    
    // Helper method to process a piece bitboard for hashing
    private void ProcessPieceBitboard(ulong pieceBitboard, int pieceType, int color, ref ulong hash)
    {
        ulong bb = pieceBitboard;
        while (bb != 0)
        {
            int sq = BitOperations.TrailingZeroCount(bb);
            hash ^= zobristKeys[pieceType, color, sq];
            bb &= bb - 1; // Clear the least significant bit
        }
    }
    
    // Stockfish-style move ordering
    private void OrderMoves(List<int> moves, int ttMove, int ply)
    {
        const int TT_SCORE = 20000;
        const int WINNING_CAPTURE_SCORE = 10000;
        const int PROMOTION_SCORE = 9000;
        const int KILLER_SCORE = 8000;
        
        // Use a dictionary to store scores for each move
        Dictionary<int, int> moveScores = new Dictionary<int, int>();
        
        foreach (int move in moves)
        {
            int score = 0;
            
            // 1. Hash move from transposition table (highest priority)
            if (ttMove != 0 && 
                MoveUtil.GetSource(move) == MoveUtil.GetSource(ttMove) && 
                MoveUtil.GetDestination(move) == MoveUtil.GetDestination(ttMove))
            {
                score = TT_SCORE;
            }
            // 2. Captures - score by MVV/LVA 
            else if (IsMoveCapture(move))
            {
                int victimValue = 0;
                int dest = MoveUtil.GetDestination(move);
                ulong destBit = 1UL << dest;
                
                // Find captured piece value
                if (MoveUtil.IsWhite(move))
                {
                    if ((bitboard.BlackPawn & destBit) != 0) victimValue = 100;
                    else if ((bitboard.BlackKnight & destBit) != 0) victimValue = 320;
                    else if ((bitboard.BlackBishop & destBit) != 0) victimValue = 330;
                    else if ((bitboard.BlackRook & destBit) != 0) victimValue = 500;
                    else if ((bitboard.BlackQueen & destBit) != 0) victimValue = 900;
                    else if (MoveUtil.IsEnPassant(move)) victimValue = 100; // Pawn value for en passant
                }
                else
                {
                    if ((bitboard.WhitePawn & destBit) != 0) victimValue = 100;
                    else if ((bitboard.WhiteKnight & destBit) != 0) victimValue = 320;
                    else if ((bitboard.WhiteBishop & destBit) != 0) victimValue = 330;
                    else if ((bitboard.WhiteRook & destBit) != 0) victimValue = 500;
                    else if ((bitboard.WhiteQueen & destBit) != 0) victimValue = 900;
                    else if (MoveUtil.IsEnPassant(move)) victimValue = 100; // Pawn value for en passant
                }
                
                // Apply MVV-LVA scoring
                if (victimValue > 0)
                {
                    // Most Valuable Victim (higher value) - Least Valuable Attacker (lower value)
                    score = WINNING_CAPTURE_SCORE + victimValue - (MoveUtil.GetPieceType(move) * 10);
                }
            }
            // 3. Promotions
            else if (MoveUtil.IsPromotion(move))
            {
                score = PROMOTION_SCORE + MoveUtil.GetPromotionPieceType(move) * 10;
            }
            // 4. Killer moves (quiet moves that caused beta cutoffs at this ply)
            else if (ply < killerMoves.GetLength(1))
            {
                if (killerMoves[0, ply] != 0 && 
                    MoveUtil.GetSource(move) == MoveUtil.GetSource(killerMoves[0, ply]) && 
                    MoveUtil.GetDestination(move) == MoveUtil.GetDestination(killerMoves[0, ply]))
                {
                    score = KILLER_SCORE;
                }
                else if (killerMoves[1, ply] != 0 && 
                        MoveUtil.GetSource(move) == MoveUtil.GetSource(killerMoves[1, ply]) && 
                        MoveUtil.GetDestination(move) == MoveUtil.GetDestination(killerMoves[1, ply]))
                {
                    score = KILLER_SCORE - 1;
                }
            }
            
            // 5. History heuristic (for quiet moves)
            if (score == 0 && !IsMoveCapture(move) && !MoveUtil.IsPromotion(move))
            {
                bool isWhite = MoveUtil.IsWhite(move);
                int source = MoveUtil.GetSource(move);
                int dest = MoveUtil.GetDestination(move);
                int historyScore = historyTable[isWhite ? 0 : 1, source * 64 + dest];
                score = historyScore;
            }
            
            moveScores[move] = score;
        }
        
        // Sort moves by score (descending)
        moves.Sort((a, b) => moveScores[b].CompareTo(moveScores[a]));
    }
    
    // The main function that will be called to get the best move
    public int GetBestMove(int depth, bool isWhite, int previousMove)
    {
        // Reset node counters
        nodesSearched = 0;
        qNodesSearched = 0;
        
        // Copy the game history for this search
        positionHistory = new Dictionary<ulong, int>(gamePositionHistory);
        
        // Increment age for the transposition table
        transpositionTable.IncrementAge();
        
        // Reset killer moves
        Array.Clear(killerMoves, 0, killerMoves.Length);
        
        // Decay history table values (keep some historical information)
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 64 * 64; j++)
                historyTable[i, j] = historyTable[i, j] * 80 / 100; // 80% decay
        
        // Get all possible legal moves for the current position
        List<int> possibleMoves = findMoves.GetAllPossibleMoves(isWhite, previousMove);
        List<int> legalMoves = new List<int>();
        
        // Filter out moves that would leave the king in check
        foreach (int move in possibleMoves)
        {
            bitboard.UpdateBitBoard(move);
            
            bool kingInCheck = evaluation.IsInCheck(isWhite, 
                                                bitboard.returnWhitePiecesByTypes(), 
                                                bitboard.returnBlackPiecesByTypes(), 
                                                bitboard.returnAllPieces());
            
            bitboard.UndoBitboard();
            
            if (!kingInCheck)
            {
                legalMoves.Add(move);
            }
        }
        
        // If no legal moves available, return 0 (checkmate or stalemate)
        if (legalMoves.Count == 0)
        {
            bool inCheck = evaluation.IsInCheck(isWhite, 
                                            bitboard.returnWhitePiecesByTypes(), 
                                            bitboard.returnBlackPiecesByTypes(), 
                                            bitboard.returnAllPieces());
            Debug.Log($"No legal moves: {(inCheck ? "Checkmate" : "Stalemate")}");
            return 0;
        }
        
        // Iterative Deepening
        int bestMove = 0;
        int bestValue = -INFINITY;
        
        // Check transposition table for a stored best move
        ulong currentHash = ComputeHash(isWhite);
        int ttMove = transpositionTable.GetMove(currentHash);
        
        // Perform iterative deepening to improve move ordering
        for (int currentDepth = 1; currentDepth <= depth; currentDepth++)
        {
            int alpha = -INFINITY;
            int beta = INFINITY;
            
            // Order moves based on previous iterations
            OrderMoves(legalMoves, ttMove, 0);
            
            int iterationBestMove = 0;
            int iterationBestValue = -INFINITY;
            
            for (int i = 0; i < legalMoves.Count; i++)
            {
                int move = legalMoves[i];
                
                // Make the move
                bitboard.UpdateBitBoard(move);
                
                // Update position history for this move
                ulong newHash = ComputeHash(!isWhite);
                if (positionHistory.ContainsKey(newHash))
                    positionHistory[newHash]++;
                else
                    positionHistory[newHash] = 1;
                
                // Get the value for this move
                int value = -Negamax(currentDepth - 1, -beta, -alpha, !isWhite, move, 1);
                
                // Restore position history
                positionHistory[newHash]--;
                if (positionHistory[newHash] == 0)
                    positionHistory.Remove(newHash);
                
                // Undo the move
                bitboard.UndoBitboard();
                
                if (value > iterationBestValue)
                {
                    iterationBestValue = value;
                    iterationBestMove = move;
                    
                    if (value > alpha)
                    {
                        alpha = value;
                        
                        // Log progress for deeper iterations
                        if (currentDepth >= 3)
                        {
                            Debug.Log($"Depth {currentDepth}, new best: {BitboardUtils.IndexToAlgebraic(MoveUtil.GetSource(move))}-{BitboardUtils.IndexToAlgebraic(MoveUtil.GetDestination(move))}, score: {value}");
                        }
                    }
                }
            }
            
            if (iterationBestMove != 0)
            {
                bestValue = iterationBestValue;
                bestMove = iterationBestMove;
                ttMove = bestMove; // Update for next iteration
                
                // Store in transposition table 
                transpositionTable.Store(currentHash, currentDepth, TranspositionTable.TT_EXACT, bestValue, bestMove);
            }
            
            Debug.Log($"Depth {currentDepth} complete: Best move {BitboardUtils.IndexToAlgebraic(MoveUtil.GetSource(bestMove))}-{BitboardUtils.IndexToAlgebraic(MoveUtil.GetDestination(bestMove))}, score: {bestValue}");
        }
        
        // Update game position history with the best move
        if (bestMove != 0)
        {
            bitboard.UpdateBitBoard(bestMove);
            ulong newPositionHash = ComputeHash(!isWhite);
            
            if (gamePositionHistory.ContainsKey(newPositionHash))
                gamePositionHistory[newPositionHash]++;
            else
                gamePositionHistory[newPositionHash] = 1;
                
            bitboard.UndoBitboard();
            
            
        }
        
        // Report search statistics
        Debug.Log($"Search completed: {nodesSearched} regular nodes, {qNodesSearched} quiescence nodes");
        
        return bestMove;
    }

    private int Negamax(int depth, int alpha, int beta, bool isWhite, int previousMove, int ply)
    {
        // Increment node counter
        nodesSearched++;
        
        // Generate hash for current position
        ulong positionHash = ComputeHash(isWhite);
        
        // Check for threefold repetition
        if (positionHistory.TryGetValue(positionHash, out int count) && count >= 2)
        {
            // This would be the third occurrence - return draw score
            return 0;
        }
        
        // Check transposition table for this position
        int ttMove = 0;
        if (transpositionTable.Probe(positionHash, depth, alpha, beta, out int ttValue, out ttMove))
        {
            return ttValue;
        }
        
        // Check if king is in check - needed for check extension
        bool inCheck = evaluation.IsInCheck(isWhite, 
                                        bitboard.returnWhitePiecesByTypes(), 
                                        bitboard.returnBlackPiecesByTypes(), 
                                        bitboard.returnAllPieces());
        
        // Check extension - search one level deeper if in check
        if (inCheck && depth > 0)
            depth += 1;
            
        // if we've reached maximum depth, use quiescence search
        if (depth <= 0) 
        {
            return QuiescenceSearch(alpha, beta, isWhite, previousMove, ply);
        }
        
        // Null Move Pruning (skip if in check or at low depths)
        if (depth >= 3 && !inCheck && HasNonPawnMaterial(isWhite))
        {
            int R = 2 + depth/4;  // Adaptive reduction
            
            // Try a reduced depth search after passing the turn
            int nullValue = -Negamax(depth - 1 - R, -beta, -beta + 1, !isWhite, 0, ply);
            
            // If this reduced search still exceeds beta, we can prune
            if (nullValue >= beta)
                return beta;
        }

        // Get all possible moves
        List<int> possibleMoves = findMoves.GetAllPossibleMoves(isWhite, previousMove);
        List<int> legalMoves = new List<int>();
        
        // Filter out moves that would leave the king in check
        foreach (int move in possibleMoves)
        {
            bitboard.UpdateBitBoard(move);
            
            bool kingInCheck = evaluation.IsInCheck(isWhite, 
                                                bitboard.returnWhitePiecesByTypes(), 
                                                bitboard.returnBlackPiecesByTypes(), 
                                                bitboard.returnAllPieces());
            
            bitboard.UndoBitboard();
            
            if (!kingInCheck)
            {
                legalMoves.Add(move);
            }
        }

        // If no legal moves are available, it's either checkmate or stalemate
        if (legalMoves.Count == 0)
        {
            if (inCheck)
                return -MATE_SCORE + ply; // Checkmate (adjust by ply for shortest mate)
            return 0; // Stalemate
        }
        
        // Order moves using Stockfish approach
        OrderMoves(legalMoves, ttMove, ply);
        
        int bestValue = -INFINITY;
        int bestMove = 0;
        int nodeType = TranspositionTable.TT_ALPHA;
        
        // Search loop
        for (int i = 0; i < legalMoves.Count; i++)
        {
            int move = legalMoves[i];
            
            // Make the move
            bitboard.UpdateBitBoard(move);
            
            // Track position for repetition detection
            ulong newPositionHash = ComputeHash(!isWhite);
            if (positionHistory.ContainsKey(newPositionHash))
                positionHistory[newPositionHash]++;
            else
                positionHistory[newPositionHash] = 1;
            
            int value;
            
            // Late Move Reduction for non-tactical moves after searching several moves
            if (depth >= 3 && i >= 3 && !inCheck && !IsMoveCapture(move) && !MoveUtil.IsPromotion(move))
            {
                // Try a reduced depth search first
                int reduction = 1 + (i >= 6 ? 1 : 0);
                value = -Negamax(depth - 1 - reduction, -alpha - 1, -alpha, !isWhite, move, ply + 1);
                
                // If the reduced search looks promising, do a full search
                if (value > alpha)
                {
                    value = -Negamax(depth - 1, -beta, -alpha, !isWhite, move, ply + 1);
                }
            }
            else
            {
                // Full-depth search
                value = -Negamax(depth - 1, -beta, -alpha, !isWhite, move, ply + 1);
            }
            
            // Restore position history before undoing the move
            positionHistory[newPositionHash]--;
            if (positionHistory[newPositionHash] == 0)
                positionHistory.Remove(newPositionHash);
            
            // Undo the move
            bitboard.UndoBitboard();
            
            // Update best value and move
            if (value > bestValue)
            {
                bestValue = value;
                bestMove = move;
                
                // Update alpha if improved
                if (value > alpha)
                {
                    alpha = value;
                    nodeType = TranspositionTable.TT_EXACT;
                    
                    // Alpha-beta pruning
                    if (alpha >= beta)
                    {
                        nodeType = TranspositionTable.TT_BETA;
                        
                        // Update killer moves and history table for quiet moves
                        if (!IsMoveCapture(move) && !MoveUtil.IsPromotion(move))
                        {
                            // Update killer moves (moves that caused a beta cutoff)
                            if (!MovesEqual(move, killerMoves[0, ply]))
                            {
                                killerMoves[1, ply] = killerMoves[0, ply];
                                killerMoves[0, ply] = move;
                            }
                            
                            // Update history heuristic with depth-squared bonus
                            bool isWhiteMove = MoveUtil.IsWhite(move);
                            int source = MoveUtil.GetSource(move);
                            int dest = MoveUtil.GetDestination(move);
                            historyTable[isWhiteMove ? 0 : 1, source * 64 + dest] += depth * depth;
                        }
                        
                        break; // Beta cutoff
                    }
                }
            }
        }

        // Store position in transposition table
        transpositionTable.Store(positionHash, depth, nodeType, bestValue, bestMove);

        return bestValue;
    }
    
    // Quiescence search to resolve tactical positions
    private int QuiescenceSearch(int alpha, int beta, bool isWhite, int previousMove, int ply)
    {
        // Increment quiescence node counter
        qNodesSearched++;
        
        // Check maximum depth
        if (ply >= MAX_QUIESCENCE_DEPTH)
            return EvaluatePosition(isWhite);
        
        // Generate hash for current position
        ulong positionHash = ComputeHash(isWhite);
        
        // Check for threefold repetition in quiescence search too
        if (positionHistory.TryGetValue(positionHash, out int count) && count >= 2)
        {
            // This would be the third occurrence - return draw score
            return 0;
        }
        
        // First, get the stand-pat score
        int standPat = EvaluatePosition(isWhite);
        
        // Check for early cutoff
        if (standPat >= beta)
            return beta;
            
        // Update alpha if standing pat is better
        if (standPat > alpha)
            alpha = standPat;
            
        // Delta pruning - if even a queen capture can't improve alpha
        if (standPat + 900 + DELTA_MARGIN < alpha)
            return alpha;
            
        // Generate capture moves
        List<int> captureMoves = GenerateCaptureMoves(isWhite, previousMove);
        
        // Sort captures by MVV-LVA
        OrderMoves(captureMoves, 0, ply);
        
        // Search captures
        foreach (int move in captureMoves)
        {
            // Make the capture move
            bitboard.UpdateBitBoard(move);
            
            // Check if move would leave king in check
            bool kingInCheck = evaluation.IsInCheck(isWhite, 
                                                bitboard.returnWhitePiecesByTypes(), 
                                                bitboard.returnBlackPiecesByTypes(), 
                                                bitboard.returnAllPieces());
                                                
            if (kingInCheck) {
                bitboard.UndoBitboard();
                continue; // Skip illegal moves
            }
            
            // Track position for repetition detection
            ulong newPositionHash = ComputeHash(!isWhite);
            if (positionHistory.ContainsKey(newPositionHash))
                positionHistory[newPositionHash]++;
            else
                positionHistory[newPositionHash] = 1;
            
            // Recursive quiescence search
            int score = -QuiescenceSearch(-beta, -alpha, !isWhite, move, ply + 1);
            
            // Restore position history
            positionHistory[newPositionHash]--;
            if (positionHistory[newPositionHash] == 0)
                positionHistory.Remove(newPositionHash);
            
            // Undo the move
            bitboard.UndoBitboard();
            
            // Update alpha if better move found
            if (score > alpha) {
                alpha = score;
                
                // Alpha-beta cutoff
                if (alpha >= beta)
                    return beta;
            }
        }
        
        return alpha;
    }
    
    // Generate only capture moves for quiescence search
    private List<int> GenerateCaptureMoves(bool isWhite, int previousMove)
    {
        List<int> allMoves = findMoves.GetAllPossibleMoves(isWhite, previousMove);
        List<int> captureMoves = new List<int>();
        
        foreach (int move in allMoves)
        {
            if (IsMoveCapture(move))
                captureMoves.Add(move);
                
            // Also include pawn promotions
            else if (MoveUtil.IsPromotion(move))
                captureMoves.Add(move);
        }
        
        return captureMoves;
    }

    // Helper methods
    private bool IsMoveCapture(int move)
    {
        if (move == 0) return false;
        
        int dest = MoveUtil.GetDestination(move);
        ulong destBit = 1UL << dest;
        
        if (MoveUtil.IsWhite(move))
        {
            // For white, see if there's a black piece at the destination
            return (destBit & bitboard.returnAllBlackPieces()) != 0 || MoveUtil.IsEnPassant(move);
        }
        else
        {
            // For black, see if there's a white piece at the destination
            return (destBit & bitboard.returnAllWhitePieces()) != 0 || MoveUtil.IsEnPassant(move);
        }
    }

    private bool MovesEqual(int a, int b)
    {
        if (a == 0 || b == 0) return false;
        
        return MoveUtil.GetSource(a) == MoveUtil.GetSource(b) && 
            MoveUtil.GetDestination(a) == MoveUtil.GetDestination(b) && 
            MoveUtil.IsWhite(a) == MoveUtil.IsWhite(b);
    }

    // Check if side has non-pawn material (needed for null move pruning)
    private bool HasNonPawnMaterial(bool isWhite)
    {
        if (isWhite)
            return (bitboard.WhiteKnight | bitboard.WhiteBishop | bitboard.WhiteRook | bitboard.WhiteQueen) != 0;
        else
            return (bitboard.BlackKnight | bitboard.BlackBishop | bitboard.BlackRook | bitboard.BlackQueen) != 0;
    }
    private int EvaluatePosition(bool isWhite)
    {
        return evaluation.EvaluatePosition(
            bitboard.WhitePawn, bitboard.WhiteKnight, bitboard.WhiteBishop, 
            bitboard.WhiteRook, bitboard.WhiteQueen, bitboard.WhiteKing,
            bitboard.BlackPawn, bitboard.BlackKnight, bitboard.BlackBishop, 
            bitboard.BlackRook, bitboard.BlackQueen, bitboard.BlackKing, 
            isWhite);
    }
    
    private string IndexToAlgebraic(int index)
    {
        int file = index % 8;
        int rank = index / 8;
        return $"{(char)('a' + file)}{rank + 1}";
    }
    
    // For C# in Unity
    private static class BitOperations
    {
        public static int TrailingZeroCount(ulong value)
        {
            if (value == 0) return 64;
            
            int count = 0;
            while ((value & 1) == 0)
            {
                count++;
                value >>= 1;
            }
            return count;
        }
    }
    

    public class TranspositionTable
    {
        // Constants for entry types
        public const int TT_EXACT = 0;  // Exact score
        public const int TT_ALPHA = 1;  // Upper bound
        public const int TT_BETA = 2;   // Lower bound
        
        // Size should be a power of 2 for efficient indexing with bit operations
        #if UNITY_WEBGL
            private const int TT_SIZE = 0x200000; // 2 million for WebGL
        #else
            private const int TT_SIZE = 0x1000000; // 16 million for desktop
        #endif
        private const int TT_MASK = TT_SIZE - 1; // For efficient modulo with bitwise AND
        
        // Structure for table entries
        public struct TTEntry
        {
            public ulong Key;        // Zobrist hash for verification
            public short Depth;      // Search depth
            public byte Flag;        // Entry type (EXACT, ALPHA, BETA)
            public byte Age;         // For replacement strategy
            public int Value;        // Evaluation score
            public int Move;         // Integer move representation
        }
        
        // Main table storage
        private TTEntry[] table;
        
        // Current age for replacement strategy
        private byte currentAge;
        
        // Statistics
        private long probes;
        private long hits;
        
        // Constructor
        public TranspositionTable()
        {
            table = new TTEntry[TT_SIZE];
            currentAge = 0;
            ResetStats();
        }
        
        // Clear the entire table
        public void Clear()
        {
            Array.Clear(table, 0, table.Length);
        }
        
        // New age for new search
        public void IncrementAge()
        {
            currentAge = (byte)((currentAge + 1) & 0xFF);
        }
        
        // Store an entry with enhanced replacement strategy
        public void Store(ulong key, int depth, int flag, int value, int bestMove)
        {
            int index = (int)(key & TT_MASK);
            ref TTEntry entry = ref table[index];
            
            // Create new entry
            TTEntry newEntry = new TTEntry
            {
                Key = key,
                Depth = (short)depth,
                Flag = (byte)flag,
                Age = currentAge,
                Value = value,
                Move = bestMove
            };
            
            // Replacement strategy
            if (depth > entry.Depth || currentAge != entry.Age || key == entry.Key)
            {
                entry = newEntry;
            }
        }
        
        // Probe the table
        public bool Probe(ulong key, int depth, int alpha, int beta, out int value, out int bestMove)
        {
            probes++;
            int index = (int)(key & TT_MASK);
            ref TTEntry entry = ref table[index];
            
            // Check if entry is valid for this position
            if (entry.Key == key)
            {
                bestMove = entry.Move;
                
                // Only use the score if depth is sufficient
                if (entry.Depth >= depth)
                {
                    hits++;
                    value = entry.Value;
                    
                    // Check if we can return immediately based on the bound type
                    switch (entry.Flag)
                    {
                        case TT_EXACT:
                            return true;
                        case TT_ALPHA:
                            if (value <= alpha)
                            {
                                value = alpha;
                                return true;
                            }
                            break;
                        case TT_BETA:
                            if (value >= beta)
                            {
                                value = beta;
                                return true;
                            }
                            break;
                    }
                }
                else
                {
                    // Entry exists but depth is insufficient
                    // Still return the move for move ordering
                    value = 0;
                    return false;
                }
            }
            
            // No useful entry found
            value = 0;
            bestMove = 0;
            return false;
        }
        
        // Get a move for move ordering, regardless of depth
        public int GetMove(ulong key)
        {
            int index = (int)(key & TT_MASK);
            ref TTEntry entry = ref table[index];
            
            if (entry.Key == key && entry.Move != 0)
            {
                return entry.Move;
            }
            
            return 0;
        }
        
        // Stats for debugging and tuning
        public void ResetStats()
        {
            probes = 0;
            hits = 0;
        }
        
        // Estimate memory usage
        public long GetMemoryUsage()
        {
            // Size of the TTEntry struct * number of entries
            return TT_SIZE * (sizeof(ulong) + sizeof(short) + sizeof(byte) * 2 + sizeof(int) * 2);
        }
    }
}