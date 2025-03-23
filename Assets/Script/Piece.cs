using UnityEngine;

public struct Piece {
    public int pieceType;
    public bool isWhite;
    public GameObject gameObject;
    public Piece(int pieceType, bool isWhite, GameObject gameObject) {
        this.pieceType = pieceType;
        this.isWhite = isWhite;
        this.gameObject = gameObject;
    }
}