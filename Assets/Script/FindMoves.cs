using System;
using System.Collections.Generic;
using UnityEngine;

public class FindMoves
{
    private Bitboard bitboard;
    private Evaluation evaluation;
    
    // Updated constants for the correct bit positions
    private const int WHITE_KING_START = 3;     // e1
    private const int BLACK_KING_START = 59;    // e8
    private const int WHITE_KINGSIDE_ROOK_START = 0;  // h1
    private const int WHITE_QUEENSIDE_ROOK_START = 7; // a1
    private const int BLACK_KINGSIDE_ROOK_START = 56; // h8
    private const int BLACK_QUEENSIDE_ROOK_START = 63; // a8
    
    // Debug mode to log move generation details
    private bool debugMode = false;

    public FindMoves(Bitboard bitboard)
    {
        this.bitboard = bitboard;
        this.evaluation = new Evaluation();
        Debug.Log("FindMoves initialized with bitboard");
    }


    /// Gets all legal moves for the specified side

    public List<Move> GetAllPossibleMoves(bool isWhite, Move previousMove)
    {
        if (debugMode) Debug.Log("GetAllPossibleMoves called for " + (isWhite ? "white" : "black"));
        
        List<Move> moves = new List<Move>();
        
        // Get piece bitboards based on color
        ulong ownPawns = isWhite ? bitboard.WhitePawn : bitboard.BlackPawn;
        ulong ownKnights = isWhite ? bitboard.WhiteKnight : bitboard.BlackKnight;
        ulong ownBishops = isWhite ? bitboard.WhiteBishop : bitboard.BlackBishop;
        ulong ownRooks = isWhite ? bitboard.WhiteRook : bitboard.BlackRook;
        ulong ownQueens = isWhite ? bitboard.WhiteQueen : bitboard.BlackQueen;
        ulong ownKing = isWhite ? bitboard.WhiteKing : bitboard.BlackKing;
        
        if (debugMode)
        {
            Debug.Log("Own pieces bitboards:");
            Debug.Log($"Pawns: {ownPawns}, Knights: {ownKnights}, Bishops: {ownBishops}");
            Debug.Log($"Rooks: {ownRooks}, Queens: {ownQueens}, King: {ownKing}");
        }

        // Get positions of all pieces by type
        List<int> pawnPositions = GetSetBitPositions(ownPawns);
        List<int> knightPositions = GetSetBitPositions(ownKnights);
        List<int> bishopPositions = GetSetBitPositions(ownBishops);
        List<int> rookPositions = GetSetBitPositions(ownRooks);
        List<int> queenPositions = GetSetBitPositions(ownQueens);
        List<int> kingPositions = GetSetBitPositions(ownKing);

        // Process each piece type
        foreach (int position in pawnPositions)
        {
            AddMovesForPosition(moves, position, PieceType.Pawn, isWhite, previousMove);
        }
        
        foreach (int position in knightPositions)
        {
            AddMovesForPosition(moves, position, PieceType.Knight, isWhite, previousMove);
        }
        
        foreach (int position in bishopPositions)
        {
            AddMovesForPosition(moves, position, PieceType.Bishop, isWhite, previousMove);
        }
        
        foreach (int position in rookPositions)
        {
            AddMovesForPosition(moves, position, PieceType.Rook, isWhite, previousMove);
        }
        
        foreach (int position in queenPositions)
        {
            AddMovesForPosition(moves, position, PieceType.Queen, isWhite, previousMove);
        }
        
        foreach (int position in kingPositions)
        {
            AddMovesForPosition(moves, position, PieceType.King, isWhite, previousMove);
        }
        
        // Filter out illegal moves (moves that leave the king in check)
        List<Move> legalMoves = FilterLegalMoves(moves, isWhite);
        
        if (debugMode) Debug.Log($"Found {legalMoves.Count} legal moves");
        
        // Check castling moves and add them if legal
        int kingStartPosition = isWhite ? WHITE_KING_START : BLACK_KING_START;
        bool kingMoved = isWhite ? bitboard.whiteKingMoved : bitboard.blackKingMoved;

        if (!kingMoved) {
            ulong kingBitboard = isWhite ? bitboard.WhiteKing : bitboard.BlackKing;
            
            // Verify king is at starting position
            if ((kingBitboard & (1UL << kingStartPosition)) != 0) {
                // Check kingside castling
                bool kingsideRookMoved = isWhite ? bitboard.whiteKingsideRookMoved : bitboard.blackKingsideRookMoved;
                if (!kingsideRookMoved) {
                    // Adjusted paths for reversed bit ordering
                    ulong kingsidePath = isWhite ? 
                                        ((1UL << 1) | (1UL << 2)) :  // g1,f1 in new mapping
                                        ((1UL << 57) | (1UL << 58));  // g8,f8 in new mapping
                    
                    if ((bitboard.returnAllPieces() & kingsidePath) == 0) {
                        // Check if king is in check
                        if (!evaluation.IsInCheck(isWhite, 
                                                bitboard.returnWhitePiecesByTypes(), 
                                                bitboard.returnBlackPiecesByTypes(), 
                                                bitboard.returnAllPieces())) {
                                                
                            // Check if king passes through check
                            int passThroughSquare = isWhite ? 2 : 58; // f1 or f8 in new mapping
                            
                            // Try moving king to pass through square
                            Move passThroughMove = new Move(kingStartPosition, passThroughSquare, previousMove, (int)PieceType.King, isWhite);
                            bitboard.UpdateBitBoard(passThroughMove);
                            
                            bool passThroughCheck = evaluation.IsInCheck(isWhite, 
                                                                        bitboard.returnWhitePiecesByTypes(), 
                                                                        bitboard.returnBlackPiecesByTypes(), 
                                                                        bitboard.returnAllPieces());
                                                                        
                            bitboard.UndoBitboard();
                            
                            if (!passThroughCheck) {
                                // Check destination square
                                int destSquare = isWhite ? 1 : 57; // g1 or g8 in new mapping
                                
                                Move destMove = new Move(kingStartPosition, destSquare, previousMove, (int)PieceType.King, isWhite);
                                bitboard.UpdateBitBoard(destMove);
                                
                                bool destCheck = evaluation.IsInCheck(isWhite, 
                                                                    bitboard.returnWhitePiecesByTypes(), 
                                                                    bitboard.returnBlackPiecesByTypes(), 
                                                                    bitboard.returnAllPieces());
                                                                    
                                bitboard.UndoBitboard();
                                
                                if (!destCheck) {
                                    // Add kingside castling move
                                    legalMoves.Add(new Move(kingStartPosition, destSquare, previousMove, (int)PieceType.King, isWhite));
                                    if (debugMode) Debug.Log($"Added {(isWhite ? "white" : "black")} kingside castling move");
                                }
                            }
                        }
                    }
                }
                
                // Check queenside castling
                bool queensideRookMoved = isWhite ? bitboard.whiteQueensideRookMoved : bitboard.blackQueensideRookMoved;
                if (!queensideRookMoved) {
                    // Adjusted paths for reversed bit ordering
                    ulong queensidePath = isWhite ? 
                                        ((1UL << 4) | (1UL << 5) | (1UL << 6)) :  // d1,c1,b1 in new mapping
                                        ((1UL << 60) | (1UL << 61) | (1UL << 62)); // d8,c8,b8 in new mapping
                    
                    if ((bitboard.returnAllPieces() & queensidePath) == 0) {
                        // Check if king is in check
                        if (!evaluation.IsInCheck(isWhite, 
                                                bitboard.returnWhitePiecesByTypes(), 
                                                bitboard.returnBlackPiecesByTypes(), 
                                                bitboard.returnAllPieces())) {
                                                
                            // Check if king passes through check
                            int passThroughSquare = isWhite ? 4 : 60; // d1 or d8 in new mapping
                            
                            // Try moving king to pass through square
                            Move passThroughMove = new Move(kingStartPosition, passThroughSquare, previousMove, (int)PieceType.King, isWhite);
                            bitboard.UpdateBitBoard(passThroughMove);
                            
                            bool passThroughCheck = evaluation.IsInCheck(isWhite, 
                                                                        bitboard.returnWhitePiecesByTypes(), 
                                                                        bitboard.returnBlackPiecesByTypes(), 
                                                                        bitboard.returnAllPieces());
                                                                        
                            bitboard.UndoBitboard();
                            
                            if (!passThroughCheck) {
                                // Check destination square
                                int destSquare = isWhite ? 5 : 61; // c1 or c8 in new mapping
                                
                                Move destMove = new Move(kingStartPosition, destSquare, previousMove, (int)PieceType.King, isWhite);
                                bitboard.UpdateBitBoard(destMove);
                                
                                bool destCheck = evaluation.IsInCheck(isWhite, 
                                                                    bitboard.returnWhitePiecesByTypes(), 
                                                                    bitboard.returnBlackPiecesByTypes(), 
                                                                    bitboard.returnAllPieces());
                                                                    
                                bitboard.UndoBitboard();
                                
                                if (!destCheck) {
                                    // Add queenside castling move
                                    legalMoves.Add(new Move(kingStartPosition, destSquare, previousMove, (int)PieceType.King, isWhite));
                                    if (debugMode) Debug.Log($"Added {(isWhite ? "white" : "black")} queenside castling move");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        return legalMoves;
    }

    // Helper method to get positions of set bits in a bitboard
    private List<int> GetSetBitPositions(ulong bitboard)
    {
        List<int> positions = new List<int>();
        ulong mask = 1UL;
        
        for (int i = 0; i < 64; i++)
        {
            if ((bitboard & mask) != 0)
            {
                positions.Add(i);
            }
            mask <<= 1;
        }
        
        return positions;
    }

    // Add moves for a piece at a specific position
    private void AddMovesForPosition(List<Move> moves, int position, PieceType pieceType, bool isWhite, Move previousMove)
    {
        // Double-check that this position actually contains a piece of the right color
        ulong positionMask = 1UL << position;
        bool isPieceWhite = (bitboard.WhitePawn & positionMask) != 0 ||
                            (bitboard.WhiteKnight & positionMask) != 0 ||
                            (bitboard.WhiteBishop & positionMask) != 0 ||
                            (bitboard.WhiteRook & positionMask) != 0 ||
                            (bitboard.WhiteQueen & positionMask) != 0 ||
                            (bitboard.WhiteKing & positionMask) != 0;
                            
        // Skip this position if the piece color doesn't match the expected color
        if (isPieceWhite != isWhite)
        {
            Debug.LogError($"Piece color mismatch at position {position} ({GetSquareName(position)}): " +
                        $"Expected {(isWhite ? "white" : "black")} but found {(isPieceWhite ? "white" : "black")}");
            return;
        }
        
        ulong moveBitboard = GetPossibleMovesForPiece(position, pieceType, isWhite);
        
        // Convert bits to Move objects
        for (int i = 0; i < 64; i++)
        {
            if ((moveBitboard & (1UL << i)) != 0)
            {
                if (debugMode) Debug.Log($"  - Move to {GetSquareName(i)}");
                moves.Add(new Move(position, i, previousMove, (int)pieceType, isWhite));
            }
        }
    }
    

    // Adds moves for all pieces of a specific type

    private void AddMovesForPieceType(List<Move> moves, ulong pieceBitboard, int pieceType, bool isWhite, Move previousMove)
    {
        // Process each piece of this type
        while (pieceBitboard != 0)
        {
            // Find position of least significant bit (first piece)
            int piecePosition = BitOperations.TrailingZeroCount(pieceBitboard);
            
            // Remove this piece for next iteration
            pieceBitboard &= ~(1UL << piecePosition);
            
            if (debugMode) Debug.Log($"Generating moves for {(PieceType)pieceType} at position {piecePosition} ({GetSquareName(piecePosition)})");
            
            // Generate moves for this piece
            ulong moveBitboard = GetPossibleMovesForPiece(piecePosition, (PieceType)pieceType, isWhite);
            
            // Convert bits to Move objects
            for (int i = 0; i < 64; i++)
            {
                if ((moveBitboard & (1UL << i)) != 0)
                {
                    if (debugMode) Debug.Log($"  - Move to {GetSquareName(i)}");
                    moves.Add(new Move(piecePosition, i, previousMove, pieceType, isWhite));
                }
            }
        }
    }
   
    private List<Move> FilterLegalMoves(List<Move> moves, bool isWhite)
    {
        List<Move> legalMoves = new List<Move>();
        
        foreach (Move move in moves)
        {
            // Make the move
            bitboard.UpdateBitBoard(move);
            
            // Check if the king is in check
            ulong[] whitePieces = bitboard.returnWhitePiecesByTypes();
            ulong[] blackPieces = bitboard.returnBlackPiecesByTypes();
            ulong allPieces = bitboard.returnAllPieces();
            
            bool kingInCheck = evaluation.IsInCheck(isWhite, whitePieces, blackPieces, allPieces);
            
            // Undo the move
            bitboard.UndoBitboard();
            
            // If the move doesn't leave the king in check, it's legal
            if (!kingInCheck)
            {
                legalMoves.Add(move);
            }
            else if (debugMode)
            {
                Debug.Log($"Filtered illegal move: {GetSquareName(move.Source)} to {GetSquareName(move.Destination)} - leaves king in check");
            }
        }
        
        return legalMoves;
    }
    
    public bool IsPiecePinned(int pieceIndex, bool isWhite)
    {
        // Find the king
        ulong kingBitboard = isWhite ? bitboard.WhiteKing : bitboard.BlackKing;
        if (kingBitboard == 0) return false;
        
        int kingIndex = BitboardUtils.GetLSB(kingBitboard);
        
        // Get coordinates
        int pieceRank = pieceIndex / 8;
        int pieceFile = pieceIndex % 8;
        int kingRank = kingIndex / 8;
        int kingFile = kingIndex % 8;
        
        // Check if on same rank, file, or diagonal
        bool onSameRank = kingRank == pieceRank;
        bool onSameFile = kingFile == pieceFile;
        bool onSameDiagonal = Math.Abs(kingRank - pieceRank) == Math.Abs(kingFile - pieceFile);
        
        // If not aligned, can't be pinned
        if (!onSameRank && !onSameFile && !onSameDiagonal) return false;
        
        // Determine search direction from king to piece
        int rankDir = 0;
        int fileDir = 0;
        
        if (onSameRank) fileDir = pieceFile > kingFile ? 1 : -1;
        else if (onSameFile) rankDir = pieceRank > kingRank ? 1 : -1;
        else { // on diagonal
            rankDir = pieceRank > kingRank ? 1 : -1;
            fileDir = pieceFile > kingFile ? 1 : -1;
        }
        
        // Get enemy sliding pieces
        ulong enemyRooks = isWhite ? bitboard.BlackRook : bitboard.WhiteRook;
        ulong enemyBishops = isWhite ? bitboard.BlackBishop : bitboard.WhiteBishop;
        ulong enemyQueens = isWhite ? bitboard.BlackQueen : bitboard.WhiteQueen;
        
        // Check for pinning piece beyond the piece being checked
        int r = pieceRank + rankDir;
        int f = pieceFile + fileDir;
        
        while (r >= 0 && r < 8 && f >= 0 && f < 8)
        {
            int squareIndex = r * 8 + f;
            ulong squareMask = 1UL << squareIndex;
            
            // If we hit a piece
            if ((bitboard.returnAllPieces() & squareMask) != 0)
            {

                bool isPinner = false;
                
                if (onSameRank || onSameFile)
                {
                    isPinner = (enemyRooks & squareMask) != 0 || (enemyQueens & squareMask) != 0;
                }
                else // on diagonal
                {
                    isPinner = (enemyBishops & squareMask) != 0 || (enemyQueens & squareMask) != 0;
                }
                
                return isPinner;
            }
            
            r += rankDir;
            f += fileDir;
        }
        
        return false;
    }
    public ulong GetPossibleMoves(int position)
    {
        // Determine what piece is at this position
        ulong positionBit = 1UL << position;
        
        if ((bitboard.WhitePawn & positionBit) != 0)
            return GetPossibleMovesForPiece(position, PieceType.Pawn, true);
        else if ((bitboard.BlackPawn & positionBit) != 0)
            return GetPossibleMovesForPiece(position, PieceType.Pawn, false);
        else if ((bitboard.WhiteKnight & positionBit) != 0)
            return GetPossibleMovesForPiece(position, PieceType.Knight, true);
        else if ((bitboard.BlackKnight & positionBit) != 0)
            return GetPossibleMovesForPiece(position, PieceType.Knight, false);
        else if ((bitboard.WhiteBishop & positionBit) != 0)
            return GetPossibleMovesForPiece(position, PieceType.Bishop, true);
        else if ((bitboard.BlackBishop & positionBit) != 0)
            return GetPossibleMovesForPiece(position, PieceType.Bishop, false);
        else if ((bitboard.WhiteRook & positionBit) != 0)
            return GetPossibleMovesForPiece(position, PieceType.Rook, true);
        else if ((bitboard.BlackRook & positionBit) != 0)
            return GetPossibleMovesForPiece(position, PieceType.Rook, false);
        else if ((bitboard.WhiteQueen & positionBit) != 0)
            return GetPossibleMovesForPiece(position, PieceType.Queen, true);
        else if ((bitboard.BlackQueen & positionBit) != 0)
            return GetPossibleMovesForPiece(position, PieceType.Queen, false);
        else if ((bitboard.WhiteKing & positionBit) != 0)
            return GetPossibleMovesForPiece(position, PieceType.King, true);
        else if ((bitboard.BlackKing & positionBit) != 0)
            return GetPossibleMovesForPiece(position, PieceType.King, false);
        else
            return 0; // No piece at this position
    }
    private ulong GetPossibleMovesForPiece(int position, PieceType pieceType, bool isWhite)
    {  
        // Get combined piece bitboards
        ulong allPieces = bitboard.WhitePawn | bitboard.WhiteKnight | bitboard.WhiteBishop | 
                        bitboard.WhiteRook | bitboard.WhiteQueen | bitboard.WhiteKing |
                        bitboard.BlackPawn | bitboard.BlackKnight | bitboard.BlackBishop |
                        bitboard.BlackRook | bitboard.BlackQueen | bitboard.BlackKing;
                        
        ulong ownPieces = isWhite ? 
            (bitboard.WhitePawn | bitboard.WhiteKnight | bitboard.WhiteBishop | 
            bitboard.WhiteRook | bitboard.WhiteQueen | bitboard.WhiteKing) :
            (bitboard.BlackPawn | bitboard.BlackKnight | bitboard.BlackBishop |
            bitboard.BlackRook | bitboard.BlackQueen | bitboard.BlackKing);
            
        ulong enemyPieces = isWhite ?
            (bitboard.BlackPawn | bitboard.BlackKnight | bitboard.BlackBishop |
            bitboard.BlackRook | bitboard.BlackQueen | bitboard.BlackKing) :
            (bitboard.WhitePawn | bitboard.WhiteKnight | bitboard.WhiteBishop |
            bitboard.WhiteRook | bitboard.WhiteQueen | bitboard.WhiteKing);
        
        // Get basic moves based on piece type
        ulong moves = 0;
        
        // Delegate to the appropriate move generator
        switch (pieceType)
        {
            case PieceType.Pawn:
                moves = CalculatePawnMoves(position, isWhite, allPieces, ownPieces, enemyPieces);
                break;
            case PieceType.Knight:
                moves = CalculateKnightMoves(position, ownPieces);
                break;
            case PieceType.Bishop:
                moves = CalculateBishopMoves(position, allPieces, ownPieces);
                break;
            case PieceType.Rook:
                moves = CalculateRookMoves(position, allPieces, ownPieces);
                break;
            case PieceType.Queen:
                moves = CalculateQueenMoves(position, allPieces, ownPieces);
                break;
            case PieceType.King:
                moves = CalculateKingMoves(position, ownPieces);
                break;
            default:
                Debug.LogError($"Unknown piece type: {pieceType}");
                return 0;
        }
        
        // Add castling moves for kings with the corrected bit positions
        if (pieceType == PieceType.King) {
            // Check if this is a king in its starting position
            if (isWhite && position == WHITE_KING_START && !bitboard.whiteKingMoved) {
                // White kingside castling
                if (!bitboard.whiteKingsideRookMoved) {
                    ulong pathMask = (1UL << 1) | (1UL << 2); // g1, f1 in new mapping
                    if ((allPieces & pathMask) == 0) {
                        // Add kingside castling move if path is clear
                        moves |= 1UL << 1; // g1 in new mapping
                        if (debugMode) Debug.Log("Added potential white kingside castling to moveset");
                    }
                }
                
                // White queenside castling
                if (!bitboard.whiteQueensideRookMoved) {
                    ulong pathMask = (1UL << 4) | (1UL << 5) | (1UL << 6); // d1, c1, b1 in new mapping
                    if ((allPieces & pathMask) == 0) {
                        // Add queenside castling move if path is clear
                        moves |= 1UL << 5; // c1 in new mapping
                        if (debugMode) Debug.Log("Added potential white queenside castling to moveset");
                    }
                }
            }
            else if (!isWhite && position == BLACK_KING_START && !bitboard.blackKingMoved) {
                // Black kingside castling
                if (!bitboard.blackKingsideRookMoved) {
                    ulong pathMask = (1UL << 57) | (1UL << 58); // g8, f8 in new mapping
                    if ((allPieces & pathMask) == 0) {
                        // Add kingside castling move if path is clear
                        moves |= 1UL << 57; // g8 in new mapping
                        if (debugMode) Debug.Log("Added potential black kingside castling to moveset");
                    }
                }
                
                // Black queenside castling
                if (!bitboard.blackQueensideRookMoved) {
                    ulong pathMask = (1UL << 60) | (1UL << 61) | (1UL << 62); // d8, c8, b8 in new mapping
                    if ((allPieces & pathMask) == 0) {
                        // Add queenside castling move if path is clear
                        moves |= 1UL << 61; // c8 in new mapping
                        if (debugMode) Debug.Log("Added potential black queenside castling to moveset");
                    }
                }
            }
        }
        
        // If pinned, filter moves to only those along the pin line
        if (pieceType != PieceType.King && IsPiecePinned(position, isWhite)) {
            // Find king position
            ulong kingBitboard = isWhite ? bitboard.WhiteKing : bitboard.BlackKing;
            int kingIndex = BitOperations.TrailingZeroCount(kingBitboard);
            
            int pieceRank = position / 8;
            int pieceFile = position % 8;
            int kingRank = kingIndex / 8;
            int kingFile = kingIndex % 8;
            
            ulong pinLine = 0;
            
            // Same rank
            if (pieceRank == kingRank) {
                for (int f = 0; f < 8; f++) {
                    pinLine |= 1UL << (pieceRank * 8 + f);
                }
            }
            // Same file
            else if (pieceFile == kingFile) {
                for (int r = 0; r < 8; r++) {
                    pinLine |= 1UL << (r * 8 + pieceFile);
                }
            }
            // Same diagonal
            else if (Math.Abs(pieceRank - kingRank) == Math.Abs(pieceFile - kingFile)) {
                // Determine diagonal direction
                int rankDir = kingRank < pieceRank ? 1 : -1;
                int fileDir = kingFile < pieceFile ? 1 : -1;
                
                // Add all squares on this diagonal
                for (int r = 0, f = 0; r < 8 && f < 8; r++, f++) {
                    int newRank = kingRank + r * rankDir;
                    int newFile = kingFile + f * fileDir;
                    
                    if (newRank >= 0 && newRank < 8 && newFile >= 0 && newFile < 8) {
                        pinLine |= 1UL << (newRank * 8 + newFile);
                    }
                }
            }
            
            // For knights, can't move at all when pinned
            if (pieceType == PieceType.Knight) {
                return 0;
            }
            
            // For other pieces, can only move along the pin line
            moves &= pinLine;
        }
        
        return moves;
    }
    private ulong CalculatePawnMoves(int position, bool isWhite, ulong allPieces, ulong ownPieces, ulong enemyPieces)
    {
        ulong moves = 0;
        ulong positionBit = 1UL << position;
        
        int rank = position / 8;
        int file = position % 8;
        
        if (isWhite)
        {
            // Forward move (1 square)
            if (rank < 7 && ((allPieces & (1UL << (position + 8))) == 0))
            {
                moves |= 1UL << (position + 8);
                
                // Double forward move (from starting position)
                if (rank == 1 && ((allPieces & (1UL << (position + 16))) == 0))
                {
                    moves |= 1UL << (position + 16);
                }
            }
            
            // Capture moves
            if (rank < 7)
            {
                // Capture to the left
                if (file > 0 && ((enemyPieces & (1UL << (position + 7))) != 0))
                {
                    moves |= 1UL << (position + 7);
                }
                
                // Capture to the right
                if (file < 7 && ((enemyPieces & (1UL << (position + 9))) != 0))
                {
                    moves |= 1UL << (position + 9);
                }
            }
        }
        else // Black pawn
        {
            // Forward move (1 square)
            if (rank > 0 && ((allPieces & (1UL << (position - 8))) == 0))
            {
                moves |= 1UL << (position - 8);
                
                // Double forward move (from starting position)
                if (rank == 6 && ((allPieces & (1UL << (position - 16))) == 0))
                {
                    moves |= 1UL << (position - 16);
                }
            }
            
            // Capture moves
            if (rank > 0)
            {
                // Capture to the left
                if (file < 7 && ((enemyPieces & (1UL << (position - 7))) != 0))
                {
                    moves |= 1UL << (position - 7);
                }
                
                // Capture to the right
                if (file > 0 && ((enemyPieces & (1UL << (position - 9))) != 0))
                {
                    moves |= 1UL << (position - 9);
                }
            }
        }
        
        return moves;
    }

    // Calculate possible knight moves
    private ulong CalculateKnightMoves(int position, ulong friendlyPieces)
    {
        ulong knightPositionBitboard = 1UL << position;
        ulong knightMoves = 0;
        
        int file = position % 8;
        int rank = position / 8;
        
        // The eight possible knight moves (up 2 right 1, up 1 right 2, etc.)
        int[][] moveOffsets = {
            new int[] { 2, 1 }, new int[] { 1, 2 },
            new int[] { -1, 2 }, new int[] { -2, 1 },
            new int[] { -2, -1 }, new int[] { -1, -2 },
            new int[] { 1, -2 }, new int[] { 2, -1 }
        };
        
        foreach (int[] offset in moveOffsets)
        {
            int newRank = rank + offset[0];
            int newFile = file + offset[1];
            
            // Check if the target square is on the board
            if (newRank >= 0 && newRank < 8 && newFile >= 0 && newFile < 8)
            {
                int targetSquare = newRank * 8 + newFile;
                ulong targetMask = 1UL << targetSquare;
                
                // Check if the target square doesn't contain a friendly piece
                if ((targetMask & friendlyPieces) == 0)
                {
                    knightMoves |= targetMask;
                    
                    if (debugMode)
                    {
                        Debug.Log($"Knight at {GetSquareName(position)} can move to {GetSquareName(targetSquare)}");
                    }
                }
                else
                {
                    if (debugMode)
                    {
                        Debug.Log($"Knight at {GetSquareName(position)} can't move to {GetSquareName(targetSquare)} - occupied by friendly piece");
                    }
                }
            }
        }
        
        return knightMoves;
    }

    // Calculate possible bishop moves
    private ulong CalculateBishopMoves(int position, ulong allPieces, ulong ownPieces)
    {
        ulong moves = 0;
        int rank = position / 8;
        int file = position % 8;
        
        // The four diagonal directions
        int[][] directions = {
            new int[] { 1, 1 },   // Up-right
            new int[] { 1, -1 },  // Up-left
            new int[] { -1, 1 },  // Down-right
            new int[] { -1, -1 }  // Down-left
        };
        
        foreach (int[] dir in directions)
        {
            int newRank = rank;
            int newFile = file;
            
            // Continue in this direction until we hit a piece or the edge of the board
            while (true)
            {
                newRank += dir[0];
                newFile += dir[1];
                
                // Check if we're still on the board
                if (newRank < 0 || newRank >= 8 || newFile < 0 || newFile >= 8)
                    break;
                
                int newPosition = newRank * 8 + newFile;
                ulong newPositionBit = 1UL << newPosition;
                
                // If the square is occupied by our own piece, we can't move here
                if ((ownPieces & newPositionBit) != 0)
                    break;
                
                // Add this move
                moves |= newPositionBit;
                
                // If the square is occupied by any piece, we can't move past it
                if ((allPieces & newPositionBit) != 0)
                    break;
            }
        }
        
        return moves;
    }


    // Calculate possible rook moves
    private ulong CalculateRookMoves(int position, ulong allPieces, ulong ownPieces)
    {
        ulong moves = 0;
        int rank = position / 8;
        int file = position % 8;
        
        // The four orthogonal directions
        int[][] directions = {
            new int[] { 1, 0 },   // Up
            new int[] { -1, 0 },  // Down
            new int[] { 0, 1 },   // Right
            new int[] { 0, -1 }   // Left
        };
        
        foreach (int[] dir in directions)
        {
            int newRank = rank;
            int newFile = file;
            
            // Continue in this direction until we hit a piece or the edge of the board
            while (true)
            {
                newRank += dir[0];
                newFile += dir[1];
                
                // Check if we're still on the board
                if (newRank < 0 || newRank >= 8 || newFile < 0 || newFile >= 8)
                    break;
                
                int newPosition = newRank * 8 + newFile;
                ulong newPositionBit = 1UL << newPosition;
                
                // If the square is occupied by our own piece, we can't move here
                if ((ownPieces & newPositionBit) != 0)
                    break;
                
                // Add this move
                moves |= newPositionBit;
                
                // If the square is occupied by any piece, we can't move past it
                if ((allPieces & newPositionBit) != 0)
                    break;
            }
        }
        
        return moves;
    }


    // Calculate possible queen moves (combines bishop and rook moves)
    private ulong CalculateQueenMoves(int position, ulong allPieces, ulong ownPieces)
    {
        return CalculateBishopMoves(position, allPieces, ownPieces) | 
               CalculateRookMoves(position, allPieces, ownPieces);
    }


    // Calculate possible king moves
    private ulong CalculateKingMoves(int position, ulong ownPieces)
    {
        ulong moves = 0;
        int rank = position / 8;
        int file = position % 8;
        
        // All 8 possible directions
        for (int rankOffset = -1; rankOffset <= 1; rankOffset++)
        {
            for (int fileOffset = -1; fileOffset <= 1; fileOffset++)
            {
                // Skip the current position
                if (rankOffset == 0 && fileOffset == 0) continue;
                
                int newRank = rank + rankOffset;
                int newFile = file + fileOffset;
                
                // Check if the new position is on the board
                if (newRank >= 0 && newRank < 8 && newFile >= 0 && newFile < 8)
                {
                    int newPosition = newRank * 8 + newFile;
                    
                    // Make sure we're not moving to a square occupied by our own piece
                    if ((ownPieces & (1UL << newPosition)) == 0)
                    {
                        moves |= 1UL << newPosition;
                    }
                }
            }
        }
        
        return moves;
    }
    private string GetSquareName(int position)
    {
        return BitboardUtils.IndexToAlgebraic(position);
    }
    
    /// Helper method to find the trailing zero count in a 64-bit integer

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