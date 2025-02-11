using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    public const int BoardSize = 8;
    public ChessPiece[,] board = new ChessPiece[BoardSize, BoardSize];

    void Start()
    {
        InitializeBoard();
    }

    void InitializeBoard()
    {
        // Initialize all squares as empty
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                board[x, y] = new ChessPiece(PieceType.N,
             PieceColor.None);
            }
        }

        // Place white pieces (bottom of the board)
        board[0, 0] = new ChessPiece(PieceType.R,
PieceColor.White);
        board[0, 1] = new ChessPiece(PieceType.N,
PieceColor.White);
        board[0, 2] = new ChessPiece(PieceType.B,
PieceColor.White);
        board[0, 3] = new ChessPiece(PieceType.Q,
PieceColor.White);
        board[0, 4] = new ChessPiece(PieceType.K,
PieceColor.White);
        board[0, 5] = new ChessPiece(PieceType.B,
PieceColor.White);
        board[0, 6] = new ChessPiece(PieceType.N,
PieceColor.White);
        board[0, 7] = new ChessPiece(PieceType.R,
PieceColor.White);

        for (int y = 0; y < BoardSize; y++)
        {
            board[1, y] = new ChessPiece(PieceType.P,
PieceColor.White);
        }

        // Place black pieces (top of the board)
        board[7, 0] = new ChessPiece(PieceType.R,
PieceColor.Black);
        board[7, 1] = new ChessPiece(PieceType.N,
    PieceColor.Black);
        board[7, 2] = new ChessPiece(PieceType.B,
    PieceColor.Black);
        board[7, 3] = new ChessPiece(PieceType.Q,
    PieceColor.Black);
        board[7, 4] = new ChessPiece(PieceType.K,
    PieceColor.Black);
        board[7, 5] = new ChessPiece(PieceType.B,
    PieceColor.Black);
        board[7, 6] = new ChessPiece(PieceType.N,
    PieceColor.Black);
        board[7, 7] = new ChessPiece(PieceType.R,
    PieceColor.Black);

        for (int y = 0; y < BoardSize; y++)
        {
            board[6, y] = new ChessPiece(PieceType.P,
    PieceColor.Black);
        }
    }
}
