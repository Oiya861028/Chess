public class Move
{
    public int Source { get; private set; }
    public int Destination { get; private set; }
    public Move previousMove { get; private set; }
    public int PieceType { get; private set; }
    public bool IsWhite { get; private set; }
    public bool IsEnPassant { get; private set; }
    public bool IsPawnDoubleMove { get; private set; }
    public bool IsPromotion { get; private set; }          // New field for promotion
    public int PromotionPieceType { get; private set; }    // New field for promotion piece type
    public int Score { get; set; }
    // Constructor without promotion parameters
    public Move(int source, int destination, Move previousMove, int pieceType, bool isWhite, bool isEnPassant = false, bool isPawnDoubleMove = false)
    {
        Source = source;
        Destination = destination;
        this.previousMove = previousMove;
        PieceType = pieceType;
        IsWhite = isWhite;
        IsEnPassant = isEnPassant;
        IsPawnDoubleMove = isPawnDoubleMove;
        IsPromotion = false;                      // Default to false
        PromotionPieceType = 4; // Default to Queen
    }

    // Constructor with promotion parameters
    public Move(int source, int destination, Move previousMove, int pieceType, bool isWhite, 
               bool isEnPassant, bool isPawnDoubleMove, bool isPromotion, int promotionPieceType)
    {
        Source = source;
        Destination = destination;
        this.previousMove = previousMove;
        PieceType = pieceType;
        IsWhite = isWhite;
        IsEnPassant = isEnPassant;
        IsPawnDoubleMove = isPawnDoubleMove;
        IsPromotion = isPromotion;
        PromotionPieceType = promotionPieceType;
    }
}