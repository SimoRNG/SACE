using System.Numerics;

namespace SACE_Core.Utils;

internal static class BitUtils
{
    /// <summary>
    /// Imposta il bit alla posizione pos al valore indicato
    /// </summary>
    public static void SetBitToValue(ref ulong bitboard, int pos, bool value = true)
    {
        if (value)
            bitboard |= (1UL << pos);
        else
            bitboard &= ~(1UL << pos);
    }

    /// <summary>
    /// Ritorna il valore del bit alla posizione pos
    /// </summary>
    public static bool GetBit(ulong bitboard, int pos)
    {
        return ((bitboard >> pos) & 1UL) != 0;
    }


    public static int CountBits(ulong bitboard)
    {
        return BitOperations.PopCount(bitboard);
    }

    /// <summary>
    /// Trova l'indice del primo bit a 1 (LSB) e lo rimuove
    /// </summary>
    public static int PopLSB(ref ulong bitboard)
    {
        int i = BitOperations.TrailingZeroCount(bitboard);
        bitboard &= (bitboard - 1);
        return i;
    }

    public static int GetLSB(ulong bb)
    {
        // Equivalente a PopLSB ma senza modificare il valore
        return BitOperations.TrailingZeroCount(bb);
    }


    /// <summary>
    /// Uno ulong ha 64 bit, l'indice deve essere compreso tra 0 e 63
    /// </summary>
    public static bool IsUlongIndexValid(short index)
    {
        if (index < 0 || index > 63) return false;
        else return true;
    }

    /// <summary>
    /// Dato un bitboard, ritorna true se il bit a quell'indice è 1
    /// </summary>
    public static bool IsBit1(ulong bitboard, int pos)
    {
        ulong mask = 1UL << pos;

        return ((bitboard & mask) != 0);
    }


    /// <summary>
    /// Ritorna un bitboard con impostato a 1 il bit della casa corrispondente
    /// </summary>
    public static ulong IndexToBitBoard(short index)
    {
        if (IsUlongIndexValid(index))
        {
            return (ulong)1 << index;
        }

        return 0;
    }

    

    /// <summary>
    /// Ritorna l'indice della casa a partire da uno ulong che rappresenta un bitboard
    /// </summary>
    /// <param name="bb">Il bitboard deve avere solo un bit impostato ad 1</param>
    /// <returns>-1 se il bitboard non è valido</returns>
    public static short BitBoardToIndex(ulong bb)
    {
        if (bb == 0 || (bb & (bb - 1)) != 0) return -1;

        return (short)BitOperations.TrailingZeroCount(bb);
    }


}
