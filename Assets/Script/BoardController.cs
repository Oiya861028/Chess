using System;
using Unity.VisualScripting;
using UnityEngine;
public class BoardController : MonoBehaviour
{
    //Bitboard class that contains the bitboards for the pieces
    Bitboard bitboard;

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

    //Utility Functions
    private FindMoves findMoves;
    void Start()
    {
        bitboard = new Bitboard();
        findMoves = new FindMoves(bitboard);
        
        //Instantiate All Pieces on Board   
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
        //Get the bitboards for the pieces
        ulong WhitePawn = bitboard.WhitePawn;
        ulong WhiteRook = bitboard.WhiteRook;
        ulong WhiteKnight = bitboard.WhiteKnight;
        ulong WhiteBishop = bitboard.WhiteBishop;
        ulong WhiteQueen = bitboard.WhiteQueen;
        ulong WhiteKing = bitboard.WhiteKing;
        ulong BlackPawn = bitboard.BlackPawn;
        ulong BlackRook = bitboard.BlackRook;
        ulong BlackKnight = bitboard.BlackKnight;
        ulong BlackBishop = bitboard.BlackBishop;
        ulong BlackQueen = bitboard.BlackQueen;
        ulong BlackKing = bitboard.BlackKing;
        //Instantiate the pieces on the board
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


    //Information about the Tile clicked
    private GameObject SelectedPiece;
    private GameObject SelectedTile;
    private Color OriginalTileColor;
    private int TileIndex;
    private 

    void HandleClick()
    {
        if(SelectedTile != null) //At the start of each click, if there is a selected tile from last click, revert it back to its original color
        {
            SelectedTile.GetComponent<Renderer>().material.color = OriginalTileColor;
            SelectedTile = null;
        }


        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            //TODO: Only highlight if it's the piece's turn
            if(hit.collider.CompareTag("ChessTile"))
            {

                ExtractCellInfo(hit, true);
                if((possibleMoves & (1UL << TileIndex)) != 0) //If the tile clicked is a possible move
                {
                    Debug.Log("Move Made. Index: " + TileIndex);
                    UpdateBoard();
                    EraseHighlights();
                }
                else{
                    SelectedTile.GetComponent<Renderer>().material.color = Color.red; //Highlight the selected tile
                    DisplayPossibleMoves();
                }
            }       
            else if(hit.collider.CompareTag("ChessPiece")) //In case a piece is clicked
            {
                ExtractCellInfo(hit, false);
                if((possibleMoves & (1UL << TileIndex)) != 0) //If the tile clicked is a possible move
                {
                    Debug.Log("Move Made. Index: " + TileIndex);
                    EraseHighlights();
                }
                else{
                    SelectedTile.GetComponent<Renderer>().material.color = Color.red; //Highlight the selected tile
                    DisplayPossibleMoves();
                }
            }
            else
            {
                Debug.Log("No Chess Tile Clicked");
                
            }
        }
    }

    private void UpdateBoard()
    {
        //TODO: Update the bitboard
        UpdateBitboard();

        //TODO: Update the physical piece on board
        UpdatePiece();
    }

    private void UpdatePiece()
    {
        SelectedPiece.transform.position = GetWorldPositionForBit(TileIndex);
        
    }

    private void UpdateBitboard()
    {
        throw new NotImplementedException();
    }

    private void ExtractCellInfo(RaycastHit hit, bool isTileHit)
    {
        //In order to find both piece and tile, we need to check if the hit is on a tile or a piece
        if(isTileHit){
            SelectedTile = hit.collider.gameObject;
            Ray upRay = new(SelectedTile.transform.position + Vector3.up, Vector3.down);
            if (Physics.Raycast(upRay, out RaycastHit pieceHit, 10f))
            {
                if (pieceHit.collider.tag == "ChessPiece")
                {
                    SelectedPiece = pieceHit.collider.gameObject;
                }
            }
        }
        else{
            SelectedPiece = hit.collider.gameObject;
            Ray downRay = new(SelectedPiece.transform.position + Vector3.up, Vector3.down);
            if (Physics.Raycast(downRay, out RaycastHit tileHit, 10f))
            {
                if (tileHit.collider.tag == "ChessTile")
                {
                    SelectedTile = tileHit.collider.gameObject;
                }
            }
        }
        OriginalTileColor = SelectedTile.GetComponent<Renderer>().material.color;
        TileIndex = int.Parse(SelectedTile.name);
    }
    ulong possibleMoves;
    private void DisplayPossibleMoves()
    {
        EraseHighlights();
        Debug.Log("Tile Index: " + TileIndex);
        possibleMoves = findMoves.GetPossibleMoves(TileIndex);
        Debug.Log(possibleMoves);
        for (int i = 0; i < 64; i++)
        {
            if ((possibleMoves & (1UL << i)) != 0)
            {
                GameObject tile = GameObject.Find(i.ToString());
                tile.GetComponent<Renderer>().material.color = Color.green;
            }
        }
    }
    private void EraseHighlights()
    {
        for (int i = 0; i < 64; i++)
        {
            GameObject tile = GameObject.Find(i.ToString());
            if ((i + i / 8) % 2 == 0)
            {
                tile.GetComponent<Renderer>().material.color = Color.white; // White tile
            }
            else
            {
                tile.GetComponent<Renderer>().material.color = Color.black; // Black tile
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
