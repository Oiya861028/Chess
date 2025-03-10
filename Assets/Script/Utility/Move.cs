using System;
using Unity.Mathematics;
using UnityEngine;

public class Move {
    public int Source;
    public int Destination;    
    public Move previousMove;
    public int PieceType;
    public bool IsWhite;
    public bool IsEnPassant;
    public bool IsPawnDoubleMove;
    public Move(int start, int end, Move prevMove, int PieceType, bool isWhite, 
                bool isEnPassant = false, bool isPawnDoubleMove = false)
    {
        Source = start;
        Destination = end;
        previousMove = prevMove;
        this.PieceType = PieceType;
        this.IsWhite = isWhite;
        this.IsEnPassant = isEnPassant;
        this.IsPawnDoubleMove = isPawnDoubleMove;
    }
}