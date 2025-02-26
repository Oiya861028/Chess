using UnityEngine;

public class FindMoves : MonoBehaviour
{
    // Function to find all possible moves for a piece at a specific position
    public ulong GetPossibleMoves(int position, ulong whitePawn, ulong whiteRook, ulong whiteKnight, ulong whiteBishop, ulong whiteQueen, ulong whiteKing, ulong blackPawn, ulong blackRook, ulong blackKnight, ulong blackBishop, ulong blackQueen, ulong blackKing)
    {
        // Check if the position contains a piece
        ulong positionMask = 1UL << position;
        
        // Determine the type and color of the piece
        bool isWhite = (whitePawn & positionMask) != 0 || (whiteRook & positionMask) != 0 || (whiteKnight & positionMask) != 0 || (whiteBishop & positionMask) != 0 || (whiteQueen & positionMask) != 0 || (whiteKing & positionMask) != 0;
        
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
                if ((pawnPositionBitboard & 0b0000000000000000000000000000000000000000000000001111111100000000) != 0)
                {
                    ulong twoStep = oneStep << 8;
                    if ((twoStep & allPieces) == 0)
                    {
                        pawnMoves |= twoStep;
                    }
                }
            }
            
            // Capture diagonally
            ulong captureLeft = (pawnPositionBitboard << 7) & ~0b1000000010000000100000001000000010000000100000001000000010000000; // Avoid wrapping to h-file
            ulong captureRight = (pawnPositionBitboard << 9) & ~0b0000000100000001000000010000000100000001000000010000000100000001; // Avoid wrapping to a-file
            
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
                if ((pawnPositionBitboard & 0b0000000011111111000000000000000000000000000000000000000000000000) != 0)
                {
                    ulong twoStep = oneStep >> 8;
                    if ((twoStep & allPieces) == 0)
                    {
                        pawnMoves |= twoStep;
                    }
                }
            }
            
            // Capture diagonally
            ulong captureLeft = (pawnPositionBitboard >> 9) & ~0b1000000010000000100000001000000010000000100000001000000010000000; // Avoid wrapping to h-file
            ulong captureRight = (pawnPositionBitboard >> 7) & ~0b0000000100000001000000010000000100000001000000010000000100000001; // Avoid wrapping to a-file
            
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
        ulong notABFile = ~(0b0000001100000011000000110000001100000011000000110000001100000011);
        // Avoid a-file (leftmost file)
        ulong notAFile = ~(0b0000000100000001000000010000000100000001000000010000000100000001);
        // Avoid h-file (rightmost file)
        ulong notHFile = ~(0b1000000010000000100000001000000010000000100000001000000010000000);
        // Avoid g-file and h-file (rightmost two files)
        ulong notGHFile = ~(0b1100000011000000110000001100000011000000110000001100000011000000);
        
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