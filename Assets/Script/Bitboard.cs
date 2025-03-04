using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

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
    public Bitboard() {
        previousMove = null;
    }
    //Utility functions
    public ulong[] returnWhitePiecesByTypes(){
        ulong[] whitePieces = {WhitePawn, WhiteRook, WhiteKnight, WhiteBishop, WhiteQueen, WhiteKing};
        return whitePieces;
    }
    public ulong[] returnBlackPiecesByTypes(){
        ulong[] blackPieces = {BlackPawn, BlackRook, BlackKnight, BlackBishop, BlackQueen, BlackKing};
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
        // Update the board following the move 
        // Assuming move contains source and destination positions and piece type
        ulong sourceMask = 1UL << move.Source;
        ulong destinationMask = 1UL << move.Destination;

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
            case (int)PieceType.Knight:
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
        previousMove = previousMove.previousMove; //set the previous move to the move before the last move
    }
}