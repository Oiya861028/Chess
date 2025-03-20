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
    
    // Fixed-size transposition table parameters
    private const int TT_SIZE = 1024 * 1024 * 4; // 4M entries (power of 2)
    private const int TT_MASK = TT_SIZE - 1;     // Bit mask for quick indexing
    
    // Quiescence search parameters
    private const int MAX_QUIESCENCE_DEPTH = 4; // Limit how deep quiescence can go
    private const int DELTA_MARGIN = 200; // Delta pruning margin
    
    // Null Move Pruning parameters
    private const int NULL_MOVE_REDUCTION = 3; // R value in R=3 null move pruning
    private const int NULL_MOVE_MIN_DEPTH = 3; // Minimum depth for null move pruning
    
    // Simple move ordering constants - piece values for MVV-LVA
    private readonly int[] PieceValues = { 0, 100, 320, 330, 500, 900, 20000 }; // None, Pawn, Knight, Bishop, Rook, Queen, King
    
    // Optimized transposition table
    private TTEntry[] transpositionTable;
    private readonly ulong[,,] zobristKeys; // [piece][color][square]
    private ulong sideToMoveKey;
    
    // Node statistics for debugging/tuning
    private int nodesSearched = 0;
    private int qNodesSearched = 0;
    private int nullMoveCutoffs = 0;
    private int ttHits = 0;
    private int ttCollisions = 0;
    
    // Transposition table entry struct (more memory efficient than class)
    private struct TTEntry
    {
        public ulong Key;         // Full Zobrist hash (for collision detection)
        public short Depth;       // Search depth
        public byte Flag;         // Node type (EXACT, ALPHA, BETA)
        public int Value;         // Evaluation score
        public ushort BestMoveFrom;  // Best move source square
        public ushort BestMoveTo;    // Best move destination square
        public byte BestMoveIsWhite; // Flag for move color
        public byte Age;          // For aging entries across searches
    }

    public StockFridge(FindMoves findMoves, Bitboard bitboard)
    {
        this.findMoves = findMoves;
        this.bitboard = bitboard;
        this.evaluation = new Evaluation(); // Initialize the evaluation
        
        // Initialize transposition table (fixed size array)
        this.transpositionTable = new TTEntry[TT_SIZE];
        
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
        
        Debug.Log($"Initialized transposition table with {TT_SIZE} entries ({TT_SIZE * sizeof(ulong) / (1024 * 1024)} MB)");
    }
    
    // Clear the transposition table (reset entries)
    private void ClearTranspositionTable()
    {
        Array.Clear(transpositionTable, 0, transpositionTable.Length);
        Debug.Log("Transposition table cleared");
    }
    
    // Age the transposition table (increment age of all entries)
    private void AgeTranspositionTable()
    {
        byte currentAge = 0;
        for (int i = 0; i < TT_SIZE; i++)
        {
            if (transpositionTable[i].Key != 0) // If entry exists
            {
                currentAge = transpositionTable[i].Age;
                if (currentAge < 255) // Avoid overflow
                {
                    transpositionTable[i].Age = (byte)(currentAge + 1);
                }
            }
        }
    }
    
    // Store an entry in the transposition table
    private void StoreTranspositionEntry(ulong positionHash, int depth, int flag, int value, Move bestMove)
    {
        // Index into the table using the lower bits of the hash
        int index = (int)(positionHash & TT_MASK);
        
        // Check if we should replace the existing entry
        bool replace = false;
        
        // Always replace if empty or if same position
        if (transpositionTable[index].Key == 0 || transpositionTable[index].Key == positionHash)
        {
            replace = true;
        }
        else
        {
            // Replacement strategy: prefer deeper searches or older entries
            if (transpositionTable[index].Depth <= depth || transpositionTable[index].Age > 1)
            {
                replace = true;
                ttCollisions++;
            }
        }
        
        if (replace)
        {
            TTEntry entry = new TTEntry
            {
                Key = positionHash,
                Depth = (short)depth,
                Flag = (byte)flag,
                Value = value,
                Age = 0 // New entry
            };
            
            // Store best move if available
            if (bestMove != null)
            {
                entry.BestMoveFrom = (ushort)bestMove.Source;
                entry.BestMoveTo = (ushort)bestMove.Destination;
                entry.BestMoveIsWhite = (byte)(bestMove.IsWhite ? 1 : 0);
            }
            
            transpositionTable[index] = entry;
        }
    }
    
    // Probe the transposition table for a position
    private bool ProbeTranspositionTable(ulong positionHash, int depth, ref int value, ref int flag, ref Move bestMove)
    {
        // Index into the table using the lower bits of the hash
        int index = (int)(positionHash & TT_MASK);
        
        TTEntry entry = transpositionTable[index];
        
        // Check if this entry is for the current position
        if (entry.Key == positionHash)
        {
            // Found a valid entry
            ttHits++;
            
            // Skip if entry is from a shallower search
            if (entry.Depth < depth)
            {
                // Only use the move, not the score
                if (entry.BestMoveFrom != 0 || entry.BestMoveTo != 0)
                {
                    bestMove = new Move(
                        (int)entry.BestMoveFrom, 
                        (int)entry.BestMoveTo, 
                        null, // No previous move info
                        0,    // No piece type info (will be determined when needed)
                        entry.BestMoveIsWhite == 1);
                }
                return false;
            }
            
            // Return the stored value and flag
            value = entry.Value;
            flag = entry.Flag;
            
            // Create best move if available
            if (entry.BestMoveFrom != 0 || entry.BestMoveTo != 0)
            {
                bestMove = new Move(
                    (int)entry.BestMoveFrom, 
                    (int)entry.BestMoveTo, 
                    null, // No previous move info
                    0,    // No piece type info (will be determined when needed)
                    entry.BestMoveIsWhite == 1);
            }
            
            return true;
        }
        
        return false;
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
        
        // Reset node counters
        nodesSearched = 0;
        qNodesSearched = 0;
        nullMoveCutoffs = 0;
        ttHits = 0;
        ttCollisions = 0;
        
        // Age transposition table entries
        AgeTranspositionTable();
        
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
        int ttValue = 0;
        int ttFlag = 0;
        Move ttMove = null;
        
        if (ProbeTranspositionTable(currentHash, depth, ref ttValue, ref ttFlag, ref ttMove))
        {
            // If we have an exact score from a deep enough search, use it
            if (ttFlag == TT_EXACT && ttMove != null)
            {
                // Verify the move is still legal
                foreach (Move move in legalMoves)
                {
                    if (MovesEqual(move, ttMove))
                    {
                        Debug.Log("Using cached best move from transposition table");
                        return move;
                    }
                }
            }
        }
        
        // Sort moves based on simple heuristics (captures score higher)
        SortMoves(legalMoves, ttMove);
        
        // For the root node, use a simple alpha-beta search for each move
        foreach (Move move in legalMoves)
        {
            // Make the move
            bitboard.UpdateBitBoard(move);
            
            // Get the value for this move
            int value = -Negamax(depth - 1, -INFINITY, INFINITY, !isWhite, move, false);
            
            // Undo the move
            bitboard.UndoBitboard();
            
            if (value > bestValue)
            {
                bestValue = value;
                bestMove = move;
            }
        }
        
        // Store the result in the transposition table
        StoreTranspositionEntry(currentHash, depth, TT_EXACT, bestValue, bestMove);
        
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
        
        // Report nodes searched for performance monitoring
        Debug.Log($"Search completed: {nodesSearched} regular nodes, {qNodesSearched} quiescence nodes");
        Debug.Log($"TT stats: {ttHits} hits, {ttCollisions} collisions, usage: {((ttHits + ttCollisions) * 100.0 / TT_SIZE):F2}%");
        Debug.Log($"Null move cutoffs: {nullMoveCutoffs}");
        
        return bestMove;
    }

    private int Negamax(int depth, int alpha, int beta, bool isWhite, Move previousMove, bool isNullMove)
    {
        // Increment node counter
        nodesSearched++;
        
        // Generate hash for current position
        ulong positionHash = ComputeHash(isWhite);
        
        // Check transposition table for this position
        int ttValue = 0;
        int ttFlag = 0;
        Move ttMove = null;
        
        if (ProbeTranspositionTable(positionHash, depth, ref ttValue, ref ttFlag, ref ttMove))
        {
            if (ttFlag == TT_EXACT)
                return ttValue;
            else if (ttFlag == TT_ALPHA && ttValue <= alpha)
                return alpha;
            else if (ttFlag == TT_BETA && ttValue >= beta)
                return beta;
        }
        
        // Check if king is in check - needed for check extension and null move pruning
        bool inCheck = evaluation.IsInCheck(isWhite, 
                                        bitboard.returnWhitePiecesByTypes(), 
                                        bitboard.returnBlackPiecesByTypes(), 
                                        bitboard.returnAllPieces());
        
        // Check extension - search one level deeper if in check
        if (inCheck)
            depth += 1;
            
        // Base case: if we've reached maximum depth, use quiescence search
        if (depth <= 0) 
        {
            // Call quiescence search to resolve captures
            return QuiescenceSearch(alpha, beta, isWhite, previousMove);
        }
        
        // Null Move Pruning
        // Only do this if:
        // 1. Not in check
        // 2. Not already in a null move (to avoid consecutive null moves)
        // 3. At sufficient depth
        // 4. Position has enough material (have at least some non-pawn pieces)
        // 5. NOT in a pawn endgame (to avoid zugzwang)
        if (depth >= NULL_MOVE_MIN_DEPTH && !inCheck && !isNullMove && 
            HasNonPawnMaterial(isWhite) && !IsPawnEndgame())
        {
            // Make a "null move" - skip our turn
            // No actual move is made on the bitboard, we just switch sides
            
            // Try a reduced depth search with a null move
            int nullValue = -Negamax(depth - 1 - NULL_MOVE_REDUCTION, -beta, -beta + 1, !isWhite, null, true);
            
            // If the reduced search fails high, we can prune this branch
            if (nullValue >= beta)
            {
                nullMoveCutoffs++;
                // Store a lower bound in the transposition table
                StoreTranspositionEntry(positionHash, depth, TT_BETA, beta, null);
                return beta; // Return a lower bound on the score
            }
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
            if (inCheck)
            {
                return -INFINITY + nodesSearched; // Checkmate (add distance to mate)
            }
            return 0; // Stalemate
        }
        
        // Sort moves based on simple heuristics
        SortMoves(legalMoves, ttMove);
        
        int bestValue = -INFINITY;
        Move bestMove = null;
        int nodeType = TT_ALPHA;
        
        foreach (Move move in legalMoves)
        {
            // Make the move
            bitboard.UpdateBitBoard(move);
            
            // Recursively evaluate position
            int value = -Negamax(depth - 1, -beta, -alpha, !isWhite, move, false);
            
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
        StoreTranspositionEntry(positionHash, depth, nodeType, bestValue, bestMove);

        return bestValue;
    }
    
    // Check if we're in a pawn endgame (kings + pawns only)
    private bool IsPawnEndgame()
    {
        // If we only have kings and pawns, it's a pawn endgame
        return ((bitboard.WhiteKnight | bitboard.WhiteBishop | 
                bitboard.WhiteRook | bitboard.WhiteQueen |
                bitboard.BlackKnight | bitboard.BlackBishop |
                bitboard.BlackRook | bitboard.BlackQueen) == 0);
    }
    
    // Helper function to count bits in a ulong
    private int PopCount(ulong x)
    {
        int count = 0;
        while (x != 0)
        {
            count++;
            x &= x - 1; // Clear the least significant bit
        }
        return count;
    }
    
    // Helper function to check if side has enough material for null move pruning
    private bool HasNonPawnMaterial(bool isWhite)
    {
        if (isWhite)
        {
            // Check if white has any pieces other than pawns and king
            return (bitboard.WhiteKnight | bitboard.WhiteBishop | 
                   bitboard.WhiteRook | bitboard.WhiteQueen) != 0;
        }
        else
        {
            // Check if black has any pieces other than pawns and king
            return (bitboard.BlackKnight | bitboard.BlackBishop | 
                   bitboard.BlackRook | bitboard.BlackQueen) != 0;
        }
    }
    
    // Quiescence Search - resolve tactical sequences at leaf nodes
    private int QuiescenceSearch(int alpha, int beta, bool isWhite, Move previousMove, int qdepth = 0)
    {
        // Increment quiescence node counter
        qNodesSearched++;
        
        // First, get the stand-pat score (evaluate current position)
        int standPat = EvaluatePosition(isWhite);
        
        // Check for early cutoff
        if (standPat >= beta)
            return beta;
            
        // Update alpha if standing pat is better than current alpha
        if (standPat > alpha)
            alpha = standPat;
            
        // Limit quiescence search depth to avoid excessive searching
        if (qdepth >= MAX_QUIESCENCE_DEPTH)
            return standPat;
            
        // Get only capture moves
        List<Move> captureMoves = GenerateCaptureMoves(isWhite, previousMove);
        
        // Sort captures by MVV-LVA
        SortMoves(captureMoves, null);
        
        foreach (Move move in captureMoves)
        {
            // Delta pruning - skip if even the best capture + margin can't improve alpha
            int captureValue = EstimateCaptureValue(move);
            if (standPat + captureValue + DELTA_MARGIN <= alpha)
                continue;
                
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
            
            // Recursive quiescence search
            int score = -QuiescenceSearch(-beta, -alpha, !isWhite, move, qdepth + 1);
            
            // Undo the move
            bitboard.UndoBitboard();
            
            // Update alpha if a better move is found
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
    private List<Move> GenerateCaptureMoves(bool isWhite, Move previousMove)
    {
        List<Move> allMoves = findMoves.GetAllPossibleMoves(isWhite, previousMove);
        List<Move> captureMoves = new List<Move>();
        
        foreach (Move move in allMoves)
        {
            if (IsMoveCapture(move))
                captureMoves.Add(move);
                
            // Also include pawn promotions which are tactically important
            else if (move.IsPromotion)
                captureMoves.Add(move);
        }
        
        return captureMoves;
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
            // Priority for promotions
            else if (move.IsPromotion)
            {
                score = 900; // Value close to a queen capture
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
            // For white, see if there's a white piece at the destination
            return (destBit & bitboard.returnAllWhitePieces()) != 0;
        }
    }
    
    // Simple estimation of capture value based on piece types
    private int EstimateCaptureValue(Move move)
    {
        int score = 0;
        ulong destBit = 1UL << move.Destination;
        
        // Determine captured piece value (MVV - Most Valuable Victim)
        if (move.IsWhite)
        {
            // Check which black piece is being captured
            if ((destBit & bitboard.BlackPawn) != 0) score = PieceValues[1];
            else if ((destBit & bitboard.BlackKnight) != 0) score = PieceValues[2];
            else if ((destBit & bitboard.BlackBishop) != 0) score = PieceValues[3];
            else if ((destBit & bitboard.BlackRook) != 0) score = PieceValues[4];
            else if ((destBit & bitboard.BlackQueen) != 0) score = PieceValues[5];
        }
        else
        {
            // Check which white piece is being captured
            if ((destBit & bitboard.WhitePawn) != 0) score = PieceValues[1];
            else if ((destBit & bitboard.WhiteKnight) != 0) score = PieceValues[2];
            else if ((destBit & bitboard.WhiteBishop) != 0) score = PieceValues[3];
            else if ((destBit & bitboard.WhiteRook) != 0) score = PieceValues[4];
            else if ((destBit & bitboard.WhiteQueen) != 0) score = PieceValues[5];
        }
        
        // LVA (Least Valuable Attacker) - subtract a value based on capturing piece type
        score = score * 10 - move.PieceType;
        
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