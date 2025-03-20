using System;
using UnityEngine;

public class Evaluation
{
    // Piece values for middlegame and endgame (aligned with Stockfish values)
    private static readonly int[] PieceValueMg = { 0, 126, 781, 825, 1276, 2538, 0 }; // None, Pawn, Knight, Bishop, Rook, Queen, King
    private static readonly int[] PieceValueEg = { 0, 208, 854, 915, 1380, 2682, 0 }; // None, Pawn, Knight, Bishop, Rook, Queen, King
    
    // Phase values for tapered evaluation
    private const int PHASE_MIDGAME = 24;
    private const int PHASE_ENDGAME = 0;
    private static readonly int[] PhaseValues = { 0, 0, 1, 1, 2, 4, 0 }; // None, Pawn, Knight, Bishop, Rook, Queen, King
    
    // Thresholds
    private const int LazyThreshold = 1400;
    private const int SpaceThreshold = 12222;
    
    // Pre-calculated attack tables and lookup tables
    private ulong[] KnightAttacks = new ulong[64];
    private ulong[] KingAttacks = new ulong[64];
    private ulong[] PawnAttacks = new ulong[2 * 64]; // [color][square]
    
    // King attack weights by piece type (None, Pawn, Knight, Bishop, Rook, Queen, King)
    private static readonly int[] KingAttackWeights = { 0, 0, 81, 52, 44, 10, 0 };
    
    // Safe check penalties (from Stockfish)
    private const int QueenSafeCheck = 780;
    private const int RookSafeCheck = 1080;
    private const int BishopSafeCheck = 635;
    private const int KnightSafeCheck = 790;
    
    // Piece mobility bonuses (aligned with Stockfish)
    // Knights (0-8 squares)
    private static readonly int[] KnightMobilityMg = { -62, -53, -12, -4, 3, 13, 22, 28, 33 };
    private static readonly int[] KnightMobilityEg = { -81, -56, -30, -14, 8, 15, 23, 27, 33 };
    
    // Bishops (0-13 squares)
    private static readonly int[] BishopMobilityMg = { -48, -20, 16, 26, 38, 51, 55, 63, 63, 68, 81, 81, 91, 98 };
    private static readonly int[] BishopMobilityEg = { -59, -23, -3, 13, 24, 42, 54, 57, 65, 73, 78, 86, 88, 97 };
    
    // Rooks (0-14 squares)
    private static readonly int[] RookMobilityMg = { -58, -27, -15, -10, -5, -2, 9, 16, 30, 29, 32, 38, 46, 48, 58 };
    private static readonly int[] RookMobilityEg = { -76, -18, 28, 55, 69, 82, 112, 118, 132, 142, 155, 165, 166, 169, 171 };
    
    // Queens (first 15 of 0-27 squares)
    private static readonly int[] QueenMobilityMg = { -39, -21, 3, 3, 14, 22, 28, 41, 43, 48, 56, 60, 60, 66, 67, 70, 71, 73, 79, 88, 88, 99, 102, 102, 106, 109, 113, 116 };
    private static readonly int[] QueenMobilityEg = { -36, -15, 8, 18, 34, 54, 61, 73, 79, 92, 94, 104, 113, 120, 123, 126, 133, 136, 140, 143, 148, 166, 170, 175, 184, 191, 206, 212 };
    
    // File and rank masks
    private static readonly ulong[] FileMasks = {
        0x0101010101010101UL, 0x0202020202020202UL, 0x0404040404040404UL, 0x0808080808080808UL,
        0x1010101010101010UL, 0x2020202020202020UL, 0x4040404040404040UL, 0x8080808080808080UL
    };
    
    private static readonly ulong[] RankMasks = {
        0x00000000000000FFUL, 0x000000000000FF00UL, 0x0000000000FF0000UL, 0x00000000FF000000UL,
        0x000000FF00000000UL, 0x0000FF0000000000UL, 0x00FF000000000000UL, 0xFF00000000000000UL
    };
    
    // Center files for space evaluation (files D-E)
    private static readonly ulong CenterFiles = FileMasks[3] | FileMasks[4];
    
    // King flank (files A-C or F-H)
    private static readonly ulong KingSide = FileMasks[5] | FileMasks[6] | FileMasks[7];
    private static readonly ulong QueenSide = FileMasks[0] | FileMasks[1] | FileMasks[2];
    private static readonly ulong[] KingFlank = { 
        QueenSide, QueenSide, QueenSide, CenterFiles, CenterFiles, KingSide, KingSide, KingSide
    };
    
    // Center squares for outpost evaluation
    private static readonly ulong Center = 0x0000001818000000UL; // e4, d4, e5, d5
    
    // Passed pawn bonuses by rank (aligned with Stockfish)
    private static readonly int[] PassedRankMg = { 0, 10, 17, 15, 62, 168, 276, 0 };
    private static readonly int[] PassedRankEg = { 0, 28, 33, 41, 72, 177, 260, 0 };
    
    // Outpost ranks (based on Stockfish's definition)
    private static readonly ulong OutpostRanks = 0x00000000FF7F7F00UL; // Ranks 3-6 for white perspective
    
    // Rook on file bonuses (semi-open, open)
    private static readonly int[] RookOnFileMg = { 21, 47 };
    private static readonly int[] RookOnFileEg = { 4, 25 };
    
    // Threat bonuses by piece type (from Stockfish)
    private static readonly int[] ThreatByMinorMg = { 0, 6, 59, 79, 90, 79, 0 };
    private static readonly int[] ThreatByMinorEg = { 0, 32, 41, 56, 119, 161, 0 };
    
    private static readonly int[] ThreatByRookMg = { 0, 3, 38, 38, 0, 51, 0 };
    private static readonly int[] ThreatByRookEg = { 0, 44, 71, 61, 38, 38, 0 };
    
    // Positional bonuses from Stockfish
    private const int BishopPawnsMg = 3; private const int BishopPawnsEg = 7;
    private const int CorneredBishopMg = 50; private const int CorneredBishopEg = 50;
    private const int FlankAttacksMg = 8; private const int FlankAttacksEg = 0;
    private const int HangingMg = 69; private const int HangingEg = 36;
    private const int KingProtectorMg = 7; private const int KingProtectorEg = 8;
    private const int KnightOnQueenMg = 16; private const int KnightOnQueenEg = 12;
    private const int LongDiagonalBishopMg = 45; private const int LongDiagonalBishopEg = 0;
    private const int MinorBehindPawnMg = 18; private const int MinorBehindPawnEg = 3;
    private const int OutpostMg = 30; private const int OutpostEg = 21;
    private const int PawnlessFlankMg = 17; private const int PawnlessFlankEg = 95;
    private const int RestrictedPieceMg = 7; private const int RestrictedPieceEg = 7;
    private const int RookOnQueenFileMg = 7; private const int RookOnQueenFileEg = 6;
    private const int SliderOnQueenMg = 59; private const int SliderOnQueenEg = 18;
    private const int ThreatByKingMg = 24; private const int ThreatByKingEg = 89;
    private const int ThreatByPawnPushMg = 48; private const int ThreatByPawnPushEg = 39;
    private const int ThreatBySafePawnMg = 173; private const int ThreatBySafePawnEg = 94;
    private const int TrappedRookMg = 52; private const int TrappedRookEg = 10;
    private const int WeakQueenMg = 49; private const int WeakQueenEg = 15;
    
    // NEW: Castling bonuses (not explicitly in Stockfish but derived from its approach)
    private const int CastlingRightsMg = 17; private const int CastlingRightsEg = 3;
    private const int CastledPositionMg = 48; private const int CastledPositionEg = 18;
    private const int UncastledKingMg = -30; private const int UncastledKingEg = -10;
    
    // NEW: Unsafe piece penalty
    private const int UnsafePieceMg = -35; private const int UnsafePieceEg = -25;
    
    // Tempo bonus
    private const int TempoMg = 28;
    private const int TempoEg = 12;
    
    // Working data for evaluation
    private ulong[] attackedBy = new ulong[2 * 8]; // [color][piece type] - expanded to include empty, ALL_PIECES
    private ulong[] attackedBy2 = new ulong[2]; // [color]
    private ulong[] mobilityArea = new ulong[2]; // [color]
    private ulong[] kingRing = new ulong[2]; // [color]
    private int[] kingAttackersCount = new int[2]; // [color]
    private int[] kingAttackersWeight = new int[2]; // [color]
    private int[] kingAttacksCount = new int[2]; // [color]
    private ulong[] pinnedPieces = new ulong[2]; // NEW: pinned pieces [color]
    private ulong[] kingBlockers = new ulong[2]; // NEW: blockers for king [color]
    
    // Score structure to hold middlegame and endgame values
    private struct Score
    {
        public int Mg;
        public int Eg;
        
        public Score(int mg, int eg)
        {
            Mg = mg;
            Eg = eg;
        }
        
        public static Score operator +(Score a, Score b)
        {
            return new Score(a.Mg + b.Mg, a.Eg + b.Eg);
        }
        
        public static Score operator -(Score a, Score b)
        {
            return new Score(a.Mg - b.Mg, a.Eg - b.Eg);
        }
        
        public static Score operator *(Score a, int b)
        {
            return new Score(a.Mg * b, a.Eg * b);
        }
        
        public static Score operator /(Score a, int b)
        {
            return new Score(a.Mg / b, a.Eg / b);
        }
    }
    
    // Constants for piece types and colors
    private const int WHITE = 0;
    private const int BLACK = 1;
    private const int EMPTY = 0;  // NEW: Added empty piece type
    private const int PAWN = 1;
    private const int KNIGHT = 2;
    private const int BISHOP = 3;
    private const int ROOK = 4;
    private const int QUEEN = 5;
    private const int KING = 6;
    private const int ALL_PIECES = 7;  // NEW: Changed to 7 to follow array indexing
    
    // Constructor - initialize attack tables
    public Evaluation()
    {
        InitializeAttackTables();
    }
    
    public void InitializeAttackTables()
    {
        // Initialize Knight attack table
        for (int sq = 0; sq < 64; sq++)
        {
            int rank = sq / 8;
            int file = sq % 8;
            ulong attacks = 0UL;
            
            int[][] knightOffsets = new int[][]
            {
                new int[] { -2, -1 }, new int[] { -2, 1 }, new int[] { -1, -2 }, new int[] { -1, 2 },
                new int[] { 1, -2 }, new int[] { 1, 2 }, new int[] { 2, -1 }, new int[] { 2, 1 }
            };
            
            foreach (var offset in knightOffsets)
            {
                int r = rank + offset[0];
                int f = file + offset[1];
                if (r >= 0 && r < 8 && f >= 0 && f < 8)
                {
                    attacks |= 1UL << (r * 8 + f);
                }
            }
            
            KnightAttacks[sq] = attacks;
        }
        
        // Initialize King attack table
        for (int sq = 0; sq < 64; sq++)
        {
            int rank = sq / 8;
            int file = sq % 8;
            ulong attacks = 0UL;
            
            for (int r = Math.Max(0, rank - 1); r <= Math.Min(7, rank + 1); r++)
            {
                for (int f = Math.Max(0, file - 1); f <= Math.Min(7, file + 1); f++)
                {
                    if (r != rank || f != file)
                    {
                        attacks |= 1UL << (r * 8 + f);
                    }
                }
            }
            
            KingAttacks[sq] = attacks;
        }
        
        // Initialize Pawn attack table
        for (int sq = 0; sq < 64; sq++)
        {
            int rank = sq / 8;
            int file = sq % 8;
            
            // White pawn attacks
            ulong wAttacks = 0UL;
            if (rank < 7)
            {
                if (file > 0) wAttacks |= 1UL << (sq + 7);
                if (file < 7) wAttacks |= 1UL << (sq + 9);
            }
            PawnAttacks[WHITE * 64 + sq] = wAttacks;
            
            // Black pawn attacks
            ulong bAttacks = 0UL;
            if (rank > 0)
            {
                if (file > 0) bAttacks |= 1UL << (sq - 9);
                if (file < 7) bAttacks |= 1UL << (sq - 7);
            }
            PawnAttacks[BLACK * 64 + sq] = bAttacks;
        }
    }
    
    // Calculate pawn attacks for a given side and bitboard of pawns
    private ulong CalculatePawnAttacks(int side, ulong pawns)
    {
        ulong attacks = 0UL;
        while (pawns != 0)
        {
            int sq = BitOperations.TrailingZeroCount(pawns);
            attacks |= PawnAttacks[side * 64 + sq];
            pawns &= pawns - 1; // Clear least significant bit
        }
        return attacks;
    }
    
    // Calculate knight attacks for a bitboard of knights
    private ulong CalculateKnightAttacks(ulong knights)
    {
        ulong attacks = 0UL;
        while (knights != 0)
        {
            int sq = BitOperations.TrailingZeroCount(knights);
            attacks |= KnightAttacks[sq];
            knights &= knights - 1; // Clear least significant bit
        }
        return attacks;
    }
    
    // Calculate king attacks for a king at square 'sq'
    private ulong CalculateKingAttacks(int sq)
    {
        return KingAttacks[sq];
    }
    
    // Calculate bishop attacks (with precise blocking pieces evaluation)
    private ulong CalculateBishopAttacks(int sq, ulong occupied)
    {
        ulong attacks = 0UL;
        int rank = sq / 8;
        int file = sq % 8;
        
        // Northeast
        for (int r = rank + 1, f = file + 1; r < 8 && f < 8; r++, f++)
        {
            ulong target = 1UL << (r * 8 + f);
            attacks |= target;
            if ((occupied & target) != 0) break;
        }
        
        // Southeast
        for (int r = rank - 1, f = file + 1; r >= 0 && f < 8; r--, f++)
        {
            ulong target = 1UL << (r * 8 + f);
            attacks |= target;
            if ((occupied & target) != 0) break;
        }
        
        // Southwest
        for (int r = rank - 1, f = file - 1; r >= 0 && f >= 0; r--, f--)
        {
            ulong target = 1UL << (r * 8 + f);
            attacks |= target;
            if ((occupied & target) != 0) break;
        }
        
        // Northwest
        for (int r = rank + 1, f = file - 1; r < 8 && f >= 0; r++, f--)
        {
            ulong target = 1UL << (r * 8 + f);
            attacks |= target;
            if ((occupied & target) != 0) break;
        }
        
        return attacks;
    }
    
    // Calculate rook attacks (with precise blocking pieces evaluation)
    private ulong CalculateRookAttacks(int sq, ulong occupied)
    {
        ulong attacks = 0UL;
        int rank = sq / 8;
        int file = sq % 8;
        
        // North
        for (int r = rank + 1; r < 8; r++)
        {
            ulong target = 1UL << (r * 8 + file);
            attacks |= target;
            if ((occupied & target) != 0) break;
        }
        
        // East
        for (int f = file + 1; f < 8; f++)
        {
            ulong target = 1UL << (rank * 8 + f);
            attacks |= target;
            if ((occupied & target) != 0) break;
        }
        
        // South
        for (int r = rank - 1; r >= 0; r--)
        {
            ulong target = 1UL << (r * 8 + file);
            attacks |= target;
            if ((occupied & target) != 0) break;
        }
        
        // West
        for (int f = file - 1; f >= 0; f--)
        {
            ulong target = 1UL << (rank * 8 + f);
            attacks |= target;
            if ((occupied & target) != 0) break;
        }
        
        return attacks;
    }
    
    // Calculate queen attacks (bishop + rook)
    private ulong CalculateQueenAttacks(int sq, ulong occupied)
    {
        return CalculateBishopAttacks(sq, occupied) | CalculateRookAttacks(sq, occupied);
    }
    
    // NEW: Calculate sliding attackers that can attack a specific square
    private ulong SlidingAttackers(int sq, ulong occupied, ulong bishops, ulong rooks, ulong queens)
    {
        ulong bishopAttacks = CalculateBishopAttacks(sq, occupied);
        ulong rookAttacks = CalculateRookAttacks(sq, occupied);
        
        return (bishopAttacks & (bishops | queens)) | (rookAttacks & (rooks | queens));
    }
    
    // NEW: Calculate pinned pieces and king blockers
    private void CalculatePinsAndBlockers(int side, int kingSq, ulong[] ownPieces, ulong[] enemyPieces, ulong allPieces)
    {
        pinnedPieces[side] = 0UL;
        kingBlockers[side] = 0UL;
        
        int enemySide = 1 - side;
        ulong enemyBishops = enemyPieces[BISHOP];
        ulong enemyRooks = enemyPieces[ROOK];
        ulong enemyQueens = enemyPieces[QUEEN];
        ulong ourPieces = ownPieces[0]; // All our pieces
        ulong kingBit = 1UL << kingSq;
        
        // For each direction, find pinned pieces and blockers
        ulong potentialPinners = (CalculateBishopAttacks(kingSq, 0) & (enemyBishops | enemyQueens)) |
                                (CalculateRookAttacks(kingSq, 0) & (enemyRooks | enemyQueens));
        
        while (potentialPinners != 0)
        {
            int pinnerSq = BitOperations.TrailingZeroCount(potentialPinners);
            ulong pinnerBit = 1UL << pinnerSq;
            
            // Get the line between pinner and king
            ulong line = GetLineBetween(kingSq, pinnerSq);
            
            // Get our pieces on this line (potential pinned pieces)
            ulong pinCandidates = line & ourPieces;
            
            // If exactly one of our pieces is on this line, it might be pinned
            if (PopCount(pinCandidates) == 1)
            {
                // Our piece is a blocker for the king
                kingBlockers[side] |= pinCandidates;
                
                // If the line connects to an enemy slider, our piece is pinned
                int pinnerPieceType = 0;
                for (int pt = BISHOP; pt <= QUEEN; pt++)
                {
                    if ((enemyPieces[pt] & pinnerBit) != 0)
                    {
                        pinnerPieceType = pt;
                        break;
                    }
                }
                
                bool isPinner = false;
                
                // Check if piece is an appropriate slider for the direction
                if ((pinnerPieceType == BISHOP || pinnerPieceType == QUEEN) && IsDiagonal(kingSq, pinnerSq))
                    isPinner = true;
                else if ((pinnerPieceType == ROOK || pinnerPieceType == QUEEN) && !IsDiagonal(kingSq, pinnerSq))
                    isPinner = true;
                
                if (isPinner)
                    pinnedPieces[side] |= pinCandidates;
            }
            
            potentialPinners &= potentialPinners - 1; // Clear LSB
        }
    }
    
    // Helper to check if two squares are on a diagonal
    private bool IsDiagonal(int sq1, int sq2)
    {
        int fileDiff = Math.Abs((sq1 % 8) - (sq2 % 8));
        int rankDiff = Math.Abs((sq1 / 8) - (sq2 / 8));
        return fileDiff == rankDiff;
    }
    
    // Helper to get the line (squares) between two squares
    private ulong GetLineBetween(int sq1, int sq2)
    {
        ulong line = 0UL;
        
        int rank1 = sq1 / 8, file1 = sq1 % 8;
        int rank2 = sq2 / 8, file2 = sq2 % 8;
        
        // Vertical line
        if (file1 == file2 && rank1 != rank2)
        {
            int minRank = Math.Min(rank1, rank2);
            int maxRank = Math.Max(rank1, rank2);
            for (int r = minRank + 1; r < maxRank; r++)
                line |= 1UL << (r * 8 + file1);
            return line;
        }
        
        // Horizontal line
        if (rank1 == rank2 && file1 != file2)
        {
            int minFile = Math.Min(file1, file2);
            int maxFile = Math.Max(file1, file2);
            for (int f = minFile + 1; f < maxFile; f++)
                line |= 1UL << (rank1 * 8 + f);
            return line;
        }
        
        // Diagonal line
        int fileDiff = file2 - file1;
        int rankDiff = rank2 - rank1;
        
        if (Math.Abs(fileDiff) == Math.Abs(rankDiff) && fileDiff != 0)
        {
            int fileStep = fileDiff > 0 ? 1 : -1;
            int rankStep = rankDiff > 0 ? 1 : -1;
            int steps = Math.Abs(fileDiff);
            
            for (int i = 1; i < steps; i++)
            {
                int r = rank1 + i * rankStep;
                int f = file1 + i * fileStep;
                line |= 1UL << (r * 8 + f);
            }
        }
        
        return line;
    }
    
    // Make a score from middlegame and endgame values
    private Score MakeScore(int mg, int eg)
    {
        return new Score(mg, eg);
    }
    
    // Check if a king is in check
    public bool IsInCheck(bool isWhite, ulong[] whitePieces, ulong[] blackPieces, ulong allPieces)
    {
        // Sanity check
        if (whitePieces == null || whitePieces.Length < 7 || blackPieces == null || blackPieces.Length < 7)
        {
            Debug.LogError("Invalid piece arrays passed to IsInCheck. Arrays must have at least 7 elements.");
            return false;
        }

        // Get king square
        ulong kingBitboard = isWhite ? whitePieces[KING] : blackPieces[KING];
        if (kingBitboard == 0)
        {
            Debug.LogError("No king found for " + (isWhite ? "white" : "black") + " in IsInCheck!");
            return false;
        }

        int kingSq = BitOperations.TrailingZeroCount(kingBitboard);
        
        // Check for pawn attacks
        ulong enemyPawnAttacks = isWhite ? 
            CalculatePawnAttacks(BLACK, blackPieces[PAWN]) : 
            CalculatePawnAttacks(WHITE, whitePieces[PAWN]);
            
        if ((enemyPawnAttacks & kingBitboard) != 0)
            return true;
            
        // Check for knight attacks
        ulong enemyKnights = isWhite ? blackPieces[KNIGHT] : whitePieces[KNIGHT];
        if ((CalculateKnightAttacks(1UL << kingSq) & enemyKnights) != 0)
            return true;
            
        // Check for bishop/queen attacks
        ulong enemyBishops = isWhite ? blackPieces[BISHOP] : whitePieces[BISHOP];
        ulong enemyQueens = isWhite ? blackPieces[QUEEN] : whitePieces[QUEEN];
        if ((CalculateBishopAttacks(kingSq, allPieces) & (enemyBishops | enemyQueens)) != 0)
            return true;
            
        // Check for rook/queen attacks
        ulong enemyRooks = isWhite ? blackPieces[ROOK] : whitePieces[ROOK];
        if ((CalculateRookAttacks(kingSq, allPieces) & (enemyRooks | enemyQueens)) != 0)
            return true;
            
        // Check for adjacent enemy king
        ulong enemyKing = isWhite ? blackPieces[KING] : whitePieces[KING];
        if ((CalculateKingAttacks(kingSq) & enemyKing) != 0)
            return true;
            
        return false;
    }
    
    // Main evaluation function that includes new Stockfish features
    public int EvaluatePosition(
        ulong whitePawn, ulong whiteKnight, ulong whiteBishop, ulong whiteRook, ulong whiteQueen, ulong whiteKing,
        ulong blackPawn, ulong blackKnight, ulong blackBishop, ulong blackRook, ulong blackQueen, ulong blackKing,
        bool whiteToMove)
    {
        // Reset evaluation arrays
        Array.Clear(attackedBy, 0, attackedBy.Length);
        Array.Clear(attackedBy2, 0, attackedBy2.Length);
        Array.Clear(mobilityArea, 0, mobilityArea.Length);
        Array.Clear(kingRing, 0, kingRing.Length);
        Array.Clear(kingAttackersCount, 0, kingAttackersCount.Length);
        Array.Clear(kingAttackersWeight, 0, kingAttackersWeight.Length);
        Array.Clear(kingAttacksCount, 0, kingAttacksCount.Length);
        Array.Clear(pinnedPieces, 0, pinnedPieces.Length);
        Array.Clear(kingBlockers, 0, kingBlockers.Length);
        
        // All pieces bitboards
        ulong[] whitePieces = { 
            whitePawn | whiteKnight | whiteBishop | whiteRook | whiteQueen | whiteKing, // ALL_PIECES
            whitePawn, whiteKnight, whiteBishop, whiteRook, whiteQueen, whiteKing 
        };
        
        ulong[] blackPieces = { 
            blackPawn | blackKnight | blackBishop | blackRook | blackQueen | blackKing, // ALL_PIECES
            blackPawn, blackKnight, blackBishop, blackRook, blackQueen, blackKing 
        };
        
        ulong allPieces = whitePieces[0] | blackPieces[0];
        
        // Find king squares
        int whiteKingSq = BitOperations.TrailingZeroCount(whiteKing);
        int blackKingSq = BitOperations.TrailingZeroCount(blackKing);
        
        // NEW: Calculate pins and blockers for both sides
        CalculatePinsAndBlockers(WHITE, whiteKingSq, whitePieces, blackPieces, allPieces);
        CalculatePinsAndBlockers(BLACK, blackKingSq, blackPieces, whitePieces, allPieces);
        
        // Initialize king safety
        InitializeKingSafety(WHITE, whiteKingSq, whitePieces, blackPieces);
        InitializeKingSafety(BLACK, blackKingSq, blackPieces, whitePieces);
        
        // Calculate phase for tapered evaluation
        int phase = CalculateGamePhase(whitePieces, blackPieces);
        
        // Start with material evaluation
        Score score = EvaluateMaterial(whitePieces, blackPieces);
        
        // Add piece-square table evaluation (now more sophisticated)
        score += EvaluatePiecePlacement(whitePieces, blackPieces);
        
        // Early exit if big advantage
        int earlyExitValue = ((score.Mg * phase) + (score.Eg * (PHASE_MIDGAME - phase))) / PHASE_MIDGAME;
        if (Math.Abs(earlyExitValue) > LazyThreshold)
        {
            return whiteToMove ? earlyExitValue : -earlyExitValue;
        }
        
        // Evaluate pieces and mobility
        score += EvaluatePiecesAndMobility(WHITE, whitePieces, blackPieces, allPieces, whiteKingSq, blackKingSq);
        score += EvaluatePiecesAndMobility(BLACK, blackPieces, whitePieces, allPieces, blackKingSq, whiteKingSq) * -1;
        
        // King safety evaluation
        score += EvaluateKingSafety(WHITE, whiteKingSq, whitePieces, blackPieces, allPieces);
        score += EvaluateKingSafety(BLACK, blackKingSq, blackPieces, whitePieces, allPieces) * -1;
        
        // Threats evaluation
        score += EvaluateThreats(WHITE, whitePieces, blackPieces, allPieces);
        score += EvaluateThreats(BLACK, blackPieces, whitePieces, allPieces) * -1;
        
        // Passed pawns
        score += EvaluatePassedPawns(WHITE, whitePieces[PAWN], blackPieces[PAWN], whiteKingSq, blackKingSq);
        score += EvaluatePassedPawns(BLACK, blackPieces[PAWN], whitePieces[PAWN], blackKingSq, whiteKingSq) * -1;
        
        // Space evaluation
        if (((whitePieces[0] | blackPieces[0]) & ~(whitePawn | blackPawn | whiteKing | blackKing)) != 0UL)
        {
            score += EvaluateSpace(WHITE, whitePieces, blackPieces);
            score += EvaluateSpace(BLACK, blackPieces, whitePieces) * -1;
        }
        
        // NEW: Evaluate castling status
        score += EvaluateCastling(WHITE, whiteKingSq, whitePieces, blackPieces);
        score += EvaluateCastling(BLACK, blackKingSq, blackPieces, whitePieces) * -1;
        
        // Initiative bonus
        score += EvaluateInitiative(whitePieces, blackPieces, whitePawn, blackPawn, whiteKingSq, blackKingSq, score);
        
        // Final tapered evaluation
        int finalValue = ((score.Mg * phase) + (score.Eg * (PHASE_MIDGAME - phase))) / PHASE_MIDGAME;
        
        // Add tempo bonus
        if (whiteToMove)
            finalValue += TempoMg * phase / PHASE_MIDGAME + TempoEg * (PHASE_MIDGAME - phase) / PHASE_MIDGAME;
        else
            finalValue -= TempoMg * phase / PHASE_MIDGAME + TempoEg * (PHASE_MIDGAME - phase) / PHASE_MIDGAME;
        
        // Return from side to move perspective
        return whiteToMove ? finalValue : -finalValue;
    }
    
    private void InitializeKingSafety(int side, int kingSq, ulong[] ownPieces, ulong[] enemyPieces)
    {
        // Initialize attack tables for king and pawns
        attackedBy[side * 8 + KING] = CalculateKingAttacks(kingSq);
        attackedBy[side * 8 + PAWN] = CalculatePawnAttacks(side, ownPieces[PAWN]);
        attackedBy[side * 8 + ALL_PIECES] = attackedBy[side * 8 + KING] | attackedBy[side * 8 + PAWN];
        
        // Setup king ring - squares around king and a few more based on Stockfish approach
        kingRing[side] = CalculateKingAttacks(kingSq);
        
        // NEW: Adjust king ring based on king position (like Stockfish does)
        int kingsideFile = Math.Min(6, Math.Max(1, kingSq % 8));
        int kingsideRank = side == WHITE ? Math.Min(6, kingSq / 8) : Math.Max(1, kingSq / 8);
        int adjustedSq = kingsideRank * 8 + kingsideFile;
        
        kingRing[side] |= CalculateKingAttacks(adjustedSq);
        
        // Remove squares defended by two pawns from king ring (safer squares)
        ulong doublePawnDefended = attackedBy[side * 8 + PAWN] & (CalculatePawnAttacks(side, ownPieces[PAWN]));
        kingRing[side] &= ~doublePawnDefended;
        
        // Calculate pawn attacks on king ring
        int enemySide = 1 - side;
        kingAttackersCount[enemySide] = PopCount(kingRing[side] & attackedBy[enemySide * 8 + PAWN]);
    }
    
    private int CalculateGamePhase(ulong[] whitePieces, ulong[] blackPieces)
    {
        int phase = PHASE_MIDGAME;
        
        // Subtract phase value for each missing piece
        phase -= PopCount(whitePieces[KNIGHT]) * PhaseValues[KNIGHT];
        phase -= PopCount(whitePieces[BISHOP]) * PhaseValues[BISHOP];
        phase -= PopCount(whitePieces[ROOK]) * PhaseValues[ROOK];
        phase -= PopCount(whitePieces[QUEEN]) * PhaseValues[QUEEN];
        
        phase -= PopCount(blackPieces[KNIGHT]) * PhaseValues[KNIGHT];
        phase -= PopCount(blackPieces[BISHOP]) * PhaseValues[BISHOP];
        phase -= PopCount(blackPieces[ROOK]) * PhaseValues[ROOK];
        phase -= PopCount(blackPieces[QUEEN]) * PhaseValues[QUEEN];
        
        // Ensure phase is between PHASE_ENDGAME and PHASE_MIDGAME
        phase = Math.Max(PHASE_ENDGAME, Math.Min(PHASE_MIDGAME, phase));
        
        return phase;
    }
    
    private Score EvaluateMaterial(ulong[] whitePieces, ulong[] blackPieces)
    {
        int whiteMaterialMg = 0, whiteMaterialEg = 0;
        int blackMaterialMg = 0, blackMaterialEg = 0;
        
        // Count material for each piece type
        whiteMaterialMg += PopCount(whitePieces[PAWN]) * PieceValueMg[PAWN];
        whiteMaterialMg += PopCount(whitePieces[KNIGHT]) * PieceValueMg[KNIGHT];
        whiteMaterialMg += PopCount(whitePieces[BISHOP]) * PieceValueMg[BISHOP];
        whiteMaterialMg += PopCount(whitePieces[ROOK]) * PieceValueMg[ROOK];
        whiteMaterialMg += PopCount(whitePieces[QUEEN]) * PieceValueMg[QUEEN];
        
        whiteMaterialEg += PopCount(whitePieces[PAWN]) * PieceValueEg[PAWN];
        whiteMaterialEg += PopCount(whitePieces[KNIGHT]) * PieceValueEg[KNIGHT];
        whiteMaterialEg += PopCount(whitePieces[BISHOP]) * PieceValueEg[BISHOP];
        whiteMaterialEg += PopCount(whitePieces[ROOK]) * PieceValueEg[ROOK];
        whiteMaterialEg += PopCount(whitePieces[QUEEN]) * PieceValueEg[QUEEN];
        
        blackMaterialMg += PopCount(blackPieces[PAWN]) * PieceValueMg[PAWN];
        blackMaterialMg += PopCount(blackPieces[KNIGHT]) * PieceValueMg[KNIGHT];
        blackMaterialMg += PopCount(blackPieces[BISHOP]) * PieceValueMg[BISHOP];
        blackMaterialMg += PopCount(blackPieces[ROOK]) * PieceValueMg[ROOK];
        blackMaterialMg += PopCount(blackPieces[QUEEN]) * PieceValueMg[QUEEN];
        
        blackMaterialEg += PopCount(blackPieces[PAWN]) * PieceValueEg[PAWN];
        blackMaterialEg += PopCount(blackPieces[KNIGHT]) * PieceValueEg[KNIGHT];
        blackMaterialEg += PopCount(blackPieces[BISHOP]) * PieceValueEg[BISHOP];
        blackMaterialEg += PopCount(blackPieces[ROOK]) * PieceValueEg[ROOK];
        blackMaterialEg += PopCount(blackPieces[QUEEN]) * PieceValueEg[QUEEN];
        
        // Bishop pair bonus (like Stockfish)
        if (PopCount(whitePieces[BISHOP]) >= 2)
        {
            whiteMaterialMg += 30;
            whiteMaterialEg += 50;
        }
        
        if (PopCount(blackPieces[BISHOP]) >= 2)
        {
            blackMaterialMg += 30;
            blackMaterialEg += 50;
        }
        
        // NEW: Material imbalance evaluation (simplified from Stockfish)
        // Bonus for knight in closed positions
        int whitePawnCount = PopCount(whitePieces[PAWN]);
        int blackPawnCount = PopCount(blackPieces[PAWN]);
        int totalPawns = whitePawnCount + blackPawnCount;
        
        // Adjust knight value in closed positions
        int knightAdjustment = (totalPawns > 12) ? 5 : 0;
        whiteMaterialMg += PopCount(whitePieces[KNIGHT]) * knightAdjustment;
        whiteMaterialEg += PopCount(whitePieces[KNIGHT]) * knightAdjustment / 2;
        blackMaterialMg += PopCount(blackPieces[KNIGHT]) * knightAdjustment;
        blackMaterialEg += PopCount(blackPieces[KNIGHT]) * knightAdjustment / 2;
        
        // Penalty for rook in positions with no pawns
        if (whitePawnCount == 0)
        {
            whiteMaterialMg -= PopCount(whitePieces[ROOK]) * 10;
            whiteMaterialEg -= PopCount(whitePieces[ROOK]) * 15;
        }
        
        if (blackPawnCount == 0)
        {
            blackMaterialMg -= PopCount(blackPieces[ROOK]) * 10;
            blackMaterialEg -= PopCount(blackPieces[ROOK]) * 15;
        }
        
        return new Score(whiteMaterialMg - blackMaterialMg, whiteMaterialEg - blackMaterialEg);
    }
    
    private Score EvaluatePiecePlacement(ulong[] whitePieces, ulong[] blackPieces)
    {
        Score score = new Score(0, 0);
        ulong pieces;
        
        // Centralized piece bonuses (more refined than before)
        ulong centralSquares = 0x0000001818000000UL; // e4, d4, e5, d5
        ulong extendedCenter = 0x00003C3C3C3C0000UL; // d3-g3, d4-g4, d5-g5, d6-g6
        ulong innerRing = 0x00003C24243C0000UL;      // d3-g3, d6-g6, d4, g4, d5, g5
        
        // Knights in center
        pieces = whitePieces[KNIGHT];
        while (pieces != 0)
        {
            int sq = BitOperations.TrailingZeroCount(pieces);
            ulong bit = 1UL << sq;
            
            if ((bit & centralSquares) != 0)
            {
                score.Mg += 25;
                score.Eg += 10;
            }
            else if ((bit & extendedCenter) != 0)
            {
                score.Mg += 15;
                score.Eg += 5;
            }
            
            // Penalty for knight on rim
            if ((sq % 8 == 0) || (sq % 8 == 7) || (sq / 8 == 0) || (sq / 8 == 7))
            {
                score.Mg -= 20;
                score.Eg -= 15;
            }
            
            pieces &= pieces - 1;
        }
        
        pieces = blackPieces[KNIGHT];
        while (pieces != 0)
        {
            int sq = BitOperations.TrailingZeroCount(pieces);
            ulong bit = 1UL << sq;
            
            if ((bit & centralSquares) != 0)
            {
                score.Mg -= 25;
                score.Eg -= 10;
            }
            else if ((bit & extendedCenter) != 0)
            {
                score.Mg -= 15;
                score.Eg -= 5;
            }
            
            // Penalty for knight on rim
            if ((sq % 8 == 0) || (sq % 8 == 7) || (sq / 8 == 0) || (sq / 8 == 7))
            {
                score.Mg += 20;
                score.Eg += 15;
            }
            
            pieces &= pieces - 1;
        }
        
        // Bishops in center and on long diagonals
        ulong longDiagonals = 0x8142241818244281UL;
        
        pieces = whitePieces[BISHOP];
        while (pieces != 0)
        {
            int sq = BitOperations.TrailingZeroCount(pieces);
            ulong bit = 1UL << sq;
            
            if ((bit & centralSquares) != 0)
            {
                score.Mg += 20;
                score.Eg += 10;
            }
            else if ((bit & extendedCenter) != 0)
            {
                score.Mg += 10;
                score.Eg += 5;
            }
            
            // Bonus for bishop on long diagonal
            if ((bit & longDiagonals) != 0)
            {
                score.Mg += LongDiagonalBishopMg;
                score.Eg += LongDiagonalBishopEg;
            }
            
            pieces &= pieces - 1;
        }
        
        pieces = blackPieces[BISHOP];
        while (pieces != 0)
        {
            int sq = BitOperations.TrailingZeroCount(pieces);
            ulong bit = 1UL << sq;
            
            if ((bit & centralSquares) != 0)
            {
                score.Mg -= 20;
                score.Eg -= 10;
            }
            else if ((bit & extendedCenter) != 0)
            {
                score.Mg -= 10;
                score.Eg -= 5;
            }
            
            // Bonus for bishop on long diagonal
            if ((bit & longDiagonals) != 0)
            {
                score.Mg -= LongDiagonalBishopMg;
                score.Eg -= LongDiagonalBishopEg;
            }
            
            pieces &= pieces - 1;
        }
        
        // Bishops blocked by pawns
        for (int sq = 0; sq < 64; sq++)
        {
            ulong bit = 1UL << sq;
            
            if ((bit & whitePieces[BISHOP]) != 0)
            {
                // Count pawns on same color squares
                int squareColor = ((sq / 8) + (sq % 8)) % 2;
                ulong sameColorSquares = (squareColor == 0) ? 0x55AA55AA55AA55AAUL : 0xAA55AA55AA55AA55UL;
                int pawnsOnSameColor = PopCount(whitePieces[PAWN] & sameColorSquares);
                
                score.Mg -= pawnsOnSameColor * BishopPawnsMg;
                score.Eg -= pawnsOnSameColor * BishopPawnsEg;
                
                // Penalty for blocked center pawns (more severe)
                ulong centerPawns = whitePieces[PAWN] & 0x0000001818000000UL;
                if (centerPawns != 0)
                {
                    int blockedPawns = PopCount(centerPawns & (whitePieces[0] | blackPieces[0]));
                    score.Mg -= blockedPawns * BishopPawnsMg * 2;
                    score.Eg -= blockedPawns * BishopPawnsEg * 2;
                }
            }
            
            if ((bit & blackPieces[BISHOP]) != 0)
            {
                // Count pawns on same color squares
                int squareColor = ((sq / 8) + (sq % 8)) % 2;
                ulong sameColorSquares = (squareColor == 0) ? 0x55AA55AA55AA55AAUL : 0xAA55AA55AA55AA55UL;
                int pawnsOnSameColor = PopCount(blackPieces[PAWN] & sameColorSquares);
                
                score.Mg += pawnsOnSameColor * BishopPawnsMg;
                score.Eg += pawnsOnSameColor * BishopPawnsEg;
                
                // Penalty for blocked center pawns (more severe)
                ulong centerPawns = blackPieces[PAWN] & 0x0000001818000000UL;
                if (centerPawns != 0)
                {
                    int blockedPawns = PopCount(centerPawns & (whitePieces[0] | blackPieces[0]));
                    score.Mg += blockedPawns * BishopPawnsMg * 2;
                    score.Eg += blockedPawns * BishopPawnsEg * 2;
                }
            }
        }
        
        // Rook positioning
        
        // Rook on 7th rank
        ulong seventhRank = 0x00FF000000000000UL;
        ulong secondRank = 0x000000000000FF00UL;
        
        // White rooks on 7th
        int whiteRooksOn7th = PopCount(whitePieces[ROOK] & seventhRank);
        if (whiteRooksOn7th > 0 && (blackPieces[KING] & 0xFF00000000000000UL) != 0)
        {
            score.Mg += whiteRooksOn7th * 30;
            score.Eg += whiteRooksOn7th * 50;
        }
        else if (whiteRooksOn7th > 0)
        {
            score.Mg += whiteRooksOn7th * 20;
            score.Eg += whiteRooksOn7th * 30;
        }
        
        // Black rooks on 2nd
        int blackRooksOn2nd = PopCount(blackPieces[ROOK] & secondRank);
        if (blackRooksOn2nd > 0 && (whitePieces[KING] & 0x00000000000000FFUL) != 0)
        {
            score.Mg -= blackRooksOn2nd * 30;
            score.Eg -= blackRooksOn2nd * 50;
        }
        else if (blackRooksOn2nd > 0)
        {
            score.Mg -= blackRooksOn2nd * 20;
            score.Eg -= blackRooksOn2nd * 30;
        }
        
        // Rooks on open and semi-open files
        pieces = whitePieces[ROOK];
        while (pieces != 0)
        {
            int sq = BitOperations.TrailingZeroCount(pieces);
            int file = sq % 8;
            ulong fileSquares = FileMasks[file];
            
            if ((fileSquares & whitePieces[PAWN]) == 0)
            {
                if ((fileSquares & blackPieces[PAWN]) == 0)
                {
                    // Open file
                    score.Mg += RookOnFileMg[1];
                    score.Eg += RookOnFileEg[1];
                }
                else 
                {
                    // Semi-open file
                    score.Mg += RookOnFileMg[0];
                    score.Eg += RookOnFileEg[0];
                }
            }
            
            pieces &= pieces - 1;
        }
        
        pieces = blackPieces[ROOK];
        while (pieces != 0)
        {
            int sq = BitOperations.TrailingZeroCount(pieces);
            int file = sq % 8;
            ulong fileSquares = FileMasks[file];
            
            if ((fileSquares & blackPieces[PAWN]) == 0)
            {
                if ((fileSquares & whitePieces[PAWN]) == 0)
                {
                    // Open file
                    score.Mg -= RookOnFileMg[1];
                    score.Eg -= RookOnFileEg[1];
                }
                else 
                {
                    // Semi-open file
                    score.Mg -= RookOnFileMg[0];
                    score.Eg -= RookOnFileEg[0];
                }
            }
            
            pieces &= pieces - 1;
        }
        
        // Bonus for doubled rooks on open file (more severe than single rook)
        for (int file = 0; file < 8; file++)
        {
            int whiteRooksOnFile = PopCount(whitePieces[ROOK] & FileMasks[file]);
            int blackRooksOnFile = PopCount(blackPieces[ROOK] & FileMasks[file]);
            
            if (whiteRooksOnFile >= 2)
            {
                if ((FileMasks[file] & whitePieces[PAWN]) == 0)
                {
                    score.Mg += 40;
                    score.Eg += 30;
                }
            }
            
            if (blackRooksOnFile >= 2)
            {
                if ((FileMasks[file] & blackPieces[PAWN]) == 0)
                {
                    score.Mg -= 40;
                    score.Eg -= 30;
                }
            }
        }
        
        // Rook on queen file bonus
        ulong queenFile = FileMasks[3]; // d-file
        score.Mg += PopCount(whitePieces[ROOK] & queenFile) * RookOnQueenFileMg;
        score.Eg += PopCount(whitePieces[ROOK] & queenFile) * RookOnQueenFileEg;
        score.Mg -= PopCount(blackPieces[ROOK] & queenFile) * RookOnQueenFileMg;
        score.Eg -= PopCount(blackPieces[ROOK] & queenFile) * RookOnQueenFileEg;
        
        return score;
    }
    
    // This is now a combined function for piece evaluation and mobility (following Stockfish's approach)
    private Score EvaluatePiecesAndMobility(int side, ulong[] ownPieces, ulong[] enemyPieces, ulong allPieces,
                                          int ownKingSq, int enemyKingSq)
    {
        int enemySide = 1 - side;
        Score score = new Score(0, 0);
        
        // Setup mobility area (squares not attacked by enemy pawns, not occupied by our pawns or king)
        mobilityArea[side] = ~(attackedBy[enemySide * 8 + PAWN] | ownPieces[PAWN] | ownPieces[KING]);
        
        // Calculate ranks from each side's perspective
        ulong rank3BB = side == WHITE ? 0x0000000000FF0000UL : 0x0000FF0000000000UL;
        ulong rank2BB = side == WHITE ? 0x000000000000FF00UL : 0x00FF000000000000UL;
        
        // --------------- KNIGHTS ---------------
        ulong knights = ownPieces[KNIGHT];
        while (knights != 0)
        {
            int sq = BitOperations.TrailingZeroCount(knights);
            ulong bit = 1UL << sq;
            
            // Skip if pinned piece
            if ((bit & pinnedPieces[side]) != 0)
            {
                knights &= knights - 1;
                continue;
            }
            
            ulong attacks = KnightAttacks[sq];
            
            // Update attack tables
            attackedBy[side * 8 + KNIGHT] |= attacks;
            attackedBy[side * 8 + ALL_PIECES] |= attacks;
            attackedBy2[side] |= attackedBy[side * 8 + ALL_PIECES] & attacks;
            
            // Count attacked squares in mobility area
            int mobility = PopCount(attacks & mobilityArea[side]);
            
            // Apply mobility bonus (cap at maximum index)
            mobility = Math.Min(mobility, KnightMobilityMg.Length - 1);
            score.Mg += KnightMobilityMg[mobility];
            score.Eg += KnightMobilityEg[mobility];
            
            // Outpost squares (protected by our pawn, can't be attacked by enemy pawn)
            ulong outpostSquares = OutpostRanks & attackedBy[side * 8 + PAWN] & ~enemyPieces[PAWN];
            if ((bit & outpostSquares) != 0)
            {
                score.Mg += OutpostMg;
                score.Eg += OutpostEg;
            }
            else if ((attacks & outpostSquares & ~ownPieces[0]) != 0)
            {
                // Knight can reach an outpost square that's not occupied by our piece
                score.Mg += OutpostMg / 2;
                score.Eg += OutpostEg / 2;
            }
            
            // Knight behind pawn bonus
            ulong pawnPushDirection = side == WHITE ? bit >> 8 : bit << 8;
            if ((pawnPushDirection & ownPieces[PAWN]) != 0)
            {
                score.Mg += MinorBehindPawnMg;
                score.Eg += MinorBehindPawnEg;
            }
            
            // Distance to king penalty
            int distToOwnKing = Distance(sq, ownKingSq);
            score.Mg -= distToOwnKing * KingProtectorMg;
            score.Eg -= distToOwnKing * KingProtectorEg;
            
            // NEW: Knight attacking enemy queen
            if (enemyPieces[QUEEN] != 0)
            {
                int enemyQueenSq = BitOperations.TrailingZeroCount(enemyPieces[QUEEN]);
                if ((attacks & (1UL << enemyQueenSq)) != 0)
                {
                    score.Mg += KnightOnQueenMg;
                    score.Eg += KnightOnQueenEg;
                }
            }
            
            // King attack contributions (following Stockfish)
            if ((attacks & kingRing[enemySide]) != 0UL)
            {
                kingAttackersCount[side]++;
                kingAttackersWeight[side] += KingAttackWeights[KNIGHT];
                kingAttacksCount[side] += PopCount(attacks & kingRing[enemySide]);
            }
            
            knights &= knights - 1; // Clear LSB
        }
        
        // --------------- BISHOPS ---------------
        ulong bishops = ownPieces[BISHOP];
        while (bishops != 0)
        {
            int sq = BitOperations.TrailingZeroCount(bishops);
            ulong bit = 1UL << sq;
            
            // Get legal moves for pinned bishop
            ulong attacks;
            if ((bit & pinnedPieces[side]) != 0)
            {
                // Pinned piece can only move along the pin line
                ulong pinLine = GetLineBetween(sq, ownKingSq) | bit | (1UL << ownKingSq);
                attacks = CalculateBishopAttacks(sq, allPieces) & pinLine;
            }
            else
            {
                attacks = CalculateBishopAttacks(sq, allPieces);
            }
            
            // Update attack tables
            attackedBy[side * 8 + BISHOP] |= attacks;
            attackedBy[side * 8 + ALL_PIECES] |= attacks;
            attackedBy2[side] |= attackedBy[side * 8 + ALL_PIECES] & attacks;
            
            // Count attacked squares in mobility area
            int mobility = PopCount(attacks & mobilityArea[side]);
            
            // Apply mobility bonus (cap at maximum index)
            mobility = Math.Min(mobility, BishopMobilityMg.Length - 1);
            score.Mg += BishopMobilityMg[mobility];
            score.Eg += BishopMobilityEg[mobility];
            
            // NEW: Bad bishop penalty (based on pawns on same color squares)
            int squareColor = ((sq / 8) + (sq % 8)) % 2;
            ulong sameColorSquares = (squareColor == 0) ? 0x55AA55AA55AA55AAUL : 0xAA55AA55AA55AA55UL;
            int pawnsOnSameColor = PopCount(ownPieces[PAWN] & sameColorSquares);
            
            score.Mg -= pawnsOnSameColor * BishopPawnsMg;
            score.Eg -= pawnsOnSameColor * BishopPawnsEg;
            
            // Bonus for bishop on a long diagonal
            if ((bit & 0x8142241818244281UL) != 0)
            {
                score.Mg += LongDiagonalBishopMg;
                score.Eg += LongDiagonalBishopEg;
            }
            
            // Fianchetto bishop bonus
            bool isFianchetto = false;
            if (side == WHITE && (sq == 10 || sq == 13)) // g2 or b2
            {
                isFianchetto = true;
            }
            else if (side == BLACK && (sq == 50 || sq == 53)) // g7 or b7
            {
                isFianchetto = true;
            }
            
            if (isFianchetto)
            {
                score.Mg += 15;
                score.Eg += 10;
            }
            
            // Bishop behind pawn bonus
            ulong pawnPushDirection = side == WHITE ? bit >> 8 : bit << 8;
            if ((pawnPushDirection & ownPieces[PAWN]) != 0)
            {
                score.Mg += MinorBehindPawnMg;
                score.Eg += MinorBehindPawnEg;
            }
            
            // Outpost bonus
            ulong outpostSquares = OutpostRanks & attackedBy[side * 8 + PAWN] & ~enemyPieces[PAWN];
            if ((bit & outpostSquares) != 0)
            {
                score.Mg += OutpostMg;
                score.Eg += OutpostEg;
            }
            
            // Distance to king penalty
            int distToOwnKing = Distance(sq, ownKingSq);
            score.Mg -= distToOwnKing * KingProtectorMg;
            score.Eg -= distToOwnKing * KingProtectorEg;
            
            // King attack contributions
            if ((attacks & kingRing[enemySide]) != 0UL)
            {
                kingAttackersCount[side]++;
                kingAttackersWeight[side] += KingAttackWeights[BISHOP];
                kingAttacksCount[side] += PopCount(attacks & kingRing[enemySide]);
            }
            
            bishops &= bishops - 1; // Clear LSB
        }
        
        // --------------- ROOKS ---------------
        ulong rooks = ownPieces[ROOK];
        while (rooks != 0)
        {
            int sq = BitOperations.TrailingZeroCount(rooks);
            ulong bit = 1UL << sq;
            
            // Get legal moves for pinned rook
            ulong attacks;
            if ((bit & pinnedPieces[side]) != 0)
            {
                // Pinned piece can only move along the pin line
                ulong pinLine = GetLineBetween(sq, ownKingSq) | bit | (1UL << ownKingSq);
                attacks = CalculateRookAttacks(sq, allPieces) & pinLine;
            }
            else
            {
                attacks = CalculateRookAttacks(sq, allPieces);
            }
            
            // Update attack tables
            attackedBy[side * 8 + ROOK] |= attacks;
            attackedBy[side * 8 + ALL_PIECES] |= attacks;
            attackedBy2[side] |= attackedBy[side * 8 + ALL_PIECES] & attacks;
            
            // Count attacked squares in mobility area
            int mobility = PopCount(attacks & mobilityArea[side]);
            
            // Apply mobility bonus (cap at maximum index)
            mobility = Math.Min(mobility, RookMobilityMg.Length - 1);
            score.Mg += RookMobilityMg[mobility];
            score.Eg += RookMobilityEg[mobility];
            
            // Rook on file bonuses
            int file = sq % 8;
            ulong fileSquares = FileMasks[file];
            
            // Check if file is open or semi-open
            bool openFile = (fileSquares & (ownPieces[PAWN] | enemyPieces[PAWN])) == 0;
            bool semiOpenFile = !openFile && (fileSquares & ownPieces[PAWN]) == 0;
            
            if (openFile)
            {
                score.Mg += RookOnFileMg[1];
                score.Eg += RookOnFileEg[1];
            }
            else if (semiOpenFile)
            {
                score.Mg += RookOnFileMg[0];
                score.Eg += RookOnFileEg[0];
            }
            
            // Rook on 7th rank attacking pawns
            bool onSeventhRank = (side == WHITE && sq / 8 == 6) || (side == BLACK && sq / 8 == 1);
            bool enemyKingOnEighthRank = (side == WHITE && enemyKingSq / 8 == 7) || 
                                         (side == BLACK && enemyKingSq / 8 == 0);
            
            if (onSeventhRank && (enemyKingOnEighthRank || (enemyPieces[PAWN] & (side == WHITE ? 0xFF00000000000000UL : 0x00000000000000FFUL)) != 0))
            {
                score.Mg += 25;
                score.Eg += 40;
            }
            
            // Rook trapped by king (if king blocks rook's mobility and rook has few escape squares)
            if (mobility <= 3)
            {
                int kf = ownKingSq % 8;
                bool kingBlockingRook = (kf < 4) == (file < kf);
                
                // If king castling rights are lost, penalty is higher
                int rookTrappedPenalty = kingBlockingRook ? TrappedRookMg : 0;
                score.Mg -= rookTrappedPenalty;
                score.Eg -= TrappedRookEg;
            }
            
            // King attack contributions
            if ((attacks & kingRing[enemySide]) != 0UL)
            {
                kingAttackersCount[side]++;
                kingAttackersWeight[side] += KingAttackWeights[ROOK];
                kingAttacksCount[side] += PopCount(attacks & kingRing[enemySide]);
            }
            
            rooks &= rooks - 1; // Clear LSB
        }
        
        // --------------- QUEENS ---------------
        ulong queens = ownPieces[QUEEN];
        while (queens != 0)
        {
            int sq = BitOperations.TrailingZeroCount(queens);
            ulong bit = 1UL << sq;
            
            // Get legal moves for pinned queen
            ulong attacks;
            if ((bit & pinnedPieces[side]) != 0)
            {
                // Pinned piece can only move along the pin line
                ulong pinLine = GetLineBetween(sq, ownKingSq) | bit | (1UL << ownKingSq);
                attacks = CalculateQueenAttacks(sq, allPieces) & pinLine;
            }
            else
            {
                attacks = CalculateQueenAttacks(sq, allPieces);
            }
            
            // Update attack tables
            attackedBy[side * 8 + QUEEN] |= attacks;
            attackedBy[side * 8 + ALL_PIECES] |= attacks;
            attackedBy2[side] |= attackedBy[side * 8 + ALL_PIECES] & attacks;
            
            // Count attacked squares in mobility area
            int mobility = PopCount(attacks & mobilityArea[side]);
            
            // Apply mobility bonus (cap at maximum index)
            mobility = Math.Min(mobility, QueenMobilityMg.Length - 1);
            score.Mg += QueenMobilityMg[mobility];
            score.Eg += QueenMobilityEg[mobility];
            
            // NEW: Early queen development penalty
            if (side == WHITE && sq / 8 < 4 && PopCount(ownPieces[KNIGHT] | ownPieces[BISHOP] | ownPieces[ROOK]) < 3)
            {
                score.Mg -= 25;
            }
            else if (side == BLACK && sq / 8 > 3 && PopCount(ownPieces[KNIGHT] | ownPieces[BISHOP] | ownPieces[ROOK]) < 3)
            {
                score.Mg -= 25;
            }
            
            // NEW: Penalty for queen when not enough defenders
            ulong attackers = SlidingAttackers(sq, allPieces ^ bit, enemyPieces[BISHOP], enemyPieces[ROOK], enemyPieces[QUEEN]);
            if (attackers != 0UL)
            {
                ulong defenders = attackedBy[side * 8 + ALL_PIECES] & bit;
                if (PopCount(attackers) > PopCount(defenders))
                {
                    score.Mg -= WeakQueenMg;
                    score.Eg -= WeakQueenEg;
                }
            }
            
            // Queen on 7th rank bonus (similar to rook but lower value)
            bool onSeventhRank = (side == WHITE && sq / 8 == 6) || (side == BLACK && sq / 8 == 1);
            if (onSeventhRank && (enemyPieces[PAWN] & (side == WHITE ? 0xFF00000000000000UL : 0x00000000000000FFUL)) != 0)
            {
                score.Mg += 15;
                score.Eg += 25;
            }
            
            // King attack contributions
            if ((attacks & kingRing[enemySide]) != 0UL)
            {
                kingAttackersCount[side]++;
                kingAttackersWeight[side] += KingAttackWeights[QUEEN];
                kingAttacksCount[side] += PopCount(attacks & kingRing[enemySide]);
            }
            
            queens &= queens - 1; // Clear LSB
        }
        
        return score;
    }
    
    private Score EvaluateKingSafety(int side, int kingSq, ulong[] ownPieces, ulong[] enemyPieces, ulong allPieces)
    {
        int enemySide = 1 - side;
        Score score = new Score(0, 0);
        
        // King shelter evaluation (pawn shield)
        score += EvaluateKingShelter(side, kingSq, ownPieces, enemyPieces);
        
        // Only evaluate king danger if enemy has attackers
        if (kingAttackersCount[enemySide] > 0)
        {
            // Calculate king danger using Stockfish approach
            int kingDanger = 0;
            
            // Base king danger based on attackers
            kingDanger += kingAttackersCount[enemySide] * kingAttackersWeight[enemySide];
            
            // Attacked squares around king
            ulong weakSquares = kingRing[side] & ~attackedBy2[side] & 
                              (~attackedBy[side * 8 + ALL_PIECES] | attackedBy[enemySide * 8 + ALL_PIECES]);
                              
            kingDanger += 185 * PopCount(weakSquares);
            
            // Safe checks
            ulong unsafeSquares = ~attackedBy[side * 8 + ALL_PIECES] | 
                                (attackedBy[enemySide * 8 + PAWN] | attackedBy[enemySide * 8 + KNIGHT] | 
                                 attackedBy[enemySide * 8 + BISHOP] | attackedBy[enemySide * 8 + ROOK] | 
                                 attackedBy[enemySide * 8 + QUEEN]);
            
            // Knight checks
            ulong safeKnightChecks = KnightAttacks[kingSq] & attackedBy[enemySide * 8 + KNIGHT] & unsafeSquares;
            
            // Bishop checks
            ulong safeBishopChecks = CalculateBishopAttacks(kingSq, allPieces) & 
                                  attackedBy[enemySide * 8 + BISHOP] & unsafeSquares;
            
            // Rook checks
            ulong safeRookChecks = CalculateRookAttacks(kingSq, allPieces) & 
                                attackedBy[enemySide * 8 + ROOK] & unsafeSquares;
            
            // Queen checks
            ulong safeQueenChecks = (CalculateBishopAttacks(kingSq, allPieces) | CalculateRookAttacks(kingSq, allPieces)) & 
                                  attackedBy[enemySide * 8 + QUEEN] & unsafeSquares & 
                                  ~(attackedBy[enemySide * 8 + ROOK] | attackedBy[enemySide * 8 + BISHOP]);
            
            // Sum up all safe checks
            ulong unsafeChecks = 0UL;
            
            if (safeKnightChecks != 0UL)
                kingDanger += KnightSafeCheck;
            else
                unsafeChecks |= KnightAttacks[kingSq] & attackedBy[enemySide * 8 + KNIGHT];
                
            if (safeBishopChecks != 0UL)
                kingDanger += BishopSafeCheck;
            else
                unsafeChecks |= CalculateBishopAttacks(kingSq, allPieces) & attackedBy[enemySide * 8 + BISHOP];
                
            if (safeRookChecks != 0UL)
                kingDanger += RookSafeCheck;
            else
                unsafeChecks |= CalculateRookAttacks(kingSq, allPieces) & attackedBy[enemySide * 8 + ROOK];
                
            if (safeQueenChecks != 0UL)
                kingDanger += QueenSafeCheck;
                
            // Add unsafe checks to king danger
            kingDanger += 148 * PopCount(unsafeChecks);
            
            // King blockers danger
            kingDanger += 98 * PopCount(kingBlockers[side]);
            
            // Direct attacks
            kingDanger += 69 * kingAttacksCount[enemySide];
            
            // Flank attack bonus based on Stockfish approach
            // Get the file-based king flank
            ulong kingFlankBB = KingFlank[kingSq % 8];
            
            // Count enemy attacks on king flank
            int kingFlankAttack = PopCount(kingFlankBB & attackedBy[enemySide * 8 + ALL_PIECES]);
            kingDanger += 3 * kingFlankAttack * kingFlankAttack / 8;
            
            // Adjust for knight defenses (Stockfish trait)
            if ((attackedBy[side * 8 + KNIGHT] & attackedBy[side * 8 + KING]) != 0)
                kingDanger -= 100;
                
            // Reduce danger if enemy doesn't have a queen
            if (enemyPieces[QUEEN] == 0)
                kingDanger -= 873;
                
            // Scale king danger to get a score
            if (kingDanger > 100)
            {
                int kingDangerScore = kingDanger * kingDanger / 4096;
                score.Mg -= kingDangerScore;
                score.Eg -= kingDangerScore / 16;
            }
        }
        
        // Pawnless flank penalty (if king is on a flank with no pawns)
        int kf = kingSq % 8;
        ulong kingFlank = kf < 4 ? QueenSide : KingSide;
        
        if ((ownPieces[PAWN] & kingFlank) == 0)
        {
            score.Mg -= PawnlessFlankMg;
            score.Eg -= PawnlessFlankEg;
        }
        
        return score;
    }
    
    private Score EvaluateKingShelter(int side, int kingSq, ulong[] ownPieces, ulong[] enemyPieces)
    {
        Score score = new Score(0, 0);
        
        // Evaluate pawn shelter in front of the king
        int kf = kingSq % 8;
        
        // Define the files we care about (king file and adjacent files)
        int[] filesToCheck = { Math.Max(0, kf - 1), kf, Math.Min(7, kf + 1) };
        
        // Direction for "in front of king"
        int frontRank = side == WHITE ? kingSq / 8 + 1 : kingSq / 8 - 1;
        
        if (frontRank >= 0 && frontRank < 8)
        {
            for (int f = 0; f < filesToCheck.Length; f++)
            {
                int file = filesToCheck[f];
                
                // Check for our pawns in the shield
                int shieldBonus = 0;
                bool foundPawn = false;
                
                for (int r = 0; r < 3; r++)
                {
                    int checkRank = side == WHITE ? Math.Min(7, kingSq / 8 + r + 1) : Math.Max(0, kingSq / 8 - r - 1);
                    int checkSq = checkRank * 8 + file;
                    
                    if ((ownPieces[PAWN] & (1UL << checkSq)) != 0)
                    {
                        // The closer the pawn, the better the shield
                        shieldBonus += 20 - r * 10;
                        foundPawn = true;
                        break;
                    }
                }
                
                // Missing shield penalty
                if (!foundPawn)
                {
                    // Bigger penalty for missing shield on king file
                    if (file == kf)
                    {
                        score.Mg -= 25;
                        score.Eg -= 10;
                    }
                    else
                    {
                        score.Mg -= 15;
                        score.Eg -= 5;
                    }
                }
                else
                {
                    score.Mg += shieldBonus;
                    score.Eg += shieldBonus / 2;
                }
                
                // Pawn storm evaluation (enemy pawns attacking our king position)
                for (int r = 0; r < 4; r++)
                {
                    int checkRank = side == WHITE ? Math.Min(7, kingSq / 8 + r) : Math.Max(0, kingSq / 8 - r);
                    int checkSq = checkRank * 8 + file;
                    
                    if ((enemyPieces[PAWN] & (1UL << checkSq)) != 0)
                    {
                        // The closer the enemy pawn, the more dangerous
                        int stormPenalty = 15 - r * 5;
                        score.Mg -= stormPenalty;
                        score.Eg -= stormPenalty / 2;
                        break;
                    }
                }
            }
        }
        
        return score;
    }
    
    private Score EvaluateThreats(int side, ulong[] ownPieces, ulong[] enemyPieces, ulong allPieces)
    {
        int enemySide = 1 - side;
        Score score = new Score(0, 0);
        
        // Squares strongly protected by the enemy
        ulong stronglyProtected = attackedBy[enemySide * 8 + PAWN] | 
                                (attackedBy2[enemySide] & ~attackedBy2[side]);
        
        // Non-pawn enemy pieces
        ulong nonPawnEnemies = enemyPieces[0] & ~enemyPieces[PAWN];
        
        // Attacked and poorly defended enemy pieces
        ulong poorlyDefended = nonPawnEnemies & attackedBy[side * 8 + ALL_PIECES] & ~stronglyProtected;
        
        // Minor pieces attacking enemy pieces
        ulong minorAttacks = attackedBy[side * 8 + KNIGHT] | attackedBy[side * 8 + BISHOP];
        
        // Minor piece threats
        ulong minorThreats = minorAttacks & poorlyDefended;
        while (minorThreats != 0)
        {
            int sq = BitOperations.TrailingZeroCount(minorThreats);
            ulong bit = 1UL << sq;
            
            // Determine the piece being attacked
            int pieceType = 0;
            for (int pt = KNIGHT; pt <= KING; pt++)
            {
                if ((enemyPieces[pt] & bit) != 0)
                {
                    pieceType = pt;
                    break;
                }
            }
            
            score.Mg += ThreatByMinorMg[pieceType];
            score.Eg += ThreatByMinorEg[pieceType];
            
            minorThreats &= minorThreats - 1; // Clear LSB
        }
        
        // Rook threats
        ulong rookThreats = attackedBy[side * 8 + ROOK] & poorlyDefended;
        while (rookThreats != 0)
        {
            int sq = BitOperations.TrailingZeroCount(rookThreats);
            ulong bit = 1UL << sq;
            
            // Determine the piece being attacked
            int pieceType = 0;
            for (int pt = KNIGHT; pt <= KING; pt++)
            {
                if ((enemyPieces[pt] & bit) != 0)
                {
                    pieceType = pt;
                    break;
                }
            }
            
            score.Mg += ThreatByRookMg[pieceType];
            score.Eg += ThreatByRookEg[pieceType];
            
            rookThreats &= rookThreats - 1; // Clear LSB
        }
        
        // King threats
        ulong kingThreats = attackedBy[side * 8 + KING] & poorlyDefended;
        if (kingThreats != 0)
        {
            score.Mg += ThreatByKingMg * PopCount(kingThreats);
            score.Eg += ThreatByKingEg * PopCount(kingThreats);
        }
        
        // Hanging pieces (not defended and attacked)
        ulong hanging = nonPawnEnemies & attackedBy[side * 8 + ALL_PIECES] & 
                      ~attackedBy[enemySide * 8 + ALL_PIECES];
                      
        if (hanging != 0)
        {
            score.Mg += HangingMg * PopCount(hanging);
            score.Eg += HangingEg * PopCount(hanging);
        }
        
        //Unsafe pieces (attacked by lower value piece)
        ulong unsafePieces = 0UL;
        
        // Knights attacked by pawns
        unsafePieces |= enemyPieces[KNIGHT] & attackedBy[side * 8 + PAWN];
        
        // Bishops attacked by pawns
        unsafePieces |= enemyPieces[BISHOP] & attackedBy[side * 8 + PAWN];
        
        // Rooks attacked by minor pieces
        unsafePieces |= enemyPieces[ROOK] & (attackedBy[side * 8 + KNIGHT] | attackedBy[side * 8 + BISHOP]);
        
        // Queens attacked by rooks or minor pieces
        unsafePieces |= enemyPieces[QUEEN] & (attackedBy[side * 8 + ROOK] | 
                                           attackedBy[side * 8 + KNIGHT] | 
                                           attackedBy[side * 8 + BISHOP]);
        
        score.Mg += UnsafePieceMg * PopCount(unsafePieces);
        score.Eg += UnsafePieceEg * PopCount(unsafePieces);
        
        // Safe pawn threats
        ulong safePawnThreats = attackedBy[side * 8 + PAWN] & nonPawnEnemies & 
                              ~attackedBy[enemySide * 8 + PAWN];
                              
        if (safePawnThreats != 0)
        {
            score.Mg += ThreatBySafePawnMg * PopCount(safePawnThreats);
            score.Eg += ThreatBySafePawnEg * PopCount(safePawnThreats);
        }
        
        // Pawn push threats (squares where pawns can push and attack enemy pieces)
        ulong pawnPushes;
        if (side == WHITE)
        {
            // White pawns push up
            pawnPushes = (ownPieces[PAWN] << 8) & ~allPieces;
            // Include double push for pawns on 2nd rank
            pawnPushes |= ((pawnPushes & 0x0000000000FF0000UL) << 8) & ~allPieces;
        }
        else
        {
            // Black pawns push down
            pawnPushes = (ownPieces[PAWN] >> 8) & ~allPieces;
            // Include double push for pawns on 7th rank
            pawnPushes |= ((pawnPushes & 0x0000FF0000000000UL) >> 8) & ~allPieces;
        }
        
        // Calculate potential pawn attacks after pushing
        ulong pawnPushThreats = 0UL;
        while (pawnPushes != 0)
        {
            int sq = BitOperations.TrailingZeroCount(pawnPushes);
            pawnPushThreats |= PawnAttacks[side * 64 + sq];
            pawnPushes &= pawnPushes - 1;
        }
        
        // Count threats to non-pawn pieces
        pawnPushThreats &= nonPawnEnemies;
        
        if (pawnPushThreats != 0)
        {
            score.Mg += ThreatByPawnPushMg * PopCount(pawnPushThreats);
            score.Eg += ThreatByPawnPushEg * PopCount(pawnPushThreats);
        }
        
        return score;
    }
    
    private Score EvaluatePassedPawns(int side, ulong pawns, ulong enemyPawns, int kingSq, int enemyKingSq)
    {
        Score score = new Score(0, 0);
        
        // Define forward masks for pawns
        ulong[] forwardMasks = new ulong[2 * 64]; // [color][square]
        
        // Initialize forward masks for pawns
        for (int sq = 0; sq < 64; sq++)
        {
            int file = sq % 8;
            int rank = sq / 8;
            
            ulong forward = 0UL;
            
            // Forward mask for white
            for (int r = rank + 1; r < 8; r++)
            {
                forward |= 1UL << (r * 8 + file);
            }
            forwardMasks[WHITE * 64 + sq] = forward;
            
            // Forward mask for black
            forward = 0UL;
            for (int r = rank - 1; r >= 0; r--)
            {
                forward |= 1UL << (r * 8 + file);
            }
            forwardMasks[BLACK * 64 + sq] = forward;
        }
        
        // Check for passed pawns
        while (pawns != 0)
        {
            int sq = BitOperations.TrailingZeroCount(pawns);
            int file = sq % 8;
            
            // Define adjacent files and span (like Stockfish)
            ulong adjacentFiles = 0UL;
            if (file > 0) adjacentFiles |= FileMasks[file - 1];
            adjacentFiles |= FileMasks[file];
            if (file < 7) adjacentFiles |= FileMasks[file + 1];
            
            // Define span mask - all squares in front of the pawn, including diagonals
            ulong span = forwardMasks[side * 64 + sq] & adjacentFiles;
            
            // Check if this is a passed pawn (no enemy pawns in front or diagonally in front)
            if ((span & enemyPawns) == 0UL)
            {
                // Get rank from the correct perspective
                int relativeRank = side == WHITE ? sq / 8 : 7 - sq / 8;
                
                // Base bonus by rank (following Stockfish approach)
                score.Mg += PassedRankMg[relativeRank];
                score.Eg += PassedRankEg[relativeRank];
                
                // Additional bonuses for advanced passed pawns
                if (relativeRank >= 4) // Only for advanced pawns
                {
                    int advancementSquare = side == WHITE ? sq + 8 : sq - 8;
                    
                    // Bonus based on king proximity
                    int kingProximityBonus = 5 * relativeRank; // Higher weight for more advanced pawns
                    
                    // Distance of enemy king to advancement square
                    int enemyKingDistance = Math.Min(Distance(enemyKingSq, advancementSquare), 5);
                    
                    // Distance of friendly king to advancement square
                    int friendlyKingDistance = Math.Min(Distance(kingSq, advancementSquare), 5);
                    
                    // Bonus based on king proximity (Stockfish approach)
                    int w = 5 * relativeRank - 13;
                    
                    // Endgame bonus based on king proximity
                    score.Eg += (((enemyKingDistance * 19) / 4 - friendlyKingDistance * 2) * w) / 8;
                    
                    // Check if the path to promotion is clear
                    ulong pathToQueen = forwardMasks[side * 64 + sq] & FileMasks[file];
                    bool pathClear = (pathToQueen & (pawns | enemyPawns)) == 0;
                    
                    // Check if the path is safe from attacks
                    bool pathSafe = (pathToQueen & attackedBy[enemySide * 8 + ALL_PIECES]) == 0;
                    
                    // Bonus based on path safety
                    if (pathClear && pathSafe)
                    {
                        score.Mg += 15 * relativeRank;
                        score.Eg += 30 * relativeRank;
                    }
                    else if (pathClear)
                    {
                        score.Mg += 10 * relativeRank;
                        score.Eg += 20 * relativeRank;
                    }
                    
                    // Bonus if the square in front is defended
                    if ((attackedBy[side * 8 + ALL_PIECES] & (1UL << advancementSquare)) != 0)
                    {
                        score.Mg += 5 * relativeRank;
                        score.Eg += 10 * relativeRank;
                    }
                }
            }
            
            pawns &= pawns - 1; // Clear LSB