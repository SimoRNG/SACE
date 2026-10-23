using SACE_Core.Enums;

namespace SACE_Core.Classes;
/*
 * Struttura della mossa:
 * Ogni mossa è rappresentata da un solo valore ushort dove:
 * bit dalla posizione 0 alla 5 == indice della casa da cui parto (rappresenta un valore da 0 a 63)
 * (6 - 11) == indice della casa di arrivo (come sopra)
 * (12 - 15) == altri 4 bit per dei flag, in particolare:
 * 0000 : mossa normale
 * 0001 : doppia spinta di pedone (dalla casa iniziale)
 * 0010 : arrocco corto
 * 0011 : arrocco lungo
 * 0100 : cattura
 * 0101 : en passant
 * 
 */
internal readonly struct Move
{
    private readonly ushort _moveData;

    // Maschere per estrarre i dati
    private const ushort FromMask = 0x3F;       // 0000 0000 0011 1111 (Primi 6 bit)
    private const ushort ToMask = 0xFC0;        // 0000 1111 1100 0000 (Bit 6-11)
    private const ushort FlagMask = 0xF000;     // 1111 0000 0000 0000 (Ultimi 4 bit)

    public Move(int from, int to, MoveFlag flags)
    {
        _moveData = (ushort)(from | (to << 6) | ((int)flags << 12));
    }

    public int From => _moveData & FromMask;
    public int To => (_moveData & ToMask) >> 6;
    public MoveFlag Flag => (MoveFlag)((_moveData & FlagMask) >> 12);

    public override string ToString() => $"Mossa da: {From} a {To} (Flag: {Flag})";
}

/// <summary>
/// Rappresenta i tipi speciali di mossa usando 4 bit (0-15)
/// </summary>
internal enum MoveFlag : byte
{
    Quiet = 0,             // 0000
    DoublePawnPush = 1,    // 0001
    ShortCastle = 2,        // 0010 
    LongCastle = 3,       // 0011 
    Capture = 4,           // 0100
    EnPassant = 5,         // 0101

    // Promozioni "Silenziose" (il pedone avanza e promuove in una casa vuota)
    PromoteToQueen = 8,
    PromoteToRook = 9,
    PromoteToBishop = 10,
    PromoteToKnight = 11,

    // Promozioni con "Cattura" (il pedone mangia un pezzo e promuove)
    PromoteToQueenCapture = 12,
    PromoteToRookCapture = 13,
    PromoteToBishopCapture = 14,
    PromoteToKnightCapture = 15
}


// Struct usata per fare 'undo' di una mossa
internal struct UndoInfo
{
    public MoveFlag EnPassantFlag; // Per ripristinare EnPassantTarget
    public ulong EnPassantTarget;
    public bool WhiteShortCastle, WhiteLongCastle, BlackShortCastle, BlackLongCastle;
    public int HalfMoves;
    public PieceType CapturedPiece; // Necessaria per UnmakeMove
}

