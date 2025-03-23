using System;
using UnityEngine;

public class MagicBitboards
{
    // Magic numbers for bishops and rooks (adjusted for your bit ordering)
    private readonly ulong[] bishopMagics = new ulong[64];
    private readonly ulong[] rookMagics = new ulong[64];
    
    // Attack tables for bishops and rooks
    private readonly ulong[][] bishopAttacks = new ulong[64][];
    private readonly ulong[][] rookAttacks = new ulong[64][];
    
    // Blocker masks for bishops and rooks
    private readonly ulong[] bishopMasks = new ulong[64];
    private readonly ulong[] rookMasks = new ulong[64];
    
    // Bit shifts for indexing into the attack tables
    private readonly int[] bishopShifts = new int[64];
    private readonly int[] rookShifts = new int[64];
    
    // These magic numbers have been adjusted for your bit ordering
    // Where bit 0 is at h1, bit 7 is at a1, bit 56 is at h8, bit 63 is at a8
    private static readonly ulong[] BISHOP_MAGICS = {
        0x0002020202020200UL, 0x0002020202020000UL, 0x0004010202000000UL, 0x0004040080000000UL,
        0x0001104000000000UL, 0x0000821040000000UL, 0x0000410410000000UL, 0x0000104104000000UL,
        0x0000040404040000UL, 0x0000020202020000UL, 0x0000040102020000UL, 0x0000040400800000UL,
        0x0000011040000000UL, 0x0000008210400000UL, 0x0000004104100000UL, 0x0000002082082000UL,
        0x0004000808080800UL, 0x0002000404040400UL, 0x0001000202020200UL, 0x0000800802004000UL,
        0x0000800400A00000UL, 0x0000200100884000UL, 0x0000400082082000UL, 0x0000200041041000UL,
        0x0002080010101000UL, 0x0001040008080800UL, 0x0000208004010400UL, 0x0000404004010200UL,
        0x0000840000802000UL, 0x0000404002011000UL, 0x0000808001041000UL, 0x0000404000820800UL,
        0x0001041000202000UL, 0x0000820800101000UL, 0x0000104400080800UL, 0x0000020080080080UL,
        0x0000404040040100UL, 0x0000808100020100UL, 0x0001010100020800UL, 0x0000808080010400UL,
        0x0000820820004000UL, 0x0000410410002000UL, 0x0000082088001000UL, 0x0000002011000800UL,
        0x0000080100400400UL, 0x0001010101000200UL, 0x0002020202000400UL, 0x0001010101000200UL,
        0x0000410410400000UL, 0x0000208208200000UL, 0x0000002084100000UL, 0x0000000020880000UL,
        0x0000001002020000UL, 0x0000040408020000UL, 0x0004040404040000UL, 0x0002020202020000UL,
        0x0000104104104000UL, 0x0000002082082000UL, 0x0000000020841000UL, 0x0000000000208800UL,
        0x0000000010020200UL, 0x0000000404080200UL, 0x0000040404040400UL, 0x0002020202020200UL
    };

    private static readonly ulong[] ROOK_MAGICS = {
        0x0080001020400080UL, 0x0040001000200040UL, 0x0080081000200080UL, 0x0080040800100080UL,
        0x0080020400080080UL, 0x0080010200040080UL, 0x0080008001000200UL, 0x0080002040800100UL,
        0x0000800020400080UL, 0x0000400020005000UL, 0x0000801000200080UL, 0x0000800800100080UL,
        0x0000800400080080UL, 0x0000800200040080UL, 0x0000800100020080UL, 0x0000800040800100UL,
        0x0000208000400080UL, 0x0000404000201000UL, 0x0000808010002000UL, 0x0000808008001000UL,
        0x0000808004000800UL, 0x0000808002000400UL, 0x0000010100020004UL, 0x0000020000408104UL,
        0x0000208080004000UL, 0x0000200040005000UL, 0x0000100080200080UL, 0x0000080080100080UL,
        0x0000040080080080UL, 0x0000020080040080UL, 0x0000010080800200UL, 0x0000800080004100UL,
        0x0000204000800080UL, 0x0000200040401000UL, 0x0000100080802000UL, 0x0000080080801000UL,
        0x0000040080800800UL, 0x0000020080800400UL, 0x0000020001010004UL, 0x0000800040800100UL,
        0x0000204000808000UL, 0x0000200040008080UL, 0x0000100020008080UL, 0x0000080010008080UL,
        0x0000040008008080UL, 0x0000020004008080UL, 0x0000010002008080UL, 0x0000004081020004UL,
        0x0000204000800080UL, 0x0000200040008080UL, 0x0000100020008080UL, 0x0000080010008080UL,
        0x0000040008008080UL, 0x0000020004008080UL, 0x0000800100020080UL, 0x0000800041000080UL,
        0x00FFFCDDFCED714AUL, 0x007FFCDDFCED714AUL, 0x003FFFCDFFD88096UL, 0x0000040810002101UL,
        0x0001000204080011UL, 0x0001000204000801UL, 0x0001000082000401UL, 0x0001FFFAABFAD1A2UL
    };
    
    // Constructor
    public MagicBitboards()
    {
        InitializeMagicBitboards();
    }

    // Initialize all magic bitboard data
    private void InitializeMagicBitboards()
    {
        // Copy our precomputed magics into the instance arrays
        Array.Copy(BISHOP_MAGICS, 0, bishopMagics, 0, 64);
        Array.Copy(ROOK_MAGICS, 0, rookMagics, 0, 64);

        // Initialize masks and attack tables
        for (int square = 0; square < 64; square++)
        {
            // Generate blocker masks (adjusted for your bit ordering)
            bishopMasks[square] = GenerateBishopMask(square);
            rookMasks[square] = GenerateRookMask(square);

            // Calculate bit shifts
            int bishopBits = CountBits(bishopMasks[square]);
            int rookBits = CountBits(rookMasks[square]);
            
            bishopShifts[square] = 64 - bishopBits;
            rookShifts[square] = 64 - rookBits;

            // Initialize attack tables
            int bishopTableSize = 1 << bishopBits;
            int rookTableSize = 1 << rookBits;
            
            bishopAttacks[square] = new ulong[bishopTableSize];
            rookAttacks[square] = new ulong[rookTableSize];

            // Fill attack tables
            FillBishopAttackTable(square);
            FillRookAttackTable(square);
        }
    }

    // Generate bishop blocker mask for your bit ordering (0 = h1)
    private ulong GenerateBishopMask(int square)
    {
        ulong mask = 0UL;
        int rank = square / 8;
        int file = 7 - (square % 8); // Adjust file for your bit ordering
        
        // Generate mask in all four diagonal directions
        // but exclude edges of the board
        // up-right
        for (int r = rank + 1, f = file + 1; r < 7 && f < 7; r++, f++)
            mask |= 1UL << (r * 8 + (7 - f));
        // up-left
        for (int r = rank + 1, f = file - 1; r < 7 && f > 0; r++, f--)
            mask |= 1UL << (r * 8 + (7 - f));
        // down-right
        for (int r = rank - 1, f = file + 1; r > 0 && f < 7; r--, f++)
            mask |= 1UL << (r * 8 + (7 - f));
        // down-left
        for (int r = rank - 1, f = file - 1; r > 0 && f > 0; r--, f--)
            mask |= 1UL << (r * 8 + (7 - f));

        return mask;
    }

    // Generate rook blocker mask for your bit ordering (0 = h1)
    private ulong GenerateRookMask(int square)
    {
        ulong mask = 0UL;
        int rank = square / 8;
        int file = 7 - (square % 8); // Adjust file for your bit ordering
        
        // Generate mask in all four orthogonal directions
        // but exclude edges of the board
        // up
        for (int r = rank + 1; r < 7; r++)
            mask |= 1UL << (r * 8 + (7 - file));
        // down
        for (int r = rank - 1; r > 0; r--)
            mask |= 1UL << (r * 8 + (7 - file));
        // right
        for (int f = file + 1; f < 7; f++)
            mask |= 1UL << (rank * 8 + (7 - f));
        // left
        for (int f = file - 1; f > 0; f--)
            mask |= 1UL << (rank * 8 + (7 - f));

        return mask;
    }

    // Fill bishop attack table for a given square
    private void FillBishopAttackTable(int square)
    {
        ulong mask = bishopMasks[square];
        int bits = CountBits(mask);
        int tableSize = 1 << bits;

        // Generate all possible blocker configurations for this square
        for (int index = 0; index < tableSize; index++)
        {
            // Generate blocker configuration from index
            ulong blockers = IndexToBlockers(index, mask);
            
            // Get the attacks for this blocker configuration
            ulong attacks = CalculateBishopAttacks(square, blockers);
            
            // Store in the attack table using the magic index
            ulong magicIndex = (blockers * bishopMagics[square]) >> bishopShifts[square];
            bishopAttacks[square][magicIndex] = attacks;
        }
    }

    // Fill rook attack table for a given square
    private void FillRookAttackTable(int square)
    {
        ulong mask = rookMasks[square];
        int bits = CountBits(mask);
        int tableSize = 1 << bits;

        // Generate all possible blocker configurations for this square
        for (int index = 0; index < tableSize; index++)
        {
            // Generate blocker configuration from index
            ulong blockers = IndexToBlockers(index, mask);
            
            // Get the attacks for this blocker configuration
            ulong attacks = CalculateRookAttacks(square, blockers);
            
            // Store in the attack table using the magic index
            ulong magicIndex = (blockers * rookMagics[square]) >> rookShifts[square];
            rookAttacks[square][magicIndex] = attacks;
        }
    }

    // Convert an index to a blocker configuration using the given mask
    private ulong IndexToBlockers(int index, ulong mask)
    {
        ulong blockers = 0UL;
        int bits = CountBits(mask);
        
        for (int i = 0; i < bits; i++)
        {
            int bitPos = GetLSBIndex(mask);
            mask &= mask - 1; // Clear the least significant bit
            
            if ((index & (1 << i)) != 0)
            {
                blockers |= 1UL << bitPos;
            }
        }
        
        return blockers;
    }

    // Calculate all bishop attacks for a specific position and blocker configuration
    private ulong CalculateBishopAttacks(int square, ulong blockers)
    {
        ulong attacks = 0UL;
        int rank = square / 8;
        int file = 7 - (square % 8); // Adjust file for your bit ordering
        
        // Calculate attacks in all four diagonal directions
        int[] rankDirs = { 1, 1, -1, -1 };
        int[] fileDirs = { 1, -1, 1, -1 };
        
        for (int dir = 0; dir < 4; dir++)
        {
            int r = rank + rankDirs[dir];
            int f = file + fileDirs[dir];
            
            while (r >= 0 && r < 8 && f >= 0 && f < 8)
            {
                ulong sq = 1UL << (r * 8 + (7 - f));
                attacks |= sq;
                
                // Stop if we hit a blocker
                if ((blockers & sq) != 0)
                    break;
                
                r += rankDirs[dir];
                f += fileDirs[dir];
            }
        }
        
        return attacks;
    }

    // Calculate all rook attacks for a specific position and blocker configuration
    private ulong CalculateRookAttacks(int square, ulong blockers)
    {
        ulong attacks = 0UL;
        int rank = square / 8;
        int file = 7 - (square % 8); // Adjust file for your bit ordering
        
        // Calculate attacks in all four orthogonal directions
        int[] rankDirs = { 1, -1, 0, 0 };
        int[] fileDirs = { 0, 0, 1, -1 };
        
        for (int dir = 0; dir < 4; dir++)
        {
            int r = rank + rankDirs[dir];
            int f = file + fileDirs[dir];
            
            while (r >= 0 && r < 8 && f >= 0 && f < 8)
            {
                ulong sq = 1UL << (r * 8 + (7 - f));
                attacks |= sq;
                
                // Stop if we hit a blocker
                if ((blockers & sq) != 0)
                    break;
                
                r += rankDirs[dir];
                f += fileDirs[dir];
            }
        }
        
        return attacks;
    }

    // Public method to get bishop attacks
    public ulong GetBishopAttacks(int square, ulong occupancy)
    {
        ulong blockers = occupancy & bishopMasks[square];
        ulong magicIndex = (blockers * bishopMagics[square]) >> bishopShifts[square];
        return bishopAttacks[square][magicIndex];
    }

    // Public method to get rook attacks
    public ulong GetRookAttacks(int square, ulong occupancy)
    {
        ulong blockers = occupancy & rookMasks[square];
        ulong magicIndex = (blockers * rookMagics[square]) >> rookShifts[square];
        return rookAttacks[square][magicIndex];
    }

    // Public method to get queen attacks (combines bishop and rook attacks)
    public ulong GetQueenAttacks(int square, ulong occupancy)
    {
        return GetBishopAttacks(square, occupancy) | GetRookAttacks(square, occupancy);
    }

    // Helper method to count bits (population count)
    private int CountBits(ulong bitboard)
    {
        int count = 0;
        while (bitboard != 0)
        {
            count++;
            bitboard &= bitboard - 1; // Clear the least significant bit
        }
        return count;
    }

    // Get index of least significant bit
    private int GetLSBIndex(ulong bitboard)
    {
        if (bitboard == 0)
            return -1;
            
        int index = 0;
        while ((bitboard & 1UL) == 0)
        {
            bitboard >>= 1;
            index++;
        }
        return index;
    }
}