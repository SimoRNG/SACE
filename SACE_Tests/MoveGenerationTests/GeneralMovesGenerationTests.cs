using SACE_Core.Classes;
using SACE_Core.Enums;

namespace SACE_Tests.MoveGenerationTests;

public class GeneralMovesGenerationTests
{
    [Theory]
    [InlineData(1, 48)]
    [InlineData(2, 2039)]
    [InlineData(3, 97862)]
    [InlineData(4, 4085603)]
    //[InlineData(5, 193690690)] - Qua chiedo troppo per ora
    public void AllPiecesLegalMoves_Kiwipete_ShouldReturnCorrectAmountOfMovesAtGivenDepth(int depth, int expected)
    {
        // Arrange
        string fenPosition = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
        Board b = new Board(fenPosition);

        // Act
        long actualNodes = MoveGenerator.Perft(b, depth);

        // Assert
        Assert.Equal((long)expected, actualNodes);
    }

    [Theory]
    [InlineData(1, 44)]
    [InlineData(2, 1486)]
    [InlineData(3, 62379)]
    [InlineData(4, 2103487)]
    public void AllPiecesLegalMoves_Position5_ShouldReturnCorrectAmountOfMovesAtGivenDepth(int depth, int expected)
    {
        // Arrange
        string fenPosition = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
        Board b = new Board(fenPosition);

        // Act
        long actualNodes = MoveGenerator.Perft(b, depth);

        // Assert
        Assert.Equal((long)expected, actualNodes);
    }
}
