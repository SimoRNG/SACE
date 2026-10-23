using SACE_Core.Utils;
using Xunit;

namespace SACE_Tests;

public class BitOperationsTests
{
    [Theory]
    [InlineData(0UL, 0, true, 1UL)]       // Imposta bit 0
    [InlineData(0UL, 63, true, 0x8000000000000000UL)] // Imposta bit 63
    [InlineData(0xFFUL, 0, false, 0xFEUL)] // Rimuove bit 0
    public void SetBit_ShouldModifyBitboardCorrectly(ulong initial, int pos, bool val, ulong expected)
    {
        BitUtils.SetBitToValue(ref initial, pos, val);
        Assert.Equal(expected, initial);
    }

    [Theory]
    [InlineData(0b1010, 1, true)]
    [InlineData(0b1010, 0, false)]
    [InlineData(0x8000000000000000UL, 63, true)]
    public void GetBit_ShouldReturnCorrectValue(ulong bitboard, int pos, bool expected)
    {
        bool result = BitUtils.GetBit(bitboard, pos);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0UL, 0)]
    [InlineData(0b1011, 3)]
    [InlineData(0xFFFFFFFFFFFFFFFFUL, 64)]
    public void CountBits_ShouldReturnCorrectCount(ulong bitboard, int expected)
    {
        int count = BitUtils.CountBits(bitboard);
        Assert.Equal(expected, count);
    }

    [Fact]
    public void PopLSB_ShouldReturnIndexAndClearBit()
    {
        ulong bitboard = 0b10100; // Bit a 2 e 4

        int firstPop = BitUtils.PopLSB(ref bitboard);
        Assert.Equal(2, firstPop);
        Assert.Equal(0b10000UL, bitboard);

        int secondPop = BitUtils.PopLSB(ref bitboard);
        Assert.Equal(4, secondPop);
        Assert.Equal(0UL, bitboard);
    }
}
