using UnityEngine;
using System;
using System.Collections.Generic;
public class BoardController : MonoBehaviour
{
    //Bitboard class that contains the bitboards for the pieces
    Bitboard bitboard;
    public bool debugMode = true;
    //half pli, which I call a move, that precedes the current board state
    Move previousMove;

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
    private Evaluation evaluation; // Add an Evaluation instance

    // Track current player's turn (true = white, false = black)
    private bool isWhiteTurn = true;

    // Track selected piece
    private GameObject selectedPiece;
    private int selectedPieceIndex = -1;
    private PieceType selectedPieceType;
    private bool selectedPieceIsWhite;

    //AI
    private StockFridge AI;
    public bool isAIWhite = false;
    public bool isAIBlack = true;
    public int depth = 3;

    void Start()
    {
        bitboard = new Bitboard();
        previousMove = null;
        evaluation = new Evaluation(); // Initialize the evaluation object
        findMoves = new FindMoves(bitboard);
        
        //Instantiate All Pieces on Board   
        InstantiateBoard();
        InstantiatePieces();
        
        // Print the initial board layout for debugging
        Debug.Log("Initial board setup:");
        DebugPrintBoard();

        //Instantiate the AI
        AI = new StockFridge(findMoves, bitboard);
    }
    
    // Print the current board state for debugging
    private void DebugPrintBoard()
    {
    if (!debugMode) return;
    
    Debug.Log("--------- CURRENT BOARD STATE ---------");
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
    Debug.Log("--------------------------------------");
    }
    private bool IsInCheck(bool isWhite)
    {
        return evaluation.IsInCheck(isWhite, 
                                bitboard.returnWhitePiecesByTypes(), 
                                bitboard.returnBlackPiecesByTypes(), 
                                bitboard.returnAllPieces());
    }


    private bool IsMoveLegal(int fromIndex, int toIndex)
    {
        if (!debugMode) return true;
        
        // Check if the source square has a piece
        ulong sourceMask = 1UL << fromIndex;
        bool isWhitePiece = 
            (bitboard.WhitePawn & sourceMask) != 0 ||
            (bitboard.WhiteRook & sourceMask) != 0 ||
            (bitboard.WhiteKnight & sourceMask) != 0 ||
            (bitboard.WhiteBishop & sourceMask) != 0 ||
            (bitboard.WhiteQueen & sourceMask) != 0 ||
            (bitboard.WhiteKing & sourceMask) != 0;
        
        bool isBlackPiece = 
            (bitboard.BlackPawn & sourceMask) != 0 ||
            (bitboard.BlackRook & sourceMask) != 0 ||
            (bitboard.BlackKnight & sourceMask) != 0 ||
            (bitboard.BlackBishop & sourceMask) != 0 ||
            (bitboard.BlackQueen & sourceMask) != 0 ||
            (bitboard.BlackKing & sourceMask) != 0;
        
        if (!isWhitePiece && !isBlackPiece)
        {
            Debug.LogError($"No piece at source square {GetAlgebraicNotation(fromIndex)}");
            return false;
        }
        
        // Check if the destination is a valid move
        ulong possibleMoves = findMoves.GetPossibleMoves(fromIndex);
        bool isValidMove = (possibleMoves & (1UL << toIndex)) != 0;
        
        if (!isValidMove)
        {
            Debug.LogError($"Invalid move from {GetAlgebraicNotation(fromIndex)} to {GetAlgebraicNotation(toIndex)}");
            return false;
        }
        
        // Check if the move would leave the king in check
        // First determine piece type
        PieceType pieceType = PieceType.Pawn; // Default
        if ((bitboard.WhitePawn & sourceMask) != 0 || (bitboard.BlackPawn & sourceMask) != 0)
            pieceType = PieceType.Pawn;
        else if ((bitboard.WhiteRook & sourceMask) != 0 || (bitboard.BlackRook & sourceMask) != 0)
            pieceType = PieceType.Rook;
        else if ((bitboard.WhiteKnight & sourceMask) != 0 || (bitboard.BlackKnight & sourceMask) != 0)
            pieceType = PieceType.Knight;
        else if ((bitboard.WhiteBishop & sourceMask) != 0 || (bitboard.BlackBishop & sourceMask) != 0)
            pieceType = PieceType.Bishop;
        else if ((bitboard.WhiteQueen & sourceMask) != 0 || (bitboard.BlackQueen & sourceMask) != 0)
            pieceType = PieceType.Queen;
        else if ((bitboard.WhiteKing & sourceMask) != 0 || (bitboard.BlackKing & sourceMask) != 0)
            pieceType = PieceType.King;
        
        // Create a move object
        Move move = new Move(fromIndex, toIndex, previousMove, (int)pieceType, isWhitePiece);
        
        // Make the move
        bitboard.UpdateBitBoard(move);
        
        // Check if the king is in check
        ulong[] whitePieces = bitboard.returnWhitePiecesByTypes();
        ulong[] blackPieces = bitboard.returnBlackPiecesByTypes();
        ulong allPieces = bitboard.returnAllPieces();
        
        bool kingInCheck = evaluation.IsInCheck(isWhitePiece, whitePieces, blackPieces, allPieces);
        
        // Undo the move
        bitboard.UndoBitboard();
        
        if (kingInCheck)
        {
            Debug.LogError($"Move would leave king in check: {GetAlgebraicNotation(fromIndex)} to {GetAlgebraicNotation(toIndex)}");
            return false;
        }
        
        return true;
    }
    private bool aiIsThinking = false;

    void Update()
    {
        // Check for checkmate for the current player
        if (isWhiteTurn && DetectCheckmate(true))
        {
            Debug.Log("Checkmate! Black wins!");
            // You could add game over UI elements or other handling here
        }
        else if (!isWhiteTurn && DetectCheckmate(false))
        {
            Debug.Log("Checkmate! White wins!");
            // You could add game over UI elements or other handling here
        }
        
        // Rest of Update method
        if (Input.GetMouseButtonDown(0) && isWhiteTurn)
        {
            HandleClick();
        }
        else if (!isWhiteTurn && !aiIsThinking) // Assuming you've added aiIsThinking from previous fixes
        {
            aiIsThinking = true;
            MakeAIMove();
        }
    }

    private void MakeAIMove()
    {
        try
        {
            // Get the best move from StockFridge
            Move aiMove = AI.GetBestMove(depth, false, previousMove);
            
            if (aiMove != null)
            {
                // Find the piece at the source position
                selectedPiece = FindPieceAtPosition(aiMove.Source);
                if (selectedPiece != null)
                {
                    // Verify the piece is actually black (add this check)
                    if (!selectedPiece.name.StartsWith("Black"))
                    {
                        Debug.LogError("AI tried to move a white piece: " + selectedPiece.name);
                        isWhiteTurn = true;
                        aiIsThinking = false;
                        return;
                    }
                    
                    selectedPieceIndex = aiMove.Source;
                    selectedPieceType = (PieceType)aiMove.PieceType;
                    selectedPieceIsWhite = false;

                    // Display the move visually before making it
                    EraseHighlights();
                    HighlightMove(aiMove.Source, aiMove.Destination);

                    // Make the move
                    MovePiece(aiMove.Source, aiMove.Destination);
                    
                    // Clean up and switch turns
                    EraseHighlights();
                    selectedPiece = null;
                    selectedPieceIndex = -1;
                    possibleMoves = 0;
                    
                    Debug.Log("AI moved from " + GetAlgebraicNotation(aiMove.Source) + 
                            " to " + GetAlgebraicNotation(aiMove.Destination));
                    previousMove = aiMove;
                }
                else
                {
                    Debug.LogError("AI's piece not found at " + GetAlgebraicNotation(aiMove.Source));
                }
            }
            else
            {
                Debug.LogError("AI couldn't find a valid move!");
                // Check if in checkmate or stalemate
                bool inCheck = evaluation.IsInCheck(false, 
                                                bitboard.returnWhitePiecesByTypes(), 
                                                bitboard.returnBlackPiecesByTypes(), 
                                                bitboard.returnAllPieces());
                if (inCheck)
                {
                    Debug.Log("Checkmate - White wins!");
                }
                else
                {
                    Debug.Log("Stalemate - Draw!");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error in AI move: " + e.Message);
        }
        finally
        {
            // Always ensure we go back to white's turn and reset AI thinking flag
            isWhiteTurn = true;
            aiIsThinking = false;
        }
    }
    
    // Highlight the AI's move for visualization
    private void HighlightMove(int fromIndex, int toIndex)
    {
        GameObject fromTile = GameObject.Find(fromIndex.ToString());
        GameObject toTile = GameObject.Find(toIndex.ToString());
        
        if (fromTile != null && toTile != null)
        {
            fromTile.GetComponent<Renderer>().material.color = Color.blue;
            toTile.GetComponent<Renderer>().material.color = Color.red;
        }
    }

    // Rest of your methods remain largely unchanged...
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
                Instantiate(whiteKnightPrefab, GetWorldPositionForBit(i), Quaternion.Euler(0,180,0), PieceParent).name = "WhiteKnight_" + i;
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
        // First, determine if the current player is in check
        bool inCheck = IsInCheck(isWhiteTurn);
        
        if (inCheck)
        {
            Debug.Log((isWhiteTurn ? "White" : "Black") + " is in check!");
        }
        
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
    private bool DetectCheckmate(bool forWhite)
    {
        // Get all possible moves for the current player
        List<Move> allPossibleMoves = findMoves.GetAllPossibleMoves(forWhite, previousMove);
        
        // Check if any of these moves can escape check
        foreach (Move move in allPossibleMoves)
        {
            // Try making this move
            bitboard.UpdateBitBoard(move);
            
            // Is the king still in check after this move?
            bool stillInCheck = IsInCheck(forWhite);
            
            // Undo the move
            bitboard.UndoBitboard();
            
            // If this move escapes check, it's not checkmate
            if (!stillInCheck)
            {
                return false;
            }
        }
        
        // If we're in check and no move can escape it, it's checkmate
        return IsInCheck(forWhite);
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
        
        // Get all possible moves for this piece
        possibleMoves = findMoves.GetPossibleMoves(TileIndex);
        Debug.Log("Possible moves bitboard: " + possibleMoves);
        
        // Check if the king is in check
        bool inCheck = IsInCheck(isWhiteTurn);
        
        if (inCheck)
        {
            Debug.Log((isWhiteTurn ? "White" : "Black") + " is in check!");
            
            // If king is in check, filter the moves to only those that address the check
            ulong legalMovesInCheck = 0;
            
            // For each potential move, check if it would still leave the king in check
            for (int i = 0; i < 64; i++)
            {
                if ((possibleMoves & (1UL << i)) != 0)
                {
                    // Try making this move
                    Move testMove = new Move(TileIndex, i, previousMove, (int)selectedPieceType, selectedPieceIsWhite);
                    bitboard.UpdateBitBoard(testMove);
                    
                    // After the move, is the king still in check?
                    bool stillInCheck = IsInCheck(isWhiteTurn);
                    
                    // Undo the move
                    bitboard.UndoBitboard();
                    
                    // If this move gets out of check, include it in legal moves
                    if (!stillInCheck)
                    {
                        legalMovesInCheck |= (1UL << i);
                        GameObject tile = GameObject.Find(i.ToString());
                        tile.GetComponent<Renderer>().material.color = Color.green;
                        Debug.Log("- Can move to " + GetAlgebraicNotation(i) + " to escape check");
                    }
                }
            }
            
            // Replace the possible moves with only the ones that address check
            possibleMoves = legalMovesInCheck;
            
            if (possibleMoves == 0)
            {
                Debug.Log("Checkmate! No legal moves to escape check.");
            }
        }
        else
        {
            // If not in check, display all legal moves as usual
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
                Debug.Log("No legal moves available - potential stalemate");
            }
        }
        
        // Always highlight the selected tile
        SelectedTile.GetComponent<Renderer>().material.color = Color.red;
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
        
        // Verify the move is legal
        if (debugMode && !IsMoveLegal(fromIndex, toIndex))
        {
            Debug.LogError("Illegal move attempted!");
            // You might want to add additional handling here
            return;
        }
        
        // Check if there's a piece at the destination (capture)
        GameObject capturedPiece = FindPieceAtPosition(toIndex);
        if (capturedPiece != null)
        {
            Debug.Log("Capturing: " + capturedPiece.name);
            Destroy(capturedPiece);
        }
        
        // Move the piece in the bitboard
        Move move = new Move(fromIndex, toIndex, previousMove, (int)selectedPieceType, selectedPieceIsWhite);
        bitboard.UpdateBitBoard(move);

        //Update the Move in the previousMove to keep track of the move that got to the current board state
        previousMove = move;
        
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
        
        // Debug the board state after the move
        if (debugMode) DebugPrintBoard();
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