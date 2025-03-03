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

    // Track current player's turn (true = white, false = black)
    private bool isWhiteTurn = true;

    // Track selected piece
    private GameObject selectedPiece;
    private int selectedPieceIndex = -1;
    private PieceType selectedPieceType;
    private bool selectedPieceIsWhite;

    void Start()
    {
        bitboard = new Bitboard();
        findMoves = new FindMoves(bitboard);
        
        //Instantiate All Pieces on Board   
        InstantiateBoard();
        InstantiatePieces();
        
        // Print the initial board layout for debugging
        Debug.Log("Initial board setup:");
        DebugPrintBoard();
    }
    
    // Print the current board state for debugging
    private void DebugPrintBoard()
    {
        for (int rank = 7; rank >= 0; rank--)
        {
            string rankStr = (rank+1) + " ";
            for (int file = 0; file < 8; file++)
            {
                int index = rank * 8 + file;
                ulong mask = 1UL << index;
                
                char piece = '.';
                
                if ((bitboard.WhitePawn & mask) != 0) piece = 'P';
                else if ((bitboard.WhiteRook & mask) != 0) piece = 'R';
                else if ((bitboard.WhiteKnight & mask) != 0) piece = 'N';
                else if ((bitboard.WhiteBishop & mask) != 0) piece = 'B';
                else if ((bitboard.WhiteQueen & mask) != 0) piece = 'Q';
                else if ((bitboard.WhiteKing & mask) != 0) piece = 'K';
                else if ((bitboard.BlackPawn & mask) != 0) piece = 'p';
                else if ((bitboard.BlackRook & mask) != 0) piece = 'r';
                else if ((bitboard.BlackKnight & mask) != 0) piece = 'n';
                else if ((bitboard.BlackBishop & mask) != 0) piece = 'b';
                else if ((bitboard.BlackQueen & mask) != 0) piece = 'q';
                else if ((bitboard.BlackKing & mask) != 0) piece = 'k';
                
                rankStr += piece + " ";
            }
            Debug.Log(rankStr);
        }
        Debug.Log("  a b c d e f g h");
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
                Instantiate(whitePawnPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent).name = "WhitePawn_" + i;
            if ((WhiteRook & (1UL << i)) != 0)
                Instantiate(whiteRookPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent).name = "WhiteRook_" + i;
            if ((WhiteKnight & (1UL << i)) != 0)
                Instantiate(whiteKnightPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent).name = "WhiteKnight_" + i;
            if ((WhiteBishop & (1UL << i)) != 0)
                Instantiate(whiteBishopPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent).name = "WhiteBishop_" + i;
            if ((WhiteQueen & (1UL << i)) != 0)
                Instantiate(whiteQueenPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent).name = "WhiteQueen_" + i;
            if ((WhiteKing & (1UL << i)) != 0)
                Instantiate(whiteKingPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent).name = "WhiteKing_" + i;
            if ((BlackPawn & (1UL << i)) != 0)
                Instantiate(blackPawnPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent).name = "BlackPawn_" + i;
            if ((BlackRook & (1UL << i)) != 0)
                Instantiate(blackRookPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent).name = "BlackRook_" + i;
            if ((BlackKnight & (1UL << i)) != 0)
                Instantiate(blackKnightPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent).name = "BlackKnight_" + i;
            if ((BlackBishop & (1UL << i)) != 0)
                Instantiate(blackBishopPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent).name = "BlackBishop_" + i;
            if ((BlackQueen & (1UL << i)) != 0)
                Instantiate(blackQueenPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent).name = "BlackQueen_" + i;
            if ((BlackKing & (1UL << i)) != 0)
                Instantiate(blackKingPrefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent).name = "BlackKing_" + i;
        }
    }


    //Information about the Tile clicked
    private GameObject SelectedTile;
    private Color TileColor;
    private int TileIndex;
    private ulong possibleMoves;

    void HandleClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Case 1: Clicked on a highlighted tile or a piece on a highlighted tile - this is a move destination
            int clickedTileIndex = -1;
            
            // If clicked directly on a tile
            if (hit.collider.tag == "ChessTile")
            {
                clickedTileIndex = int.Parse(hit.collider.gameObject.name);
            }
            // If clicked on a piece, find the tile underneath
            else if (hit.collider.tag == "ChessPiece")
            {
                // Cast a ray downward from the piece position to find the tile
                Ray downRay = new Ray(hit.collider.transform.position + Vector3.up, Vector3.down);
                RaycastHit tileHit;
                if (Physics.Raycast(downRay, out tileHit, 10f))
                {
                    if (tileHit.collider.tag == "ChessTile")
                    {
                        clickedTileIndex = int.Parse(tileHit.collider.gameObject.name);
                    }
                }
            }
            
            // Check if the clicked position is a valid move destination
            if (clickedTileIndex != -1 && (possibleMoves & (1UL << clickedTileIndex)) != 0)
            {
                Debug.Log("Valid move destination: " + GetAlgebraicNotation(clickedTileIndex));
                
                // This tile is a valid move destination
                MovePiece(selectedPieceIndex, clickedTileIndex);
                
                // Clean up highlights and selections
                EraseHighlights();
                if (SelectedTile != null)
                {
                    SelectedTile.GetComponent<Renderer>().material.color = TileColor;
                    SelectedTile = null;
                }
                selectedPiece = null;
                selectedPieceIndex = -1;
                possibleMoves = 0;
                
                // Switch turns
                isWhiteTurn = !isWhiteTurn;
                Debug.Log(isWhiteTurn ? "White's turn" : "Black's turn");
                return;
            }

            // Case 2: Clicked on a chess tile - select it for highlighting moves
            if (hit.collider.tag == "ChessTile")
            {
                // Clean up previous selection
                if (SelectedTile != null)
                {
                    SelectedTile.GetComponent<Renderer>().material.color = TileColor;
                }
                
                StoreTileInformation(hit);
                // Try to find a piece on this tile
                selectedPiece = FindPieceAtPosition(TileIndex);
                
                if (selectedPiece != null)
                {
                    // Check if this piece belongs to the current player
                    bool isPieceWhite = selectedPiece.name.StartsWith("White");
                    if (isPieceWhite == isWhiteTurn)
                    {
                        // Valid piece selection
                        selectedPieceIndex = TileIndex;
                        selectedPieceIsWhite = isPieceWhite;
                        
                        // Determine piece type
                        if (selectedPiece.name.Contains("Pawn"))
                            selectedPieceType = PieceType.Pawn;
                        else if (selectedPiece.name.Contains("Rook"))
                            selectedPieceType = PieceType.Rook;
                        else if (selectedPiece.name.Contains("Knight"))
                            selectedPieceType = PieceType.Knight;
                        else if (selectedPiece.name.Contains("Bishop"))
                            selectedPieceType = PieceType.Bishop;
                        else if (selectedPiece.name.Contains("Queen"))
                            selectedPieceType = PieceType.Queen;
                        else if (selectedPiece.name.Contains("King"))
                            selectedPieceType = PieceType.King;
                            
                        SelectedTile.GetComponent<Renderer>().material.color = Color.red; // Highlight the selected tile
                        DisplayPossibleMoves();
                    }
                    else
                    {
                        // Selected opponent's piece
                        Debug.Log("It's " + (isWhiteTurn ? "White" : "Black") + "'s turn");
                        selectedPiece = null;
                        EraseHighlights();
                    }
                }
                else
                {
                    // No piece on this tile
                    selectedPieceIndex = -1;
                    possibleMoves = 0;
                    EraseHighlights();
                }
            }       
            else if (hit.collider.tag == "ChessPiece") // In case a piece is clicked
            {
                // Clean up previous selection
                if (SelectedTile != null)
                {
                    SelectedTile.GetComponent<Renderer>().material.color = TileColor;
                }
                
                selectedPiece = hit.collider.gameObject;
                bool isPieceWhite = selectedPiece.name.StartsWith("White");
                
                // Check if this piece belongs to the current player
                if (isPieceWhite == isWhiteTurn)
                {
                    // Cast a ray downward from the piece position to find the tile
                    Ray downRay = new Ray(selectedPiece.transform.position + Vector3.up, Vector3.down);
                    RaycastHit tileHit;
                    if (Physics.Raycast(downRay, out tileHit, 10f))
                    {
                        if (tileHit.collider.tag == "ChessTile") 
                        {
                            StoreTileInformation(tileHit);
                            selectedPieceIndex = TileIndex;
                            selectedPieceIsWhite = isPieceWhite;
                            
                            // Determine piece type
                            if (selectedPiece.name.Contains("Pawn"))
                                selectedPieceType = PieceType.Pawn;
                            else if (selectedPiece.name.Contains("Rook"))
                                selectedPieceType = PieceType.Rook;
                            else if (selectedPiece.name.Contains("Knight"))
                                selectedPieceType = PieceType.Knight;
                            else if (selectedPiece.name.Contains("Bishop"))
                                selectedPieceType = PieceType.Bishop;
                            else if (selectedPiece.name.Contains("Queen"))
                                selectedPieceType = PieceType.Queen;
                            else if (selectedPiece.name.Contains("King"))
                                selectedPieceType = PieceType.King;
                                
                            SelectedTile.GetComponent<Renderer>().material.color = Color.red;
                            DisplayPossibleMoves();
                        }
                    }
                }
                else
                {
                    // Selected opponent's piece
                    Debug.Log("It's " + (isWhiteTurn ? "White" : "Black") + "'s turn");
                    selectedPiece = null;
                    EraseHighlights();
                }
            }
            else
            {
                // Clicked on something else
                if (SelectedTile != null)
                {
                    SelectedTile.GetComponent<Renderer>().material.color = TileColor;
                    SelectedTile = null;
                }
                
                selectedPiece = null;
                selectedPieceIndex = -1;
                possibleMoves = 0;
                EraseHighlights();
                Debug.Log("No Chess Tile Clicked");
            }
        }
    }

    private void StoreTileInformation(RaycastHit hit)
    {
        SelectedTile = hit.collider.gameObject;
        TileColor = SelectedTile.GetComponent<Renderer>().material.color;
        TileIndex = int.Parse(hit.collider.gameObject.name);
    }
    
    private void DisplayPossibleMoves()
    {
        EraseHighlights();
        Debug.Log("Tile Index: " + TileIndex + " (" + GetAlgebraicNotation(TileIndex) + ")");
        
        possibleMoves = findMoves.GetPossibleMoves(TileIndex);
        Debug.Log("Possible moves bitboard: " + possibleMoves);
        
        // Log each potential move in algebraic notation for clarity
        Debug.Log("Legal moves from " + GetAlgebraicNotation(TileIndex) + ":");
        bool hasMoves = false;
        
        for (int i = 0; i < 64; i++)
        {
            if ((possibleMoves & (1UL << i)) != 0)
            {
                hasMoves = true;
                GameObject tile = GameObject.Find(i.ToString());
                tile.GetComponent<Renderer>().material.color = Color.green;
                Debug.Log("- Can move to " + GetAlgebraicNotation(i));
            }
        }
        
        if (!hasMoves)
        {
            Debug.Log("No legal moves available");
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
    
    private GameObject FindPieceAtPosition(int bitIndex)
    {
        Vector3 worldPos = GetWorldPositionForBit(bitIndex);
        Collider[] colliders = Physics.OverlapSphere(worldPos, 0.4f);
        foreach (Collider collider in colliders)
        {
            if (collider.tag == "ChessPiece")
            {
                return collider.gameObject;
            }
        }
        return null;
    }
    
    private void MovePiece(int fromIndex, int toIndex)
    {
        // Debug the move indexes
        Debug.Log("Moving from bit " + fromIndex + " to bit " + toIndex);
        Debug.Log("That's from position " + GetAlgebraicNotation(fromIndex) + " to " + GetAlgebraicNotation(toIndex));
        
        // Check if there's a piece at the destination (capture)
        GameObject capturedPiece = FindPieceAtPosition(toIndex);
        if (capturedPiece != null)
        {
            Debug.Log("Capturing: " + capturedPiece.name);
            Destroy(capturedPiece);
        }
        
        // Move the piece in the bitboard
        Move move = new Move(fromIndex, toIndex, null, (int)selectedPieceType, selectedPieceIsWhite);
        bitboard.UpdateBitBoard(move);
        
        // Calculate the exact world position for the destination
        Vector3 destinationPosition = GetWorldPositionForBit(toIndex);
        Debug.Log("Moving to world position: " + destinationPosition);
        
        // Move the piece in the scene
        selectedPiece.transform.position = destinationPosition;
        
        // Update the piece name with the new position
        string pieceName = selectedPiece.name;
        if (pieceName.Contains("_"))
        {
            pieceName = pieceName.Substring(0, pieceName.IndexOf("_") + 1) + toIndex;
            selectedPiece.name = pieceName;
        }
        
        Debug.Log("Moved " + selectedPiece.name + " from " + fromIndex + " to " + toIndex);
    }
    
    // Convert a bitboard index to algebraic notation (e.g., e4)
    private string GetAlgebraicNotation(int index)
    {
        int file = index % 8;
        int rank = index / 8;
        char fileChar = (char)('a' + file);
        int rankNum = rank + 1;
        return fileChar.ToString() + rankNum.ToString();
    }
    
    // Convert a bitboard index to a world position
    Vector3 GetWorldPositionForBit(int bitIndex)
    {
        // In the bitboard:
        // - Bit 0 is a1 (bottom-left from white's perspective)
        // - Bit 63 is h8 (top-right from white's perspective)
        int file = bitIndex % 8;            // 0-7 (a-h files)
        int rank = bitIndex / 8;            // 0-7 (1-8 ranks)
        
        // Looking at the issue description (e3 goes to e6), 
        // it seems the rank is being flipped (rank 3 becomes rank 6)
        // Let's try flipping the rank instead of the file
        rank = 7 - rank;  // Flip the rank (1=7, 8=0)
        
        return boardOrigin + new Vector3(file * squareSize, 0f, rank * squareSize);
    }
    
    // Convert a world position back to a bitboard index
    int GetBitIndexFromWorldPosition(Vector3 worldPos)
    {
        int file = Mathf.RoundToInt((worldPos.x - boardOrigin.x) / squareSize);
        int rank = Mathf.RoundToInt((worldPos.z - boardOrigin.z) / squareSize);
        
        // Reverse the flip from GetWorldPositionForBit
        rank = 7 - rank;
        
        // Convert to bitboard index
        return rank * 8 + file;
    }
}