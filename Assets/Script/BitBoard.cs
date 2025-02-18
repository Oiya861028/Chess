public class BitBoard
{
    private ulong[] bitboards = {
        0b0000000000000000000000000000000000000000000000001111111100000000, // WhitePawn
        0b0000000000000000000000000000000000000000000000000000000010000001, // WhiteRook
        0b0000000000000000000000000000000000000000000000000000000001000010, // WhiteKnight
        0b0000000000000000000000000000000000000000000000000000000000100100, // WhiteBishop
        0b0000000000000000000000000000000000000000000000000000000000010000, // WhiteQueen
        0b0000000000000000000000000000000000000000000000000000000000001000, // WhiteKing
        0b0000000011111111000000000000000000000000000000000000000000000000, // BlackPawn
        0b1000000100000000000000000000000000000000000000000000000000000000, // BlackRook
        0b0100001000000000000000000000000000000000000000000000000000000000, // BlackKnight
        0b0010010000000000000000000000000000000000000000000000000000000000, // BlackBishop
        0b0001000000000000000000000000000000000000000000000000000000000000, // BlackQueen
        0b0000100000000000000000000000000000000000000000000000000000000000  // BlackKing
    };
    private Move previousMove;
    private bool whiteTurn = true;
    private int castleRight;
    private int enPassantSquare;

    public void UpdateBitBoard(Move move)
    {
        // Update the board following the move
        // This is a simplified example, you need to handle all piece types and special moves
        bitboards[move.Piece] ^= move.FromSquare;
        bitboards[move.Piece] ^= move.ToSquare;

        // Handle captures
        if (move.CapturedPiece != -1)
        {
            bitboards[move.CapturedPiece] ^= move.ToSquare;
        }

        // Update previous move
        previousMove = move;

        // Toggle turn
        whiteTurn = !whiteTurn;
    }

    public void UndoBitBoard()
    {
        // Reverse the last move in the bitboard, using previousMove and reversal techniques
        Move move = previousMove;

        // Undo the move
        bitboards[move.Piece] ^= move.ToSquare;
        bitboards[move.Piece] ^= move.FromSquare;

        // Handle captures
        if (move.CapturedPiece != -1)
        {
            bitboards[move.CapturedPiece] ^= move.ToSquare;
        }

        // Toggle turn back
        whiteTurn = !whiteTurn;
    }

}
