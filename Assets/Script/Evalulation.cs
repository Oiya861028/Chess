using System;
using UnityEngine;

public class Evaluation
{
    // Piece values for middlegame and endgame
    private static readonly int[] PieceValueMg = { 0, 126, 781, 825, 1276, 2538 }; // None, Pawn, Knight, Bishop, Rook, Queen
    private static readonly int[] PieceValueEg = { 0, 208, 854, 915, 1380, 2682 }; // None, Pawn, Knight, Bishop, Rook, Queen
    
    // Phase values for tapered evaluation
    private const int PHASE_MIDGAME = 24;
    private const int PHASE_ENDGAME = 0;
    private static readonly int[] PhaseValues = { 0, 0, 1, 1, 2, 4 }; // None, Pawn, Knight, Bishop, Rook, Queen
    
    // Thresholds
    private const int LazyThreshold = 1400;
    private const int SpaceThreshold = 12222;
    
    // Pre-calculated attack tables and lookup tables
    private ulong[] KnightAttacks = new ulong[64];
    private ulong[] KingAttacks = new ulong[64];
    private ulong[] PawnAttacks = new ulong[2 * 64]; // [color][square]
    
    // King attack weights by piece type (None, Pawn, Knight, Bishop, Rook, Queen)
    private static readonly int[] KingAttackWeights = { 0, 0, 81, 52, 44, 10 };
    
    // Safe check penalties
    private const int QueenSafeCheck = 780;
    private const int RookSafeCheck = 1080;
    private const int BishopSafeCheck = 635;
    private const int KnightSafeCheck = 790;
    
    // Piece mobility bonuses stored as separate arrays to avoid C# array syntax issues
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
    private static readonly int[] QueenMobilityMg = { -39, -21, 3, 3, 14, 22, 28, 41, 43, 48, 56, 60, 60, 66, 67 };
    private static readonly int[] QueenMobilityEg = { -36, -15, 8, 18, 34, 54, 61, 73, 79, 92, 94, 104, 113, 120, 123 };
    
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
    
    // Passed pawn bonuses by rank
    private static readonly int[] PassedRankMg = { 0, 10, 17, 15, 62, 168, 276, 552 };  // Added value for 8th rank
    private static readonly int[] PassedRankEg = { 0, 28, 33, 41, 72, 177, 260, 520 };  // Added value for 8th rank
    
    // Other bonuses
    private static readonly int[] RookOnFileMg = { 21, 47 }; // Semi-open, Open
    private static readonly int[] RookOnFileEg = { 4, 25 };
    
    private static readonly int[] ThreatByMinorMg = { 0, 6, 59, 79, 90, 79 };
    private static readonly int[] ThreatByMinorEg = { 0, 32, 41, 56, 119, 161 };
    
    private static readonly int[] ThreatByRookMg = { 0, 3, 38, 38, 0, 51 };
    private static readonly int[] ThreatByRookEg = { 0, 44, 71, 61, 38, 38 };
    
    private const int CastlingBonusMg = 50;  // Middlegame bonus for having castled
    private const int CastlingBonusEg = 0;  
    // Common positional bonuses (middlegame, endgame)
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
    private const int PawnlessFlankPenaltyMg = 17; private const int PawnlessFlankPenaltyEg = 95; 
    private const int RestrictedPieceMg = 7; private const int RestrictedPieceEg = 7;
    private const int RookOnQueenFileMg = 7; private const int RookOnQueenFileEg = 6;
    private const int SliderOnQueenMg = 59; private const int SliderOnQueenEg = 18;
    private const int ThreatByKingMg = 24; private const int ThreatByKingEg = 89;
    private const int ThreatByPawnPushMg = 48; private const int ThreatByPawnPushEg = 39;
    private const int ThreatBySafePawnMg = 173; private const int ThreatBySafePawnEg = 94;
    private const int TrappedRookMg = 52; private const int TrappedRookEg = 10;
    private const int WeakQueenMg = 49; private const int WeakQueenEg = 15;
    
    // Tempo bonus
    private const int TempoMg = 28;
    private const int TempoEg = 12;
    private static readonly ulong[] KingFlank = {
        // Files A-C
        FileMasks[0] | FileMasks[1] | FileMasks[2],
        FileMasks[0] | FileMasks[1] | FileMasks[2],
        FileMasks[0] | FileMasks[1] | FileMasks[2],
        // Files D-E
        FileMasks[2] | FileMasks[3] | FileMasks[4],
        FileMasks[2] | FileMasks[3] | FileMasks[4],
        // Files F-H
        FileMasks[5] | FileMasks[6] | FileMasks[7],
        FileMasks[5] | FileMasks[6] | FileMasks[7],
        FileMasks[5] | FileMasks[6] | FileMasks[7]
    };
    // Working data for evaluation
    private ulong[] attackedBy = new ulong[2 * 7]; // [color][piece type]
    private ulong[] attackedBy2 = new ulong[2]; // [color]
    private ulong[] mobilityArea = new ulong[2]; // [color]
    private ulong[] kingRing = new ulong[2]; // [color]
    private int[] kingAttackersCount = new int[2]; // [color]
    private int[] kingAttackersWeight = new int[2]; // [color]
    private int[] kingAttacksCount = new int[2]; // [color]
    
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
    private const int PAWN = 1;
    private const int KNIGHT = 2;
    private const int BISHOP = 3;
    private const int ROOK = 4;
    private const int QUEEN = 5;
    private const int KING = 6;
    private const int ALL_PIECES = 0;
    
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
    
    // Calculate bishop attacks (simplified - no blocking pieces)
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
    
    // Calculate rook attacks (simplified - no blocking pieces)
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
    
    // Make a score from middlegame and endgame values
    private Score MakeScore(int mg, int eg)
    {
        return new Score(mg, eg);
    }
    public bool IsInCheck(bool isWhite, ulong[] whitePieces, ulong[] blackPieces, ulong allPieces)
{
    // Sanity check to prevent array index out of bounds
    if (whitePieces == null || whitePieces.Length < 7 || blackPieces == null || blackPieces.Length < 7) {
        Debug.LogError("Invalid piece arrays passed to IsInCheck. Arrays must have at least 7 elements.");
        return false;
    }

    // Get king square - make sure the king exists
    ulong kingBitboard = isWhite ? whitePieces[KING] : blackPieces[KING];
    if (kingBitboard == 0) {
        Debug.LogError("No king found for " + (isWhite ? "white" : "black") + " in IsInCheck!");
        return false;
    }

    int kingSq = BitOperations.TrailingZeroCount(kingBitboard);
    int enemySide = isWhite ? BLACK : WHITE;

    // Check for pawn attacks
    if ((PawnAttacks[enemySide * 64 + kingSq] & (isWhite ? blackPieces[PAWN] : whitePieces[PAWN])) != 0)
        return true;

    // Check for knight attacks
    if ((KnightAttacks[kingSq] & (isWhite ? blackPieces[KNIGHT] : whitePieces[KNIGHT])) != 0)
        return true;

    // Check for bishop/queen diagonal attacks
    ulong bishopAttacks = CalculateBishopAttacks(kingSq, allPieces);
    if ((bishopAttacks & (isWhite ? (blackPieces[BISHOP] | blackPieces[QUEEN]) : 
                                   (whitePieces[BISHOP] | whitePieces[QUEEN]))) != 0)
        return true;

    // Check for rook/queen straight attacks
    ulong rookAttacks = CalculateRookAttacks(kingSq, allPieces);
    if ((rookAttacks & (isWhite ? (blackPieces[ROOK] | blackPieces[QUEEN]) : 
                                 (whitePieces[ROOK] | whitePieces[QUEEN]))) != 0)
        return true;

    return false;
}
    // Main evaluation function
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
        
        // Initialize king safety
        InitializeKingSafety(WHITE, whiteKingSq, whitePieces, blackPieces);
        InitializeKingSafety(BLACK, blackKingSq, blackPieces, whitePieces);
        
        // Calculate phase for tapered evaluation
        int phase = CalculateGamePhase(whitePieces, blackPieces);
        
        // Start with material evaluation
        Score score = EvaluateMaterial(whitePieces, blackPieces);
        
        // Add piece-square table evaluation (simplified)
        score += EvaluatePiecePlacement(whitePieces, blackPieces);
        
        // Early exit if big advantage
        int earlyExitValue = ((score.Mg * phase) + (score.Eg * (PHASE_MIDGAME - phase))) / PHASE_MIDGAME;
        if (Math.Abs(earlyExitValue) > LazyThreshold)
        {
            return whiteToMove ? earlyExitValue : -earlyExitValue;
        }
        
        // Mobility evaluation
        score += EvaluateMobility(WHITE, whitePieces, blackPieces, allPieces);
        score += EvaluateMobility(BLACK, blackPieces, whitePieces, allPieces) * -1;
        
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
        if (((allPieces & ~(whitePieces[PAWN] | blackPieces[PAWN] | whiteKing | blackKing)) != 0UL))
        {
            score += EvaluateSpace(WHITE, whitePieces, blackPieces);
            score += EvaluateSpace(BLACK, blackPieces, whitePieces) * -1;
        }
        
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
        attackedBy[side * 7 + KING] = CalculateKingAttacks(kingSq);
        attackedBy[side * 7 + PAWN] = CalculatePawnAttacks(side, ownPieces[PAWN]);
        attackedBy[side * 7 + ALL_PIECES] = attackedBy[side * 7 + KING] | attackedBy[side * 7 + PAWN];
        
        // Setup king ring - squares around king and a few more
        kingRing[side] = CalculateKingAttacks(kingSq);
        
        // Calculate pawn attacks on king ring
        int enemySide = 1 - side;
        kingAttackersCount[enemySide] = PopCount(kingRing[side] & attackedBy[enemySide * 7 + PAWN]);
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
        
        // Handle bishop pair bonus
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
        
        return new Score(whiteMaterialMg - blackMaterialMg, whiteMaterialEg - blackMaterialEg);
    }
    
    private Score EvaluatePiecePlacement(ulong[] whitePieces, ulong[] blackPieces)
    {
        // Simplified piece-square table evaluation - using central bonus
        Score score = new Score(0, 0);
        
        // Central bonus for pieces
        ulong centralSquares = 0x0000001818000000UL; // e4, d4, e5, d5
        ulong extendedCenter = 0x00003C3C3C3C0000UL; // d3-g3, d4-g4, d5-g5, d6-g6
        
        // Piece placement bonuses
        score.Mg += PopCount(whitePieces[KNIGHT] & extendedCenter) * 15;
        score.Mg += PopCount(whitePieces[KNIGHT] & centralSquares) * 10;
        score.Mg -= PopCount(blackPieces[KNIGHT] & extendedCenter) * 15;
        score.Mg -= PopCount(blackPieces[KNIGHT] & centralSquares) * 10;
        
        score.Mg += PopCount(whitePieces[BISHOP] & extendedCenter) * 10;
        score.Mg += PopCount(whitePieces[BISHOP] & centralSquares) * 15;
        score.Mg -= PopCount(blackPieces[BISHOP] & extendedCenter) * 10;
        score.Mg -= PopCount(blackPieces[BISHOP] & centralSquares) * 15;
        
        // Rook on 7th rank
        ulong seventhRank = 0x00FF000000000000UL; // 7th rank for white
        ulong secondRank = 0x000000000000FF00UL; // 2nd rank for black
        
        score.Mg += PopCount(whitePieces[ROOK] & seventhRank) * 20;
        score.Eg += PopCount(whitePieces[ROOK] & seventhRank) * 40;
        score.Mg -= PopCount(blackPieces[ROOK] & secondRank) * 20;
        score.Eg -= PopCount(blackPieces[ROOK] & secondRank) * 40;
        
        // Penalize knights on the rim
        ulong rim = 0xFF818181818181FFUL;
        score.Mg -= PopCount(whitePieces[KNIGHT] & rim) * 15;
        score.Eg -= PopCount(whitePieces[KNIGHT] & rim) * 10;
        score.Mg += PopCount(blackPieces[KNIGHT] & rim) * 15;
        score.Eg += PopCount(blackPieces[KNIGHT] & rim) * 10;
        
        return score;
    }
    
    private Score EvaluateMobility(int side, ulong[] ownPieces, ulong[] enemyPieces, ulong allPieces)
    {
        int enemySide = 1 - side;
        Score score = new Score(0, 0);
        
        // Setup mobility area - squares not attacked by enemy pawns and not occupied by our pawns or king
        mobilityArea[side] = ~(attackedBy[enemySide * 7 + PAWN] | ownPieces[PAWN] | ownPieces[KING]);
        
        // Knights mobility
        ulong knights = ownPieces[KNIGHT];
        while (knights != 0)
        {
            int sq = BitOperations.TrailingZeroCount(knights);
            ulong attacks = KnightAttacks[sq];
            
            // Update attack tables
            attackedBy[side * 7 + KNIGHT] |= attacks;
            attackedBy[side * 7 + ALL_PIECES] |= attacks;
            attackedBy2[side] |= attackedBy[side * 7 + ALL_PIECES] & attacks;
            
            // Count attacked squares in mobility area
            int mobility = PopCount(attacks & mobilityArea[side]);
            
            // Apply mobility bonus (cap at maximum index)
            mobility = Math.Min(mobility, 8);
            score.Mg += KnightMobilityMg[mobility];
            score.Eg += KnightMobilityEg[mobility];
            
            // King attack contributions
            if ((attacks & kingRing[enemySide]) != 0UL)
            {
                kingAttackersCount[side]++;
                kingAttackersWeight[side] += KingAttackWeights[KNIGHT];
                kingAttacksCount[side] += PopCount(attacks & kingRing[enemySide]);
            }
            
            knights &= knights - 1; // Clear LSB
        }
        
        // Bishops mobility
        ulong bishops = ownPieces[BISHOP];
        while (bishops != 0)
        {
            int sq = BitOperations.TrailingZeroCount(bishops);
            ulong attacks = CalculateBishopAttacks(sq, allPieces);
            
            // Update attack tables
            attackedBy[side * 7 + BISHOP] |= attacks;
            attackedBy[side * 7 + ALL_PIECES] |= attacks;
            attackedBy2[side] |= attackedBy[side * 7 + ALL_PIECES] & attacks;
            
            // Count attacked squares in mobility area
            int mobility = PopCount(attacks & mobilityArea[side]);
            
            // Apply mobility bonus (cap at maximum index)
            mobility = Math.Min(mobility, 13);
            score.Mg += BishopMobilityMg[mobility];
            score.Eg += BishopMobilityEg[mobility];
            
            // King attack contributions
            if ((attacks & kingRing[enemySide]) != 0UL)
            {
                kingAttackersCount[side]++;
                kingAttackersWeight[side] += KingAttackWeights[BISHOP];
                kingAttacksCount[side] += PopCount(attacks & kingRing[enemySide]);
            }
            
            bishops &= bishops - 1; // Clear LSB
        }
        
        // Rooks mobility
        ulong rooks = ownPieces[ROOK];
        while (rooks != 0)
        {
            int sq = BitOperations.TrailingZeroCount(rooks);
            ulong attacks = CalculateRookAttacks(sq, allPieces);
            
            // Update attack tables
            attackedBy[side * 7 + ROOK] |= attacks;
            attackedBy[side * 7 + ALL_PIECES] |= attacks;
            attackedBy2[side] |= attackedBy[side * 7 + ALL_PIECES] & attacks;
            
            // Count attacked squares in mobility area
            int mobility = PopCount(attacks & mobilityArea[side]);
            
            // Apply mobility bonus (cap at maximum index)
            mobility = Math.Min(mobility, 14);
            score.Mg += RookMobilityMg[mobility];
            score.Eg += RookMobilityEg[mobility];
            
            // King attack contributions
            if ((attacks & kingRing[enemySide]) != 0UL)
            {
                kingAttackersCount[side]++;
                kingAttackersWeight[side] += KingAttackWeights[ROOK];
                kingAttacksCount[side] += PopCount(attacks & kingRing[enemySide]);
            }
            
            // Rook on open or semi-open file
            int file = sq % 8;
            ulong pawnsOnFile = (ownPieces[PAWN] | enemyPieces[PAWN]) & FileMasks[file];
            
            if (pawnsOnFile == 0) // Open file
            {
                score.Mg += RookOnFileMg[1];
                score.Eg += RookOnFileEg[1];
            }
            else if ((pawnsOnFile & ownPieces[PAWN]) == 0) // Semi-open file
            {
                score.Mg += RookOnFileMg[0];
                score.Eg += RookOnFileEg[0];
            }
            
            rooks &= rooks - 1; // Clear LSB
        }
        
        // Queens mobility
        ulong queens = ownPieces[QUEEN];
        while (queens != 0)
        {
            int sq = BitOperations.TrailingZeroCount(queens);
            ulong attacks = CalculateQueenAttacks(sq, allPieces);
            
            // Update attack tables
            attackedBy[side * 7 + QUEEN] |= attacks;
            attackedBy[side * 7 + ALL_PIECES] |= attacks;
            attackedBy2[side] |= attackedBy[side * 7 + ALL_PIECES] & attacks;
            
            // Count attacked squares in mobility area
            int mobility = PopCount(attacks & mobilityArea[side]);
            
            // Apply mobility bonus (cap at maximum index)
            mobility = Math.Min(mobility, 14); // Limit to array bounds
            score.Mg += QueenMobilityMg[mobility]; 
            score.Eg += QueenMobilityEg[mobility];
            
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
        
        // King shelter (pawn shield)
        ulong kingFile = FileMasks[kingSq % 8];
        ulong adjacentFiles = ((kingSq % 8) > 0 ? FileMasks[(kingSq % 8) - 1] : 0) |
                              ((kingSq % 8) < 7 ? FileMasks[(kingSq % 8) + 1] : 0);
        
        ulong kingArea = kingFile | adjacentFiles;
        
        // Shield bonus depends on the side and rank of the king
        ulong pawnShield;
        if (side == WHITE)
        {
            // For white, check pawns on rank 2 and 3 in front of king
            pawnShield = ownPieces[PAWN] & (kingArea & (RankMasks[1] | RankMasks[2]));
        }
        else
        {
            // For black, check pawns on rank 7 and 6 in front of king
            pawnShield = ownPieces[PAWN] & (kingArea & (RankMasks[6] | RankMasks[5]));
        }
        
        int shieldBonus = PopCount(pawnShield) * 10;
        score.Mg += shieldBonus;
        
        // King safety based on attackers
        if (kingAttackersCount[enemySide] > 0)
        {
            // Calculate king danger
            int kingDanger = 0;
            
            kingDanger += kingAttackersCount[enemySide] * kingAttackersWeight[enemySide];
            kingDanger += kingAttacksCount[enemySide] * 10;
            
            // Weak squares around king
            ulong weakSquares = kingRing[side] & ~attackedBy2[side] & 
                                (~attackedBy[side * 7 + ALL_PIECES] | attackedBy[enemySide * 7 + ALL_PIECES]);
                                
            kingDanger += PopCount(weakSquares) * 15;
            
            // Check potential
            ulong safeChecks = 0;
            
            // Knight checks
            safeChecks |= KnightAttacks[kingSq] & attackedBy[enemySide * 7 + KNIGHT] & 
                          ~attackedBy[side * 7 + ALL_PIECES];
                          
            // Bishop checks
            ulong bishopChecks = CalculateBishopAttacks(kingSq, allPieces) & 
                                attackedBy[enemySide * 7 + BISHOP] & 
                                ~attackedBy[side * 7 + ALL_PIECES];
            
            // Rook checks
            ulong rookChecks = CalculateRookAttacks(kingSq, allPieces) & 
                              attackedBy[enemySide * 7 + ROOK] & 
                              ~attackedBy[side * 7 + ALL_PIECES];
                              
            if (bishopChecks != 0UL)
                kingDanger += BishopSafeCheck;
                
            if (rookChecks != 0UL)
                kingDanger += RookSafeCheck;
                
            if (((bishopChecks | rookChecks) & attackedBy[enemySide * 7 + QUEEN]) != 0UL)
                kingDanger += QueenSafeCheck;
                
            if (safeChecks != 0UL)
                kingDanger += KnightSafeCheck;
            
            // Scale king danger into a score
            if (kingDanger > 0)
            {
                score.Mg -= Math.Min(kingDanger * kingDanger / 4096, 1000);
            }
        }
        if ((ownPieces[PAWN] & KingFlank[kingFile]) == 0)
            {
                score.Mg -= PawnlessFlankPenaltyMg;
                score.Eg -= PawnlessFlankPenaltyEg;
            }
        
        return score;
    }
    
    private Score EvaluateThreats(int side, ulong[] ownPieces, ulong[] enemyPieces, ulong allPieces)
    {
        int enemySide = 1 - side;
        Score score = new Score(0, 0);
        
        // Squares strongly protected by the enemy
        ulong stronglyProtected = attackedBy[enemySide * 7 + PAWN] | 
                                 (attackedBy2[enemySide] & ~attackedBy2[side]);
        
        // Non-pawn enemy pieces
        ulong nonPawnEnemies = enemyPieces[0] & ~enemyPieces[PAWN];
        
        // Attacked and poorly defended enemy pieces
        ulong poorlyDefended = nonPawnEnemies & attackedBy[side * 7 + ALL_PIECES] & ~stronglyProtected;
        
        // Threat bonus by piece type
        ulong minorAttacks = attackedBy[side * 7 + KNIGHT] | attackedBy[side * 7 + BISHOP];
        ulong rookAttacks = attackedBy[side * 7 + ROOK];
        
        // Minor pieces attacking enemy pieces
        ulong minorThreats = minorAttacks & poorlyDefended;
        while (minorThreats != 0)
        {
            int sq = BitOperations.TrailingZeroCount(minorThreats);
            ulong piece = 1UL << sq;
            
            // Determine what type of piece is being attacked
            int pieceType = 0;
            for (int pt = KNIGHT; pt <= QUEEN; pt++)
            {
                if ((enemyPieces[pt] & piece) != 0)
                {
                    pieceType = pt;
                    break;
                }
            }
            
            score.Mg += ThreatByMinorMg[pieceType];
            score.Eg += ThreatByMinorEg[pieceType];
            
            minorThreats &= minorThreats - 1; // Clear LSB
        }
        
        // Rooks attacking enemy pieces
        ulong rookThreats = rookAttacks & poorlyDefended;
        while (rookThreats != 0)
        {
            int sq = BitOperations.TrailingZeroCount(rookThreats);
            ulong piece = 1UL << sq;
            
            // Determine what type of piece is being attacked
            int pieceType = 0;
            for (int pt = KNIGHT; pt <= QUEEN; pt++)
            {
                if ((enemyPieces[pt] & piece) != 0)
                {
                    pieceType = pt;
                    break;
                }
            }
            
            score.Mg += ThreatByRookMg[pieceType];
            score.Eg += ThreatByRookEg[pieceType];
            
            rookThreats &= rookThreats - 1; // Clear LSB
        }
        
        // King attacking enemy pieces
        ulong kingThreats = attackedBy[side * 7 + KING] & poorlyDefended;
        if (kingThreats != 0UL)
        {
            score.Mg += ThreatByKingMg * PopCount(kingThreats);
            score.Eg += ThreatByKingEg * PopCount(kingThreats);
        }
        
        // Enemy pieces not defended by pawns and attacked
        ulong hanging = poorlyDefended & ~attackedBy[enemySide * 7 + PAWN];
        if (hanging != 0UL)
        {
            score.Mg += HangingMg * PopCount(hanging);
            score.Eg += HangingEg * PopCount(hanging);
        }
        
        // Safe pawn threats to enemy pieces
        ulong safePawnThreats = attackedBy[side * 7 + PAWN] & nonPawnEnemies & ~attackedBy[enemySide * 7 + PAWN];
        if (safePawnThreats != 0UL)
        {
            score.Mg += ThreatBySafePawnMg * PopCount(safePawnThreats);
            score.Eg += ThreatBySafePawnEg * PopCount(safePawnThreats);
        }
        
        return score;
    }
    
    private Score EvaluatePassedPawns(int side, ulong pawns, ulong enemyPawns, int kingSq, int enemyKingSq)
    {
        Score score = new Score(0, 0);
        
        // Define forward masks for pawns
        ulong[] forwardMasks = new ulong[2 * 64]; // [color][square]
        
        // Initialize forward masks (simplified)
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
            int rank = sq / 8;
            int file = sq % 8;
            
            // Define adjacent files
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
                int relativeRank = side == WHITE ? rank : 7 - rank;
                
                // Base bonus by rank
                score.Mg += PassedRankMg[relativeRank];
                score.Eg += PassedRankEg[relativeRank];
                
                // Bonus based on king proximity
                if (relativeRank >= 4) // Only for advanced passers
                {
                    int advancementSquare = side == WHITE ? sq + 8 : sq - 8;
                    
                    // Bonus based on king proximity
                    int kingProximityBonus = 5 * (relativeRank - 3); // Higher weight for more advanced pawns
                    
                    // Distance from enemy king to advancement square
                    int enemyKingDistance = Math.Min(Distance(enemyKingSq, advancementSquare), 5);
                    
                    // Distance from friendly king to advancement square
                    int friendlyKingDistance = Math.Min(Distance(kingSq, advancementSquare), 5);
                    
                    // Bonus based on king proximity - more important in endgame
                    score.Eg += (enemyKingDistance * 5 - friendlyKingDistance * 2) * kingProximityBonus;
                }
            }
            
            pawns &= pawns - 1; // Clear LSB
        }
        
        return score;
    }
    
    private Score EvaluateSpace(int side, ulong[] ownPieces, ulong[] enemyPieces)
    {
        Score score = new Score(0, 0);
        
        // Only evaluate space when both sides have relatively many pieces
        if (PopCount(ownPieces[0]) + PopCount(enemyPieces[0]) < SpaceThreshold / 100)
            return score;
        
        // Define space mask - central files on specific ranks
        ulong spaceMask;
        if (side == WHITE)
            spaceMask = CenterFiles & (RankMasks[1] | RankMasks[2] | RankMasks[3]);
        else
            spaceMask = CenterFiles & (RankMasks[6] | RankMasks[5] | RankMasks[4]);
            
        // Find squares not occupied by pawns and not attacked by enemy pawns
        ulong safeSquares = spaceMask & ~ownPieces[PAWN] & ~attackedBy[(1-side) * 7 + PAWN];
        
        // Bonus for each safe square
        int spaceBonus = PopCount(safeSquares) * 2;
        
        // Weight by number of pieces
        int pieceCount = PopCount(ownPieces[KNIGHT]) + PopCount(ownPieces[BISHOP]) + 
                         PopCount(ownPieces[ROOK]) + 2 * PopCount(ownPieces[QUEEN]);
                         
        spaceBonus = spaceBonus * pieceCount / 4;
        
        score.Mg += spaceBonus;
        
        return score;
    }
    
    private Score EvaluateInitiative(ulong[] whitePieces, ulong[] blackPieces, ulong whitePawn, ulong blackPawn, 
                                   int whiteKingSq, int blackKingSq, Score mainScore)
    {
        // Extract middlegame and endgame values
        int mg = mainScore.Mg;
        int eg = mainScore.Eg;
        
        // Compute outflanking (king distance)
        int outflanking = Distance(whiteKingSq % 8, blackKingSq % 8) - 
                         Distance(whiteKingSq / 8, blackKingSq / 8);
                         
        // Check if a king has infiltrated
        bool infiltration = (whiteKingSq / 8 > 4) || (blackKingSq / 8 < 3);
        
        // Check if pawns are on both flanks
        bool pawnsOnBothFlanks = ((whitePawn | blackPawn) & QueenSide) != 0 && 
                                ((whitePawn | blackPawn) & KingSide) != 0;
        
        // Calculate passed pawns count (simplified)
        int passedCount = 0; 
        
        // Calculate complexity score
        int complexity = 9 * passedCount +
                         11 * (PopCount(whitePawn) + PopCount(blackPawn)) +
                         9 * outflanking +
                         12 * (infiltration ? 1 : 0) +
                         21 * (pawnsOnBothFlanks ? 1 : 0) +
                         51 * ((whitePieces[0] == (whitePawn | whitePieces[KING]) && 
                               blackPieces[0] == (blackPawn | blackPieces[KING])) ? 1 : 0) -
                         100;
        
        // Apply initiative bonus
        int mgBonus = ((mg > 0 ? 1 : 0) - (mg < 0 ? 1 : 0)) * Math.Max(Math.Min(complexity + 50, 0), -Math.Abs(mg));
        int egBonus = ((eg > 0 ? 1 : 0) - (eg < 0 ? 1 : 0)) * Math.Max(complexity, -Math.Abs(eg));
        
        return new Score(mgBonus, egBonus);
    }
    
    // Helper functions
    
    // Count bits set in a ulong
    private int PopCount(ulong x)
    {
        int count = 0;
        while (x != 0)
        {
            count++;
            x &= x - 1; // Clear the least significant bit set
        }
        return count;
    }
    
    // Calculate distance between two squares
    private int Distance(int sq1, int sq2)
    {
        int file1 = sq1 % 8, file2 = sq2 % 8;
        int rank1 = sq1 / 8, rank2 = sq2 / 8;
        return Math.Max(Math.Abs(file1 - file2), Math.Abs(rank1 - rank2));
    }
    private ulong GetKingFlank(int file)
    {
        // Kingside (files E-H)
        if (file >= 4)
            return FileMasks[5] | FileMasks[6] | FileMasks[7];
        // Queenside (files A-D)
        else
            return FileMasks[0] | FileMasks[1] | FileMasks[2];
    }
    // For C# in Unity, we'll need this BitOperations class since it's not available by default
    public static class BitOperations
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