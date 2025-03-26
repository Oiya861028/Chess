using System;
using System.Collections.Generic;
using UnityEngine;

public class FindMoves
{
    private Bitboard bitboard;
    private Evaluation evaluation;
    private int previousMove; // Changed from Move to int
    private MagicBitboards magicBitboards; // Added magic bitboards
    
    // Constants for the correct bit positions
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
        this.previousMove = 0; // Default integer value (was null)
        this.magicBitboards = new MagicBitboards(); // Initialize magic bitboards
        Debug.Log("FindMoves initialized with bitboard and magic bitboards");
    }

    public void SetPreviousMove(int move)
    {
        previousMove = move;
        if (debugMode && move != 0)
        {
            Debug.Log($"Previous move set to: {BitboardUtils.IndexToAlgebraic(MoveUtil.GetSource(move))} to {BitboardUtils.IndexToAlgebraic(MoveUtil.GetDestination(move))}, IsPawnDoubleMove: {MoveUtil.IsPawnDoubleMove(move)}");
        }
    }

    /// Gets all legal moves for the specified side
    public List<int> GetAllPossibleMoves(bool isWhite, int previousMove)
    {
        if (debugMode) Debug.Log("GetAllPossibleMoves called for " + (isWhite ? "white" : "black"));
        
        List<int> moves = new List<int>();
        
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
        List<int> legalMoves = FilterLegalMoves(moves, isWhite);
        
        if (debugMode) Debug.Log($"Found {legalMoves.Count} legal moves");
        
        // Check castling moves and add them if legal
        int kingStartPosition = isWhite ? WHITE_KING_START : BLACK_KING_START;
        bool kingMoved = isWhite ? bitboard.whiteKingMoved : bitboard.blackKingMoved;
        bool kingInCheck = evaluation.IsInCheck(isWhite, bitboard.returnWhitePiecesByTypes(), bitboard.returnBlackPiecesByTypes(), bitboard.returnAllPieces());
        if (!kingInCheck && !kingMoved) {
            ulong kingBitboard = isWhite ? bitboard.WhiteKing : bitboard.BlackKing;
            
            // Verify king is at starting position
            if ((kingBitboard & (1UL << kingStartPosition)) != 0) {
                // Check kingside castling
                bool kingsideRookMoved = isWhite ? bitboard.whiteKingsideRookMoved : bitboard.blackKingsideRookMoved;
                if (!kingsideRookMoved) {
                    // Adjusted paths for reversed bit ordering
                    ulong kingsidePath = isWhite ? 
                                        ((1UL << 1) | (1UL << 2)) :  
                                        ((1UL << 57) | (1UL << 58));  
                    
                    if ((bitboard.returnAllPieces() & kingsidePath) == 0) {
                        // Check if king passes through check
                        int passThroughSquare = isWhite ? 2 : 58; 
                        
                        // Try moving king to pass through square
                        int passThroughMove = MoveUtil.EncodeMove(
                            kingStartPosition, passThroughSquare, 
                            (int)PieceType.King, isWhite);
                            
                        bitboard.UpdateBitBoard(passThroughMove);
                        
                        bool passThroughCheck = evaluation.IsInCheck(isWhite, 
                                                                    bitboard.returnWhitePiecesByTypes(), 
                                                                    bitboard.returnBlackPiecesByTypes(), 
                                                                    bitboard.returnAllPieces());
                                                                    
                        bitboard.UndoBitboard();
                        
                        if (!passThroughCheck) {
                            // Check destination square
                            int destSquare = isWhite ? 1 : 57; 
                            
                            int destMove = MoveUtil.EncodeMove(
                                kingStartPosition, destSquare, 
                                (int)PieceType.King, isWhite);
                                
                            bitboard.UpdateBitBoard(destMove);
                            
                            bool destCheck = evaluation.IsInCheck(isWhite, 
                                                                bitboard.returnWhitePiecesByTypes(), 
                                                                bitboard.returnBlackPiecesByTypes(), 
                                                                bitboard.returnAllPieces());
                                                                
                            bitboard.UndoBitboard();
                            
                            if (!destCheck) {
                                // add kingside castling move
                                int castlingMove = MoveUtil.EncodeMove(
                                    kingStartPosition, destSquare, 
                                    (int)PieceType.King, isWhite,
                                    false, false, false, 0, true);
                                    
                                legalMoves.Add(castlingMove);
                                
                                if (debugMode) Debug.Log($"Added {(isWhite ? "white" : "black")} kingside castling move");
                            }
                        }
                    }
                }
                
                // Check queenside castling
                bool queensideRookMoved = isWhite ? bitboard.whiteQueensideRookMoved : bitboard.blackQueensideRookMoved;
                if (!queensideRookMoved) {
                    // Adjusted paths for reversed bit ordering
                    ulong queensidePath = isWhite ? 
                                        ((1UL << 4) | (1UL << 5) | (1UL << 6)) : 
                                        ((1UL << 60) | (1UL << 61) | (1UL << 62));
                    
                    if ((bitboard.returnAllPieces() & queensidePath) == 0) {
                        // Check if king passes through check
                        int passThroughSquare = isWhite ? 4 : 60; 
                        // Try moving king to pass through square
                        int passThroughMove = MoveUtil.EncodeMove(
                            kingStartPosition, passThroughSquare, 
                            (int)PieceType.King, isWhite);
                            
                        bitboard.UpdateBitBoard(passThroughMove);
                        
                        bool passThroughCheck = evaluation.IsInCheck(isWhite, 
                                                                    bitboard.returnWhitePiecesByTypes(), 
                                                                    bitboard.returnBlackPiecesByTypes(), 
                                                                    bitboard.returnAllPieces());
                                                                    
                        bitboard.UndoBitboard();
                        
                        if (!passThroughCheck) {
                            // Check destination square
                            int destSquare = isWhite ? 5 : 61; 
                            
                            int destMove = MoveUtil.EncodeMove(
                                kingStartPosition, destSquare, 
                                (int)PieceType.King, isWhite);
                                
                            bitboard.UpdateBitBoard(destMove);
                            
                            bool destCheck = evaluation.IsInCheck(isWhite, 
                                                                bitboard.returnWhitePiecesByTypes(), 
                                                                bitboard.returnBlackPiecesByTypes(), 
                                                                bitboard.returnAllPieces());
                                                                
                            bitboard.UndoBitboard();
                            
                            if (!destCheck) {
                                // Add queenside castling move
                                int castlingMove = MoveUtil.EncodeMove(
                                    kingStartPosition, destSquare, 
                                    (int)PieceType.King, isWhite,
                                    false, false, false, 0, true);
                                    
                                legalMoves.Add(castlingMove);
                                
                                if (debugMode) Debug.Log($"Added {(isWhite ? "white" : "black")} queenside castling move");
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
    private void AddMovesForPosition(List<int> moves, int position, PieceType pieceType, bool isWhite, int previousMove)
    {
        ulong moveBitboard = GetPossibleMovesForPiece(position, pieceType, isWhite);
        
        // Convert bits to integer moves
        for (int i = 0; i < 64; i++)
        {
            if ((moveBitboard & (1UL << i)) != 0)
            {
                bool isEnPassant = false;
                bool isPawnDoubleMove = false;
                bool isPromotion = false;
                int promotionPieceType = (int)PieceType.Queen; // Default to Queen
                
                if (pieceType == PieceType.Pawn)
                {
                    // Check for pawn double move
                    if (Math.Abs(position - i) == 16)
                    {
                        isPawnDoubleMove = true;
                    }
                    
                    // Check for promotion
                    int destRank = i / 8;
                    if ((isWhite && destRank == 7) || (!isWhite && destRank == 0))
                    {
                        isPromotion = true;
                        if (debugMode) Debug.Log($"Pawn promotion detected from {BitboardUtils.IndexToAlgebraic(position)} to {BitboardUtils.IndexToAlgebraic(i)}");
                    }
                    
                    // Check for en passant capture
                    int rank = position / 8;
                    int file = position % 8;
                    int targetRank = i / 8;
                    int targetFile = i % 8;
                    
                    // If moving diagonally to an empty square, it might be en passant
                    if (file != targetFile && (bitboard.returnAllPieces() & (1UL << i)) == 0)
                    {
                        // For white pawns on rank 5
                        if (isWhite && rank == 4 && previousMove != 0 &&
                            MoveUtil.GetPieceType(previousMove) == (int)PieceType.Pawn &&
                            !MoveUtil.IsWhite(previousMove) && MoveUtil.IsPawnDoubleMove(previousMove))
                        {
                            int enPassantFile = MoveUtil.GetDestination(previousMove) % 8;
                            if (targetFile == enPassantFile)
                            {
                                isEnPassant = true;
                                if (debugMode) Debug.Log($"En passant capture from {BitboardUtils.IndexToAlgebraic(position)} to {BitboardUtils.IndexToAlgebraic(i)}");
                            }
                        }
                        // For black pawns on rank 4 (index 3)
                        else if (!isWhite && rank == 3 && previousMove != 0 &&
                                MoveUtil.GetPieceType(previousMove) == (int)PieceType.Pawn &&
                                MoveUtil.IsWhite(previousMove) && MoveUtil.IsPawnDoubleMove(previousMove))
                        {
                            int enPassantFile = MoveUtil.GetDestination(previousMove) % 8;
                            if (targetFile == enPassantFile)
                            {
                                isEnPassant = true;
                                if (debugMode) Debug.Log($"En passant capture from {BitboardUtils.IndexToAlgebraic(position)} to {BitboardUtils.IndexToAlgebraic(i)}");
                            }
                        }
                    }
                }
                
                // Create integer move instead of a Move object
                int move = MoveUtil.EncodeMove(
                    position, i, (int)pieceType, isWhite,
                    isEnPassant, isPawnDoubleMove, isPromotion, promotionPieceType);
                    
                moves.Add(move);
            }
        }
    }
   
    private List<int> FilterLegalMoves(List<int> moves, bool isWhite)
    {
        List<int> legalMoves = new List<int>();
        
        foreach (int move in moves)
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
                int source = MoveUtil.GetSource(move);
                int dest = MoveUtil.GetDestination(move);
                Debug.Log($"Filtered illegal move: {GetSquareName(source)} to {GetSquareName(dest)} - leaves king in check");
            }
        }
        
        return legalMoves;
    }
    
    public bool IsPiecePinned(int pieceIndex, bool isWhite)
    {
        // Find the king
        ulong kingBitboard = isWhite ? bitboard.WhiteKing : bitboard.BlackKing;
        if (kingBitboard == 0) return false;
        
        int kingIndex = BitOperations.TrailingZeroCount(kingBitboard);
        
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
        
        // Check for pieces between king and the potentially pinned piece
        int r = kingRank + rankDir;
        int f = kingFile + fileDir;
        
        while ((r != pieceRank || f != pieceFile) && r >= 0 && r < 8 && f >= 0 && f < 8)
        {
            int squareIndex = r * 8 + f;
            ulong squareMask = 1UL << squareIndex;
            
            // If any piece exists between king and potentially pinned piece, 
            // then this piece cannot be pinned
            if ((bitboard.returnAllPieces() & squareMask) != 0)
            {
                return false;
            }
            
            r += rankDir;
            f += fileDir;
        }
        
        // Get enemy sliding pieces
        ulong enemyRooks = isWhite ? bitboard.BlackRook : bitboard.WhiteRook;
        ulong enemyBishops = isWhite ? bitboard.BlackBishop : bitboard.WhiteBishop;
        ulong enemyQueens = isWhite ? bitboard.BlackQueen : bitboard.WhiteQueen;
        
        // Check for pinning piece beyond the piece being checked
        r = pieceRank + rankDir;
        f = pieceFile + fileDir;
        
        bool foundPinner = false;
        
        while (r >= 0 && r < 8 && f >= 0 && f < 8)
        {
            int squareIndex = r * 8 + f;
            ulong squareMask = 1UL << squareIndex;
            
            // If we hit a piece
            if ((bitboard.returnAllPieces() & squareMask) != 0)
            {
                // Check if this piece is a potential pinner
                bool isPinner = false;
                
                if (onSameRank || onSameFile)
                {
                    isPinner = (enemyRooks & squareMask) != 0 || (enemyQueens & squareMask) != 0;
                }
                else // on diagonal
                {
                    isPinner = (enemyBishops & squareMask) != 0 || (enemyQueens & squareMask) != 0;
                }
                
                // If it's a pinner, we found our pin
                if (isPinner)
                {
                    foundPinner = true;
                }
                
                // Whether it's a pinner or not, we found a piece, so stop searching
                break;
            }
            
            r += rankDir;
            f += fileDir;
        }
        
        return foundPinner;
    }
    
    public ulong GetPossibleMoves(int position)
    {
        // Determine what piece is at this position
        ulong positionBit = 1UL << position;
        bool isWhite = false;
        PieceType pieceType = PieceType.Pawn;
        
        // First determine what piece type and color is at this position
        if ((bitboard.WhitePawn & positionBit) != 0) {
            pieceType = PieceType.Pawn;
            isWhite = true;
        }
        else if ((bitboard.BlackPawn & positionBit) != 0) {
            pieceType = PieceType.Pawn;
            isWhite = false;
        }
        else if ((bitboard.WhiteKnight & positionBit) != 0) {
            pieceType = PieceType.Knight;
            isWhite = true;
        }
        else if ((bitboard.BlackKnight & positionBit) != 0) {
            pieceType = PieceType.Knight;
            isWhite = false;
        }
        else if ((bitboard.WhiteBishop & positionBit) != 0) {
            pieceType = PieceType.Bishop;
            isWhite = true;
        }
        else if ((bitboard.BlackBishop & positionBit) != 0) {
            pieceType = PieceType.Bishop;
            isWhite = false;
        }
        else if ((bitboard.WhiteRook & positionBit) != 0) {
            pieceType = PieceType.Rook;
            isWhite = true;
        }
        else if ((bitboard.BlackRook & positionBit) != 0) {
            pieceType = PieceType.Rook;
            isWhite = false;
        }
        else if ((bitboard.WhiteQueen & positionBit) != 0) {
            pieceType = PieceType.Queen;
            isWhite = true;
        }
        else if ((bitboard.BlackQueen & positionBit) != 0) {
            pieceType = PieceType.Queen;
            isWhite = false;
        }
        else if ((bitboard.WhiteKing & positionBit) != 0) {
            pieceType = PieceType.King;
            isWhite = true;
        }
        else if ((bitboard.BlackKing & positionBit) != 0) {
            pieceType = PieceType.King;
            isWhite = false;
        }
        else
            return 0; // No piece at this position
        
        // Get pseudo-legal moves
        ulong pseudoLegalMoves = GetPossibleMovesForPiece(position, pieceType, isWhite);
        
        // Now filter out moves that would leave the king in check
        ulong legalMoves = 0;
        
        // Check each possible move
        for (int i = 0; i < 64; i++)
        {
            if ((pseudoLegalMoves & (1UL << i)) != 0)
            {
                // Try this move
                int testMove = MoveUtil.EncodeMove(position, i, (int)pieceType, isWhite);
                bitboard.UpdateBitBoard(testMove);
                
                // Check if king is in check after this move
                bool kingInCheck = evaluation.IsInCheck(isWhite, 
                                                    bitboard.returnWhitePiecesByTypes(), 
                                                    bitboard.returnBlackPiecesByTypes(), 
                                                    bitboard.returnAllPieces());
                
                // Undo the move
                bitboard.UndoBitboard();
                
                // If this move doesn't leave king in check, it's legal
                if (!kingInCheck)
                {
                    legalMoves |= (1UL << i);
                }
                else if (debugMode && pieceType == PieceType.King)
                {
                    Debug.Log($"FILTERED illegal king move from {BitboardUtils.IndexToAlgebraic(position)} to {BitboardUtils.IndexToAlgebraic(i)} - would be in check");
                }
            }
        }
        
        return legalMoves;
    }
    
    public ulong GetPossibleMovesForPiece(int position, PieceType pieceType, bool isWhite)
    {  
        // Get combined piece bitboards
        ulong allPieces = bitboard.returnAllPieces();
        ulong ownPieces = isWhite ? bitboard.returnAllWhitePieces() : bitboard.returnAllBlackPieces();
        ulong enemyPieces = isWhite ? bitboard.returnAllBlackPieces() : bitboard.returnAllWhitePieces();
        
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
                // Use magic bitboards for bishops
                moves = magicBitboards.GetBishopAttacks(position, allPieces) & ~ownPieces;
                break;
            case PieceType.Rook:
                // Use magic bitboards for rooks
                moves = magicBitboards.GetRookAttacks(position, allPieces) & ~ownPieces;
                break;
            case PieceType.Queen:
                // Use magic bitboards for queens
                moves = magicBitboards.GetQueenAttacks(position, allPieces) & ~ownPieces;
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
                // First check if king is in check - can't castle while in check
                bool kingInCheck = evaluation.IsInCheck(isWhite, 
                                                    bitboard.returnWhitePiecesByTypes(), 
                                                    bitboard.returnBlackPiecesByTypes(), 
                                                    bitboard.returnAllPieces());
                
                if (!kingInCheck) {
                    // White kingside castling
                    if (!bitboard.whiteKingsideRookMoved) {
                        ulong pathMask = (1UL << 1) | (1UL << 2); // g1, f1 in new mapping
                        if ((allPieces & pathMask) == 0) {
                            // Check if king passes through check during castling
                            bool canCastle = true;
                            
                            // Check the intermediate square (f1 - position 2)
                            int passThroughMove = MoveUtil.EncodeMove(position, 2, (int)PieceType.King, isWhite);
                            bitboard.UpdateBitBoard(passThroughMove);
                            bool passThroughCheck = evaluation.IsInCheck(isWhite, 
                                                                        bitboard.returnWhitePiecesByTypes(), 
                                                                        bitboard.returnBlackPiecesByTypes(), 
                                                                        bitboard.returnAllPieces());
                            bitboard.UndoBitboard();
                            
                            if (passThroughCheck) {
                                canCastle = false;
                            }
                            
                            // Check the destination square (g1 - position 1)
                            if (canCastle) {
                                int destMove = MoveUtil.EncodeMove(position, 1, (int)PieceType.King, isWhite);
                                bitboard.UpdateBitBoard(destMove);
                                bool destCheck = evaluation.IsInCheck(isWhite, 
                                                                    bitboard.returnWhitePiecesByTypes(), 
                                                                    bitboard.returnBlackPiecesByTypes(), 
                                                                    bitboard.returnAllPieces());
                                bitboard.UndoBitboard();
                                
                                if (destCheck) {
                                    canCastle = false;
                                }
                            }
                            
                            // Add kingside castling move if it's legal
                            if (canCastle) {
                                moves |= 1UL << 1; // g1 in new mapping
                                if (debugMode) Debug.Log("Added validated white kingside castling to moveset");
                            }
                        }
                    }
                    
                    // White queenside castling
                    if (!bitboard.whiteQueensideRookMoved) {
                        ulong pathMask = (1UL << 4) | (1UL << 5) | (1UL << 6); // d1, c1, b1 in new mapping
                        if ((allPieces & pathMask) == 0) {
                            // Check if king passes through check during castling
                            bool canCastle = true;
                            
                            // Check the intermediate square (d1 - position 4)
                            int passThroughMove = MoveUtil.EncodeMove(position, 4, (int)PieceType.King, isWhite);
                            bitboard.UpdateBitBoard(passThroughMove);
                            bool passThroughCheck = evaluation.IsInCheck(isWhite, 
                                                                        bitboard.returnWhitePiecesByTypes(), 
                                                                        bitboard.returnBlackPiecesByTypes(), 
                                                                        bitboard.returnAllPieces());
                            bitboard.UndoBitboard();
                            
                            if (passThroughCheck) {
                                canCastle = false;
                            }
                            
                            // Check the destination square (c1 - position 5)
                            if (canCastle) {
                                int destMove = MoveUtil.EncodeMove(position, 5, (int)PieceType.King, isWhite);
                                bitboard.UpdateBitBoard(destMove);
                                bool destCheck = evaluation.IsInCheck(isWhite, 
                                                                    bitboard.returnWhitePiecesByTypes(), 
                                                                    bitboard.returnBlackPiecesByTypes(), 
                                                                    bitboard.returnAllPieces());
                                bitboard.UndoBitboard();
                                
                                if (destCheck) {
                                    canCastle = false;
                                }
                            }
                            
                            // Add queenside castling move if it's legal
                            if (canCastle) {
                                moves |= 1UL << 5; // c1 in new mapping
                                if (debugMode) Debug.Log("Added validated white queenside castling to moveset");
                            }
                        }
                    }
                }
            }
            else if (!isWhite && position == BLACK_KING_START && !bitboard.blackKingMoved) {
                // First check if king is in check - can't castle while in check
                bool kingInCheck = evaluation.IsInCheck(isWhite, 
                                                    bitboard.returnWhitePiecesByTypes(), 
                                                    bitboard.returnBlackPiecesByTypes(), 
                                                    bitboard.returnAllPieces());
                
                if (!kingInCheck) {
                    // Black kingside castling
                    if (!bitboard.blackKingsideRookMoved) {
                        ulong pathMask = (1UL << 57) | (1UL << 58); // g8, f8 in new mapping
                        if ((allPieces & pathMask) == 0) {
                            // Check if king passes through check during castling
                            bool canCastle = true;
                            
                            // Check the intermediate square (f8 - position 58)
                            int passThroughMove = MoveUtil.EncodeMove(position, 58, (int)PieceType.King, isWhite);
                            bitboard.UpdateBitBoard(passThroughMove);
                            bool passThroughCheck = evaluation.IsInCheck(isWhite, 
                                                                        bitboard.returnWhitePiecesByTypes(), 
                                                                        bitboard.returnBlackPiecesByTypes(), 
                                                                        bitboard.returnAllPieces());
                            bitboard.UndoBitboard();
                            
                            if (passThroughCheck) {
                                canCastle = false;
                            }
                            
                            // Check the destination square (g8 - position 57)
                            if (canCastle) {
                                int destMove = MoveUtil.EncodeMove(position, 57, (int)PieceType.King, isWhite);
                                bitboard.UpdateBitBoard(destMove);
                                bool destCheck = evaluation.IsInCheck(isWhite, 
                                                                    bitboard.returnWhitePiecesByTypes(), 
                                                                    bitboard.returnBlackPiecesByTypes(), 
                                                                    bitboard.returnAllPieces());
                                bitboard.UndoBitboard();
                                
                                if (destCheck) {
                                    canCastle = false;
                                }
                            }
                            
                            // Add kingside castling move if it's legal
                            if (canCastle) {
                                moves |= 1UL << 57; // g8 in new mapping
                                if (debugMode) Debug.Log("Added validated black kingside castling to moveset");
                            }
                        }
                    }
                    
                    // Black queenside castling
                    if (!bitboard.blackQueensideRookMoved) {
                        ulong pathMask = (1UL << 60) | (1UL << 61) | (1UL << 62); // d8, c8, b8 in new mapping
                        if ((allPieces & pathMask) == 0) {
                            // Check if king passes through check during castling
                            bool canCastle = true;
                            
                            // Check the intermediate square (d8 - position 60)
                            int passThroughMove = MoveUtil.EncodeMove(position, 60, (int)PieceType.King, isWhite);
                            bitboard.UpdateBitBoard(passThroughMove);
                            bool passThroughCheck = evaluation.IsInCheck(isWhite, 
                                                                        bitboard.returnWhitePiecesByTypes(), 
                                                                        bitboard.returnBlackPiecesByTypes(), 
                                                                        bitboard.returnAllPieces());
                            bitboard.UndoBitboard();
                            
                            if (passThroughCheck) {
                                canCastle = false;
                            }
                            
                            // Check the destination square (c8 - position 61)
                            if (canCastle) {
                                int destMove = MoveUtil.EncodeMove(position, 61, (int)PieceType.King, isWhite);
                                bitboard.UpdateBitBoard(destMove);
                                bool destCheck = evaluation.IsInCheck(isWhite, 
                                                                    bitboard.returnWhitePiecesByTypes(), 
                                                                    bitboard.returnBlackPiecesByTypes(), 
                                                                    bitboard.returnAllPieces());
                                bitboard.UndoBitboard();
                                
                                if (destCheck) {
                                    canCastle = false;
                                }
                            }
                            
                            // Add queenside castling move if it's legal
                            if (canCastle) {
                                moves |= 1UL << 61; // c8 in new mapping
                                if (debugMode) Debug.Log("Added validated black queenside castling to moveset");
                            }
                        }
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
            // Forward move
            if (rank < 7 && ((allPieces & (1UL << (position + 8))) == 0))
            {
                moves |= 1UL << (position + 8);
                
                // Double forward move 
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
                
                // En passant captures - only on rank 5 for white pawns
                if (rank == 4 && previousMove != 0 && 
                    MoveUtil.GetPieceType(previousMove) == (int)PieceType.Pawn && 
                    !MoveUtil.IsWhite(previousMove) && 
                    MoveUtil.IsPawnDoubleMove(previousMove))
                {
                    int enPassantFile = MoveUtil.GetDestination(previousMove) % 8;
                    
                    // Check if our pawn is adjacent to the double moved pawn
                    if (file > 0 && enPassantFile == file - 1)
                    {
                        // En passant capture to the left
                        moves |= 1UL << (position + 7);
                        if (debugMode) Debug.Log($"White pawn at {BitboardUtils.IndexToAlgebraic(position)} can en passant capture to {BitboardUtils.IndexToAlgebraic(position + 7)}");
                    }
                    if (file < 7 && enPassantFile == file + 1)
                    {
                        // En passant capture to the right
                        moves |= 1UL << (position + 9);
                        if (debugMode) Debug.Log($"White pawn at {BitboardUtils.IndexToAlgebraic(position)} can en passant capture to {BitboardUtils.IndexToAlgebraic(position + 9)}");
                    }
                }
            }
        }
        else // Black pawn
        {
            // Forward move 
            if (rank > 0 && ((allPieces & (1UL << (position - 8))) == 0))
            {
                moves |= 1UL << (position - 8);
                
                // Double forward move 
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
                
                // En passant captures - only on rank 4 for black pawns
                if (rank == 3 && previousMove != 0 && 
                    MoveUtil.GetPieceType(previousMove) == (int)PieceType.Pawn && 
                    MoveUtil.IsWhite(previousMove) && 
                    MoveUtil.IsPawnDoubleMove(previousMove))
                {
                    int enPassantFile = MoveUtil.GetDestination(previousMove) % 8;
                    
                    // Check if our pawn is adjacent to the double-moved pawn
                    if (file < 7 && enPassantFile == file + 1)
                    {
                        // En passant capture to the left 
                        moves |= 1UL << (position - 7);
                        if (debugMode) Debug.Log($"Black pawn at {BitboardUtils.IndexToAlgebraic(position)} can en passant capture to {BitboardUtils.IndexToAlgebraic(position - 7)}");
                    }
                    if (file > 0 && enPassantFile == file - 1)
                    {
                        // En passant capture to the right
                        moves |= 1UL << (position - 9);
                        if (debugMode) Debug.Log($"Black pawn at {BitboardUtils.IndexToAlgebraic(position)} can en passant capture to {BitboardUtils.IndexToAlgebraic(position - 9)}");
                    }
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
    
    /// Helper method to find the trailing zero count in a 64 bit integer
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