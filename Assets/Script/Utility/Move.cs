public static class MoveUtil
{
    // Bit masks and shifts
    private const int SOURCE_MASK = 0x3F;
    private const int DEST_SHIFT = 6;
    private const int DEST_MASK = 0x3F << DEST_SHIFT;
    private const int PIECE_SHIFT = 12;
    private const int PIECE_MASK = 0x7 << PIECE_SHIFT;
    private const int WHITE_SHIFT = 15;
    private const int WHITE_MASK = 0x1 << WHITE_SHIFT;
    private const int EN_PASSANT_SHIFT = 16;
    private const int EN_PASSANT_MASK = 0x1 << EN_PASSANT_SHIFT;
    private const int DOUBLE_PAWN_SHIFT = 17;
    private const int DOUBLE_PAWN_MASK = 0x1 << DOUBLE_PAWN_SHIFT;
    private const int PROMOTION_SHIFT = 18;
    private const int PROMOTION_MASK = 0x1 << PROMOTION_SHIFT;
    private const int PROMOTION_TYPE_SHIFT = 19;
    private const int PROMOTION_TYPE_MASK = 0x3 << PROMOTION_TYPE_SHIFT;
    private const int CASTLING_SHIFT = 21;
    private const int CASTLING_MASK = 0x1 << CASTLING_SHIFT;

    // Encode a move
    public static int EncodeMove(int source, int dest, int pieceType, bool isWhite, 
                               bool isEnPassant = false, bool isPawnDoubleMove = false,
                               bool isPromotion = false, int promotionPieceType = 0,
                               bool isCastling = false)
    {
        int move = source;
        move |= (dest << DEST_SHIFT);
        move |= (pieceType << PIECE_SHIFT);
        
        if (isWhite)
            move |= WHITE_MASK;
        if (isEnPassant)
            move |= EN_PASSANT_MASK;
        if (isPawnDoubleMove)
            move |= DOUBLE_PAWN_MASK;
        if (isPromotion)
            move |= PROMOTION_MASK;
        if (isCastling)
            move |= CASTLING_MASK;
            
        if (isPromotion)
            move |= (promotionPieceType << PROMOTION_TYPE_SHIFT);
        
        return move;
    }
    
    // Getters for move information
    public static int GetSource(int move) => move & SOURCE_MASK;
    public static int GetDestination(int move) => (move & DEST_MASK) >> DEST_SHIFT;
    public static int GetPieceType(int move) => (move & PIECE_MASK) >> PIECE_SHIFT;
    public static bool IsWhite(int move) => (move & WHITE_MASK) != 0;
    public static bool IsEnPassant(int move) => (move & EN_PASSANT_MASK) != 0;
    public static bool IsPawnDoubleMove(int move) => (move & DOUBLE_PAWN_MASK) != 0;
    public static bool IsPromotion(int move) => (move & PROMOTION_MASK) != 0;
    public static int GetPromotionPieceType(int move) => 
        (move & PROMOTION_TYPE_MASK) >> PROMOTION_TYPE_SHIFT;
    public static bool IsCastling(int move) => (move & CASTLING_MASK) != 0;
    
    // Convert a move to algebraic notation for debugging
    public static string MoveToString(int move)
    {
        string from = BitboardUtils.IndexToAlgebraic(GetSource(move));
        string to = BitboardUtils.IndexToAlgebraic(GetDestination(move));
        string result = $"{from}-{to}";
        
        if (IsPromotion(move))
        {
            char promotionChar = ' ';
            switch (GetPromotionPieceType(move))
            {
                case 0: promotionChar = 'N'; break; // Knight
                case 1: promotionChar = 'B'; break; // Bishop
                case 2: promotionChar = 'R'; break; // Rook
                case 3: promotionChar = 'Q'; break; // Queen
            }
            result += "=" + promotionChar;
        }
        
        return result;
    }
}