using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    // Bitboard for white pieces
    private ulong WhitePawn =   0b0000000000000000000000000000000000000000000000001111111100000000;
    private ulong WhiteRook =   0b0000000000000000000000000000000000000000000000000000000010000001;
    private ulong WhiteKnight = 0b0000000000000000000000000000000000000000000000000000000001000010;
    private ulong WhiteBishop = 0b0000000000000000000000000000000000000000000000000000000000100100;
    private ulong WhiteQueen =  0b0000000000000000000000000000000000000000000000000000000000010000;
    private ulong WhiteKing =   0b0000000000000000000000000000000000000000000000000000000000001000;
    // Bitboard for black pieces
    private ulong BlackPawn =   0b0000000011111111000000000000000000000000000000000000000000000000;
    private ulong BlackRook =   0b1000000100000000000000000000000000000000000000000000000000000000;
    private ulong BlackKnight = 0b0100001000000000000000000000000000000000000000000000000000000000;
    private ulong BlackBishop = 0b0010010000000000000000000000000000000000000000000000000000000000;
    private ulong BlackQueen =  0b0001000000000000000000000000000000000000000000000000000000000000;
    private ulong BlackKing =   0b0000100000000000000000000000000000000000000000000000000000000000;

    //Board Dimensions
    private int squareSize = 1;
    private Vector3 boardOrigin = new Vector3(0f, 0f, 0f);

    //Prefab for Board
    public GameObject boardPrefab;

    //Prefabs for the pieces 
    public GameObject whitePawnPrefab;
    public GameObject whiteRookPrefab;
    public GameObject whiteKnightPrefab;
    public GameObject whiteBishopPrefab;
    public GameObject whiteQueenPrefab;
    public GameObject whiteKingPrefab;
    public GameObject blackPawnPrefab;
    public GameObject blackRookPrefab;
    public GameObject blackKnightPrefab;
    public GameObject blackBishopPrefab;
    public GameObject blackQueenPrefab;
    public GameObject blackKingPrefab;
    
    //Instantiate All Pieces on Board
    void Start()
    {
        InstantiateBoard();
        InstantiatePieces();
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse Clicked");
            HandleClick();
        }
    }
    void InstantiateBoard()
    {
        Instantiate(boardPrefab, boardOrigin, Quaternion.identity);
    }
    void InstantiatePieces()
    {
        for (int i = 0; i < 64; i++)
        {
            if ((WhitePawn & (1UL << i)) != 0)
            Instantiate(whitePawnPrefab, GetWorldPositionForBit(i), Quaternion.identity);
            if ((WhiteRook & (1UL << i)) != 0)
            Instantiate(whiteRookPrefab, GetWorldPositionForBit(i), Quaternion.identity);
            if ((WhiteKnight & (1UL << i)) != 0)
            Instantiate(whiteKnightPrefab, GetWorldPositionForBit(i), Quaternion.identity);
            if ((WhiteBishop & (1UL << i)) != 0)
            Instantiate(whiteBishopPrefab, GetWorldPositionForBit(i), Quaternion.identity);
            if ((WhiteQueen & (1UL << i)) != 0)
            Instantiate(whiteQueenPrefab, GetWorldPositionForBit(i), Quaternion.identity);
            if ((WhiteKing & (1UL << i)) != 0)
            Instantiate(whiteKingPrefab, GetWorldPositionForBit(i), Quaternion.identity);
            if ((BlackPawn & (1UL << i)) != 0)
            Instantiate(blackPawnPrefab, GetWorldPositionForBit(i), Quaternion.identity);
            if ((BlackRook & (1UL << i)) != 0)
            Instantiate(blackRookPrefab, GetWorldPositionForBit(i), Quaternion.identity);
            if ((BlackKnight & (1UL << i)) != 0)
            Instantiate(blackKnightPrefab, GetWorldPositionForBit(i), Quaternion.identity);
            if ((BlackBishop & (1UL << i)) != 0)
            Instantiate(blackBishopPrefab, GetWorldPositionForBit(i), Quaternion.identity);
            if ((BlackQueen & (1UL << i)) != 0)
            Instantiate(blackQueenPrefab, GetWorldPositionForBit(i), Quaternion.identity);
            if ((BlackKing & (1UL << i)) != 0)
            Instantiate(blackKingPrefab, GetWorldPositionForBit(i), Quaternion.identity);
        }
    }
    void HandleClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if(hit.collider.tag == "ChessPiece")
            {
                Debug.Log("Chess Piece Clicked");
            }
        }
    }
    Vector3 GetWorldPositionForBit(int bitIndex)
    {
        int file = bitIndex % 8;
        int rank = bitIndex / 8;
        return boardOrigin + new Vector3(file * squareSize, 0f, rank * squareSize);
    }
}
