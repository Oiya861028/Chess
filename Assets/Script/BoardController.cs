using UnityEngine;

public class BoardController : MonoBehaviour
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

    //Parent location to instantiate pieces
    public Transform PieceParent;

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
            Instantiate(whitePawnPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
            if ((WhiteRook & (1UL << i)) != 0)
            Instantiate(whiteRookPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
            if ((WhiteKnight & (1UL << i)) != 0)
            Instantiate(whiteKnightPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
            if ((WhiteBishop & (1UL << i)) != 0)
            Instantiate(whiteBishopPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
            if ((WhiteQueen & (1UL << i)) != 0)
            Instantiate(whiteQueenPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
            if ((WhiteKing & (1UL << i)) != 0)
            Instantiate(whiteKingPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
            if ((BlackPawn & (1UL << i)) != 0)
            Instantiate(blackPawnPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
            if ((BlackRook & (1UL << i)) != 0)
            Instantiate(blackRookPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
            if ((BlackKnight & (1UL << i)) != 0)
            Instantiate(blackKnightPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
            if ((BlackBishop & (1UL << i)) != 0)
            Instantiate(blackBishopPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
            if ((BlackQueen & (1UL << i)) != 0)
            Instantiate(blackQueenPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
            if ((BlackKing & (1UL << i)) != 0)
            Instantiate(blackKingPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
        }
    }

    private GameObject previouslySelectedTile;
    private Color previousTileColor;
    void HandleClick()
    {
        if(previouslySelectedTile != null)
        {
            previouslySelectedTile.GetComponent<Renderer>().material.color = previousTileColor;
            previouslySelectedTile = null;
        }
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if(hit.collider.tag == "ChessTile")
            {
                GameObject selectedTile = hit.collider.gameObject;
                string index = hit.collider.gameObject.name;

                //Store the selected tile for reversal
                previouslySelectedTile = selectedTile;
                previousTileColor = selectedTile.GetComponent<Renderer>().material.color;

                selectedTile.GetComponent<Renderer>().material.color = Color.red; //Highlight the selected tile
            }
            else if(hit.collider.tag == "ChessPiece") //In case a piece is clicked
            {
                // Cast a ray downward from the piece position to find the tile
                GameObject selectedPiece = hit.collider.gameObject;
                Ray downRay = new Ray(selectedPiece.transform.position + Vector3.up, Vector3.down);
                RaycastHit tileHit;
                if (Physics.Raycast(downRay, out tileHit, 10f))
                {
                    if (tileHit.collider.tag == "ChessTile") 
                    {
                        GameObject correspondingTile = tileHit.collider.gameObject;

                        previouslySelectedTile = correspondingTile;
                        previousTileColor = correspondingTile.GetComponent<Renderer>().material.color;
                        string tileIndex = correspondingTile.name;
                        correspondingTile.GetComponent<Renderer>().material.color = Color.red;
                        Debug.Log("Piece is on tile: " + tileIndex);
                    }
                }
            }
            else
            {
                previouslySelectedTile = null;
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
