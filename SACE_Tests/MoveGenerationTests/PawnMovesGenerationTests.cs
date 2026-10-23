using SACE_Core.Classes;
using SACE_Core.Enums;
using SACE_Core.Utils;

namespace SACE_Tests.MoveGenerationTests;

public class PawnMovesGenerationTests
{
    [Theory] // Test per i pedoni del bianco
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 16)]         // Posizione iniziale
    [InlineData("rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 1 2", 13)]         
    [InlineData("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 5)]         
    [InlineData("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 8)]        // Re sotto scacco! (ignorato in questo test)
    [InlineData("r1b1k1nr/1pqp2p1/2nb1p2/pPpPp1Pp/P1P4P/8/4PP2/RNBQKBNR w KQkq a6 0 10", 10)]        // En passant disponibile in a6  
    public void PawnMovesGen_PseudoMoves_ShouldReturnCorrectAmount(string fenPosition, int expected) // Test per TUTTE le pseudo mosse
    {
        // Arrange
        Board b = new Board(fenPosition);
        List<Move> moves = new List<Move>();
        PieceColor us = b.SideToMove;
        PieceColor them = (us == PieceColor.PC_WHITE) ? PieceColor.PC_BLACK : PieceColor.PC_WHITE;

        //ulong friendlyPieces = b.GetAllPiecesBitboard(us);
        ulong enemyPieces = b.GetAllPiecesBitboard(them);
        ulong occupied = b.GetOccupied();

        ChessUtils.PrintOccupiedSquares(b);

        // Act
        MoveGenerator.GeneratePawnMoves(b, moves, PieceColor.PC_WHITE, occupied, enemyPieces);

        // Assert
        Assert.Equal(moves.Count, expected);
    }


    [Theory]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 16)]         // Posizione iniziale
    [InlineData("rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 1 2", 13)]
    [InlineData("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 4)] // Un npedone è inchiodato
    [InlineData("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 2)]        // Re sotto scacco! Due pedoni possono bloccare
    [InlineData("r1b1k1nr/1pqp2p1/2nb1p2/pPpPp1Pp/P1P4P/8/4PP2/RNBQKBNR w KQkq a6 0 10", 10)]        // En passant disponibile in a6  
    public void PawnMovesGen_LegalMoves_ShouldReturnCorrectAmount(string fenPosition, int expected)
    {
        // Arrange
        Board b = new Board(fenPosition);
        int pawnMovesCount = 0;

        // Act
        List<Move> allLegalMoves = MoveGenerator.GetLegalMoves(b);
        foreach (var move in allLegalMoves)
        {
            if (b.GetPieceAtSquare(move.From) == PieceType.PT_PAWN)
            {
                pawnMovesCount++;
            }
        }

        // Assert
        Assert.Equal(expected, pawnMovesCount);
    }
}
