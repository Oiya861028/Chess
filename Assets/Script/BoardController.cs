using UnityEngine;

public class BoardController : MonoBehaviour 
{
//The purpose of a BoardController is 
//- To initialize game board and pieces
//- To convert AI mvoes into actual moves on the board
//- To detect whether the color is AI or not, and if it is, to call the AI to get the best move

    // Final Vector3 origin for A1 on board
    public Vector3 boardOrigin = new Vector3(0f, 0f, 0f);

    // Grid size for each square (should be 1 by 1 unless board is scaled)
    public float gridSize = 1f;

    //Prefabs for the game
    public GameObject boardPrefab;
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

    // Game state tracking
    private int plyCount = 0;  // Even: white's turn, odd: black's turn
    public bool whiteIsAI = false; // Set to true if white is controlled by AI
    public bool blackIsAI = true;  // Set to true if black is controlled by AI

    // Reference to the AI component (StockFridge)
    public StockFridge stockFridge;

    // Reference to the ChessBoard component with bitboard info
    private BitBoard bitboards;

    void Start()
    {
        

        //TODO: Add the initialization function and change the way bitboard is referenced in the script so that it follows the new class rule
        bitboards = new BitBoard();
        // Initialize AI if necessary
        if (whiteIsAI || blackIsAI)
        {
            stockFridge.InitializeAI(plyCount % 2 == 0);
        }
    }

    void Update()
    {
        DetectInput();
        CheckTurn();
    }

    //Initialization
    void InstantiateBoard()
    {
        Instantiate(boardPrefab, boardOrigin, Quaternion.identity);
    }
    void InstantiatePieces() {
        ulong[] pieceLocation = bitboards.GetBitBoards();
        for (int i = 0; i < 64; i++)
        {
            if ((pieceLocation[(int)PieceType.WhitePawn] & (1UL << i)) != 0)
            Instantiate(whitePawnPrefab, ConvertBitIndexToWorldPosition(i), Quaternion.identity);
            if ((pieceLocation[(int)PieceType.WhiteRook] & (1UL << i)) != 0)
            Instantiate(whiteRookPrefab, ConvertBitIndexToWorldPosition(i), Quaternion.identity);
            if ((pieceLocation[(int)PieceType.WhiteKnight] & (1UL << i)) != 0)
            Instantiate(whiteKnightPrefab, ConvertBitIndexToWorldPosition(i), Quaternion.identity);
            if ((pieceLocation[(int)PieceType.WhiteBishop] & (1UL << i)) != 0)
            Instantiate(whiteBishopPrefab, ConvertBitIndexToWorldPosition(i), Quaternion.identity);
            if ((pieceLocation[(int)PieceType.WhiteQueen] & (1UL << i)) != 0)
            Instantiate(whiteQueenPrefab, ConvertBitIndexToWorldPosition(i), Quaternion.identity);
            if ((pieceLocation[(int)PieceType.WhiteKing] & (1UL << i)) != 0)
            Instantiate(whiteKingPrefab, ConvertBitIndexToWorldPosition(i), Quaternion.identity);
            if ((pieceLocation[(int)PieceType.BlackPawn] & (1UL << i)) != 0)
            Instantiate(blackPawnPrefab, ConvertBitIndexToWorldPosition(i), Quaternion.identity);
            if ((pieceLocation[(int)PieceType.BlackRook] & (1UL << i)) != 0)
            Instantiate(blackRookPrefab, ConvertBitIndexToWorldPosition(i), Quaternion.identity);
            if ((pieceLocation[(int)PieceType.BlackKnight] & (1UL << i)) != 0)
            Instantiate(blackKnightPrefab, ConvertBitIndexToWorldPosition(i), Quaternion.identity);
            if ((pieceLocation[(int)PieceType.BlackBishop] & (1UL << i)) != 0)
            Instantiate(blackBishopPrefab, ConvertBitIndexToWorldPosition(i), Quaternion.identity);
            if ((pieceLocation[(int)PieceType.BlackQueen] & (1UL << i)) != 0)
            Instantiate(blackQueenPrefab, ConvertBitIndexToWorldPosition(i), Quaternion.identity);
            if ((pieceLocation[(int)PieceType.BlackKing] & (1UL << i)) != 0)
            Instantiate(blackKingPrefab, ConvertBitIndexToWorldPosition(i), Quaternion.identity);
        }
    }

    
    
    void InitializeAI()
    {
        if (stockFridge == null)
        {
            Debug.LogError("StockFridge AI component not assigned!");
            return;
        }
        // Initialize StockFridge AI as needed
        stockFridge.Initialize();
    }

    void DetectInput()
    {
        // TODO: Implement input detection logic
        // For example:
        // - Use Raycast to detect piece hovering over with mouse
        // - Highlight possible moves for selected piece
    }

    void CheckTurn()
    {
        // If it's an AI turn, call StockFridge to get the best move
        if ((plyCount % 2 == 0 && whiteIsAI) || (plyCount % 2 == 1 && blackIsAI))
        {
            // Retrieve the current board bitboards (this may require exposing them from ChessBoard)
            // TODO: Replace 'currentBitboard' placeholder with actual data
            ulong[] currentBitboard = GetCurrentBitboard();

            // Get best move from the AI (passes bitboard and turn)
            Move bestMove = stockFridge.FindBestMove(currentBitboard, plyCount);

            // Update board state using the move from the AI
            UpdateBoard(bestMove);
        }
    }

    ulong[] GetCurrentBitboard()
    {
        // TODO: Construct and return a ulong array representing the current bitboards.
        // This can be achieved by having ChessBoard expose its bitboards via properties or methods.
        return new ulong[0];
    }

    void UpdateBoard(Move move)
    {
        // TODO: Update the board's bitboards and visual positions based on the selected move.
        // This might involve updating the ChessBoard instance and repositioning pieces in the world.

        // Increase move counter after a move has been made
        plyCount++;
    }

    // Optionally, a method to convert bit index to world position if not using ChessBoard's version
    Vector3 ConvertBitIndexToWorldPosition(int bitIndex)
    {
        int file = bitIndex % 8;
        int rank = bitIndex / 8;
        return boardOrigin + new Vector3(file * gridSize, 0f, rank * gridSize);
    }
}
