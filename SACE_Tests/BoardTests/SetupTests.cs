using SACE_Core.Classes;
using SACE_Core.Enums;
using SACE_Core.Utils;

namespace SACE_Tests.BoardTests;

public class SetupTests
{

    [Theory]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", true)]     // Posizione di partenza
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPPP/RNBQKBNR w KQkq - 0 1", false)]   // Stringa errata (un carattere di più) 
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPP/RNBQKBNR w KQkq - 0 1", false)]     // Stringa errata (manca un carattere)
    [InlineData("rnbqkbnr/ppkppppp/8/8/8/8/PPPPPPP/RNBQKBNR w KQkq - 0 1", false)]     // Stringa errata (ci sono due re neri)
    [InlineData("rnbqbbnr/ppkppppp/8/8/8/8/PPPPPPP/RNBQKBNR w KQkq - 0 1", false)]     // Stringa errata (non c'è re nero)
    //Posizioni da https://www.chessprogramming.org/Perft_Results (tutte valide)
    [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -", true)]
    [InlineData("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", true)]
    [InlineData("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", true)]
    [InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", true)]
    public void IsValidFen_ShouldCheckCorrectly(string fen, bool expected)
    {
        // Arrange
        bool fenCheck = false;

        // Act
        fenCheck = ChessUtils.IsValidFen(fen);

        // Assert
        Assert.True(fenCheck == expected);
    }


    [Theory] //Posizioni da https://www.chessprogramming.org/Perft_Results
    // Posizione di partenza
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                0x000000000000FFFFUL,
                0xFFFF000000000000UL)]

    // Posizione 3
    [InlineData("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1",
                0x0000000302005000UL,
                0x00040880A0000000UL)]

    public void BoardSetup_FENStringShouldSetupPiecesCorrectly(string fen, ulong whitePieces, ulong blackPieces)
    {
        // Arrange - Act
        Board b = new Board(fen);

        //ChessUtils.PrintOccupiedSquares(b);

        // Assert
        Assert.Equal(b.GetAllPiecesBitboard(PieceColor.PC_WHITE), whitePieces);
        Assert.Equal(b.GetAllPiecesBitboard(PieceColor.PC_BLACK), blackPieces);
    }
}
