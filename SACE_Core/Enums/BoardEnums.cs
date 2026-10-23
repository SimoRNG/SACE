namespace SACE_Core.Enums;

public enum PieceColor
{
    PC_WHITE = 0,
    PC_BLACK = 1
}

public enum PieceType
{
    PT_PAWN,
    PT_KNIGHT,
    PT_BISHOP,
    PT_ROOK,
    PT_QUEEN,
    PT_KING
}

//public enum PieceValue
//{
//    PV_PAWN = 100,
//    PV_KNIGHT = 300,
//    PV_BISHOP = 300,
//    PV_ROOK = 500,
//    PV_QUEEN = 900,
//    PV_KING = int.MaxValue
//}

public enum GamePhase
{
    GP_OPENING,
    GP_MIDDLEGAME,
    GP_ENDGAME
}
