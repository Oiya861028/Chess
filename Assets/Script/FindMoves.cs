using System;
using System.Collections.Generic;
using UnityEngine;

public class FindMoves
{
    private Bitboard bitboard;
    private Evaluation evaluation;
    private Move previousMove;
    private MagicBitboards magicBitboards;
    // Constants for the correct bit positions
    private const int WHITE_KING_START = 3;     // e1
    private const int BLACK_KING_START = 59;    // e8
    private const int WHITE_KINGSIDE_ROOK_START = 0;  // h1
    private const int WHITE_QUEENSIDE_ROOK_START = 7; // a1
    private const int BLACK_KINGSIDE_ROOK_START = 56; // h8
    private const int BLACK_QUEENSIDE_ROOK_START = 63; // a8
    
    // Pre-computed attack tables - massive speedup
    private ulong[] knightAttacks = new ulong[64];
    private ulong[] kingAttacks = new ulong[64];
    private ulong[,] pawnAttacks = new ulong[2,64]; // [color][square]
    
    // Reusable lists to avoid allocation
    private List<Move> moveList = new List<Move>(128);
    private List<Move> legalMoveList = new List<Move>(128);
    
    // Debug mode to log move generation details
    private bool debugMode = false;

    public FindMoves(Bitboard bitboard)
    {
        this.bitboard = bitboard;
        this.evaluation = new Evaluation();
        this.previousMove = null;
        this.magicBitboards = new MagicBitboards();
        // Initialize attack tables
        InitializeAttackTables();
        
        Debug.Log("FindMoves initialized with bitboard and pre-computed attack tables");
    }
    
    private void InitializeAttackTables()
    {
        // Initialize knight attack table
        for (int sq = 0; sq < 64; sq++)
        {
            int rank = sq / 8;
            int file = sq % 8;
            
            // Knight moves
            knightAttacks[sq] = 0UL;
            int[][] jumps = {
                new int[] {2, 1}, new int[] {1, 2}, new int[] {-1, 2}, new int[] {-2, 1},
                new int[] {-2, -1}, new int[] {-1, -2}, new int[] {1, -2}, new int[] {2, -1}
            };
            
            foreach (var jump in jumps)
            {
                int r = rank + jump[0];
                int f = file + jump[1];
                if (r >= 0 && r < 8 && f >= 0 && f < 8)
                    knightAttacks[sq] |= 1UL << (r * 8 + f);
            }
            
            // King moves
            kingAttacks[sq] = 0UL;
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int df = -1; df <= 1; df++)
                {
                    if (dr == 0 && df == 0) continue;
                    
                    int r = rank + dr;
                    int f = file + df;
                    if (r >= 0 && r < 8 && f >= 0 && f < 8)
                        kingAttacks[sq] |= 1UL << (r * 8 + f);
                }
            }
            
            // Pawn attacks (white)
            pawnAttacks[0, sq] = 0UL;
            if (rank < 7)
            {
                if (file > 0) pawnAttacks[0, sq] |= 1UL << (sq + 7);
                if (file < 7) pawnAttacks[0, sq] |= 1UL << (sq + 9);
            }
            
            // Pawn attacks (black)
            pawnAttacks[1, sq] = 0UL;
            if (rank > 0)
            {
                if (file > 0) pawnAttacks[1, sq] |= 1UL << (sq - 9);
                if (file < 7) pawnAttacks[1, sq] |= 1UL << (sq - 7);
            }
        }
    }

    public void SetPreviousMove(Move move)
    {
        previousMove = move;
        if (debugMode && move != null)
        {
            Debug.Log($"Previous move set to: {BitboardUtils.IndexToAlgebraic(move.Source)} to {BitboardUtils.IndexToAlgebraic(move.Destination)}, IsPawnDoubleMove: {move.IsPawnDoubleMove}");
        }
    }

    /// Gets all legal moves for the specified side
    public List<Move> GetAllPossibleMoves(bool isWhite, Move previousMove)
    {
        if (debugMode) Debug.Log("GetAllPossibleMoves called for " + (isWhite ? "white" : "black"));
        
        this.previousMove = previousMove;
        moveList.Clear();
        
        // Process pieces more efficiently by iterating bitboards directly
        
        // Process pawns
        ulong pawns = isWhite ? bitboard.WhitePawn : bitboard.BlackPawn;
        while (pawns != 0)
        {
            int pos = BitOperations.TrailingZeroCount(pawns);
            AddPawnMoves(moveList, pos, isWhite);
            pawns &= pawns - 1; // Clear LSB
        }
        
        // Process knights
        ulong knights = isWhite ? bitboard.WhiteKnight : bitboard.BlackKnight;
        while (knights != 0)
        {
            int pos = BitOperations.TrailingZeroCount(knights);
            AddKnightMoves(moveList, pos, isWhite);
            knights &= knights - 1; // Clear LSB
        }
        
        // Process bishops
        ulong bishops = isWhite ? bitboard.WhiteBishop : bitboard.BlackBishop;
        while (bishops != 0)
        {
            int pos = BitOperations.TrailingZeroCount(bishops);
            AddBishopMoves(moveList, pos, isWhite);
            bishops &= bishops - 1; // Clear LSB
        }
        
        // Process rooks
        ulong rooks = isWhite ? bitboard.WhiteRook : bitboard.BlackRook;
        while (rooks != 0)
        {
            int pos = BitOperations.TrailingZeroCount(rooks);
            AddRookMoves(moveList, pos, isWhite);
            rooks &= rooks - 1; // Clear LSB
        }
        
        // Process queens
        ulong queens = isWhite ? bitboard.WhiteQueen : bitboard.BlackQueen;
        while (queens != 0)
        {
            int pos = BitOperations.TrailingZeroCount(queens);
            AddQueenMoves(moveList, pos, isWhite);
            queens &= queens - 1; // Clear LSB
        }
        
        // Process kings (including castling)
        ulong kings = isWhite ? bitboard.WhiteKing : bitboard.BlackKing;
        while (kings != 0)
        {
            int pos = BitOperations.TrailingZeroCount(kings);
            AddKingMoves(moveList, pos, isWhite);
            kings &= kings - 1; // Clear LSB
        }
        
        if (debugMode) Debug.Log($"Found {moveList.Count} pseudo-legal moves");
        
        // Filter out illegal moves (moves that leave the king in check)
        legalMoveList.Clear();
        
        foreach (Move move in moveList)
        {
            bitboard.UpdateBitBoard(move);
            
            bool kingInCheck = evaluation.IsInCheck(isWhite, 
                bitboard.returnWhitePiecesByTypes(), 
                bitboard.returnBlackPiecesByTypes(), 
                bitboard.returnAllPieces());
            
            bitboard.UndoBitboard();
            
            if (!kingInCheck)
            {
                legalMoveList.Add(move);
            }
        }
        
        if (debugMode) Debug.Log($"Found {legalMoveList.Count} legal moves");
        
        return legalMoveList;
    }
    
    /// <summary>
    /// Returns a bitboard of all possible moves for the piece at the given position.
    /// </summary>
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
        ulong pseudoLegalMoves = GetPseudoLegalMoves(position, pieceType, isWhite);
        
        // Now filter out moves that would leave the king in check
        ulong legalMoves = 0;
        
        // Create a temporary Move object to test each move
        while (pseudoLegalMoves != 0)
        {
            int targetSquare = BitOperations.TrailingZeroCount(pseudoLegalMoves);
            ulong targetBit = 1UL << targetSquare;
            
            // Try this move
            Move testMove = new Move(position, targetSquare, previousMove, (int)pieceType, isWhite);
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
                legalMoves |= targetBit;
            }
            
            // Clear this bit for the next iteration
            pseudoLegalMoves &= ~targetBit;
        }
        
        return legalMoves;
    }
    
    private ulong GetPseudoLegalMoves(int position, PieceType pieceType, bool isWhite)
    {
        ulong allPieces = bitboard.returnAllPieces();
        ulong ownPieces = isWhite ? bitboard.returnAllWhitePieces() : bitboard.returnAllBlackPieces();
        
        switch (pieceType)
        {
            case PieceType.Pawn:
                return GetPawnMoves(position, isWhite, allPieces, ownPieces);
                
            case PieceType.Knight:
                return knightAttacks[position] & ~ownPieces;
                
            case PieceType.Bishop:
                return CalculateBishopAttacks(position, allPieces) & ~ownPieces;
                
            case PieceType.Rook:
                return CalculateRookAttacks(position, allPieces) & ~ownPieces;
                
            case PieceType.Queen:
                return (CalculateBishopAttacks(position, allPieces) | 
                       CalculateRookAttacks(position, allPieces)) & ~ownPieces;
                
            case PieceType.King:
                ulong moves = kingAttacks[position] & ~ownPieces;
                
                // Check if king is currently in check - can't castle when in check
                bool kingInCheck = evaluation.IsInCheck(isWhite, 
                    bitboard.returnWhitePiecesByTypes(), 
                    bitboard.returnBlackPiecesByTypes(), 
                    bitboard.returnAllPieces());
                
                // Only add castling moves if king is not in check
                if (!kingInCheck)
                {
                    // Add castling moves for kings
                    if (isWhite && position == WHITE_KING_START && !bitboard.whiteKingMoved)
                    {
                        // Check kingside castling
                        if (!bitboard.whiteKingsideRookMoved && 
                            (allPieces & ((1UL << 1) | (1UL << 2))) == 0)
                        {
                            // Check if f1 is under attack
                            bool f1UnderAttack = IsSquareUnderAttack(2, false);
                            
                            if (!f1UnderAttack)
                            {
                                moves |= 1UL << 1; // g1
                            }
                        }
                        
                        // Check queenside castling
                        if (!bitboard.whiteQueensideRookMoved && 
                            (allPieces & ((1UL << 4) | (1UL << 5) | (1UL << 6))) == 0)
                        {
                            // Check if d1 is under attack
                            bool d1UnderAttack = IsSquareUnderAttack(4, false);
                            
                            if (!d1UnderAttack)
                            {
                                moves |= 1UL << 5; // c1
                            }
                        }
                    }
                    else if (!isWhite && position == BLACK_KING_START && !bitboard.blackKingMoved)
                    {
                        // Check kingside castling
                        if (!bitboard.blackKingsideRookMoved && 
                            (allPieces & ((1UL << 57) | (1UL << 58))) == 0)
                        {
                            // Check if f8 is under attack
                            bool f8UnderAttack = IsSquareUnderAttack(58, true);
                            
                            if (!f8UnderAttack)
                            {
                                moves |= 1UL << 57; // g8
                            }
                        }
                        
                        // Check queenside castling
                        if (!bitboard.blackQueensideRookMoved && 
                            (allPieces & ((1UL << 60) | (1UL << 61) | (1UL << 62))) == 0)
                        {
                            // Check if d8 is under attack
                            bool d8UnderAttack = IsSquareUnderAttack(60, true);
                            
                            if (!d8UnderAttack)
                            {
                                moves |= 1UL << 61; // c8
                            }
                        }
                    }
                }

                
                return moves;
                
            default:
                return 0;
        }
    }
    
    private ulong GetPawnMoves(int position, bool isWhite, ulong allPieces, ulong ownPieces)
    {
        ulong moves = 0;
        ulong enemyPieces = allPieces & ~ownPieces;
        int rank = position / 8;
        int file = position % 8;
        
        if (isWhite)
        {
            // Forward movement
            if (rank < 7 && ((allPieces & (1UL << (position + 8))) == 0))
            {
                moves |= 1UL << (position + 8);
                
                // Double move from starting position
                if (rank == 1 && ((allPieces & (1UL << (position + 16))) == 0))
                {
                    moves |= 1UL << (position + 16);
                }
            }
            
            // Captures
            moves |= pawnAttacks[0, position] & enemyPieces;
            
            // En passant
            if (rank == 4 && previousMove != null && 
                previousMove.PieceType == (int)PieceType.Pawn &&
                !previousMove.IsWhite && previousMove.IsPawnDoubleMove)
            {
                int enPassantFile = previousMove.Destination % 8;
                
                // Check if our pawn is adjacent to the double-moved pawn
                if ((file > 0 && enPassantFile == file - 1) ||
                    (file < 7 && enPassantFile == file + 1))
                {
                    int epTarget = (previousMove.Source + previousMove.Destination) / 2;
                    moves |= 1UL << epTarget;
                }
            }
        }
        else // Black
        {
            // Forward movement
            if (rank > 0 && ((allPieces & (1UL << (position - 8))) == 0))
            {
                moves |= 1UL << (position - 8);
                
                // Double move from starting position
                if (rank == 6 && ((allPieces & (1UL << (position - 16))) == 0))
                {
                    moves |= 1UL << (position - 16);
                }
            }
            
            // Captures
            moves |= pawnAttacks[1, position] & enemyPieces;
            
            // En passant
            if (rank == 3 && previousMove != null && 
                previousMove.PieceType == (int)PieceType.Pawn &&
                previousMove.IsWhite && previousMove.IsPawnDoubleMove)
            {
                int enPassantFile = previousMove.Destination % 8;
                
                // Check if our pawn is adjacent to the double-moved pawn
                if ((file > 0 && enPassantFile == file - 1) ||
                    (file < 7 && enPassantFile == file + 1))
                {
                    int epTarget = (previousMove.Source + previousMove.Destination) / 2;
                    moves |= 1UL << epTarget;
                }
            }
        }
        
        return moves;
    }
    
    private void AddPawnMoves(List<Move> moves, int position, bool isWhite)
    {
        int rank = position / 8;
        int file = position % 8;
        ulong allPieces = bitboard.returnAllPieces();
        ulong enemyPieces = isWhite ? bitboard.returnAllBlackPieces() : bitboard.returnAllWhitePieces();
        
        // Single and double pawn pushes
        if (isWhite)
        {
            // Forward move (1 square)
            if (rank < 7 && ((allPieces & (1UL << (position + 8))) == 0))
            {
                // Check for promotion
                if (rank == 6)
                {
                    // Add promotion - to queen by default
                    moves.Add(new Move(position, position + 8, previousMove, (int)PieceType.Pawn, isWhite, 
                        false, false, true, (int)PieceType.Queen));
                }
                else
                {
                    moves.Add(new Move(position, position + 8, previousMove, (int)PieceType.Pawn, isWhite));
                    
                    // Double forward move (from starting position)
                    if (rank == 1 && ((allPieces & (1UL << (position + 16))) == 0))
                    {
                        moves.Add(new Move(position, position + 16, previousMove, (int)PieceType.Pawn, isWhite, 
                            false, true, false, 0));
                    }
                }
            }
        }
        else // Black pawn
        {
            // Forward move (1 square)
            if (rank > 0 && ((allPieces & (1UL << (position - 8))) == 0))
            {
                // Check for promotion
                if (rank == 1)
                {
                    // Add promotion - to queen by default
                    moves.Add(new Move(position, position - 8, previousMove, (int)PieceType.Pawn, isWhite, 
                        false, false, true, (int)PieceType.Queen));
                }
                else
                {
                    moves.Add(new Move(position, position - 8, previousMove, (int)PieceType.Pawn, isWhite));
                    
                    // Double forward move (from starting position)
                    if (rank == 6 && ((allPieces & (1UL << (position - 16))) == 0))
                    {
                        moves.Add(new Move(position, position - 16, previousMove, (int)PieceType.Pawn, isWhite, 
                            false, true, false, 0));
                    }
                }
            }
        }
        
        // Pawn captures (using pre-computed attack table)
        ulong attacks = pawnAttacks[isWhite ? 0 : 1, position] & enemyPieces;
        while (attacks != 0)
        {
            int targetSquare = BitOperations.TrailingZeroCount(attacks);
            
            // Check for promotion on capture
            if ((isWhite && targetSquare / 8 == 7) || (!isWhite && targetSquare / 8 == 0))
            {
                moves.Add(new Move(position, targetSquare, previousMove, (int)PieceType.Pawn, isWhite, 
                    false, false, true, (int)PieceType.Queen));
            }
            else
            {
                moves.Add(new Move(position, targetSquare, previousMove, (int)PieceType.Pawn, isWhite));
            }
            
            attacks &= attacks - 1; // Clear LSB
        }
        
        // En passant captures
        if (previousMove != null && previousMove.IsPawnDoubleMove && 
            previousMove.PieceType == (int)PieceType.Pawn &&
            previousMove.IsWhite != isWhite)
        {
            int epTarget = (previousMove.Source + previousMove.Destination) / 2;
            
            if (isWhite && rank == 4)
            {
                // Check if our pawn is adjacent to the double-moved pawn's destination
                if ((file > 0 && previousMove.Destination == position - 1) ||
                    (file < 7 && previousMove.Destination == position + 1))
                {
                    moves.Add(new Move(position, epTarget, previousMove, (int)PieceType.Pawn, isWhite, true, false, false, 0));
                    if (debugMode) 
                        Debug.Log($"Added en passant move: {BitboardUtils.IndexToAlgebraic(position)} to {BitboardUtils.IndexToAlgebraic(epTarget)}");
                }
            }
            else if (!isWhite && rank == 3)
            {
                // Check if our pawn is adjacent to the double-moved pawn's destination
                if ((file > 0 && previousMove.Destination == position - 1) ||
                    (file < 7 && previousMove.Destination == position + 1))
                {
                    moves.Add(new Move(position, epTarget, previousMove, (int)PieceType.Pawn, isWhite, true, false, false, 0));
                    if (debugMode) 
                        Debug.Log($"Added en passant move: {BitboardUtils.IndexToAlgebraic(position)} to {BitboardUtils.IndexToAlgebraic(epTarget)}");
                }
            }
        }
    }
    
    private void AddKnightMoves(List<Move> moves, int position, bool isWhite)
    {
        ulong ownPieces = isWhite ? bitboard.returnAllWhitePieces() : bitboard.returnAllBlackPieces();
        
        // Use pre-computed knight attacks
        ulong attacks = knightAttacks[position] & ~ownPieces;
        
        while (attacks != 0)
        {
            int targetSquare = BitOperations.TrailingZeroCount(attacks);
            moves.Add(new Move(position, targetSquare, previousMove, (int)PieceType.Knight, isWhite));
            attacks &= attacks - 1; // Clear LSB
        }
    }
    
    private void AddBishopMoves(List<Move> moves, int position, bool isWhite)
    {
        ulong ownPieces = isWhite ? bitboard.returnAllWhitePieces() : bitboard.returnAllBlackPieces();
        ulong allPieces = bitboard.returnAllPieces();
        
        // Calculate bishop attacks (diagonals)
        ulong attacks = CalculateBishopAttacks(position, allPieces) & ~ownPieces;
        
        while (attacks != 0)
        {
            int targetSquare = BitOperations.TrailingZeroCount(attacks);
            moves.Add(new Move(position, targetSquare, previousMove, (int)PieceType.Bishop, isWhite));
            attacks &= attacks - 1; // Clear LSB
        }
    }
    
    private void AddRookMoves(List<Move> moves, int position, bool isWhite)
    {
        ulong ownPieces = isWhite ? bitboard.returnAllWhitePieces() : bitboard.returnAllBlackPieces();
        ulong allPieces = bitboard.returnAllPieces();
        
        // Calculate rook attacks (ranks and files)
        ulong attacks = CalculateRookAttacks(position, allPieces) & ~ownPieces;
        
        while (attacks != 0)
        {
            int targetSquare = BitOperations.TrailingZeroCount(attacks);
            moves.Add(new Move(position, targetSquare, previousMove, (int)PieceType.Rook, isWhite));
            attacks &= attacks - 1; // Clear LSB
        }
    }
    
    private void AddQueenMoves(List<Move> moves, int position, bool isWhite)
    {
        ulong ownPieces = isWhite ? bitboard.returnAllWhitePieces() : bitboard.returnAllBlackPieces();
        ulong allPieces = bitboard.returnAllPieces();
        
        // Calculate queen attacks (bishop + rook moves)
        ulong attacks = (CalculateBishopAttacks(position, allPieces) | 
                       CalculateRookAttacks(position, allPieces)) & ~ownPieces;
        
        while (attacks != 0)
        {
            int targetSquare = BitOperations.TrailingZeroCount(attacks);
            moves.Add(new Move(position, targetSquare, previousMove, (int)PieceType.Queen, isWhite));
            attacks &= attacks - 1; // Clear LSB
        }
    }
    private bool IsSquareUnderAttack(int square, bool byWhite)
    {
        // Temporarily place a king on the square to check if it would be attacked
        ulong originalWhiteKing = bitboard.WhiteKing;
        ulong originalBlackKing = bitboard.BlackKing;
        
        if (!byWhite) // Checking if square is attacked by black
        {
            // Place white king temporarily on square
            bitboard.WhiteKing = 1UL << square;
            
            // Check if white king would be in check
            bool isAttacked = evaluation.IsInCheck(true, 
                bitboard.returnWhitePiecesByTypes(), 
                bitboard.returnBlackPiecesByTypes(), 
                bitboard.returnAllPieces());
                
            // Restore original king position
            bitboard.WhiteKing = originalWhiteKing;
            return isAttacked;
        }
        else // Checking if square is attacked by white
        {
            // Place black king temporarily on square
            bitboard.BlackKing = 1UL << square;
            
            // Check if black king would be in check
            bool isAttacked = evaluation.IsInCheck(false, 
                bitboard.returnWhitePiecesByTypes(), 
                bitboard.returnBlackPiecesByTypes(), 
                bitboard.returnAllPieces());
                
            // Restore original king position
            bitboard.BlackKing = originalBlackKing;
            return isAttacked;
        }
    }

    private void AddKingMoves(List<Move> moves, int position, bool isWhite)
    {
        ulong ownPieces = isWhite ? bitboard.returnAllWhitePieces() : bitboard.returnAllBlackPieces();
        
        // Use pre-computed king attacks
        ulong attacks = kingAttacks[position] & ~ownPieces;
        
        while (attacks != 0)
        {
            int targetSquare = BitOperations.TrailingZeroCount(attacks);
            moves.Add(new Move(position, targetSquare, previousMove, (int)PieceType.King, isWhite));
            attacks &= attacks - 1; // Clear LSB
        }
        
        // Check if king is currently in check - can't castle when in check
        bool kingInCheck = evaluation.IsInCheck(isWhite, 
            bitboard.returnWhitePiecesByTypes(), 
            bitboard.returnBlackPiecesByTypes(), 
            bitboard.returnAllPieces());
        
        // Don't allow castling if king is in check
        if (!kingInCheck)
        {
            // Castling moves
            if (isWhite && position == WHITE_KING_START && !bitboard.whiteKingMoved)
            {
                // Check kingside castling
                if (!bitboard.whiteKingsideRookMoved && 
                    (bitboard.returnAllPieces() & ((1UL << 1) | (1UL << 2))) == 0)
                {
                    // Check if square f1 (the square king passes through) is under attack
                    bool f1UnderAttack = IsSquareUnderAttack(2, false); // f1 is square 2 (0-indexed), check if attacked by black
                    
                    if (!f1UnderAttack)
                    {
                        moves.Add(new Move(position, 1, previousMove, (int)PieceType.King, isWhite));
                        if (debugMode) Debug.Log("Added white kingside castling move");
                    }
                }
                
                // Check queenside castling
                if (!bitboard.whiteQueensideRookMoved && 
                    (bitboard.returnAllPieces() & ((1UL << 4) | (1UL << 5) | (1UL << 6))) == 0)
                {
                    // Check if square d1 (the square king passes through) is under attack
                    bool d1UnderAttack = IsSquareUnderAttack(4, false); // d1 is square 4 (0-indexed), check if attacked by black
                    
                    if (!d1UnderAttack)
                    {
                        moves.Add(new Move(position, 5, previousMove, (int)PieceType.King, isWhite));
                        if (debugMode) Debug.Log("Added white queenside castling move");
                    }
                }
            }
            else if (!isWhite && position == BLACK_KING_START && !bitboard.blackKingMoved)
            {
                // Check kingside castling
                if (!bitboard.blackKingsideRookMoved && 
                    (bitboard.returnAllPieces() & ((1UL << 57) | (1UL << 58))) == 0)
                {
                    // Check if square f8 (the square king passes through) is under attack
                    bool f8UnderAttack = IsSquareUnderAttack(58, true); // f8 is square 58 (0-indexed), check if attacked by white
                    
                    if (!f8UnderAttack)
                    {
                        moves.Add(new Move(position, 57, previousMove, (int)PieceType.King, isWhite));
                        if (debugMode) Debug.Log("Added black kingside castling move");
                    }
                }
                
                // Check queenside castling
                if (!bitboard.blackQueensideRookMoved && 
                    (bitboard.returnAllPieces() & ((1UL << 60) | (1UL << 61) | (1UL << 62))) == 0)
                {
                    // Check if square d8 (the square king passes through) is under attack
                    bool d8UnderAttack = IsSquareUnderAttack(60, true); // d8 is square 60 (0-indexed), check if attacked by white
                    
                    if (!d8UnderAttack)
                    {
                        moves.Add(new Move(position, 61, previousMove, (int)PieceType.King, isWhite));
                        if (debugMode) Debug.Log("Added black queenside castling move");
                    }
                }
            }
        }
    }
    
    // Optimized bishop attack calculation
    private ulong CalculateBishopAttacks(int square, ulong occupied)
    {
        return magicBitboards.GetBishopAttacks(square, occupied);
    }
    
    // Optimized rook attack calculation
    private ulong CalculateRookAttacks(int square, ulong occupied)
    {
        return magicBitboards.GetRookAttacks(square, occupied);
    }
    
    // Efficient bit operations
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
        
        public static int PopCount(ulong x)
        {
            int count = 0;
            while (x != 0)
            {
                count++;
                x &= x - 1; // Clear the least significant bit
            }
            return count;
        }
    }
}