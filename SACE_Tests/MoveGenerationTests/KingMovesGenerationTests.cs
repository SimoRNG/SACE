using SACE_Core.Classes;
using SACE_Core.Enums;
using SACE_Core.Utils;

namespace SACE_Tests.MoveGenerationTests;

public class KingMovesGenerationTests
{
    [Theory] // Test per il re bianco
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 0)]         // Posizione iniziale
    [InlineData("rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 1 2", 1)]   // Solo una casella disponibile       
    [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -", 4)] // Entrambi gli arrocchi disponibili       
    [InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 4)]        // Arrocco corto, presa del cavallo nero e due case libere    
    [InlineData("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 2)]        // Re sotto scacco! (in questo test non mi interessa)
    public void KingMovesGen_PseudoMoves_ShouldReturnCorrectAmount(string fenPosition, int expected) // Test per Pseudo mosse
    {
        // Arrange
        Board b = new Board(fenPosition);
        List<Move> moves = new List<Move>();
        PieceColor us = b.SideToMove;
        PieceColor them = (us == PieceColor.PC_WHITE) ? PieceColor.PC_BLACK : PieceColor.PC_WHITE;

        ulong friendlyPieces = b.GetAllPiecesBitboard(us);
        ulong enemyPieces = b.GetAllPiecesBitboard(them);

        // Act
        MoveGenerator.GenerateKingMoves(b, moves, PieceColor.PC_WHITE, friendlyPieces, enemyPieces);

        // Assert
        Assert.Equal(moves.Count, expected);
    }


    [Theory]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 0)]         // Posizione iniziale
    [InlineData("rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 1 2", 1)]
    [InlineData("8/8/KPpp4/7r/1R3p1k/8/4P1P1/8 w - - 0 2", 2)] // Il re non deve andare nella linea di tiro della torre
    [InlineData("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 1)]        // Re sotto scacco! 
    [InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 4)] // Arrocco corto, presa del cavallo nero e due case libere
    public void KingMovesGen_LegalMoves_ShouldReturnCorrectAmount(string fenPosition, int expected)
    {
        // Arrange
        Board b = new Board(fenPosition);
        int kingMovesCount = 0;

        // Act
        List<Move> allLegalMoves = MoveGenerator.GetLegalMoves(b);
        foreach (var move in allLegalMoves)
        {
            if (b.GetPieceAtSquare(move.From) == PieceType.PT_KING)
            {
                kingMovesCount++;
            }
        }

        // Assert
        Assert.Equal(expected, kingMovesCount);
    }
}
