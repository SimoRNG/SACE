using SACE_Core.Classes;
using SACE_Core.Enums;
using SACE_Core.Utils;

namespace SACE_Tests.MoveGenerationTests;

public class RookMovesGenerationTests
{
    [Theory] // Test per le torri del bianco
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 0)]         // Posizione iniziale
    [InlineData("rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 1 2", 1)]
    [InlineData("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 8)] // Anche una cattura 
    [InlineData("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 4)]    // Non tengo conto del re sotto scacco
    [InlineData("r1b1k1nr/1pqp2p1/2nb1p2/pPpPp1Pp/P1P4P/8/4PP2/RNBQKBNR w KQkq a6 0 10", 4)]
    public void RookMovesGen_PseudoMoves_ShouldReturnCorrectAmount(string fenPosition, int expected) // Test per TUTTE le pseudo mosse
    {
        // Arrange
        Board b = new Board(fenPosition);
        List<Move> moves = new List<Move>();
        PieceColor us = b.SideToMove;
        PieceColor them = (us == PieceColor.PC_WHITE) ? PieceColor.PC_BLACK : PieceColor.PC_WHITE;

        ulong friendlyPieces = b.GetAllPiecesBitboard(us);
        ulong enemyPieces = b.GetAllPiecesBitboard(them);

        ChessUtils.PrintOccupiedSquares(b);

        // Act
        MoveGenerator.GenerateSlidingMoves(b, moves, PieceType.PT_ROOK, PieceColor.PC_WHITE, friendlyPieces, enemyPieces);

        // Assert
        Assert.Equal(moves.Count, expected);
    }

    [Theory]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 0)]         // Posizione iniziale
    [InlineData("rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 1 2", 1)]
    [InlineData("8/8/KPpp4/7r/1R3p1k/8/4P1P1/8 w - - 0 2", 9)] 
    [InlineData("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 1)]        // Re sotto scacco - la torre può bloccare 
    [InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 2)] // L'arrocco è una mossa del re, non va contata
    public void RookMovesGen_LegalMoves_ShouldReturnCorrectAmount(string fenPosition, int expected)
    {
        // Arrange
        Board b = new Board(fenPosition);
        int rookMovesCount = 0;

        // Act
        List<Move> allLegalMoves = MoveGenerator.GetLegalMoves(b);
        foreach (var move in allLegalMoves)
        {
            if (b.GetPieceAtSquare(move.From) == PieceType.PT_ROOK)
            {
                rookMovesCount++;
            }
        }

        // Assert
        Assert.Equal(expected, rookMovesCount);
    }
}
