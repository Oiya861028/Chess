using System;
using System.Collections.Generic;
using UnityEngine;
public class StockFridge
{
    private FindMoves findMoves;
    private Evaluation evaluation;
    private Bitboard bitboard;
    private const int INFINITY = 9999999;
    
    // Transposition table constants
    private const int TT_EXACT = 0;  // Exact score
    private const int TT_ALPHA = 1;  // Upper bound
    private const int TT_BETA = 2;   // Lower bound
    private const int MAX_TT_SIZE = 1000000; // Maximum table size
    
    // Simple move ordering constants - piece values for MVV-LVA
    private readonly int[] PieceValues = { 0, 100, 320, 330, 500, 900, 20000 }; // None, Pawn, Knight, Bishop, Rook, Queen, King
    
    // Transposition table and related fields
    private Dictionary<ulong, TTEntry> transpositionTable;
    private readonly ulong[,,] zobristKeys; // [piece][color][square]
    private ulong sideToMoveKey;
    
    // Transposition table entry class
    private class TTEntry
    {
        public int Depth;         // Search depth
        public int Flag;          // Node type (EXACT, ALPHA, BETA)
        public int Value;         // Evaluation score
        public Move BestMove;     // Best move from this position
        
        public TTEntry(int depth, int flag, int value, Move bestMove)
        {
            Depth = depth;
            Flag = flag;
            Value = value;
            BestMove = bestMove;
        }
    }

    public StockFridge(FindMoves findMoves, Bitboard bitboard)
    {
        this.findMoves = findMoves;
        this.bitboard = bitboard;
        this.evaluation = new Evaluation(); // Initialize the evaluation
        this.transpositionTable = new Dictionary<ulong, TTEntry>();
        
        // Initialize Zobrist keys for hashing
        // Use explicit System.Random to avoid ambiguity with UnityEngine.Random
        System.Random rand = new System.Random(123456); // Fixed seed for reproducibility
        zobristKeys = new ulong[7,2,64]; // 7 piece types, 2 colors, 64 squares
        
        // Generate random numbers for each piece/square combination
        for (int piece = 0; piece < 7; piece++)
            for (int color = 0; color < 2; color++)
                for (int square = 0; square < 64; square++)
                    zobristKeys[piece,color,square] = ((ulong)rand.Next() << 32) | (ulong)rand.Next();
        
        // Generate key for side to move
        sideToMoveKey = ((ulong)rand.Next() << 32) | (ulong)rand.Next();
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
    
    // The main function that will be called to get the best move for the player
    public Move GetBestMove(int depth, bool isWhite, Move previousMove)
    {
        Move bestMove = null;
        int bestValue = -INFINITY;
        
        // Clear TT periodically to prevent stale entries accumulating over multiple games
        if (transpositionTable.Count > MAX_TT_SIZE)
        {
            Debug.Log($"Transposition table exceeded size limit ({MAX_TT_SIZE}), clearing...");
            transpositionTable.Clear();
        }
        
        // Get all possible moves for the current position
        List<Move> possibleMoves = findMoves.GetAllPossibleMoves(isWhite, previousMove);
        // Add debugging to verify move color
        Debug.Log($"AI thinking as {(isWhite ? "white" : "black")}, generated {possibleMoves.Count} possible moves");
        foreach (var move in possibleMoves)
        {
            if (move.IsWhite != isWhite)
            {
                Debug.LogError($"Color mismatch in generated move: Move says {(move.IsWhite ? "white" : "black")} but AI is {(isWhite ? "white" : "black")}");
                // Skip this move
                continue;
            }
        }
        
        List<Move> legalMoves = new List<Move>();
        // Filter out moves that would leave the king in check
        foreach (Move move in possibleMoves)
        {
            // Skip moves with the wrong color
            if (move.IsWhite != isWhite)
            {
                continue;
            }
            
            // Make the move on a temporary copy of the bitboard
            bitboard.UpdateBitBoard(move);
            
            // Check if the king is in check after the move
            bool kingInCheck = evaluation.IsInCheck(isWhite, 
                                                bitboard.returnWhitePiecesByTypes(), 
                                                bitboard.returnBlackPiecesByTypes(), 
                                                bitboard.returnAllPieces());
            
            // Undo the move
            bitboard.UndoBitboard();
            
            // If move doesn't leave king in check, it's legal
            if (!kingInCheck)
            {
                legalMoves.Add(move);
            }
        }
        
        // Additional debug
        Debug.Log($"AI found {legalMoves.Count} legal moves after check validation");
        
        // If no legal moves available, return null (checkmate or stalemate)
        if (legalMoves.Count == 0)
        {
            return null;
        }
        
        // Check transposition table for a stored best move
        ulong currentHash = ComputeHash(isWhite);
        if (transpositionTable.TryGetValue(currentHash, out TTEntry ttEntry) && 
            ttEntry.BestMove != null && ttEntry.Depth >= depth)
        {
            // Verify the move is still legal
            foreach (Move move in legalMoves)
            {
                if (MovesEqual(move, ttEntry.BestMove))
                {
                    Debug.Log("Using cached best move from transposition table");
                    return move;
                }
            }
        }
        
        // Sort moves based on simple heuristics (captures score higher)
        SortMoves(legalMoves, ttEntry?.BestMove);
        
        foreach (Move move in legalMoves)
        {
            // Make the move
            bitboard.UpdateBitBoard(move);
            
            // Get the value for this move
            int value = -Negamax(depth - 1, -INFINITY, INFINITY, !isWhite, move);
            
            // Undo the move
            bitboard.UndoBitboard();
            
            if (value > bestValue)
            {
                bestValue = value;
                bestMove = move;
            }
        }
        
        // Store the result in the transposition table
        transpositionTable[currentHash] = new TTEntry(depth, TT_EXACT, bestValue, bestMove);
        
        // Final verification
        if (bestMove != null && bestMove.IsWhite != isWhite)
        {
            Debug.LogError($"AI selected move with wrong color! Move is {(bestMove.IsWhite ? "white" : "black")} but AI is {(isWhite ? "white" : "black")}");
            // Try to find any legal move with the correct color
            foreach (Move move in legalMoves)
            {
                if (move.IsWhite == isWhite)
                {
                    Debug.Log("Replacing with first valid-colored move found");
                    return move;
                }
            }
            return null; // No valid moves found
        }
        
        return bestMove;
    }

    private int Negamax(int depth, int alpha, int beta, bool isWhite, Move previousMove)
    {
        // Generate hash for current position
        ulong positionHash = ComputeHash(isWhite);
        
        // Check transposition table for this position
        if (transpositionTable.TryGetValue(positionHash, out TTEntry ttEntry) && 
            ttEntry.Depth >= depth)
        {
            if (ttEntry.Flag == TT_EXACT)
                return ttEntry.Value;
            else if (ttEntry.Flag == TT_ALPHA && ttEntry.Value <= alpha)
                return alpha;
            else if (ttEntry.Flag == TT_BETA && ttEntry.Value >= beta)
                return beta;
        }
        
        // Base case: if we've reached maximum depth
        if (depth == 0) 
        {
            int eval = EvaluatePosition(isWhite);
            // Store in transposition table
            transpositionTable[positionHash] = new TTEntry(0, TT_EXACT, eval, null);
            return eval;
        }

        // Get all possible moves
        List<Move> possibleMoves = findMoves.GetAllPossibleMoves(isWhite, previousMove);
        List<Move> legalMoves = new List<Move>();
        
        // Filter out moves that would leave the king in check
        foreach (Move move in possibleMoves)
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
            // Check if the king is in check
            if (evaluation.IsInCheck(isWhite, 
                                    bitboard.returnWhitePiecesByTypes(), 
                                    bitboard.returnBlackPiecesByTypes(), 
                                    bitboard.returnAllPieces()))
            {
                return -INFINITY; // Checkmate
            }
            return 0; // Stalemate
        }
        
        // Sort moves based on simple heuristics
        SortMoves(legalMoves, ttEntry?.BestMove);
        
        int bestValue = -INFINITY;
        Move bestMove = null;
        int nodeType = TT_ALPHA;
        
        foreach (Move move in legalMoves)
        {
            // Make the move
            bitboard.UpdateBitBoard(move);
            
            // Recursively evaluate position
            int value = -Negamax(depth - 1, -beta, -alpha, !isWhite, move);
            
            // Undo the move
            bitboard.UndoBitboard();

            if (value > bestValue)
            {
                bestValue = value;
                bestMove = move;
                
                if (value > alpha)
                {
                    alpha = value;
                    nodeType = TT_EXACT;
                    
                    // Alpha-beta pruning
                    if (alpha >= beta)
                    {
                        nodeType = TT_BETA;
                        break;
                    }
                }
            }
        }

        // Store position in transposition table
        transpositionTable[positionHash] = new TTEntry(depth, nodeType, bestValue, bestMove);

        return bestValue;
    }
    
    // Simple move ordering - prioritize captures and TT moves
    private void SortMoves(List<Move> moves, Move ttMove)
    {
        Dictionary<Move, int> moveScores = new Dictionary<Move, int>();
        
        foreach (Move move in moves)
        {
            int score = 0;
            
            // Highest priority: TT move from previous iterations
            if (ttMove != null && MovesEqual(move, ttMove))
            {
                score = 1000000;
            }
            // Next priority: captures
            else if (IsMoveCapture(move))
            {
                // MVV-LVA (Most Valuable Victim - Least Valuable Attacker)
                score = EstimateCaptureValue(move);
            }
            
            moveScores[move] = score;
        }
        
        // Sort moves by score (descending)
        moves.Sort((a, b) => moveScores[b].CompareTo(moveScores[a]));
    }
    
    // Determine if a move is a capture by using the bitboard
    private bool IsMoveCapture(Move move)
    {
        ulong destBit = 1UL << move.Destination;
        
        if (move.IsWhite)
        {
            // For white, see if there's a black piece at the destination
            return (destBit & bitboard.returnAllBlackPieces()) != 0;
        }
        else
        {
            // For black, see if there's a white piece at the destination
            return (destBit & bitboard.returnAllWhitePieces()) != 0;
        }
    }
    
    // Simple estimation of capture value based on piece types
    private int EstimateCaptureValue(Move move)
    {
        int score = 0;
        ulong destBit = 1UL << move.Destination;
        
        // Determine captured piece value
        if (move.IsWhite)
        {
            // Check which black piece is being captured
            if ((destBit & bitboard.BlackPawn) != 0) score = 100;
            else if ((destBit & bitboard.BlackKnight) != 0) score = 300;
            else if ((destBit & bitboard.BlackBishop) != 0) score = 300;
            else if ((destBit & bitboard.BlackRook) != 0) score = 500;
            else if ((destBit & bitboard.BlackQueen) != 0) score = 900;
        }
        else
        {
            // Check which white piece is being captured
            if ((destBit & bitboard.WhitePawn) != 0) score = 100;
            else if ((destBit & bitboard.WhiteKnight) != 0) score = 300;
            else if ((destBit & bitboard.WhiteBishop) != 0) score = 300;
            else if ((destBit & bitboard.WhiteRook) != 0) score = 500;
            else if ((destBit & bitboard.WhiteQueen) != 0) score = 900;
        }
        
        // MVV-LVA (subtract a small value based on capturing piece to prioritize captures with less valuable pieces)
        score -= move.PieceType * 10;
        
        return score;
    }
    
    private int EvaluatePosition(bool isWhite)
    {
        // Use the Evaluation class to evaluate the current position
        return evaluation.EvaluatePosition(
            bitboard.WhitePawn, bitboard.WhiteKnight, bitboard.WhiteBishop, 
            bitboard.WhiteRook, bitboard.WhiteQueen, bitboard.WhiteKing,
            bitboard.BlackPawn, bitboard.BlackKnight, bitboard.BlackBishop, 
            bitboard.BlackRook, bitboard.BlackQueen, bitboard.BlackKing, 
            isWhite);
    }
    
    // Helper method to compare moves - updated to match your Move class
    private bool MovesEqual(Move a, Move b)
    {
        return a.Source == b.Source && 
               a.Destination == b.Destination && 
               a.IsWhite == b.IsWhite;
    }
    
    // For C# in Unity, since System.Numerics.BitOperations might not be available
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
}