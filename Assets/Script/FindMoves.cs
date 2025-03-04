using System;
using System.Collections.Generic;
using UnityEngine;

public class FindMoves
{
    private Bitboard bitboard;
    private Evaluation evaluation;
    
    // Debug mode to log move generation details
    private bool debugMode = false;

    public FindMoves(Bitboard bitboard)
    {
        this.bitboard = bitboard;
        this.evaluation = new Evaluation();
        Debug.Log("FindMoves initialized with bitboard");
    }

    /// <summary>
    /// Gets all legal moves for the specified side
    /// </summary>
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
    
    /// <summary>
    /// Adds moves for all pieces of a specific type
    /// </summary>
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
    
    /// <summary>
    /// Filters out moves that would leave the king in check
    /// </summary>
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
    
    /// <summary>
    /// Returns all possible moves for a piece at the given position
    /// </summary>
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
    
    /// <summary>
    /// Calculate possible moves for a piece based on its type and color
    /// </summary>
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
        
        // Delegate to the appropriate move generator
        switch (pieceType)
        {
            case PieceType.Pawn:
                return CalculatePawnMoves(position, isWhite, allPieces, ownPieces, enemyPieces);
            case PieceType.Knight:
                return CalculateKnightMoves(position, ownPieces);
            case PieceType.Bishop:
                return CalculateBishopMoves(position, allPieces, ownPieces);
            case PieceType.Rook:
                return CalculateRookMoves(position, allPieces, ownPieces);
            case PieceType.Queen:
                return CalculateQueenMoves(position, allPieces, ownPieces);
            case PieceType.King:
                return CalculateKingMoves(position, ownPieces);
            default:
                Debug.LogError($"Unknown piece type: {pieceType}");
                return 0;
        }
    }

    /// <summary>
    /// Calculate possible pawn moves
    /// </summary>
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

    /// <summary>
    /// Calculate possible knight moves
    /// </summary>
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

    /// <summary>
    /// Calculate possible bishop moves
    /// </summary>
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

    /// <summary>
    /// Calculate possible rook moves
    /// </summary>
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

    /// <summary>
    /// Calculate possible queen moves (combines bishop and rook moves)
    /// </summary>
    private ulong CalculateQueenMoves(int position, ulong allPieces, ulong ownPieces)
    {
        return CalculateBishopMoves(position, allPieces, ownPieces) | 
               CalculateRookMoves(position, allPieces, ownPieces);
    }

    /// <summary>
    /// Calculate possible king moves
    /// </summary>
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
    
    /// <summary>
    /// Helper method to convert a position to algebraic notation (e.g., "e4")
    /// </summary>
    private string GetSquareName(int position)
    {
        int rank = position / 8;
        int file = position % 8;
        
        char fileChar = (char)('a' + file);
        char rankChar = (char)('1' + rank);
        
        return $"{fileChar}{rankChar}";
    }
    
    /// <summary>
    /// Helper method to find the trailing zero count in a 64-bit integer
    /// </summary>
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