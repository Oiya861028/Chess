public struct Move
{
    // The index into the bitboards array for the moving piece.
    public int Piece;
    
    // Bit mask representing the starting square (e.g., 1UL << i).
    public ulong FromSquare;
    
    // Bit mask representing the destination square.
    public ulong ToSquare;
    
    // The index for the captured piece on the bitboards array (-1 if no capture).
    public int CapturedPiece;

    public Move(int piece, ulong fromSquare, ulong toSquare, int capturedPiece = -1)
    {
        Piece = piece;
        FromSquare = fromSquare;
        ToSquare = toSquare;
        CapturedPiece = capturedPiece;
    }
}