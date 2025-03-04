using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;

public class StockFridge {
    //The Algorithm that’s responsible for predicting the optimal next move, basically it will implement the negamax algorithm to choose the best next move for the player
    //It will also implement the alpha beta pruning algorithm to optimize the negamax algorithm
    //It will also implement the quiescence search algorithm to optimize the alpha beta pruning algorithm
    private FindMoves findMoves;
    private Evaluation evaluation;
    private Bitboard bitboard;
    private const int INFINITY = 9999999;

    public StockFridge(FindMoves findMoves, Bitboard bitboard)
    {
        this.findMoves = findMoves;
        this.bitboard = bitboard;
        evaluation = new Evaluation();
    }
    
    //The main function that will be called to get the best move for the player
    public Move GetBestMove(int depth, bool isWhite, Move previousMove //the last pli that got us to this position
    )
    {
        Move bestMove = null;
        int bestValue = -INFINITY;
        
        // Get all possible moves for the current position
        List<Move> possibleMoves = findMoves.GetAllPossibleMoves(isWhite, previousMove);
        
        foreach (Move move in possibleMoves)
        {
            // Make the move
            bitboard.UpdateBitBoard(move);
            
            // Get the value for this move
            int value = -Negamax(depth - 1, -INFINITY, INFINITY, !isWhite, previousMove);
            
            // Undo the move
            bitboard.UndoBitboard();
            
            if (value > bestValue)
            {
                bestValue = value;
                bestMove = move;
            }
        }
        
        return bestMove;
    }

    private int Negamax(int depth, int alpha, int beta, bool isWhite, Move previousMove)
    {
        // Base case: if we've reached maximum depth or game is over
        if (depth == 0) 
        {
            return evaluation.EvaluatePosition(bitboard.WhitePawn, bitboard.WhiteKnight, bitboard.WhiteBishop, bitboard.WhiteRook, bitboard.WhiteQueen, bitboard.WhiteKing,
        bitboard.BlackPawn, bitboard.BlackKnight, bitboard.BlackBishop, bitboard.BlackRook, bitboard.BlackQueen, bitboard.BlackKing, isWhite);
        }

        int bestValue = -INFINITY;
        List<Move> possibleMoves = findMoves.GetAllPossibleMoves(isWhite, previousMove);

        // If no moves are available, it's either checkmate or stalemate
        if (possibleMoves.Count == 0)
        {
            if (evaluation.IsInCheck(isWhite, bitboard.returnWhitePiecesByTypes(), bitboard.returnBlackPiecesByTypes(), bitboard.returnAllPieces()))
            {
                return -INFINITY; // Checkmate
            }
            return 0; // Stalemate
        }

        foreach (Move move in possibleMoves)
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

}