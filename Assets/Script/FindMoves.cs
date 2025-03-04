using System;
using System.Collections.Generic;

public class FindMoves
{
    private Bitboard bitboard;
    public FindMoves(Bitboard bitboard) //pass in the bitboard you created in Controller class 
    {
        this.bitboard = bitboard;
    }
    public List<Move> GetAllPossibleMoves(bool isWhite, Move previousMove)
    {
       
        return (isWhite) ? GetAllPossibleWhiteMoves(previousMove) : GetAllPossibleBlackMoves(previousMove);
        
    }
    //Return a list of all possible moves for white pieces
    private List<Move> GetAllPossibleWhiteMoves(Move previousMove)
    {
        List<Move> allPossibleWhiteMoves = new List<Move>();
        ulong currentPieces = bitboard.WhitePawn | bitboard.WhiteRook | bitboard.WhiteKnight | 
                              bitboard.WhiteBishop | bitboard.WhiteQueen | bitboard.WhiteKing;
        
        for(int i = 0; i < 64; i++){
            ulong positionMask = 1UL << i;
            if((currentPieces & positionMask) != 0){
                PieceType pieceType;
                if((bitboard.WhitePawn & positionMask) != 0) pieceType = PieceType.Pawn;
                else if((bitboard.WhiteRook & positionMask) != 0) pieceType = PieceType.Rook;
                else if((bitboard.WhiteKnight & positionMask) != 0) pieceType = PieceType.Knight;
                else if((bitboard.WhiteBishop & positionMask) != 0) pieceType = PieceType.Bishop;
                else if((bitboard.WhiteQueen & positionMask) != 0) pieceType = PieceType.Queen;
                else pieceType = PieceType.King;

                ulong possibleMoves = GetPossibleMoves(i);

                for(int j = 0; j < 64; j++){
                    if((possibleMoves & (1UL << j)) != 0){
                        allPossibleWhiteMoves.Add(new Move(i, j, previousMove, (int)pieceType, true));
                    }
                }
            }
        }
        return allPossibleWhiteMoves;
    }
    //Return a list of all possible moves for black pieces
    private List<Move> GetAllPossibleBlackMoves(Move previousMove)
    {
        ulong currentPieces = bitboard.BlackPawn | bitboard.BlackRook | bitboard.BlackKnight | 
                              bitboard.BlackBishop | bitboard.BlackQueen | bitboard.BlackKing;

        List<Move> allPossibleBlackMoves = new List<Move>();
        for(int i = 0; i < 64; i++){
            ulong positionMask = 1UL << i;
            if((currentPieces & positionMask) != 0){
                PieceType pieceType;
                if((bitboard.BlackPawn & positionMask) != 0) pieceType = PieceType.Pawn;
                else if((bitboard.BlackRook & positionMask) != 0) pieceType = PieceType.Rook;
                else if((bitboard.BlackKnight & positionMask) != 0) pieceType = PieceType.Knight;
                else if((bitboard.BlackBishop & positionMask) != 0) pieceType = PieceType.Bishop;
                else if((bitboard.BlackQueen & positionMask) != 0) pieceType = PieceType.Queen;
                else pieceType = PieceType.King;

                ulong possibleMoves = GetPossibleMoves(i);

                for(int j = 0; j < 64; j++){
                    if((possibleMoves & (1UL << j)) != 0){
                        allPossibleBlackMoves.Add(new Move(i, j, previousMove, (int)pieceType, false));
                    }
                }
            }
        }
        return allPossibleBlackMoves;
    }
    
    // Function to find all possible moves for a piece at a specific position
    public ulong GetPossibleMoves(int position)
    {
        // Extract the bitboards for all pieces
        ulong whitePawn = bitboard.WhitePawn;
        ulong whiteRook = bitboard.WhiteRook;
        ulong whiteKnight = bitboard.WhiteKnight;
        ulong whiteBishop = bitboard.WhiteBishop;
        ulong whiteQueen = bitboard.WhiteQueen;
        ulong whiteKing = bitboard.WhiteKing;
        ulong blackPawn = bitboard.BlackPawn;
        ulong blackRook = bitboard.BlackRook;
        ulong blackKnight = bitboard.BlackKnight;
        ulong blackBishop = bitboard.BlackBishop;
        ulong blackQueen = bitboard.BlackQueen;
        ulong blackKing = bitboard.BlackKing;
        
        // Check if the position contains a piece
        ulong positionMask = 1UL << position;
        
        // Determine the color of the piece
        bool isWhite = (whitePawn & positionMask) != 0 || 
                        (whiteRook & positionMask) != 0 || 
                        (whiteKnight & positionMask) != 0 || 
                        (whiteBishop & positionMask) != 0 || 
                        (whiteQueen & positionMask) != 0 || 
                        (whiteKing & positionMask) != 0;
        
        // Compute all pieces on the board
        ulong allWhitePieces = whitePawn | whiteRook | whiteKnight | whiteBishop | whiteQueen | whiteKing;
        ulong allBlackPieces = blackPawn | blackRook | blackKnight | blackBishop | blackQueen | blackKing;
        ulong allPieces = allWhitePieces | allBlackPieces;
        
        // Compute friendly and enemy pieces
        ulong friendlyPieces = isWhite ? allWhitePieces : allBlackPieces;
        ulong enemyPieces = isWhite ? allBlackPieces : allWhitePieces;
        
        // Determine the type of the piece and call the appropriate function
        if ((whitePawn & positionMask) != 0 || (blackPawn & positionMask) != 0)
            return GetPawnMoves(position, isWhite, allPieces, friendlyPieces, enemyPieces);
        else if ((whiteRook & positionMask) != 0 || (blackRook & positionMask) != 0)
            return GetRookMoves(position, allPieces, friendlyPieces);
        else if ((whiteKnight & positionMask) != 0 || (blackKnight & positionMask) != 0)
            return GetKnightMoves(position, friendlyPieces);
        else if ((whiteBishop & positionMask) != 0 || (blackBishop & positionMask) != 0)
            return GetBishopMoves(position, allPieces, friendlyPieces);
        else if ((whiteQueen & positionMask) != 0 || (blackQueen & positionMask) != 0)
            return GetQueenMoves(position, allPieces, friendlyPieces);
        else if ((whiteKing & positionMask) != 0 || (blackKing & positionMask) != 0)
            return GetKingMoves(position, friendlyPieces);
        else
            return 0; // No piece at the specified position
    }
    
    private ulong GetPawnMoves(int position, bool isWhite, ulong allPieces, ulong friendlyPieces, ulong enemyPieces)
    {
        ulong pawnMoves = 0;
        ulong pawnPositionBitboard = 1UL << position;
        
        if (isWhite)
        {
            // Move forward one square
            ulong oneStep = pawnPositionBitboard << 8;
            if ((oneStep & allPieces) == 0)
            {
                pawnMoves |= oneStep;
                
                // Move forward two squares (only from the second rank)
                if ((pawnPositionBitboard & 0x000000000000FF00UL) != 0)
                {
                    ulong twoStep = oneStep << 8;
                    if ((twoStep & allPieces) == 0)
                    {
                        pawnMoves |= twoStep;
                    }
                }
            }
            
            // Capture diagonally
            ulong captureLeft = (pawnPositionBitboard << 7) & ~0x8080808080808080UL; // Avoid wrapping to h-file
            ulong captureRight = (pawnPositionBitboard << 9) & ~0x0101010101010101UL; // Avoid wrapping to a-file
            
            pawnMoves |= captureLeft & enemyPieces;
            pawnMoves |= captureRight & enemyPieces;
        }
        else
        {
            // Move forward one square
            ulong oneStep = pawnPositionBitboard >> 8;
            if ((oneStep & allPieces) == 0)
            {
                pawnMoves |= oneStep;
                
                // Move forward two squares (only from the seventh rank)
                if ((pawnPositionBitboard & 0x00FF000000000000UL) != 0)
                {
                    ulong twoStep = oneStep >> 8;
                    if ((twoStep & allPieces) == 0)
                    {
                        pawnMoves |= twoStep;
                    }
                }
            }
            
            // Capture diagonally
            ulong captureLeft = (pawnPositionBitboard >> 9) & ~0x8080808080808080UL; // Avoid wrapping to h-file
            ulong captureRight = (pawnPositionBitboard >> 7) & ~0x0101010101010101UL; // Avoid wrapping to a-file
            
            pawnMoves |= captureLeft & enemyPieces;
            pawnMoves |= captureRight & enemyPieces;
        }
        
        return pawnMoves;
    }
    
    private ulong GetKnightMoves(int position, ulong friendlyPieces)
    {
        ulong knightPositionBitboard = 1UL << position;
        ulong knightMoves = 0;
        
        // Avoid a-file and b-file (leftmost two files)
        ulong notABFile = ~0x0303030303030303UL;
        // Avoid a-file (leftmost file)
        ulong notAFile = ~0x0101010101010101UL;
        // Avoid h-file (rightmost file)
        ulong notHFile = ~0x8080808080808080UL;
        // Avoid g-file and h-file (rightmost two files)
        ulong notGHFile = ~0xC0C0C0C0C0C0C0C0UL;
        
        // Knight move patterns:
        // 1. Up 2, Right 1
        knightMoves |= (knightPositionBitboard << 17) & notAFile;
        // 2. Up 1, Right 2
        knightMoves |= (knightPositionBitboard << 10) & notABFile;
        // 3. Down 1, Right 2
        knightMoves |= (knightPositionBitboard >> 6) & notABFile;
        // 4. Down 2, Right 1
        knightMoves |= (knightPositionBitboard >> 15) & notAFile;
        // 5. Down 2, Left 1
        knightMoves |= (knightPositionBitboard >> 17) & notHFile;
        // 6. Down 1, Left 2
        knightMoves |= (knightPositionBitboard >> 10) & notGHFile;
        // 7. Up 1, Left 2
        knightMoves |= (knightPositionBitboard << 6) & notGHFile;
        // 8. Up 2, Left 1
        knightMoves |= (knightPositionBitboard << 15) & notHFile;
        
        // Remove squares occupied by friendly pieces
        knightMoves &= ~friendlyPieces;
        
        return knightMoves;
    }
    
    private ulong GetBishopMoves(int position, ulong allPieces, ulong friendlyPieces)
    {
        ulong bishopMoves = 0;
        int rankIdx = position / 8;
        int fileIdx = position % 8;
        
        // Generate moves in all four diagonal directions:
        
        // 1. Northeast (up-right)
        for (int f = fileIdx + 1, r = rankIdx + 1; f < 8 && r < 8; f++, r++)
        {
            int idx = r * 8 + f;
            ulong move = 1UL << idx;
            bishopMoves |= move;
            if ((allPieces & move) != 0) break; // Hit a piece
        }
        
        // 2. Southeast (down-right)
        for (int f = fileIdx + 1, r = rankIdx - 1; f < 8 && r >= 0; f++, r--)
        {
            int idx = r * 8 + f;
            ulong move = 1UL << idx;
            bishopMoves |= move;
            if ((allPieces & move) != 0) break; // Hit a piece
        }
        
        // 3. Southwest (down-left)
        for (int f = fileIdx - 1, r = rankIdx - 1; f >= 0 && r >= 0; f--, r--)
        {
            int idx = r * 8 + f;
            ulong move = 1UL << idx;
            bishopMoves |= move;
            if ((allPieces & move) != 0) break; // Hit a piece
        }
        
        // 4. Northwest (up-left)
        for (int f = fileIdx - 1, r = rankIdx + 1; f >= 0 && r < 8; f--, r++)
        {
            int idx = r * 8 + f;
            ulong move = 1UL << idx;
            bishopMoves |= move;
            if ((allPieces & move) != 0) break; // Hit a piece
        }
        
        // Remove squares occupied by friendly pieces
        bishopMoves &= ~friendlyPieces;
        
        return bishopMoves;
    }
    
    private ulong GetRookMoves(int position, ulong allPieces, ulong friendlyPieces)
    {
        ulong rookMoves = 0;
        int rankIdx = position / 8;
        int fileIdx = position % 8;
        
        // Generate moves in all four directions:
        
        // 1. North (up)
        for (int r = rankIdx + 1; r < 8; r++)
        {
            int idx = r * 8 + fileIdx;
            ulong move = 1UL << idx;
            rookMoves |= move;
            if ((allPieces & move) != 0) break; // Hit a piece
        }
        
        // 2. East (right)
        for (int f = fileIdx + 1; f < 8; f++)
        {
            int idx = rankIdx * 8 + f;
            ulong move = 1UL << idx;
            rookMoves |= move;
            if ((allPieces & move) != 0) break; // Hit a piece
        }
        
        // 3. South (down)
        for (int r = rankIdx - 1; r >= 0; r--)
        {
            int idx = r * 8 + fileIdx;
            ulong move = 1UL << idx;
            rookMoves |= move;
            if ((allPieces & move) != 0) break; // Hit a piece
        }
        
        // 4. West (left)
        for (int f = fileIdx - 1; f >= 0; f--)
        {
            int idx = rankIdx * 8 + f;
            ulong move = 1UL << idx;
            rookMoves |= move;
            if ((allPieces & move) != 0) break; // Hit a piece
        }
        
        // Remove squares occupied by friendly pieces
        rookMoves &= ~friendlyPieces;
        
        return rookMoves;
    }
    
    private ulong GetQueenMoves(int position, ulong allPieces, ulong friendlyPieces)
    {
        // Queen moves are a combination of rook and bishop moves
        return GetRookMoves(position, allPieces, friendlyPieces) | GetBishopMoves(position, allPieces, friendlyPieces);
    }
    
    private ulong GetKingMoves(int position, ulong friendlyPieces)
    {
        ulong kingPositionBitboard = 1UL << position;
        ulong kingMoves = 0;
        
        // Avoid a-file (leftmost file)
        ulong notAFile = ~0x0101010101010101UL;
        // Avoid h-file (rightmost file)
        ulong notHFile = ~0x8080808080808080UL;
        
        // Move in all eight directions:
        // 1. North (up)
        kingMoves |= (kingPositionBitboard << 8);
        // 2. Northeast (up-right)
        kingMoves |= (kingPositionBitboard << 9) & notAFile;
        // 3. East (right)
        kingMoves |= (kingPositionBitboard << 1) & notAFile;
        // 4. Southeast (down-right)
        kingMoves |= (kingPositionBitboard >> 7) & notAFile;
        // 5. South (down)
        kingMoves |= (kingPositionBitboard >> 8);
        // 6. Southwest (down-left)
        kingMoves |= (kingPositionBitboard >> 9) & notHFile;
        // 7. West (left)
        kingMoves |= (kingPositionBitboard >> 1) & notHFile;
        // 8. Northwest (up-left)
        kingMoves |= (kingPositionBitboard << 7) & notHFile;
        
        // Remove squares occupied by friendly pieces
        kingMoves &= ~friendlyPieces;
        
        return kingMoves;
    }
}