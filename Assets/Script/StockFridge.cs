using System;
using System.Collections.Generic;
using UnityEngine;

public class StockFridge
{
    private FindMoves findMoves;
    private Evaluation evaluation;
    private Bitboard bitboard;
    private const int INFINITY = 9999999;
    private const int MATE_SCORE = 9000000; // Score for checkmate (smaller than INFINITY to allow depth adjustments)
    
    // Quiescence search parameters
    private const int MAX_QUIESCENCE_DEPTH = 8; // Increased from 4 to 8
    private const int DELTA_MARGIN = 120; // Reduced from 200 to 120 to be more selective
    
    // Simple move ordering constants - piece values for MVV-LVA
    private readonly int[] PieceValues = { 0, 100, 320, 330, 500, 900, 20000 }; // None, Pawn, Knight, Bishop, Rook, Queen, King
    
    // Transposition table and related fields
    private TranspositionTable transpositionTable;
    private readonly ulong[,,] zobristKeys; // [piece][color][square]
    private ulong sideToMoveKey;
    
    // History heuristic table for quiet move ordering
    private int[,] historyTable = new int[2, 64 * 64]; // [color, from*64+to]
    
    // Killer moves for move ordering
    private const int MAX_DEPTH = 64;
    private Move[,] killerMoves = new Move[2, MAX_DEPTH]; // Two killer moves per ply
    
    // Node statistics for debugging/tuning
    private int nodesSearched = 0;
    private int qNodesSearched = 0;
    
    public StockFridge(FindMoves findMoves, Bitboard bitboard)
    {
        this.findMoves = findMoves;
        this.bitboard = bitboard;
        this.evaluation = new Evaluation(); // Initialize the evaluation
        
        // Initialize the optimized transposition table
        this.transpositionTable = new TranspositionTable();
        
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
        
        Debug.Log($"StockFridge initialized with optimized transposition table. Memory usage: {transpositionTable.GetMemoryUsage() / (1024 * 1024)} MB");
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
        // This is important for correctly identifying positions with different castling possibilities
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
    
    // Helper methods for adjusting mate scores
    private int AdjustScoreForMateDistance(int score, int ply)
    {
        // Store the distance to mate in the score
        if (score > MATE_SCORE - 1000)
            return score - ply; // Winning position - prefer shorter mates
        else if (score < -MATE_SCORE + 1000)
            return score + ply; // Losing position - prefer longer resistance
        return score;
    }

    private int UnadjustScoreFromTT(int score, int ply)
    {
        // Adjust the score back for the current position
        if (score > MATE_SCORE - 1000)
            return score + ply;
        else if (score < -MATE_SCORE + 1000)
            return score - ply;
        return score;
    }
    
    // The main function that will be called to get the best move for the player
    public Move GetBestMove(int depth, bool isWhite, Move previousMove)
    {
        // Reset node counters
        nodesSearched = 0;
        qNodesSearched = 0;
        
        // Increment age for the transposition table
        transpositionTable.IncrementAge();
        transpositionTable.ResetStats();
        
        // Clear killer moves
        Array.Clear(killerMoves, 0, killerMoves.Length);
        
        // Decay history table
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 64 * 64; j++)
                historyTable[i, j] = historyTable[i, j] * 80 / 100; // 80% decay
        
        // Clear TT only occasionally (to prevent old entries from lingering too long)
        if (UnityEngine.Random.value < 0.01f) // 1% chance to clear
        {
            transpositionTable.Clear();
            Debug.Log("Transposition table cleared");
        }
        
        // Get all possible legal moves for the current position
        List<Move> possibleMoves = findMoves.GetAllPossibleMoves(isWhite, previousMove);
        List<Move> legalMoves = new List<Move>();
        
        // Filter out moves that would leave the king in check
        foreach (Move move in possibleMoves)
        {
            // Skip moves with the wrong color
            if (move.IsWhite != isWhite)
            {
                Debug.LogWarning($"Incorrect move color detected: {(move.IsWhite ? "white" : "black")} vs expected {(isWhite ? "white" : "black")}");
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
        
        Debug.Log($"Generated {legalMoves.Count} legal moves for {(isWhite ? "white" : "black")}");
        
        // If no legal moves available, return null (checkmate or stalemate)
        if (legalMoves.Count == 0)
        {
            // Check if in checkmate or stalemate
            bool inCheck = evaluation.IsInCheck(isWhite, 
                                            bitboard.returnWhitePiecesByTypes(), 
                                            bitboard.returnBlackPiecesByTypes(), 
                                            bitboard.returnAllPieces());
            Debug.Log($"No legal moves: {(inCheck ? "Checkmate" : "Stalemate")}");
            return null;
        }
        
        // Check transposition table for a stored best move
        ulong currentHash = ComputeHash(isWhite);
        Move ttMove = transpositionTable.GetMove(currentHash, isWhite, previousMove);
        
        // Verify the ttMove is legal
        if (ttMove != null)
        {
            bool foundTTMove = false;
            foreach (Move move in legalMoves)
            {
                if (move.Source == ttMove.Source && move.Destination == ttMove.Destination)
                {
                    foundTTMove = true;
                    Debug.Log($"Using TT move: {BitboardUtils.IndexToAlgebraic(move.Source)} to {BitboardUtils.IndexToAlgebraic(move.Destination)}");
                    break;
                }
            }
            
            if (!foundTTMove)
            {
                Debug.Log("TT move was not valid in current position");
                ttMove = null;
            }
        }
        
        // Iterative Deepening
        Move bestMove = null;
        int bestValue = -INFINITY;
        
        // Sort moves for initial ordering
        SortMoves(legalMoves, ttMove);
        
        // Perform iterative deepening to improve move ordering and provide anytime behavior
        for (int currentDepth = 1; currentDepth <= depth; currentDepth++)
        {
            int alpha = -INFINITY;
            int beta = INFINITY;
            Move iterationBestMove = null;
            int iterationBestValue = -INFINITY;
            
            for (int i = 0; i < legalMoves.Count; i++)
            {
                Move move = legalMoves[i];
                
                // Make the move
                bitboard.UpdateBitBoard(move);
                
                // Get the value for this move with aspiration window for deeper searches
                int value;
                
                if (currentDepth >= 4 && i > 0)
                {
                    // Use narrower window for non-first moves in deeper searches
                    int delta = 50;
                    value = -Negamax(currentDepth - 1, -alpha - delta, -alpha, !isWhite, move, 1);
                    
                    // If failed high, re-search with full window
                    if (value > alpha)
                        value = -Negamax(currentDepth - 1, -beta, -alpha, !isWhite, move, 1);
                }
                else
                {
                    // Regular full window search
                    value = -Negamax(currentDepth - 1, -beta, -alpha, !isWhite, move, 1);
                }
                
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
                            Debug.Log($"Depth {currentDepth}, new best: {BitboardUtils.IndexToAlgebraic(move.Source)}-{BitboardUtils.IndexToAlgebraic(move.Destination)}, score: {value}");
                        }
                    }
                }
            }
            
            if (iterationBestMove != null)
            {
                bestValue = iterationBestValue;
                bestMove = iterationBestMove;
                
                // Store in transposition table - this is an exact value
                transpositionTable.Store(currentHash, currentDepth, TranspositionTable.TT_EXACT, bestValue, bestMove);
                
                // Reorder moves for next iteration - put best move first
                for (int i = 0; i < legalMoves.Count; i++)
                {
                    if (legalMoves[i].Source == bestMove.Source && legalMoves[i].Destination == bestMove.Destination)
                    {
                        if (i > 0)
                        {
                            Move temp = legalMoves[0];
                            legalMoves[0] = legalMoves[i];
                            legalMoves[i] = temp;
                        }
                        break;
                    }
                }
            }
            
            Debug.Log($"Depth {currentDepth} complete: Best move {BitboardUtils.IndexToAlgebraic(bestMove.Source)}-{BitboardUtils.IndexToAlgebraic(bestMove.Destination)}, score: {bestValue}");
        }
        
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
        
        // Report search statistics
        Debug.Log($"Search completed: {nodesSearched} regular nodes, {qNodesSearched} quiescence nodes");
        Debug.Log($"TT stats: {transpositionTable.GetProbeCount()} probes, {transpositionTable.GetHitRate():P2} hit rate, {transpositionTable.GetFillRate():P2} fill rate");
        
        return bestMove;
    }

    private int Negamax(int depth, int alpha, int beta, bool isWhite, Move previousMove, int ply)
    {
        // Increment node counter
        nodesSearched++;
        
        // Early draw detection (repetition check would be here in a full engine)
        
        // Generate hash for current position
        ulong positionHash = ComputeHash(isWhite);
        
        // Check transposition table for this position
        if (transpositionTable.Probe(positionHash, depth, alpha, beta, out int ttValue, out Move ttMove, isWhite, previousMove))
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
            
        // Base case: if we've reached maximum depth, use quiescence search
        if (depth <= 0) 
        {
            // Call quiescence search to resolve captures
            return QuiescenceSearch(alpha, beta, isWhite, previousMove, ply);
        }
        
        // Null Move Pruning
        // Skip this move and see if we're still above beta (indicates a very good position)
        if (depth >= 3 && !inCheck && HasNonPawnMaterial(isWhite))
        {
            // Perform a reduced depth search after passing the turn
            int R = depth > 6 ? 3 : 2; // Null move reduction
            
            // Skip our turn (not a real bitboard operation in this implementation)
            // In a real engine, we'd have a MakeNullMove method that changes side to move only
            // Here, we just try a recursive call with reduced depth
            int nullValue = -Negamax(depth - 1 - R, -beta, -beta + 1, !isWhite, null, ply);
            
            // If this reduced search still exceeds beta, we can prune this subtree
            if (nullValue >= beta)
                return beta; // Fail-hard beta cutoff
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
                return -MATE_SCORE + ply; // Checkmate (add distance to mate)
            }
            return 0; // Stalemate
        }
        
        // Sort moves based on heuristics
        SortMoves(legalMoves, ttMove, ply);
        
        int bestValue = -INFINITY;
        Move bestMove = null;
        int nodeType = TranspositionTable.TT_ALPHA;
        bool fullDepthSearch = true;
        int movesSearched = 0;
        
        foreach (Move move in legalMoves)
        {
            // Make the move
            bitboard.UpdateBitBoard(move);
            
            int value = 0;
            
            // Late Move Reduction
            if (fullDepthSearch && !inCheck && !IsMoveCapture(move) && depth >= 3 && movesSearched >= 3)
            {
                // Reduced depth search with zero window
                int reduction = movesSearched > 6 ? 2 : 1;
                value = -Negamax(depth - 1 - reduction, -alpha - 1, -alpha, !isWhite, move, ply + 1);
                fullDepthSearch = (value > alpha); // Only do full search if reduced search looks promising
            }
            else
            {
                fullDepthSearch = true;
            }
            
            // If LMR didn't fail low or didn't apply, do a full-depth zero-window search
            if (fullDepthSearch)
            {
                // Principal Variation Search
                if (movesSearched > 0)
                {
                    // Try minimal window search first (assuming this is not the best move)
                    value = -Negamax(depth - 1, -alpha - 1, -alpha, !isWhite, move, ply + 1);
                    
                    // If this move might be better than our best so far, do a full re-search
                    if (value > alpha && value < beta)
                        value = -Negamax(depth - 1, -beta, -alpha, !isWhite, move, ply + 1);
                }
                else
                {
                    // Full window search for the first move (likely the best from move ordering)
                    value = -Negamax(depth - 1, -beta, -alpha, !isWhite, move, ply + 1);
                }
            }
            
            // Undo the move
            bitboard.UndoBitboard();
            
            movesSearched++;

            if (value > bestValue)
            {
                bestValue = value;
                bestMove = move;
                
                if (value > alpha)
                {
                    alpha = value;
                    nodeType = TranspositionTable.TT_EXACT;
                    
                    // Alpha-beta pruning
                    if (alpha >= beta)
                    {
                        nodeType = TranspositionTable.TT_BETA;
                        
                        // Killer move update (for quiet moves only)
                        if (!IsMoveCapture(move))
                        {
                            // Update killer moves
                            if (!MovesEqual(move, killerMoves[0, ply]))
                            {
                                killerMoves[1, ply] = killerMoves[0, ply];
                                killerMoves[0, ply] = move;
                            }
                            
                            // Update history heuristic
                            historyTable[isWhite ? 0 : 1, move.Source * 64 + move.Destination] += depth * depth;
                        }
                        
                        break;
                    }
                }
            }
        }

        // Store position in transposition table
        transpositionTable.Store(positionHash, depth, nodeType, AdjustScoreForMateDistance(bestValue, ply), bestMove);

        return bestValue;
    }
    
    // Check if side has non-pawn material (needed for null move pruning)
    private bool HasNonPawnMaterial(bool isWhite)
    {
        if (isWhite)
            return (bitboard.WhiteKnight | bitboard.WhiteBishop | bitboard.WhiteRook | bitboard.WhiteQueen) != 0;
        else
            return (bitboard.BlackKnight | bitboard.BlackBishop | bitboard.BlackRook | bitboard.BlackQueen) != 0;
    }
    
    // Improved Quiescence Search - resolve tactical sequences at leaf nodes
    private int QuiescenceSearch(int alpha, int beta, bool isWhite, Move previousMove, int ply)
    {
        // Increment quiescence node counter
        qNodesSearched++;
        
        // Check maximum depth (bumped up from 4 to 8)
        if (ply >= 8)
            return EvaluatePosition(isWhite);
        
        // First, get the stand-pat score (evaluate current position)
        int standPat = EvaluatePosition(isWhite);
        
        // Check for early cutoff
        if (standPat >= beta)
            return beta;
            
        // Update alpha if standing pat is better than current alpha
        if (standPat > alpha)
            alpha = standPat;
            
        // Get only capture moves
        List<Move> captureMoves = GenerateCaptureMoves(isWhite, previousMove);
        
        // Sort captures by MVV-LVA and SEE
        SortMoves(captureMoves, null);
        
        foreach (Move move in captureMoves)
        {
            // Get the Static Exchange Evaluation for this capture
            int captureValue = EstimateCaptureValue(move);
            
            // Skip bad captures (negative SEE) in quiescence
            if (captureValue < 0)
                continue;
                
            // Delta pruning (if a capture couldn't possibly improve alpha even with a good margin)
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
            int score = -QuiescenceSearch(-beta, -alpha, !isWhite, move, ply + 1);
            
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
    
    // Improved move ordering - prioritize captures, TT moves, killers, and history
    private void SortMoves(List<Move> moves, Move ttMove, int ply = 0)
    {
        Dictionary<Move, int> moveScores = new Dictionary<Move, int>();
        
        foreach (Move move in moves)
        {
            int score = 0;
            
            // Highest priority: TT move from previous iterations
            if (ttMove != null && MovesEqual(move, ttMove))
            {
                score = 10000000;
            }
            // Second priority: Good captures based on SEE
            else if (IsMoveCapture(move))
            {
                // Use improved EstimateCaptureValue which considers SEE
                int captureValue = EstimateCaptureValue(move);
                
                // Winning captures get high priority
                if (captureValue > 0)
                    score = 1000000 + captureValue;
                // Equal captures get medium priority
                else if (captureValue == 0)
                    score = 500000;
                // Losing captures get lower priority but still above non-captures
                else
                    score = 200000 + captureValue; // This will be less than 200000 for bad captures
            }
            // Third priority: Killer moves
            else if (ply < MAX_DEPTH && killerMoves[0, ply] != null && MovesEqual(move, killerMoves[0, ply]))
            {
                score = 900000;
            }
            else if (ply < MAX_DEPTH && killerMoves[1, ply] != null && MovesEqual(move, killerMoves[1, ply]))
            {
                score = 800000;
            }
            // Fourth priority: Promotions
            else if (move.IsPromotion)
            {
                score = 700000 + move.PromotionPieceType;
            }
            // Fifth priority: Checks
            else if (MoveGivesCheck(move))
            {
                score = 600000;
            }
            // Last priority: History heuristic
            else
            {
                score = historyTable[move.IsWhite ? 0 : 1, move.Source * 64 + move.Destination];
            }
            
            moveScores[move] = score;
        }
        
        // Sort moves by score (descending)
        moves.Sort((a, b) => moveScores[b].CompareTo(moveScores[a]));
    }

    // Helper method to determine if a move gives check
    private bool MoveGivesCheck(Move move)
    {
        // Make the move on a temporary board
        bitboard.UpdateBitBoard(move);
        
        // Check if the opponent's king is in check
        bool givesCheck = evaluation.IsInCheck(!move.IsWhite, 
                                        bitboard.returnWhitePiecesByTypes(), 
                                        bitboard.returnBlackPiecesByTypes(), 
                                        bitboard.returnAllPieces());
        
        // Undo the move
        bitboard.UndoBitboard();
        
        return givesCheck;
    }
    
    // Static Exchange Evaluation (SEE) - determines the outcome of a capture sequence
    private int StaticExchangeEvaluation(int square, bool sideToMove, int capturedPieceType)
    {
        // If no piece is captured, SEE is 0
        if (capturedPieceType == 0) 
            return 0;
            
        int gain = PieceValues[capturedPieceType]; // Value of the captured piece
        
        // Find the least valuable attacker for the current side
        int attackingPieceType = GetLeastValuableAttacker(square, sideToMove, out int fromSquare);
        
        // If no attacker is found, return 0 (no capture possible)
        if (attackingPieceType == 0)
            return 0;
            
        // Make the capture on a temporary copy of the bitboard
        ulong originalPiece = 0;
        int originalPieceType = 0;
        bool captureFound = false;
        
        // Store the original state
        if (sideToMove) // White's move
        {
            // Check which black piece is on the square
            ulong squareMask = 1UL << square;
            if ((bitboard.BlackPawn & squareMask) != 0) { originalPiece = bitboard.BlackPawn; originalPieceType = (int)PieceType.Pawn; captureFound = true; }
            else if ((bitboard.BlackKnight & squareMask) != 0) { originalPiece = bitboard.BlackKnight; originalPieceType = (int)PieceType.Knight; captureFound = true; }
            else if ((bitboard.BlackBishop & squareMask) != 0) { originalPiece = bitboard.BlackBishop; originalPieceType = (int)PieceType.Bishop; captureFound = true; }
            else if ((bitboard.BlackRook & squareMask) != 0) { originalPiece = bitboard.BlackRook; originalPieceType = (int)PieceType.Rook; captureFound = true; }
            else if ((bitboard.BlackQueen & squareMask) != 0) { originalPiece = bitboard.BlackQueen; originalPieceType = (int)PieceType.Queen; captureFound = true; }
            else if ((bitboard.BlackKing & squareMask) != 0) { originalPiece = bitboard.BlackKing; originalPieceType = (int)PieceType.King; captureFound = true; }
        }
        else // Black's move
        {
            // Check which white piece is on the square
            ulong squareMask = 1UL << square;
            if ((bitboard.WhitePawn & squareMask) != 0) { originalPiece = bitboard.WhitePawn; originalPieceType = (int)PieceType.Pawn; captureFound = true; }
            else if ((bitboard.WhiteKnight & squareMask) != 0) { originalPiece = bitboard.WhiteKnight; originalPieceType = (int)PieceType.Knight; captureFound = true; }
            else if ((bitboard.WhiteBishop & squareMask) != 0) { originalPiece = bitboard.WhiteBishop; originalPieceType = (int)PieceType.Bishop; captureFound = true; }
            else if ((bitboard.WhiteRook & squareMask) != 0) { originalPiece = bitboard.WhiteRook; originalPieceType = (int)PieceType.Rook; captureFound = true; }
            else if ((bitboard.WhiteQueen & squareMask) != 0) { originalPiece = bitboard.WhiteQueen; originalPieceType = (int)PieceType.Queen; captureFound = true; }
            else if ((bitboard.WhiteKing & squareMask) != 0) { originalPiece = bitboard.WhiteKing; originalPieceType = (int)PieceType.King; captureFound = true; }
        }
        
        if (!captureFound)
            return 0; // No piece to capture!
        
        // Make the capture on a temporary bitboard copy
        Move captureMove = new Move(fromSquare, square, null, attackingPieceType, sideToMove);
        bitboard.UpdateBitBoard(captureMove);
        
        // Recursively call SEE to evaluate the next capture in the sequence
        // But negate the result since it's from the opponent's perspective
        int recursiveValue = -StaticExchangeEvaluation(square, !sideToMove, attackingPieceType);
        
        // Restore the board
        bitboard.UndoBitboard();
        
        // If the recursive capture is better for the opponent, don't make this capture
        if (recursiveValue > gain)
            return 0;
            
        // Otherwise, return the material gain from this capture minus the opponent's gain
        return gain - recursiveValue;
    }

    // Find the least valuable attacker to a square
    private int GetLeastValuableAttacker(int square, bool sideToMove, out int fromSquare)
    {
        fromSquare = -1;
        
        // Get all pieces from the side to move
        ulong ownPawns = sideToMove ? bitboard.WhitePawn : bitboard.BlackPawn;
        ulong ownKnights = sideToMove ? bitboard.WhiteKnight : bitboard.BlackKnight;
        ulong ownBishops = sideToMove ? bitboard.WhiteBishop : bitboard.BlackBishop;
        ulong ownRooks = sideToMove ? bitboard.WhiteRook : bitboard.BlackRook;
        ulong ownQueens = sideToMove ? bitboard.WhiteQueen : bitboard.BlackQueen;
        ulong ownKing = sideToMove ? bitboard.WhiteKing : bitboard.BlackKing;
        
        // Create attackers mask for each piece type
        ulong pawnAttackers = 0;
        ulong knightAttackers = 0;
        ulong bishopAttackers = 0;
        ulong rookAttackers = 0;
        ulong queenAttackers = 0;
        ulong kingAttackers = 0;
        
        // Check for pawn attackers
        if (sideToMove) // White's turn
        {
            // White pawns attack diagonally up-left and up-right
            if (square >= 9 && square % 8 > 0) // Check up-left (from white's view)
                pawnAttackers |= ownPawns & (1UL << (square - 9));
                
            if (square >= 7 && square % 8 < 7) // Check up-right (from white's view)
                pawnAttackers |= ownPawns & (1UL << (square - 7));
        }
        else // Black's turn
        {
            // Black pawns attack diagonally down-left and down-right
            if (square <= 54 && square % 8 > 0) // Check down-left (from black's view)
                pawnAttackers |= ownPawns & (1UL << (square + 7));
                
            if (square <= 56 && square % 8 < 7) // Check down-right (from black's view)
                pawnAttackers |= ownPawns & (1UL << (square + 9));
        }
        
        // Check for knight attackers using FindMoves methods
        ulong knightAttacks = findMoves.GetPossibleMovesForPiece(square, PieceType.Knight, !sideToMove);
        knightAttackers = knightAttacks & ownKnights;
        
        // Check for bishop/queen attackers on diagonals
        ulong allPieces = bitboard.returnAllPieces();
        ulong bishopAttacks = findMoves.GetPossibleMovesForPiece(square, PieceType.Bishop, !sideToMove);
        bishopAttackers = bishopAttacks & ownBishops;
        
        // Check for rook/queen attackers on files/ranks
        ulong rookAttacks = findMoves.GetPossibleMovesForPiece(square, PieceType.Rook, !sideToMove);
        rookAttackers = rookAttacks & ownRooks;
        
        // Get queen attackers (using both bishop and rook attacks)
        queenAttackers = (bishopAttacks | rookAttacks) & ownQueens;
        
        // Check for king attackers
        ulong kingAttacks = findMoves.GetPossibleMovesForPiece(square, PieceType.King, !sideToMove);
        kingAttackers = kingAttacks & ownKing;
        
        // Now select the least valuable attacker in order: pawn, knight, bishop, rook, queen, king
        if (pawnAttackers != 0)
        {
            fromSquare = BitOperations.TrailingZeroCount(pawnAttackers);
            return (int)PieceType.Pawn;
        }
        if (knightAttackers != 0)
        {
            fromSquare = BitOperations.TrailingZeroCount(knightAttackers);
            return (int)PieceType.Knight;
        }
        if (bishopAttackers != 0)
        {
            fromSquare = BitOperations.TrailingZeroCount(bishopAttackers);
            return (int)PieceType.Bishop;
        }
        if (rookAttackers != 0)
        {
            fromSquare = BitOperations.TrailingZeroCount(rookAttackers);
            return (int)PieceType.Rook;
        }
        if (queenAttackers != 0)
        {
            fromSquare = BitOperations.TrailingZeroCount(queenAttackers);
            return (int)PieceType.Queen;
        }
        if (kingAttackers != 0)
        {
            fromSquare = BitOperations.TrailingZeroCount(kingAttackers);
            return (int)PieceType.King;
        }
        
        // No attacker found
        return 0;
    }
    
    // Improved Capture Value Estimation with SEE
    private int EstimateCaptureValue(Move move)
    {
        if (move == null) return 0;
        
        int capturedPieceType = 0;
        ulong destBit = 1UL << move.Destination;
        
        // Determine captured piece type
        if (move.IsWhite)
        {
            // Check which black piece is being captured
            if ((destBit & bitboard.BlackPawn) != 0) capturedPieceType = 1; // PAWN
            else if ((destBit & bitboard.BlackKnight) != 0) capturedPieceType = 2; // KNIGHT
            else if ((destBit & bitboard.BlackBishop) != 0) capturedPieceType = 3; // BISHOP
            else if ((destBit & bitboard.BlackRook) != 0) capturedPieceType = 4; // ROOK
            else if ((destBit & bitboard.BlackQueen) != 0) capturedPieceType = 5; // QUEEN
            else if (move.IsEnPassant) capturedPieceType = 1; // En passant captures a pawn
        }
        else
        {
            // Check which white piece is being captured
            if ((destBit & bitboard.WhitePawn) != 0) capturedPieceType = 1; // PAWN
            else if ((destBit & bitboard.WhiteKnight) != 0) capturedPieceType = 2; // KNIGHT
            else if ((destBit & bitboard.WhiteBishop) != 0) capturedPieceType = 3; // BISHOP
            else if ((destBit & bitboard.WhiteRook) != 0) capturedPieceType = 4; // ROOK
            else if ((destBit & bitboard.WhiteQueen) != 0) capturedPieceType = 5; // QUEEN
            else if (move.IsEnPassant) capturedPieceType = 1; // En passant captures a pawn
        }
        
        // If it's not a capture, return 0
        if (capturedPieceType == 0) return 0;
        
        // Use Static Exchange Evaluation to determine the true value of this capture
        int seeValue = StaticExchangeEvaluation(move.Destination, move.IsWhite, capturedPieceType);
        
        // Make sure the score is positive even for equal exchanges (to encourage simplification)
        if (seeValue == 0) seeValue = 1;
        
        // Heavily penalize negative SEE values (bad captures)
        if (seeValue < 0) return seeValue * 2;
        
        // Still use MVV-LVA for move ordering when SEE is positive
        // This ensures we try capturing a queen with a pawn before capturing with a knight
        return (capturedPieceType * 100) - move.PieceType + seeValue;
    }
    
    // Determine if a move is a capture by using the bitboard
    private bool IsMoveCapture(Move move)
    {
        if (move == null) return false;
        
        ulong destBit = 1UL << move.Destination;
        
        if (move.IsWhite)
        {
            // For white, see if there's a black piece at the destination
            return (destBit & bitboard.returnAllBlackPieces()) != 0 || move.IsEnPassant;
        }
        else
        {
            // For black, see if there's a white piece at the destination
            return (destBit & bitboard.returnAllWhitePieces()) != 0 || move.IsEnPassant;
        }
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
    
    // Helper method to compare moves
    private bool MovesEqual(Move a, Move b)
    {
        if (a == null || b == null) return false;
        
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
    
    // Transposition Table implementation
    public class TranspositionTable
    {
        // Constants for entry types
        public const int TT_EXACT = 0;  // Exact score
        public const int TT_ALPHA = 1;  // Upper bound
        public const int TT_BETA = 2;   // Lower bound
        
        // Size should be a power of 2 for efficient indexing with bit operations
        private const int TT_SIZE = 0x1000000; // 16 million entries (adjust based on available memory)
        private const int TT_MASK = TT_SIZE - 1; // For efficient modulo with bitwise AND
        
        // Structure for table entries (optimized for memory efficiency)
        public struct TTEntry
        {
            public ulong Key;        // Zobrist hash for verification
            public short Depth;      // Search depth
            public byte Flag;        // Entry type (EXACT, ALPHA, BETA)
            public byte Age;         // For replacement strategy
            public int Value;        // Evaluation score
            public ushort Move;      // Packed move representation
            
            // Convert Move object to compact representation
            public static ushort PackMove(Move move)
            {
                if (move == null) return 0;
                // Pack move into 16 bits: 6 bits source + 6 bits destination + 4 bits piece type
                return (ushort)((move.Source << 10) | (move.Destination << 4) | (move.PieceType & 0xF));
            }
            
            // Convert packed representation back to Move object
            public static Move UnpackMove(ushort packed, bool isWhite, Move previousMove)
            {
                if (packed == 0) return null;
                
                int source = (packed >> 10) & 0x3F;
                int destination = (packed >> 4) & 0x3F;
                int pieceType = packed & 0xF;
                
                return new Move(source, destination, previousMove, pieceType, isWhite);
            }
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
            Debug.Log("Transposition table cleared");
        }
        
        // New age for new search
        public void IncrementAge()
        {
            currentAge = (byte)((currentAge + 1) & 0xFF);
        }
        
        // Store an entry with enhanced replacement strategy
        public void Store(ulong key, int depth, int flag, int value, Move bestMove)
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
                Move = TTEntry.PackMove(bestMove)
            };
            
            // Replacement strategy:
            // 1. Always replace if this is a deeper search
            // 2. Always replace if the entry is from an older search (different age)
            // 3. Always replace if keys match (same position)
            // 4. Otherwise, prefer keeping the existing entry
            if (depth > entry.Depth || currentAge != entry.Age || key == entry.Key)
            {
                entry = newEntry;
            }
        }
        
        // Probe the table
        public bool Probe(ulong key, int depth, int alpha, int beta, out int value, out Move bestMove, bool isWhite, Move previousMove)
        {
            probes++;
            int index = (int)(key & TT_MASK);
            ref TTEntry entry = ref table[index];
            
            // Check if entry is valid for this position
            if (entry.Key == key)
            {
                bestMove = TTEntry.UnpackMove(entry.Move, isWhite, previousMove);
                
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
            bestMove = null;
            return false;
        }
        
        // Get a move for move ordering, regardless of depth
        public Move GetMove(ulong key, bool isWhite, Move previousMove)
        {
            int index = (int)(key & TT_MASK);
            ref TTEntry entry = ref table[index];
            
            if (entry.Key == key && entry.Move != 0)
            {
                return TTEntry.UnpackMove(entry.Move, isWhite, previousMove);
            }
            
            return null;
        }
        
        // Stats for debugging and tuning
        public void ResetStats()
        {
            probes = 0;
            hits = 0;
        }
        
        public double GetHitRate()
        {
            return probes > 0 ? (double)hits / probes : 0;
        }
        
        public long GetProbeCount()
        {
            return probes;
        }
        
        // Estimate memory usage
        public long GetMemoryUsage()
        {
            // Size of the TTEntry struct * number of entries
            return TT_SIZE * (sizeof(ulong) + sizeof(short) + sizeof(byte) * 2 + sizeof(int) + sizeof(ushort));
        }
        
        // Get approximate fill rate
        public double GetFillRate()
        {
            // Sample a portion of the table for performance
            const int SAMPLE_SIZE = 1000;
            int count = 0;
            
            for (int i = 0; i < SAMPLE_SIZE; i++)
            {
                int index = (i * TT_SIZE / SAMPLE_SIZE) & TT_MASK;
                if (table[index].Key != 0)
                    count++;
            }
            
            return (double)count / SAMPLE_SIZE;
        }
    }
}