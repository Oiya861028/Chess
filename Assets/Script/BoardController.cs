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
    private const int WHITE_KING_START = 4;
    private const int BLACK_KING_START = 60;
    private const int WHITE_KINGSIDE_ROOK_START = 7;
    private const int WHITE_QUEENSIDE_ROOK_START = 0;
    private const int BLACK_KINGSIDE_ROOK_START = 63;
    private const int BLACK_QUEENSIDE_ROOK_START = 56;
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
            Debug.LogError($"No piece at source square {BitboardUtils.IndexToAlgebraic(fromIndex)}");
            return false;
        }
        
        // Check if the destination is a valid move
        ulong possibleMoves = findMoves.GetPossibleMoves(fromIndex);
        bool isValidMove = (possibleMoves & (1UL << toIndex)) != 0;
        
        if (!isValidMove)
        {
            Debug.LogError($"Invalid move from {BitboardUtils.IndexToAlgebraic(fromIndex)} to {BitboardUtils.IndexToAlgebraic(toIndex)}");
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
            Debug.LogError($"Move would leave king in check: {BitboardUtils.IndexToAlgebraic(fromIndex)} to {BitboardUtils.IndexToAlgebraic(toIndex)}");
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
            
        }
        else if (!isWhiteTurn && DetectCheckmate(false))
        {
            Debug.Log("Checkmate! White wins!");
        }
        
        // Rest of Update method
        if (Input.GetMouseButtonDown(0) && isWhiteTurn)
        {
            HandleClick();
        }
        else if (!isWhiteTurn && !aiIsThinking)
        {
            aiIsThinking = true;
            MakeAIMove();
        }
    }
    private bool IsPiecePinned(int pieceIndex, bool isWhite)
    {
        // Find the king
        int kingIndex = -1;
        ulong kingBitboard = isWhite ? bitboard.WhiteKing : bitboard.BlackKing;
        
        // Get the king position
        for (int i = 0; i < 64; i++)
        {
            if ((kingBitboard & (1UL << i)) != 0)
            {
                kingIndex = i;
                break;
            }
        }
        
        if (kingIndex == -1)
        {
            Debug.LogError("Could not find king for pin check");
            return false;
        }
        
        // Test if the piece is on the same rank, file, or diagonal as the king
        int pieceRank = pieceIndex / 8;
        int pieceFile = pieceIndex % 8;
        int kingRank = kingIndex / 8;
        int kingFile = kingIndex % 8;
        
        bool onSameRank = pieceRank == kingRank;
        bool onSameFile = pieceFile == kingFile;
        bool onSameDiagonal = Math.Abs(pieceRank - kingRank) == Math.Abs(pieceFile - kingFile);
        
        if (!onSameRank && !onSameFile && !onSameDiagonal)
        {
            return false; // Can't be pinned if not on the same line as the king
        }
        
        // Get potentially pinning pieces (enemy rooks, bishops, queens)
        ulong enemyPinners;
        if (isWhite)
        {
            // Enemy rooks and queens for rank/file pins
            // Enemy bishops and queens for diagonal pins
            if (onSameRank || onSameFile)
            {
                enemyPinners = bitboard.BlackRook | bitboard.BlackQueen;
            }
            else // onSameDiagonal
            {
                enemyPinners = bitboard.BlackBishop | bitboard.BlackQueen;
            }
        }
        else
        {
            if (onSameRank || onSameFile)
            {
                enemyPinners = bitboard.WhiteRook | bitboard.WhiteQueen;
            }
            else // onSameDiagonal
            {
                enemyPinners = bitboard.WhiteBishop | bitboard.WhiteQueen;
            }
        }
        
        // If no potential pinners, piece is not pinned
        if (enemyPinners == 0)
        {
            return false;
        }
        
        // Check each potential pinner
        for (int i = 0; i < 64; i++)
        {
            if ((enemyPinners & (1UL << i)) == 0)
            {
                continue; // Not a pinner
            }
            
            int pinnerRank = i / 8;
            int pinnerFile = i % 8;
            
            // Ensure the potential pinner is on the same line
            bool pinnerOnSameRank = pinnerRank == kingRank && pinnerRank == pieceRank;
            bool pinnerOnSameFile = pinnerFile == kingFile && pinnerFile == pieceFile;
            bool pinnerOnSameDiagonal = (Math.Abs(pinnerRank - kingRank) == Math.Abs(pinnerFile - kingFile)) &&
                                    (Math.Abs(pinnerRank - pieceRank) == Math.Abs(pinnerFile - pieceFile));
                                    
            if (!pinnerOnSameRank && !pinnerOnSameFile && !pinnerOnSameDiagonal)
            {
                continue; // Not aligned properly
            }
            
            // Check for other pieces between king, the piece, and the potential pinner
            ulong allPieces = bitboard.returnAllPieces();
            
            // Create a path mask for the line between king, piece, and pinner
            ulong pathMask = 0;
            
            if (pinnerOnSameRank)
            {
                // For same rank, we need min and max files
                int minFile = Math.Min(Math.Min(kingFile, pieceFile), pinnerFile);
                int maxFile = Math.Max(Math.Max(kingFile, pieceFile), pinnerFile);
                
                for (int f = minFile; f <= maxFile; f++)
                {
                    pathMask |= 1UL << (kingRank * 8 + f);
                }
            }
            else if (pinnerOnSameFile)
            {
                // For same file, we need min and max ranks
                int minRank = Math.Min(Math.Min(kingRank, pieceRank), pinnerRank);
                int maxRank = Math.Max(Math.Max(kingRank, pieceRank), pinnerRank);
                
                for (int r = minRank; r <= maxRank; r++)
                {
                    pathMask |= 1UL << (r * 8 + kingFile);
                }
            }
            else if (pinnerOnSameDiagonal)
            {
                // For diagonals, we need to determine the direction
                int rankDir = pinnerRank > kingRank ? 1 : -1;
                int fileDir = pinnerFile > kingFile ? 1 : -1;
                
                int r = kingRank;
                int f = kingFile;
                
                while (r >= 0 && r < 8 && f >= 0 && f < 8)
                {
                    pathMask |= 1UL << (r * 8 + f);
                    
                    if (r == pinnerRank && f == pinnerFile)
                        break;
                        
                    r += rankDir;
                    f += fileDir;
                }
            }
            
            // Remove the king, piece, and pinner from the path mask
            pathMask &= ~(1UL << kingIndex);
            pathMask &= ~(1UL << pieceIndex);
            pathMask &= ~(1UL << i);
            
            // If there are no other pieces on the path, the piece is pinned
            if ((pathMask & allPieces) == 0)
            {
                return true;
            }
        }
        
        return false;
    }
    private void MakeAIMove()
    {
        try
        {
            aiIsThinking = true;
            
            // Debug current board state before AI move
            Debug.Log("Board state before AI move:");
            DebugPrintBoard();
            
            // Verify board consistency before AI move
            VerifyBoardState();
            
            // Get the best move from StockFridge
    
            Move aiMove = AI.GetBestMove(depth, false, previousMove);
            
            if (aiMove != null)
            {
                // Double check the AI is playing as black
                if (aiMove.IsWhite)
                {
                    Debug.LogError("AI returned a white piece move but should be playing as black");
                    isWhiteTurn = true;
                    aiIsThinking = false;
                    return;
                }
                
                // Find the piece at the source position
                selectedPiece = FindPieceAtPosition(aiMove.Source);
                if (selectedPiece != null)
                {
                    // Verify the piece is actually black
                    if (!selectedPiece.name.StartsWith("Black"))
                    {
                        Debug.LogError("AI tried to move a white piece: " + selectedPiece.name);
                        Debug.LogError("Move source: " + BitboardUtils.IndexToAlgebraic(aiMove.Source) + 
                                    ", destination: " + BitboardUtils.IndexToAlgebraic(aiMove.Destination) +
                                    ", piece type: " + aiMove.PieceType + 
                                    ", is white move: " + aiMove.IsWhite);
                        
                        // Fix the inconsistency - update the bitboard
                        VerifyBoardState();
                        
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
                    isWhiteTurn = true;
                    
                    Debug.Log("AI moved from " + BitboardUtils.IndexToAlgebraic(aiMove.Source) + 
                            " to " + BitboardUtils.IndexToAlgebraic(aiMove.Destination));
                            
                    // Debug board state after AI move
                    Debug.Log("Board state after AI move:");
                    DebugPrintBoard();
                    
                    // Verify board consistency after AI move
                    VerifyBoardState();
                }
                else
                {
                    Debug.LogError("AI's piece not found at " + BitboardUtils.IndexToAlgebraic(aiMove.Source));
                    
                    // Try to fix the inconsistency
                    VerifyBoardState();
                    
                    isWhiteTurn = true;
                    aiIsThinking = false;
                }
                previousMove = aiMove;
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
                isWhiteTurn = true;
                aiIsThinking = false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error in AI move: " + e.Message + "\n" + e.StackTrace);
            isWhiteTurn = true;
            aiIsThinking = false;
        }
        finally
        {
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
                Debug.Log("Valid move destination: " + BitboardUtils.IndexToAlgebraic(clickedTileIndex));
                
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
    private bool IsInCheck(bool isWhite)
    {
        return evaluation.IsInCheck(isWhite, 
                                bitboard.returnWhitePiecesByTypes(), 
                                bitboard.returnBlackPiecesByTypes(), 
                                bitboard.returnAllPieces());
    }
    private void DisplayPossibleMoves()
    {
        EraseHighlights();
        Debug.Log("Tile Index: " + TileIndex + " (" + BitboardUtils.IndexToAlgebraic(TileIndex) + ")");
        
        // Get all possible moves for this piece
        possibleMoves = findMoves.GetPossibleMoves(TileIndex);
        Debug.Log("Possible moves bitboard: " + possibleMoves);
        if (selectedPieceType == PieceType.King) {
        Debug.Log("Checking castling moves for " + (selectedPieceIsWhite ? "white" : "black") + " king");
        
        // Debug castling state
        if (selectedPieceIsWhite) {
            Debug.Log($"White castling state - King moved: {bitboard.whiteKingMoved}, " +
                    $"Kingside rook moved: {bitboard.whiteKingsideRookMoved}, " + 
                    $"Queenside rook moved: {bitboard.whiteQueensideRookMoved}");
        } else {
            Debug.Log($"Black castling state - King moved: {bitboard.blackKingMoved}, " +
                    $"Kingside rook moved: {bitboard.blackKingsideRookMoved}, " + 
                    $"Queenside rook moved: {bitboard.blackQueensideRookMoved}");
        }
        
        // Add castling moves explicitly
        if (selectedPieceIsWhite && TileIndex == 4) { // White king at e1
            if (!bitboard.whiteKingMoved) {
                // Check kingside castling
                if (!bitboard.whiteKingsideRookMoved && 
                    (bitboard.returnAllPieces() & ((1UL << 5) | (1UL << 6))) == 0 && 
                    !IsInCheck(true)) {
                    
                    possibleMoves |= 1UL << 6; // g1
                    Debug.Log("Added white kingside castling move");
                }
                
                // Check queenside castling
                if (!bitboard.whiteQueensideRookMoved && 
                    (bitboard.returnAllPieces() & ((1UL << 1) | (1UL << 2) | (1UL << 3))) == 0 && 
                    !IsInCheck(true)) {
                    
                    possibleMoves |= 1UL << 2; // c1
                    Debug.Log("Added white queenside castling move");
                }
            }
        }
        else if (!selectedPieceIsWhite && TileIndex == 60) { // Black king at e8
            if (!bitboard.blackKingMoved) {
                // Check kingside castling
                if (!bitboard.blackKingsideRookMoved && 
                    (bitboard.returnAllPieces() & ((1UL << 61) | (1UL << 62))) == 0 && 
                    !IsInCheck(false)) {
                    
                    possibleMoves |= 1UL << 62; // g8
                    Debug.Log("Added black kingside castling move");
                }
                
                // Check queenside castling
                if (!bitboard.blackQueensideRookMoved && 
                    (bitboard.returnAllPieces() & ((1UL << 57) | (1UL << 58) | (1UL << 59))) == 0 && 
                    !IsInCheck(false)) {
                    
                    possibleMoves |= 1UL << 58; // c8
                    Debug.Log("Added black queenside castling move");
                }
            }
        }
    }
        
        // Highlight all possible move squares
        for (int i = 0; i < 64; i++)
        {
            if ((possibleMoves & (1UL << i)) != 0)
            {
                GameObject tile = GameObject.Find(i.ToString());
                if (tile != null)
                {
                    tile.GetComponent<Renderer>().material.color = Color.green;
                    Debug.Log("- Can move to " + BitboardUtils.IndexToAlgebraic(i));
                }
                else
                {
                    Debug.LogError("Could not find tile " + i);
                }
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
            
            Collider[] colliders = Physics.OverlapSphere(worldPos, 0.2f);
            
            // Sort colliders by distance to ensure we get the closest one
            System.Array.Sort(colliders, (a, b) => 
                Vector3.Distance(a.transform.position, worldPos).CompareTo(
                Vector3.Distance(b.transform.position, worldPos)));
            
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
        Debug.Log("That's from position " + BitboardUtils.IndexToAlgebraic(fromIndex) + " to " + BitboardUtils.IndexToAlgebraic(toIndex));
        
        // Determine the piece type and color BEFORE making any changes
        PieceType movingPieceType = selectedPieceType;
        bool isWhitePiece = selectedPieceIsWhite;
        
        // Check for castling moves
        bool isCastling = false;
        int rookFromIndex = -1;
        int rookToIndex = -1;
        
        // White king castling
        if (movingPieceType == PieceType.King && isWhitePiece && fromIndex == 4) // e1
        {
            // Kingside castling
            if (toIndex == 6) // g1
            {
                isCastling = true;
                rookFromIndex = 7; // h1
                rookToIndex = 5;   // f1
                Debug.Log("White kingside castling");
            }
            // Queenside castling
            else if (toIndex == 2) // c1
            {
                isCastling = true;
                rookFromIndex = 0; // a1
                rookToIndex = 3;   // d1
                Debug.Log("White queenside castling");
            }
        }
        // Black king castling
        else if (movingPieceType == PieceType.King && !isWhitePiece && fromIndex == 60) // e8
        {
            // Kingside castling
            if (toIndex == 62) // g8
            {
                isCastling = true;
                rookFromIndex = 63; // h8
                rookToIndex = 61;   // f8
                Debug.Log("Black kingside castling");
            }
            // Queenside castling
            else if (toIndex == 58) // c8
            {
                isCastling = true;
                rookFromIndex = 56; // a8
                rookToIndex = 59;   // d8
                Debug.Log("Black queenside castling");
            }
        }
        
        // Update castling flags
        
        
        // Check if there's a piece at the destination (capture)
        GameObject capturedPiece = FindPieceAtPosition(toIndex);
        if (capturedPiece != null)
        {
            Debug.Log("Capturing: " + capturedPiece.name);
            DestroyImmediate(capturedPiece);
        }
        
        // Move the piece in the bitboard
        Move move = new Move(fromIndex, toIndex, previousMove, (int)movingPieceType, isWhitePiece);
        bitboard.UpdateBitBoard(move);
        previousMove = move;

        Debug.Log($"Creating move: Type={movingPieceType}, IsWhite={isWhitePiece}, From={BitboardUtils.IndexToAlgebraic(fromIndex)}, To={BitboardUtils.IndexToAlgebraic(toIndex)}");
        // Calculate the exact world position for the destination
        Vector3 destinationPosition = GetWorldPositionForBit(toIndex);
        
        // Get the correct prefab based on piece type and color
        GameObject prefabToUse = GetPrefabForPiece(movingPieceType, isWhitePiece);
        
        if (prefabToUse != null)
        {
            // Destroy the old piece GameObject
            DestroyImmediate(selectedPiece);
            
            // Create a new piece GameObject at the destination
            string pieceName = (isWhitePiece ? "White" : "Black") + 
                            GetPieceTypeName(movingPieceType) + "_" + toIndex;
            
            GameObject newPiece = Instantiate(prefabToUse, destinationPosition, Quaternion.identity, PieceParent);
            newPiece.name = pieceName;
            
            Debug.Log("Created new piece: " + newPiece.name + " at " + BitboardUtils.IndexToAlgebraic(toIndex));
        }
        
        // Handle castling rook movement
        if (isCastling)
        {
            Debug.Log($"Executing castling move: {(isWhitePiece ? "White" : "Black")} {(toIndex == 6 || toIndex == 62 ? "kingside" : "queenside")}");
            
            // Find the rook object
            GameObject rookObject = FindPieceAtPosition(rookFromIndex);
            if (rookObject != null)
            {
                Debug.Log($"Found rook at {BitboardUtils.IndexToAlgebraic(rookFromIndex)}");
                
                // Move the rook in the bitboard
                Move rookMove = new Move(rookFromIndex, rookToIndex, null, (int)PieceType.Rook, isWhitePiece);
                bitboard.UpdateBitBoard(rookMove);
                
                // Get the world position for the rook
                Vector3 rookDestination = GetWorldPositionForBit(rookToIndex);
                
                // Get the correct rook prefab
                GameObject rookPrefab = isWhitePiece ? whiteRookPrefab : blackRookPrefab;
                
                // Destroy the old rook
                DestroyImmediate(rookObject);
                
                // Create the new rook at the destination
                string rookName = (isWhitePiece ? "White" : "Black") + "Rook_" + rookToIndex;
                GameObject newRook = Instantiate(rookPrefab, rookDestination, Quaternion.identity, PieceParent);
                newRook.name = rookName;
                
                Debug.Log($"Castling: Moved rook from {BitboardUtils.IndexToAlgebraic(rookFromIndex)} to {BitboardUtils.IndexToAlgebraic(rookToIndex)}");
            }
            else
            {
                Debug.LogError($"Castling rook not found at {BitboardUtils.IndexToAlgebraic(rookFromIndex)}. Verifying board state...");
                VerifyBoardState();
                
                // Try to find the rook again after verification
                rookObject = FindPieceAtPosition(rookFromIndex);
                if (rookObject != null) {
                    Debug.Log($"Found rook after verification at {BitboardUtils.IndexToAlgebraic(rookFromIndex)}");
                    
                    // Move the rook in the bitboard
                    Move rookMove = new Move(rookFromIndex, rookToIndex, null, (int)PieceType.Rook, isWhitePiece);
                    bitboard.UpdateBitBoard(rookMove);
                    
                    // Get the world position for the rook
                    Vector3 rookDestination = GetWorldPositionForBit(rookToIndex);
                    
                    // Get the correct rook prefab
                    GameObject rookPrefab = isWhitePiece ? whiteRookPrefab : blackRookPrefab;
                    
                    // Destroy the old rook
                    DestroyImmediate(rookObject);
                    
                    // Create the new rook at the destination
                    string rookName = (isWhitePiece ? "White" : "Black") + "Rook_" + rookToIndex;
                    GameObject newRook = Instantiate(rookPrefab, rookDestination, Quaternion.identity, PieceParent);
                    newRook.name = rookName;
                    
                    Debug.Log($"Castling: Moved rook from {BitboardUtils.IndexToAlgebraic(rookFromIndex)} to {BitboardUtils.IndexToAlgebraic(rookToIndex)}");
                } else {
                    // Create the rook at the destination anyway, since we know it should be there
                    Debug.Log($"Creating missing rook at destination {BitboardUtils.IndexToAlgebraic(rookToIndex)}");
                    
                    // Move the rook in the bitboard
                    Move rookMove = new Move(rookFromIndex, rookToIndex, null, (int)PieceType.Rook, isWhitePiece);
                    bitboard.UpdateBitBoard(rookMove);
                    
                    Vector3 rookDestination = GetWorldPositionForBit(rookToIndex);
                    GameObject rookPrefab = isWhitePiece ? whiteRookPrefab : blackRookPrefab;
                    string rookName = (isWhitePiece ? "White" : "Black") + "Rook_" + rookToIndex;
                    GameObject newRook = Instantiate(rookPrefab, rookDestination, Quaternion.identity, PieceParent);
                    newRook.name = rookName;
                }
            }
        }
        
        // Debug the board state after the move
        if (debugMode) DebugPrintBoard();
        
        // Check that the visual state matches the logical state
        VerifyBoardState();
    }
    private GameObject GetPrefabForPiece(PieceType pieceType, bool isWhite)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return isWhite ? whitePawnPrefab : blackPawnPrefab;
            case PieceType.Rook:
                return isWhite ? whiteRookPrefab : blackRookPrefab;
            case PieceType.Knight:
                return isWhite ? whiteKnightPrefab : blackKnightPrefab;
            case PieceType.Bishop:
                return isWhite ? whiteBishopPrefab : blackBishopPrefab;
            case PieceType.Queen:
                return isWhite ? whiteQueenPrefab : blackQueenPrefab;
            case PieceType.King:
                return isWhite ? whiteKingPrefab : blackKingPrefab;
            default:
                Debug.LogError("Unknown piece type: " + pieceType);
                return null;
        }
    }
    private string GetPieceTypeName(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn: return "Pawn";
            case PieceType.Rook: return "Rook";
            case PieceType.Knight: return "Knight";
            case PieceType.Bishop: return "Bishop";
            case PieceType.Queen: return "Queen";
            case PieceType.King: return "King";
            default: return "Unknown";
        }
    }
    private PieceType GetPieceTypeFromName(string pieceName)
    {
        if (pieceName.Contains("Pawn")) return PieceType.Pawn;
        if (pieceName.Contains("Rook")) return PieceType.Rook;
        if (pieceName.Contains("Knight")) return PieceType.Knight;
        if (pieceName.Contains("Bishop")) return PieceType.Bishop;
        if (pieceName.Contains("Queen")) return PieceType.Queen;
        if (pieceName.Contains("King")) return PieceType.King;
        
        Debug.LogError("Unknown piece name: " + pieceName);
        return PieceType.Pawn; // Default
    }
    private void VerifyBoardState()
    {
        // This will check that the visual pieces match the logical bitboard
        Debug.Log("Verifying board state consistency...");
        
        // Check all 64 squares
        for (int i = 0; i < 64; i++)
        {
            ulong bitMask = 1UL << i;
            
            // Check what's in the bitboard at this position
            bool hasPieceInBitboard = false;
            string expectedPiece = "";
            
            if ((bitboard.WhitePawn & bitMask) != 0)
            {
                hasPieceInBitboard = true;
                expectedPiece = "WhitePawn";
            }
            else if ((bitboard.WhiteRook & bitMask) != 0)
            {
                hasPieceInBitboard = true;
                expectedPiece = "WhiteRook";
            }
            else if ((bitboard.WhiteKnight & bitMask) != 0)
            {
                hasPieceInBitboard = true;
                expectedPiece = "WhiteKnight";
            }
            else if ((bitboard.WhiteBishop & bitMask) != 0)
            {
                hasPieceInBitboard = true;
                expectedPiece = "WhiteBishop";
            }
            else if ((bitboard.WhiteQueen & bitMask) != 0)
            {
                hasPieceInBitboard = true;
                expectedPiece = "WhiteQueen";
            }
            else if ((bitboard.WhiteKing & bitMask) != 0)
            {
                hasPieceInBitboard = true;
                expectedPiece = "WhiteKing";
            }
            else if ((bitboard.BlackPawn & bitMask) != 0)
            {
                hasPieceInBitboard = true;
                expectedPiece = "BlackPawn";
            }
            else if ((bitboard.BlackRook & bitMask) != 0)
            {
                hasPieceInBitboard = true;
                expectedPiece = "BlackRook";
            }
            else if ((bitboard.BlackKnight & bitMask) != 0)
            {
                hasPieceInBitboard = true;
                expectedPiece = "BlackKnight";
            }
            else if ((bitboard.BlackBishop & bitMask) != 0)
            {
                hasPieceInBitboard = true;
                expectedPiece = "BlackBishop";
            }
            else if ((bitboard.BlackQueen & bitMask) != 0)
            {
                hasPieceInBitboard = true;
                expectedPiece = "BlackQueen";
            }
            else if ((bitboard.BlackKing & bitMask) != 0)
            {
                hasPieceInBitboard = true;
                expectedPiece = "BlackKing";
            }
            
            // Check what GameObject is at this position
            GameObject pieceObject = FindPieceAtPosition(i);
            
            // Compare and fix any inconsistencies
            if (hasPieceInBitboard && pieceObject == null)
            {
                // Piece in bitboard but not on board visually
                Debug.LogError($"Inconsistency at {BitboardUtils.IndexToAlgebraic(i)}: Bitboard has {expectedPiece} but no visual piece exists");
                
                // Create the missing piece
                PieceType pieceType = GetPieceTypeFromName(expectedPiece);
                bool isWhite = expectedPiece.StartsWith("White");
                GameObject prefab = GetPrefabForPiece(pieceType, isWhite);
                
                if (prefab != null)
                {
                    GameObject newPiece = Instantiate(prefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
                    newPiece.name = expectedPiece + "_" + i;
                    Debug.Log($"Created missing piece {newPiece.name} at {BitboardUtils.IndexToAlgebraic(i)}");
                }
            }
            else if (!hasPieceInBitboard && pieceObject != null)
            {
                // Piece on board visually but not in bitboard
                Debug.LogError($"Inconsistency at {BitboardUtils.IndexToAlgebraic(i)}: Visual piece {pieceObject.name} exists but not in bitboard");
                
                // Remove the inconsistent piece
                DestroyImmediate(pieceObject);
                Debug.Log($"Removed inconsistent piece at {BitboardUtils.IndexToAlgebraic(i)}");
            }
            else if (hasPieceInBitboard && pieceObject != null)
            {
                // Both exist, but check if they match
                if (!pieceObject.name.StartsWith(expectedPiece))
                {
                    Debug.LogError($"Mismatch at {BitboardUtils.IndexToAlgebraic(i)}: Bitboard has {expectedPiece} but visual piece is {pieceObject.name}");
                    
                    // Fix the mismatch by replacing the piece
                    PieceType pieceType = GetPieceTypeFromName(expectedPiece);
                    bool isWhite = expectedPiece.StartsWith("White");
                    GameObject prefab = GetPrefabForPiece(pieceType, isWhite);
                    
                    if (prefab != null)
                    {
                        DestroyImmediate(pieceObject);
                        GameObject newPiece = Instantiate(prefab, GetWorldPositionForBit(i), Quaternion.identity, PieceParent);
                        newPiece.name = expectedPiece + "_" + i;
                        Debug.Log($"Replaced mismatched piece with {newPiece.name} at {BitboardUtils.IndexToAlgebraic(i)}");
                    }
                }
            }
        }
        
        Debug.Log("Board state verification complete");
    }
    
    // Convert a bitboard index to a world position
    Vector3 GetWorldPositionForBit(int bitIndex)
    {
        // In the bitboard:
        // - Bit 0 is a1 (bottom-left from white's perspective)
        // - Bit 63 is h8 (top-right from white's perspective)
        int file = bitIndex % 8;            // 0-7 (a-h files)
        int rank = bitIndex / 8;            // 0-7 (1-8 ranks)
    
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