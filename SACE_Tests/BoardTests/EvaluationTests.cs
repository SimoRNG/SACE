using SACE_Core.Classes;
using SACE_Core.Enums;
using SACE_Core.Utils;
using System.Diagnostics;

namespace SACE_Tests.BoardTests;

public class EvaluationTests
{
    [Theory] //Posizioni inventate
             // Propongo posizioni in cui uno dei due colori sta vincendo
    [InlineData("8/5k2/8/8/3Q4/2K5/8/8 w - - 0 1", PieceColor.PC_WHITE)]
    [InlineData("8/4k3/8/8/8/2KQ4/6Q1/8 w - - 0 1", PieceColor.PC_WHITE)]
    [InlineData("8/4k3/3q1q2/4q3/8/2K5/8/8 w - - 0 1", PieceColor.PC_BLACK)]
    [InlineData("r2qkbnr/2pppQpp/8/8/8/8/PPP3PP/RNBQKQ1R w KQkq - 0 1", PieceColor.PC_WHITE)]
    public void StaticPositionEvaluation_ShouldUnderstandWhosWinning(string fen, PieceColor expected)
    {
        // Arrange
        Board b = new Board(fen);
        PieceColor currentWinner;

        // Act
        currentWinner = BoardEvaluation.Evaluate(b) > 0 ? PieceColor.PC_WHITE : PieceColor.PC_BLACK;

        // Assert
        Assert.Equal(currentWinner, expected);
    }


    [Theory]  // Propongo posizioni in cui uno dei due colori sta vincendo, ma bisogna scavare un po'
    [InlineData("8/8/8/8/8/8/1KP2pk1/8 w - - 0 1", PieceColor.PC_BLACK)]
    [InlineData("8/8/8/2PB4/8/5r2/1K4k1/8 w - - 0 1", PieceColor.PC_WHITE)]
    [InlineData("2rq1rk1/pb3pp1/5n1p/2Qp4/8/4P1P1/PP1N1PBP/2R2RK1 w - - 1 19", PieceColor.PC_WHITE)]
    [InlineData("r1bqk1nr/pp3pbp/2n3p1/2p1p1N1/2BpPP2/P5N1/1PPP2PP/R1BQK2R b KQkq - 1 9", PieceColor.PC_BLACK)]
    [InlineData("r1bqr1k1/pp3pbp/1n2p1p1/3pP2n/2pP4/2P2N1P/PPBN1PPB/R2Q1RK1 b - - 0 13", PieceColor.PC_BLACK)]
    [InlineData("r4rk1/1p3ppp/p1nbpq2/3p1bB1/NP1Pn3/PQP2N2/4BPPP/R4RK1 b - - 4 13", PieceColor.PC_BLACK)]
    public void PositionEvaluation_ShouldUnderstandWhosWinning(string fen, PieceColor expected)
    {
        // Arrange
        Board b = new Board(fen);
        PieceColor currentWinner = PieceColor.PC_WHITE;
        int depth = 4;
        int absoluteScore = 0;

        // Act
        int score = SearchEngine.AlphaBeta(b, depth, -1000000, 1000000);
        absoluteScore = (b.SideToMove == PieceColor.PC_WHITE) ? score : -score; // Se tocca al nero il punteggio va invertito per essere confrontato con lo standard "Bianco = Positivo"

        if (absoluteScore > 0)
            currentWinner = PieceColor.PC_WHITE;
        else if (absoluteScore < 0)
            currentWinner = PieceColor.PC_BLACK;
        else
            Assert.True(false); // In questo test uno dei due pezzi sta vincendo

        //Debug.WriteLine("Punteggio calcolato: " + score.ToString());
        //Debug.WriteLine($"FEN: {fen}");
        //Move bestMove = SearchEngine.GetBestMove(b, depth);
        //Debug.WriteLine($"Migliore mossa trovata: {ChessUtils.IndexToSquareName(bestMove.From)}{ChessUtils.IndexToSquareName(bestMove.To)}");
        //Debug.WriteLine($"Score Assoluto (Bianco +): {absoluteScore}");

        // Assert
        Assert.Equal(currentWinner, expected);
    }


    [Theory]
    [InlineData("8/8/8/8/8/8/1KP2pk1/8 w - - 0 1", 5, 1000)] // Matto in x mosse secondo stockfish
    [InlineData("8/8/8/2PB4/8/5r2/1K4k1/8 w - - 0 1", -5, 1000)] // pure qui
    [InlineData("2rq1rk1/pb3pp1/5n1p/2Qp4/8/4P1P1/PP1N1PBP/2R2RK1 w - - 1 19", 3, 2)]
    [InlineData("r1bqk1nr/pp3pbp/2n3p1/2p1p1N1/2BpPP2/P5N1/1PPP2PP/R1BQK2R b KQkq - 1 9", -15, 3)]
    [InlineData("r1bqr1k1/pp3pbp/1n2p1p1/3pP2n/2pP4/2P2N1P/PPBN1PPB/R2Q1RK1 b - - 0 13", 2.5, 2)]
    [InlineData("r4rk1/1p3ppp/p1nbpq2/3p1bB1/NP1Pn3/PQP2N2/4BPPP/R4RK1 b - - 4 13", -5, 3)] // un pezzo gratis dopo un cambio per il nero
    public void PositionEvaluation_ShouldEvaluateWithinRange(string fen, double expected = 0, double marginOfError = 99999999)
    {
        // Arrange
        Board b = new Board(fen);
        int depth = 4;
        int absoluteScore = 0;

        // Act
        marginOfError = Math.Abs(marginOfError);
        int score = SearchEngine.AlphaBeta(b, depth, -1000000, 1000000);
        absoluteScore = (b.SideToMove == PieceColor.PC_WHITE) ? score : -score; // Se tocca al nero il punteggio va invertito per essere confrontato con lo standard "Bianco = Positivo"
        double resultEval = (double)absoluteScore / 1000;

        Debug.WriteLine(resultEval);

        // Assert
        Assert.InRange(resultEval, expected - marginOfError, expected + marginOfError);
    }
}
