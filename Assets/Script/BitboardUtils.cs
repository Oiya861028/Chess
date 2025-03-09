// BitboardUtils.cs
using System;
using UnityEngine;

public static class BitboardUtils
{
    // Convert a bitboard index to algebraic notation
    public static string IndexToAlgebraic(int index)
    {
        int file = index % 8;
        int rank = index / 8;
        char fileChar = (char)('a' + file);
        int rankNum = rank + 1;
        return fileChar.ToString() + rankNum.ToString();
    }
    
    // Convert algebraic notation to index
    public static int AlgebraicToIndex(string algebraic)
    {
        if (algebraic.Length != 2) return -1;
        
        char fileChar = algebraic[0];
        char rankChar = algebraic[1];
        
        int file = fileChar - 'a';
        int rank = rankChar - '1';
        
        if (file < 0 || file > 7 || rank < 0 || rank > 7) return -1;
        
        return rank * 8 + file;
    }
    
    // Get set bit positions from a bitboard
    public static int[] GetSetBitPositions(ulong bitboard)
    {
        int count = CountBits(bitboard);
        int[] positions = new int[count];
        int index = 0;
        
        for (int i = 0; i < 64; i++)
        {
            if ((bitboard & (1UL << i)) != 0)
            {
                positions[index++] = i;
            }
        }
        
        return positions;
    }
    
    // Count number of set bits in a bitboard
    public static int CountBits(ulong n)
    {
        int count = 0;
        while (n != 0)
        {
            count++;
            n &= n - 1; // Clear the least significant bit set
        }
        return count;
    }
    
    // Print a bitboard 
    public static void PrintBitboard(ulong bitboard, string label = "Bitboard")
    {
        Debug.Log($"--- {label} ---");
        for (int rank = 7; rank >= 0; rank--)
        {
            string rankStr = (rank + 1) + " ";
            for (int file = 0; file < 8; file++)
            {
                int index = rank * 8 + file;
                rankStr += ((bitboard & (1UL << index)) != 0) ? "1 " : ". ";
            }
            Debug.Log(rankStr);
        }
        Debug.Log("  a b c d e f g h");
    }
    
    // Find first set bit (LSB)
    public static int GetLSB(ulong bitboard)
    {
        if (bitboard == 0) return -1;
        
        int index = 0;
        while ((bitboard & 1) == 0)
        {
            bitboard >>= 1;
            index++;
        }
        
        return index;
    }
}