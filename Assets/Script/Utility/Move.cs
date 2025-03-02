using System;
using Unity.Mathematics;
using UnityEngine;

public class Move{
	//This struct will store the move made by the player
    //It will store the start position, end position and the previous move

	//Store this in bitindex
	public int Source;
    public int Destination;    
    public Move previousMove;
    public int PieceType;
    public bool IsWhite;
    
    //Constructor
    public Move(int start, int end, Move prevMove, int PieceType, bool isWhite)
    {
        Source = start;
        Destination = end;
        previousMove = prevMove;
        this.PieceType = PieceType;
        this.IsWhite = isWhite;
    }
}