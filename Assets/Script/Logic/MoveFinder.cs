using System.Collections;
using UnityEditor.PackageManager;

public class MoveFinder {
    private Stack possibleMoves = new Stack(); //stores all possible moves in a stack of bitboards 
    private ulong[] currentBoard = null; //Store the current board that is to be analyzed
    private bool isWhiteTurn = true;

    //Finds all possible moves in the current chess board, or whatever board that gets passed in
    //Parameter
    //ulong[] bitboards : a list containing bit board for all piece type 
    public Stack FindMoves(ulong[] bitboards, bool isWhiteTurn){
        //Setting up for new board
        possibleMoves.Clear();
        currentBoard = bitboards;
        this.isWhiteTurn = isWhiteTurn;
        return null;
    }

}