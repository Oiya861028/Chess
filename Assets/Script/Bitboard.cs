using System;
using UnityEngine;
using System.Collections.Generic;
public class Bitboard {
    // Constants for correct bit positions
    private const int WHITE_KING_START = 3;     // e1
    private const int BLACK_KING_START = 59;    // e8
    private const int WHITE_KINGSIDE_ROOK_START = 0;  // h1
    private const int WHITE_QUEENSIDE_ROOK_START = 7; // a1
    private const int BLACK_KINGSIDE_ROOK_START = 56; // h8
    private const int BLACK_QUEENSIDE_ROOK_START = 63; // a8

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
    public bool whiteKingMoved = false;
    public bool blackKingMoved = false;
    public bool whiteQueensideRookMoved = false;
    public bool whiteKingsideRookMoved = false;
    public bool blackQueensideRookMoved = false;
    public bool blackKingsideRookMoved = false;
    private int previousMove; 
    
    // Enhanced move state to track all board state completely
    private class MoveState {
        // Captured pieces
        public ulong[] capturedPieces = new ulong[12]; // Store all captured pieces
        
        // Castling flags
        public bool whiteKingMoved;
        public bool blackKingMoved;
        public bool whiteQueensideRookMoved;
        public bool whiteKingsideRookMoved;
        public bool blackQueensideRookMoved;
        public bool blackKingsideRookMoved;
        
        // Complete bitboard backup
        public ulong whitePawn;
        public ulong whiteRook; 
        public ulong whiteKnight;
        public ulong whiteBishop;
        public ulong whiteQueen;
        public ulong whiteKing;
        public ulong blackPawn;
        public ulong blackRook;
        public ulong blackKnight;
        public ulong blackBishop;
        public ulong blackQueen;
        public ulong blackKing;
        
        // Previous move reference for en passant tracking
        public int previousMove;
        
        public MoveState(Bitboard board) {
            // Store castling flags
            whiteKingMoved = board.whiteKingMoved;
            blackKingMoved = board.blackKingMoved;
            whiteQueensideRookMoved = board.whiteQueensideRookMoved;
            whiteKingsideRookMoved = board.whiteKingsideRookMoved;
            blackQueensideRookMoved = board.blackQueensideRookMoved;
            blackKingsideRookMoved = board.blackKingsideRookMoved;
            
            // Store complete bitboard state
            whitePawn = board.WhitePawn;
            whiteRook = board.WhiteRook;
            whiteKnight = board.WhiteKnight;
            whiteBishop = board.WhiteBishop;
            whiteQueen = board.WhiteQueen;
            whiteKing = board.WhiteKing;
            blackPawn = board.BlackPawn;
            blackRook = board.BlackRook;
            blackKnight = board.BlackKnight;
            blackBishop = board.BlackBishop;
            blackQueen = board.BlackQueen;
            blackKing = board.BlackKing;
            
            // Store previous move
            previousMove = board.previousMove;
        }
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
    
    // Track the number of moves for debugging
    private int moveCount = 0;
    private int undoCount = 0;
    
    public Bitboard() {
        previousMove = 0;
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
    
    // Verify no square has multiple pieces
    public bool VerifyBoardConsistency() {
        bool isConsistent = true;
        
        // Check if any square has more than one piece
        for (int i = 0; i < 64; i++) {
            ulong squareMask = 1UL << i;
            
            // Count pieces on this square
            int pieceCount = 0;
            if ((WhitePawn & squareMask) != 0) pieceCount++;
            if ((WhiteRook & squareMask) != 0) pieceCount++;
            if ((WhiteKnight & squareMask) != 0) pieceCount++;
            if ((WhiteBishop & squareMask) != 0) pieceCount++;
            if ((WhiteQueen & squareMask) != 0) pieceCount++;
            if ((WhiteKing & squareMask) != 0) pieceCount++;
            if ((BlackPawn & squareMask) != 0) pieceCount++;
            if ((BlackRook & squareMask) != 0) pieceCount++;
            if ((BlackKnight & squareMask) != 0) pieceCount++;
            if ((BlackBishop & squareMask) != 0) pieceCount++;
            if ((BlackQueen & squareMask) != 0) pieceCount++;
            if ((BlackKing & squareMask) != 0) pieceCount++;
            
            if (pieceCount > 1) {
                string square = IndexToAlgebraic(i);
                Debug.LogError($"Multiple pieces on square {square} - found {pieceCount} pieces!");
                isConsistent = false;
            }
        }
        
        // Check if kings exist
        if ((WhiteKing == 0) || (BlackKing == 0)) {
            Debug.LogError("A king is missing from the board!");
            isConsistent = false;
        }
        
        return isConsistent;
    }
    
    private string IndexToAlgebraic(int index) {
        int file = index % 8;
        int rank = index / 8;
        return $"{(char)('a' + file)}{rank + 1}";
    }
    
    public void UpdateBitBoard(int move)
    {
        // Increment move counter
        moveCount++;
        
        MoveState state = new MoveState(this);
        
        int source = MoveUtil.GetSource(move);
        int destination = MoveUtil.GetDestination(move);
        int pieceType = MoveUtil.GetPieceType(move);
        bool isWhite = MoveUtil.IsWhite(move);
        bool isEnPassant = MoveUtil.IsEnPassant(move);
        bool isPromotion = MoveUtil.IsPromotion(move);
        bool isCastling = MoveUtil.IsCastling(move);
        int promotionPieceType = MoveUtil.GetPromotionPieceType(move);
        
        ulong sourceMask = 1UL << source;
        ulong destinationMask = 1UL << destination;
        
        // Record any pieces that might be captured at the destination
        // White pieces that might be captured by black
        if (!isWhite) {
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

        // Update castling rights
        if (pieceType == (int)PieceType.King) {
            if (isWhite) {
                whiteKingMoved = true;
                
                // Handle castling
                if (source == WHITE_KING_START) {
                    // Kingside castling
                    if (destination == 1) { // g1
                        // Move the rook too
                        WhiteRook &= ~(1UL << 0); // Remove from h1
                        WhiteRook |= (1UL << 2);  // Add to f1
                    }
                    // Queenside castling
                    else if (destination == 5) { // c1
                        // Move the rook too
                        WhiteRook &= ~(1UL << 7); // Remove from a1
                        WhiteRook |= (1UL << 4);  // Add to d1
                    }
                }
            } 
            else {
                blackKingMoved = true;
                
                // Handle castling
                if (source == BLACK_KING_START) {
                    // Kingside castling
                    if (destination == 57) { // g8
                        // Move the rook too
                        BlackRook &= ~(1UL << 56); // Remove from h8
                        BlackRook |= (1UL << 58);  // Add to f8
                    }
                    // Queenside castling
                    else if (destination == 61) { // c8
                        // Move the rook too
                        BlackRook &= ~(1UL << 63); // Remove from a8
                        BlackRook |= (1UL << 60);  // Add to d8
                    }
                }
            }
        }
        else if (pieceType == (int)PieceType.Rook) {
            if (isWhite) {
                if (source == WHITE_QUEENSIDE_ROOK_START) { 
                    whiteQueensideRookMoved = true;
                } 
                else if (source == WHITE_KINGSIDE_ROOK_START) { 
                    whiteKingsideRookMoved = true;
                }
            } 
            else {
                if (source == BLACK_QUEENSIDE_ROOK_START) { 
                    blackQueensideRookMoved = true;
                } 
                else if (source == BLACK_KINGSIDE_ROOK_START) { 
                    blackKingsideRookMoved = true;
                }
            }
        }

        // Handle en passant captures
        if (pieceType == (int)PieceType.Pawn && isEnPassant) {
            int capturedPawnSquare;
            
            if (isWhite) {
                // White capturing black pawn
                capturedPawnSquare = destination - 8; // The black pawn is one rank below
                BlackPawn &= ~(1UL << capturedPawnSquare);
            } else {
                // Black capturing white pawn
                capturedPawnSquare = destination + 8; // The white pawn is one rank above
                WhitePawn &= ~(1UL << capturedPawnSquare);
            }
        }

        // Handle promotion
        if (isPromotion) {
            // Remove the pawn from source
            if (isWhite) {
                WhitePawn &= ~sourceMask;
                
                // Add the promoted piece at destination
                switch (promotionPieceType) {
                    case (int)PieceType.Queen:
                        WhiteQueen |= destinationMask;
                        break;
                    case (int)PieceType.Rook:
                        WhiteRook |= destinationMask;
                        break;
                    case (int)PieceType.Bishop:
                        WhiteBishop |= destinationMask;
                        break;
                    case (int)PieceType.Knight:
                        WhiteKnight |= destinationMask;
                        break;
                    default:
                        WhiteQueen |= destinationMask; // Default to queen
                        break;
                }
            } else {
                BlackPawn &= ~sourceMask;
                
                // Add the promoted piece at destination
                switch (promotionPieceType) {
                    case (int)PieceType.Queen:
                        BlackQueen |= destinationMask;
                        break;
                    case (int)PieceType.Rook:
                        BlackRook |= destinationMask;
                        break;
                    case (int)PieceType.Bishop:
                        BlackBishop |= destinationMask;
                        break;
                    case (int)PieceType.Knight:
                        BlackKnight |= destinationMask;
                        break;
                    default:
                        BlackQueen |= destinationMask; // Default to queen
                        break;
                }
            }
        }
        // Regular, non-promotion move
        else {
            // Remove the piece from the source position
            switch (pieceType) {
                case (int) PieceType.Pawn:
                    if (isWhite) WhitePawn &= ~sourceMask;
                    else BlackPawn &= ~sourceMask;
                    break;
                case (int) PieceType.Rook:
                    if (isWhite) WhiteRook &= ~sourceMask;
                    else BlackRook &= ~sourceMask;
                    break;
                case (int) PieceType.Knight:
                    if (isWhite) WhiteKnight &= ~sourceMask;
                    else BlackKnight &= ~sourceMask;
                    break;
                case (int) PieceType.Bishop:
                    if (isWhite) WhiteBishop &= ~sourceMask;
                    else BlackBishop &= ~sourceMask;
                    break;
                case (int) PieceType.Queen:
                    if (isWhite) WhiteQueen &= ~sourceMask;
                    else BlackQueen &= ~sourceMask;
                    break;
                case (int) PieceType.King:
                    if (isWhite) WhiteKing &= ~sourceMask;
                    else BlackKing &= ~sourceMask;
                    break;
            }

            // Place the piece at the destination position (normal move)
            switch (pieceType) {
                case (int) PieceType.Pawn:
                    if (isWhite) WhitePawn |= destinationMask;
                    else BlackPawn |= destinationMask;
                    break;
                case (int) PieceType.Rook:
                    if (isWhite) WhiteRook |= destinationMask;
                    else BlackRook |= destinationMask;
                    break;
                case (int) PieceType.Knight:
                    if (isWhite) WhiteKnight |= destinationMask;
                    else BlackKnight |= destinationMask;
                    break;
                case (int) PieceType.Bishop:
                    if (isWhite) WhiteBishop |= destinationMask;
                    else BlackBishop |= destinationMask;
                    break;
                case (int) PieceType.Queen:
                    if (isWhite) WhiteQueen |= destinationMask;
                    else BlackQueen |= destinationMask;
                    break;
                case (int) PieceType.King:
                    if (isWhite) WhiteKing |= destinationMask;
                    else BlackKing |= destinationMask;
                    break;
            }
        }
        
        // Update previous move for en passant and repetition detection
        previousMove = move;
        
        // Push the state onto our stack
        moveStack.Push(state);
        
        // Verify board consistency after move
        if (!VerifyBoardConsistency()) {
            Debug.LogError($"Board corrupted after move {MoveUtil.MoveToString(move)}");
        }
    }

    public void UndoBitboard()
    {
        // Increment undo counter
        undoCount++;
        
        // Check if there's a move to undo
        if (moveStack.Count == 0) {
            Debug.LogError("Attempting to undo a move when the move stack is empty!");
            return;
        }
        
        // Get the state from the top of the stack
        MoveState state = moveStack.Pop();
        
        // COMPLETE RESTORE - use snapshot approach for robustness
        // Restore all bitboards
        WhitePawn = state.whitePawn;
        WhiteRook = state.whiteRook;
        WhiteKnight = state.whiteKnight;
        WhiteBishop = state.whiteBishop;
        WhiteQueen = state.whiteQueen;
        WhiteKing = state.whiteKing;
        BlackPawn = state.blackPawn;
        BlackRook = state.blackRook;
        BlackKnight = state.blackKnight;
        BlackBishop = state.blackBishop;
        BlackQueen = state.blackQueen;
        BlackKing = state.blackKing;
        
        // Restore castling rights
        whiteKingMoved = state.whiteKingMoved;
        blackKingMoved = state.blackKingMoved;
        whiteQueensideRookMoved = state.whiteQueensideRookMoved;
        whiteKingsideRookMoved = state.whiteKingsideRookMoved;
        blackQueensideRookMoved = state.blackQueensideRookMoved;
        blackKingsideRookMoved = state.blackKingsideRookMoved;
        
        // Restore previous move for en passant detection
        previousMove = state.previousMove;
        
        // Verify board consistency after undo
        if (!VerifyBoardConsistency()) {
            Debug.LogError("Board corrupted after undo operation!");
        }
    }
    
    // Debugging information
    public int GetMoveCount() {
        return moveCount;
    }
    
    public int GetUndoCount() {
        return undoCount;
    }
}