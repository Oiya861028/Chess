using System;

public class StockFridge { 
    private bool color; // True for white, false for black
    public void initialize(bool color){
        this.color = color;
    }
    public Move FindBestMove(ulong[] currentBitboard, int plyCount) {
        if(plyCount % 2 == 0) {
            return NegaMax(currentBitboard, -Math.Infinity(), Math.Infinity(), plyCount, true);
        } else {
            return NegaMax(currentBitboard, plyCount, false);
        }
    }
    public int NegaMax(int depth, int alpha, int beta, bool isMaximizing) {
        if (depth == 0 | GameOver()) {
            return EvaluateBoard();
        }

        int maxEval = int.MinValue;
        var moves = GenerateMoves(isMaximizing);

        foreach (var move in moves) {
            MakeMove(move);
            int eval = -NegaMax(depth - 1, -beta, -alpha, !isMaximizing);
            UndoMove(move);
            maxEval = Math.Max(maxEval, eval);
            alpha = Math.Max(alpha, eval);
            if (alpha >= beta) {
                break;
            }
        }

        return maxEval;
    }
}