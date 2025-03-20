using System;
using UnityEngine;

public class MagicBitboards
{
    // Pre-computed magic numbers for rooks and bishops
    private readonly ulong[] rookMagicNumbers = new ulong[64] {
        0xa180022080400230UL, 0x40100040022000UL, 0x80088020001002UL, 0x80080280841000UL,
        0x4200042010460008UL, 0x4800a0003040080UL, 0x400110082041008UL, 0x8000a041000880UL,
        0x10138001a080c010UL, 0x804008200480UL, 0x10011012000c0UL, 0x22004128102200UL,
        0x200081201200cUL, 0x202a001048460004UL, 0x81000100420004UL, 0x4000800380004500UL,
        0x208002904001UL, 0x90004040026008UL, 0x208808010002001UL, 0x2002020020704940UL,
        0x8048010008110005UL, 0x6820808004002200UL, 0xa80040008023011UL, 0xb1460000811044UL,
        0x4204400080008ea0UL, 0xb002400180200184UL, 0x2020200080100380UL, 0x10080080100080UL,
        0x2204080080800400UL, 0xa40080360080UL, 0x2040604002810b1UL, 0x8c218600004104UL,
        0x8180004000402000UL, 0x488c402000401001UL, 0x4018a00080801004UL, 0x1230002105001008UL,
        0x8904800800800400UL, 0x42000c42003810UL, 0x8408110400b012UL, 0x18086182000401UL,
        0x2240088020c28000UL, 0x1001201040c004UL, 0xa02008010420020UL, 0x10003009010060UL,
        0x4008008008014UL, 0x80020004008080UL, 0x282020001008080UL, 0x50000181204a0004UL,
        0x102042111804200UL, 0x40002010004001c0UL, 0x19220045508200UL, 0x20030010060a900UL,
        0x8018028040080UL, 0x88240002008080UL, 0x10301802830400UL, 0x332a4081140200UL,
        0x8080010a601241UL, 0x1008010400021UL, 0x4082001007241UL, 0x211009001200509UL,
        0x8015001002100UL, 0x801000804000603UL, 0xc0900220024a401UL, 0x1000200608243UL
    };

    private readonly ulong[] bishopMagicNumbers = new ulong[64] {
        0x2910054208004104UL, 0x2100630a7020180UL, 0x5822022042000000UL, 0x2ca804a100200UL,
        0x204042200000900UL, 0x2002121024000002UL, 0x80404104202000e8UL, 0x812a020205010840UL,
        0x8005181184080048UL, 0x1001c20208010101UL, 0x1001080204002100UL, 0x1810080489021800UL,
        0x62040420010a00UL, 0x5028043004300020UL, 0xc0080a4402605002UL, 0x8a00a0204000UL,
        0x940000410821212UL, 0x1808024a280210UL, 0x40c0422080a0598UL, 0x4228020082004050UL,
        0x200800400e00100UL, 0x20b001230021040UL, 0x90a0201900c00UL, 0x4940120a0a0108UL,
        0x20208050a42180UL, 0x1004804b280200UL, 0x2048020024040010UL, 0x102c04004010200UL,
        0x20408204c002010UL, 0x2411100020080c1UL, 0x102a008084042100UL, 0x941030000a09846UL,
        0x244100800400200UL, 0x4000901010080696UL, 0x280404180020UL, 0x800042008240100UL,
        0x220008400088020UL, 0x4020182000904c9UL, 0x23010400020600UL, 0x41040020110302UL,
        0x412101004020818UL, 0x8022080a09404208UL, 0x1401210240484800UL, 0x22244208010080UL,
        0x1105040104000210UL, 0x2040088800c40081UL, 0x8184810252000400UL, 0x4004610041002200UL,
        0x40201a444400810UL, 0x4611010802020008UL, 0x80000b0401040402UL, 0x20004821880a00UL,
        0x8200002022440200UL, 0x9431801010068UL, 0x1040c20806108040UL, 0x804901403022a40UL,
        0x2400202602104000UL, 0x208520209440204UL, 0x40c000022013020UL, 0x2000104000420600UL,
        0x400000260142410UL, 0x800633408100500UL, 0x2404080a1410UL, 0x138200122002900UL
    };

    // Bit counts for indexing into the attack tables
    private readonly int[] rookBits = new int[64] {
        12, 11, 11, 11, 11, 11, 11, 12,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        12, 11, 11, 11, 11, 11, 11, 12
    };

    private readonly int[] bishopBits = new int[64] {
        6, 5, 5, 5, 5, 5, 5, 6,
        5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5,
        6, 5, 5, 5, 5, 5, 5, 6
    };

    // Occupancy masks for rooks and bishops
    private readonly ulong[] rookMasks = new ulong[64];
    private readonly ulong[] bishopMasks = new ulong[64];

    // Attack tables
    private readonly ulong[][] rookAttacks;
    private readonly ulong[][] bishopAttacks;

    // Constructor - initialize all the lookup tables
    public MagicBitboards()
    {
        Debug.Log("Initializing Magic Bitboards...");
        
        // Initialize attack tables
        rookAttacks = new ulong[64][];
        bishopAttacks = new ulong[64][];

        for (int square = 0; square < 64; square++)
        {
            // Calculate the masks
            rookMasks[square] = GenerateRookMask(square);
            bishopMasks[square] = GenerateBishopMask(square);

            // Initialize attack tables
            int rookEntries = 1 << rookBits[square];
            int bishopEntries = 1 << bishopBits[square];

            rookAttacks[square] = new ulong[rookEntries];
            bishopAttacks[square] = new ulong[bishopEntries];

            // Fill attack tables using Carry-Rippler trick to enumerate all subsets of the mask
            ulong rookOccupancy = 0;
            do
            {
                int magicIndex = GetRookMagicIndex(square, rookOccupancy);
                rookAttacks[square][magicIndex] = GenerateRookAttacks(square, rookOccupancy);

                rookOccupancy = (rookOccupancy - rookMasks[square]) & rookMasks[square]; // Next subset
            }
            while (rookOccupancy != 0);

            ulong bishopOccupancy = 0;
            do
            {
                int magicIndex = GetBishopMagicIndex(square, bishopOccupancy);
                bishopAttacks[square][magicIndex] = GenerateBishopAttacks(square, bishopOccupancy);

                bishopOccupancy = (bishopOccupancy - bishopMasks[square]) & bishopMasks[square]; // Next subset
            }
            while (bishopOccupancy != 0);
        }
        
        Debug.Log("Magic Bitboards initialization complete!");
    }

    // Get rook attacks using magic lookup
    public ulong GetRookAttacks(int square, ulong occupancy)
    {
        occupancy &= rookMasks[square];
        int magicIndex = GetRookMagicIndex(square, occupancy);
        return rookAttacks[square][magicIndex];
    }

    // Get bishop attacks using magic lookup
    public ulong GetBishopAttacks(int square, ulong occupancy)
    {
        occupancy &= bishopMasks[square];
        int magicIndex = GetBishopMagicIndex(square, occupancy);
        return bishopAttacks[square][magicIndex];
    }

    // Get queen attacks (combination of rook and bishop)
    public ulong GetQueenAttacks(int square, ulong occupancy)
    {
        return GetRookAttacks(square, occupancy) | GetBishopAttacks(square, occupancy);
    }

    // Calculate magic index for rook
    private int GetRookMagicIndex(int square, ulong occupancy)
    {
        return (int)((occupancy * rookMagicNumbers[square]) >> (64 - rookBits[square]));
    }

    // Calculate magic index for bishop
    private int GetBishopMagicIndex(int square, ulong occupancy)
    {
        return (int)((occupancy * bishopMagicNumbers[square]) >> (64 - bishopBits[square]));
    }

    // Generate rook occupancy mask (excludes edges if the rook is not on the edge)
    private ulong GenerateRookMask(int square)
    {
        ulong mask = 0UL;
        int rank = square / 8;
        int file = square % 8;

        // Ranks (horizontal)
        for (int f = file + 1; f < 7; f++)
            mask |= 1UL << (rank * 8 + f);
        for (int f = file - 1; f > 0; f--)
            mask |= 1UL << (rank * 8 + f);

        // Files (vertical)
        for (int r = rank + 1; r < 7; r++)
            mask |= 1UL << (r * 8 + file);
        for (int r = rank - 1; r > 0; r--)
            mask |= 1UL << (r * 8 + file);

        return mask;
    }

    // Generate bishop occupancy mask (excludes edges)
    private ulong GenerateBishopMask(int square)
    {
        ulong mask = 0UL;
        int rank = square / 8;
        int file = square % 8;

        // North-East
        for (int r = rank + 1, f = file + 1; r < 7 && f < 7; r++, f++)
            mask |= 1UL << (r * 8 + f);

        // South-East
        for (int r = rank - 1, f = file + 1; r > 0 && f < 7; r--, f++)
            mask |= 1UL << (r * 8 + f);

        // South-West
        for (int r = rank - 1, f = file - 1; r > 0 && f > 0; r--, f--)
            mask |= 1UL << (r * 8 + f);

        // North-West
        for (int r = rank + 1, f = file - 1; r < 7 && f > 0; r++, f--)
            mask |= 1UL << (r * 8 + f);

        return mask;
    }

    // Generate rook attacks considering blockers
    private ulong GenerateRookAttacks(int square, ulong blockers)
    {
        ulong attacks = 0UL;
        int rank = square / 8;
        int file = square % 8;

        // North
        for (int r = rank + 1; r < 8; r++)
        {
            ulong bit = 1UL << (r * 8 + file);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // East
        for (int f = file + 1; f < 8; f++)
        {
            ulong bit = 1UL << (rank * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // South
        for (int r = rank - 1; r >= 0; r--)
        {
            ulong bit = 1UL << (r * 8 + file);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // West
        for (int f = file - 1; f >= 0; f--)
        {
            ulong bit = 1UL << (rank * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        return attacks;
    }

    // Generate bishop attacks considering blockers
    private ulong GenerateBishopAttacks(int square, ulong blockers)
    {
        ulong attacks = 0UL;
        int rank = square / 8;
        int file = square % 8;

        // North-East
        for (int r = rank + 1, f = file + 1; r < 8 && f < 8; r++, f++)
        {
            ulong bit = 1UL << (r * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // South-East
        for (int r = rank - 1, f = file + 1; r >= 0 && f < 8; r--, f++)
        {
            ulong bit = 1UL << (r * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // South-West
        for (int r = rank - 1, f = file - 1; r >= 0 && f >= 0; r--, f--)
        {
            ulong bit = 1UL << (r * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // North-West
        for (int r = rank + 1, f = file - 1; r < 8 && f >= 0; r++, f--)
        {
            ulong bit = 1UL << (r * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        return attacks;
    }
}