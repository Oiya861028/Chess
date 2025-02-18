using UnityEngine;

public class BoardController : MonoBehaviour
//The purpose of a BoardController is 
//- To initialize game board and pieces
//- To convert AI mvoes into actual moves on the board
//- To detect whether the color is AI or not, and if it is, to call the AI to get the best move
{
    // Final Vector3 origin for A1 on board
    public Vector3 boardOrigin = new Vector3(0f, 0f, 0f);

    // Grid size for each square (should be 1 by 1 unless board is scaled)
    public float gridSize = 1f;

    //Prefabs for the game
    public GameObject boardPrefab;
    public GameObject[] piecePrefabs = [
        whitePawnPrefab,
        whiteRookPrefab,
        whiteKnightPrefab,
        whiteBishopPrefab,
        whiteQueenPrefab,
        whiteKingPrefab,
        blackPawnPrefab,
        blackRookPrefab,
        blackKnightPrefab,
        blackBishopPrefab,
        blackQueenPrefab,
        blackKingPrefab
    ]; // Array of piece prefabs for white and black
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

        // Initialize AI if necessary
        if ((whiteIsAI && (plyCount % 2 == 0)) || (blackIsAI && (plyCount % 2 == 1)))
        {
            InitializeAI();
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