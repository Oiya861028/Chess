using System;
using System.Collections.Generic;
using UnityEngine;
public class StockFridge
{
    private FindMoves findMoves;
    private Evaluation evaluation;
    private Bitboard bitboard;
    private const int INFINITY = 9999999;

    public StockFridge(FindMoves findMoves, Bitboard bitboard)
    {
        this.findMoves = findMoves;
        this.bitboard = bitboard;
        this.evaluation = new Evaluation(); // Initialize the evaluation
    }
    
    // The main function that will be called to get the best move for the player
    public Move GetBestMove(int depth, bool isWhite, Move previousMove)
    {
        Move bestMove = null;
        int bestValue = -INFINITY;
        
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
        // Base case: if we've reached maximum depth
        if (depth == 0) 
        {
            return EvaluatePosition(isWhite);
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
        
        int bestValue = -INFINITY;
        
        foreach (Move move in legalMoves)
        {
            // Make the move
            bitboard.UpdateBitBoard(move);
            
            // Recursively evaluate position
            int value = -Negamax(depth - 1, -beta, -alpha, !isWhite, move);
            
            // Undo the move
            bitboard.UndoBitboard();

            bestValue = Math.Max(bestValue, value);
            alpha = Math.Max(alpha, value);

            // Alpha-beta pruning
            if (alpha >= beta)
            {
                break;
            }
        }

        return bestValue;
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
}