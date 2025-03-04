
using System;
using UnityEngine;
using System.Collections.Generic;
public class Bitboard {
    // Bitboard for white pieces
    public ulong WhitePawn =   0b0000000000000000000000000000000000000000000000001111111100000000;
    public ulong WhiteRook =   0b0000000000000000000000000000000000000000000000000000000010000001;
    public ulong WhiteKnight = 0b0000000000000000000000000000000000000000000000000000000001000010;
    public ulong WhiteBishop = 0b0000000000000000000000000000000000000000000000000000000000100100;
    public ulong WhiteQueen =  0b0000000000000000000000000000000000000000000000000000000000010000;
    public ulong WhiteKing =   0b0000000000000000000000000000000000000000000000000000000000001000;
    // Bitboard for black pieces
    public ulong BlackPawn =   0b0000000011111111000000000000000000000000000000000000000000000000;
    public ulong BlackRook =   0b1000000100000000000000000000000000000000000000000000000000000000;
    public ulong BlackKnight = 0b0100001000000000000000000000000000000000000000000000000000000000;
    public ulong BlackBishop = 0b0010010000000000000000000000000000000000000000000000000000000000;
    public ulong BlackQueen =  0b0001000000000000000000000000000000000000000000000000000000000000;
    public ulong BlackKing =   0b0000100000000000000000000000000000000000000000000000000000000000;
    
    Move previousMove;
    
    // Move stack to track captured pieces
    private class MoveState {
        public ulong[] capturedPieces = new ulong[12]; // Store all captured pieces
    }
    
    private Stack<MoveState> moveStack = new Stack<MoveState>();
    
    // Constants for indexing the capturedPieces array
    private const int WHITE_PAWN = 0;
    private const int WHITE_KNIGHT = 1;
    private const int WHITE_BISHOP = 2;
    private const int WHITE_ROOK = 3;
    private const int WHITE_QUEEN = 4;
    private const int WHITE_KING = 5;
    private const int BLACK_PAWN = 6;
    private const int BLACK_KNIGHT = 7;
    private const int BLACK_BISHOP = 8;
    private const int BLACK_ROOK = 9;
    private const int BLACK_QUEEN = 10;
    private const int BLACK_KING = 11;
    
    public Bitboard() {
        previousMove = null;
    }
    
    //Utility functions
    public ulong[] returnWhitePiecesByTypes(){
        // Create an array that matches the index convention used in Evaluation class
        // The Evaluation class expects: [ALL_PIECES(0), PAWN(1), KNIGHT(2), BISHOP(3), ROOK(4), QUEEN(5), KING(6)]
        ulong[] whitePieces = new ulong[7];
        whitePieces[0] = WhitePawn | WhiteRook | WhiteKnight | WhiteBishop | WhiteQueen | WhiteKing; // ALL_PIECES
        whitePieces[1] = WhitePawn;   // PAWN
        whitePieces[2] = WhiteKnight; // KNIGHT
        whitePieces[3] = WhiteBishop; // BISHOP
        whitePieces[4] = WhiteRook;   // ROOK
        whitePieces[5] = WhiteQueen;  // QUEEN
        whitePieces[6] = WhiteKing;   // KING
        return whitePieces;
    }

    public ulong[] returnBlackPiecesByTypes(){
        // Create an array that matches the index convention used in Evaluation class
        // The Evaluation class expects: [ALL_PIECES(0), PAWN(1), KNIGHT(2), BISHOP(3), ROOK(4), QUEEN(5), KING(6)]
        ulong[] blackPieces = new ulong[7];
        blackPieces[0] = BlackPawn | BlackRook | BlackKnight | BlackBishop | BlackQueen | BlackKing; // ALL_PIECES
        blackPieces[1] = BlackPawn;   // PAWN
        blackPieces[2] = BlackKnight; // KNIGHT
        blackPieces[3] = BlackBishop; // BISHOP
        blackPieces[4] = BlackRook;   // ROOK
        blackPieces[5] = BlackQueen;  // QUEEN
        blackPieces[6] = BlackKing;   // KING
        return blackPieces;
    }
    
    public ulong returnAllWhitePieces(){
        return WhitePawn | WhiteRook | WhiteKnight | WhiteBishop | WhiteQueen | WhiteKing;
    }
    
    public ulong returnAllBlackPieces(){
        return BlackPawn | BlackRook | BlackKnight | BlackBishop | BlackQueen | BlackKing;
    }   
    
    public ulong returnAllPieces(){
        return WhitePawn | WhiteRook | WhiteKnight | WhiteBishop | WhiteQueen | WhiteKing | BlackPawn | BlackRook | BlackKnight | BlackBishop | BlackQueen | BlackKing;
    }
    
    public void UpdateBitBoard(Move move) {
        // Create a new state to track what might be captured in this move
        MoveState state = new MoveState();
        
        ulong sourceMask = 1UL << move.Source;
        ulong destinationMask = 1UL << move.Destination;

        // Record any pieces that might be captured at the destination
        // White pieces that might be captured by black
        if (!move.IsWhite) {
            state.capturedPieces[WHITE_PAWN] = WhitePawn & destinationMask;
            state.capturedPieces[WHITE_KNIGHT] = WhiteKnight & destinationMask;
            state.capturedPieces[WHITE_BISHOP] = WhiteBishop & destinationMask;
            state.capturedPieces[WHITE_ROOK] = WhiteRook & destinationMask;
            state.capturedPieces[WHITE_QUEEN] = WhiteQueen & destinationMask;
            // Don't include king - should never be captured
            
            // Clear any white pieces at the destination
            WhitePawn &= ~destinationMask;
            WhiteRook &= ~destinationMask;
            WhiteKnight &= ~destinationMask;
            WhiteBishop &= ~destinationMask;
            WhiteQueen &= ~destinationMask;
        } 
        // Black pieces that might be captured by white
        else {
            state.capturedPieces[BLACK_PAWN] = BlackPawn & destinationMask;
            state.capturedPieces[BLACK_KNIGHT] = BlackKnight & destinationMask;
            state.capturedPieces[BLACK_BISHOP] = BlackBishop & destinationMask;
            state.capturedPieces[BLACK_ROOK] = BlackRook & destinationMask;
            state.capturedPieces[BLACK_QUEEN] = BlackQueen & destinationMask;
            // Don't include king - should never be captured
            
            // Clear any black pieces at the destination
            BlackPawn &= ~destinationMask;
            BlackRook &= ~destinationMask;
            BlackKnight &= ~destinationMask;
            BlackBishop &= ~destinationMask;
            BlackQueen &= ~destinationMask;
        }

        // Remove the piece from the source position
        switch (move.PieceType) {
            case (int) PieceType.Pawn:
                if (move.IsWhite) WhitePawn &= ~sourceMask;
                else BlackPawn &= ~sourceMask;
                break;
            case (int) PieceType.Rook:
                if (move.IsWhite) WhiteRook &= ~sourceMask;
                else BlackRook &= ~sourceMask;
                break;
            case (int) PieceType.Knight:
                if (move.IsWhite) WhiteKnight &= ~sourceMask;
                else BlackKnight &= ~sourceMask;
                break;
            case (int) PieceType.Bishop:
                if (move.IsWhite) WhiteBishop &= ~sourceMask;
                else BlackBishop &= ~sourceMask;
                break;
            case (int) PieceType.Queen:
                if (move.IsWhite) WhiteQueen &= ~sourceMask;
                else BlackQueen &= ~sourceMask;
                break;
            case (int) PieceType.King:
                if (move.IsWhite) WhiteKing &= ~sourceMask;
                else BlackKing &= ~sourceMask;
                break;
        }

        // Place the piece at the destination position
        switch (move.PieceType) {
            case (int) PieceType.Pawn:
                if (move.IsWhite) WhitePawn |= destinationMask;
                else BlackPawn |= destinationMask;
                break;
            case (int) PieceType.Rook:
                if (move.IsWhite) WhiteRook |= destinationMask;
                else BlackRook |= destinationMask;
                break;
            case (int) PieceType.Knight:
                if (move.IsWhite) WhiteKnight |= destinationMask;
                else BlackKnight |= destinationMask;
                break;
            case (int) PieceType.Bishop:
                if (move.IsWhite) WhiteBishop |= destinationMask;
                else BlackBishop |= destinationMask;
                break;
            case (int) PieceType.Queen:
                if (move.IsWhite) WhiteQueen |= destinationMask;
                else BlackQueen |= destinationMask;
                break;
            case (int) PieceType.King:
                if (move.IsWhite) WhiteKing |= destinationMask;
                else BlackKing |= destinationMask;
                break;
        }

        // Push the state onto our stack
        moveStack.Push(state);
        previousMove = move;
    }

    public void UndoBitboard() {
        // Reverse the last move in the bitboard, using previousMove and reversal techniques
        if (previousMove == null) return;

        ulong sourceMask = 1UL << previousMove.Source;
        ulong destinationMask = 1UL << previousMove.Destination;

        // Remove the piece from the destination position
        switch ((PieceType) previousMove.PieceType) {
            case PieceType.Pawn:
                if (previousMove.IsWhite) WhitePawn &= ~destinationMask;
                else BlackPawn &= ~destinationMask;
                break;
            case PieceType.Rook:
                if (previousMove.IsWhite) WhiteRook &= ~destinationMask;
                else BlackRook &= ~destinationMask;
                break;
            case PieceType.Knight:
                if (previousMove.IsWhite) WhiteKnight &= ~destinationMask;
                else BlackKnight &= ~destinationMask;
                break;
            case PieceType.Bishop:
                if (previousMove.IsWhite) WhiteBishop &= ~destinationMask;
                else BlackBishop &= ~destinationMask;
                break;
            case PieceType.Queen:
                if (previousMove.IsWhite) WhiteQueen &= ~destinationMask;
                else BlackQueen &= ~destinationMask;
                break;
            case PieceType.King:
                if (previousMove.IsWhite) WhiteKing &= ~destinationMask;
                else BlackKing &= ~destinationMask;
                break;
        }

        // Place the piece back at the source position
        switch ((PieceType) previousMove.PieceType) {
            case PieceType.Pawn:
                if (previousMove.IsWhite) WhitePawn |= sourceMask;
                else BlackPawn |= sourceMask;
                break;
            case PieceType.Rook:
                if (previousMove.IsWhite) WhiteRook |= sourceMask;
                else BlackRook |= sourceMask;
                break;
            case PieceType.Knight:
                if (previousMove.IsWhite) WhiteKnight |= sourceMask;
                else BlackKnight |= sourceMask;
                break;
            case PieceType.Bishop:
                if (previousMove.IsWhite) WhiteBishop |= sourceMask;
                else BlackBishop |= sourceMask;
                break;
            case PieceType.Queen:
                if (previousMove.IsWhite) WhiteQueen |= sourceMask;
                else BlackQueen |= sourceMask;
                break;
            case PieceType.King:
                if (previousMove.IsWhite) WhiteKing |= sourceMask;
                else BlackKing |= sourceMask;
                break;
        }

        // Get captured pieces from the stack and restore them
        if (moveStack.Count > 0) {
            MoveState state = moveStack.Pop();
            
            // Restore any captured pieces
            WhitePawn |= state.capturedPieces[WHITE_PAWN];
            WhiteKnight |= state.capturedPieces[WHITE_KNIGHT];
            WhiteBishop |= state.capturedPieces[WHITE_BISHOP];
            WhiteRook |= state.capturedPieces[WHITE_ROOK];
            WhiteQueen |= state.capturedPieces[WHITE_QUEEN];
            
            BlackPawn |= state.capturedPieces[BLACK_PAWN];
            BlackKnight |= state.capturedPieces[BLACK_KNIGHT];
            BlackBishop |= state.capturedPieces[BLACK_BISHOP];
            BlackRook |= state.capturedPieces[BLACK_ROOK];
            BlackQueen |= state.capturedPieces[BLACK_QUEEN];
        }
        
        previousMove = previousMove.previousMove; //set the previous move to the move before the last move
    }
}