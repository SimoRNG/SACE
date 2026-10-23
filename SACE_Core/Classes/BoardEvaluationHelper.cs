using SACE_Core.Enums;

namespace SACE_Core.Classes;

internal static class BoardEvaluationHelper
{
    #region Maschere posizioni - Piece Square Tables

    /*
    public static readonly int[] BasePST = {
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0
    };
    */ // Base per copia incolla

    // Bonus/Malus per i Cavalli: incoraggio il controllo del centro e scoraggio ad andare sui bordi
    public static readonly int[] KnightPST = {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50,
    };

    // Bonus per i Pedoni: più avanzano, più valgono (specialmente al centro)
    public static readonly int[] PawnPST = {
         0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
         5,  5, 10, 25, 25, 10,  5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5, -5,-10,  0,  0,-10, -5,  5,
         5, 10, 10,-20,-20, 10, 10,  5,
         0,  0,  0,  0,  0,  0,  0,  0
    };

    // Il Re deve stare protetto (angoli) in apertura, ma al centro in finale
    public static readonly int[] KingOpeningPST = {
        -30, -40, -40, -50, -50, -40, -40, -30,
        -30, -40, -40, -50, -50, -40, -40, -30,
        -30, -40, -40, -50, -50, -40, -40, -30,
        -30, -40, -40, -50, -50, -40, -40, -30,
        -20, -30, -30, -40, -40, -30, -30, -20,
        -10, -20, -20, -20, -20, -20, -20, -10,
         20,  20,   0,   0,   0,   0,  20,  20,
         20,  30,  10,   0,   0,  10,  30,  20
    };
    public static readonly int[] KingEndgamePST = {
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0
    };

    public static int GetPstValue(int[] pst, int square, PieceColor color)
    {
        // Se è nero, invertiamo la scacchiera verticalmente
        return (color == PieceColor.PC_WHITE) ? pst[square] : pst[square ^ 56];
    }
    #endregion



    #region Pezzi che attaccano cose
    public static (PieceColor color, PieceType type)? GetSmallestAttacker(Board board, int sq, PieceColor attackerColor)
    {
        // Ritorno il pezzo di meno valore che attacca una casa se esiste, altrimenti null

        int side = (int)attackerColor;

        // Pedoni
        if (IsAttackedByPawn(board, sq, attackerColor))
            return (attackerColor, PieceType.PT_PAWN);

        // Cavalli
        if ((MoveGenerator.KnightMoves[sq] & board.PieceBitboards[side][(int)PieceType.PT_KNIGHT]) != 0)
            return (attackerColor, PieceType.PT_KNIGHT);

        // Alfieri
        if (IsAttackedBySliding(board, sq, PieceType.PT_BISHOP, attackerColor, board.GetOccupied()))
            return (attackerColor, PieceType.PT_BISHOP);

        // Torri
        if (IsAttackedBySliding(board, sq, PieceType.PT_ROOK, attackerColor, board.GetOccupied()))
            return (attackerColor, PieceType.PT_ROOK);

        // Regine
        if (IsAttackedBySliding(board, sq, PieceType.PT_QUEEN, attackerColor, board.GetOccupied()))
            return (attackerColor, PieceType.PT_QUEEN);

        // Re
        if ((MoveGenerator.KingMoves[sq] & board.PieceBitboards[side][(int)PieceType.PT_KING]) != 0)
            return (attackerColor, PieceType.PT_KING);

        return null;
    }


    /*
     * Parto dalla casa sq e proietto dei raggi nelle direzioni consentite 
     * (diagonali per l'Alfiere ed orizzontali/verticali per la Torre) 
     * Se il primo pezzo che incontriamo è un pezzo nemico del tipo giusto allora la casa è attaccata.
     * 
     * Questa cosa è fatta meglio con i magic bitboard - //TODO
     */
    public static bool IsAttackedBySliding(Board board, int sq, PieceType type, PieceColor attackerColor, ulong occupied)
    {
        // Direzioni
        int[] directions = (type == PieceType.PT_BISHOP)
            ? new int[] { 7, 9, -7, -9 }
            : new int[] { 1, -1, 8, -8 };

        foreach (int dir in directions)
        {
            int currentSq = sq;
            while (true)
            {
                if (!CanMoveInDirection(currentSq, dir)) break;

                currentSq += dir;
                ulong bit = 1UL << currentSq;

                // Se incontro un pezzo
                if ((occupied & bit) != 0)
                {
                    // Verifico se è un pezzo nemico del tipo che sto cercando
                    bool isEnemyType = (board.PieceBitboards[(int)attackerColor][(int)type] & bit) != 0;
                    bool isEnemyQueen = (board.PieceBitboards[(int)attackerColor][(int)PieceType.PT_QUEEN] & bit) != 0;

                    if (isEnemyType || isEnemyQueen) return true;

                    // Se incontriamo un pezzo qualsiasi amico o nemico di altro tipo il raggio è bloccato
                    break;
                }
            }
        }
        return false;
    }

    private static bool CanMoveInDirection(int sq, int dir)
    {
        // Helper per evitare che il raggio "salti" da un bordo all'altro
        int col = sq % 8;
        if (dir == 1 && col == 7) return false;      
        if (dir == -1 && col == 0) return false;     
        if (dir == 7 && (col == 0)) return false;    
        if (dir == -7 && (col == 7)) return false;
        if (dir == 9 && (col == 7)) return false;
        if (dir == -9 && (col == 0)) return false;
        if (sq + dir < 0 || sq + dir > 63) return false;
        return true;
    }

    public static bool IsAttackedByPawn(Board b, int sq, PieceColor attackerColor)
    {
        ulong enemyPawns = b.PieceBitboards[(int)attackerColor][(int)PieceType.PT_PAWN];
        ulong bit = 1UL << sq;

        if (attackerColor == PieceColor.PC_WHITE) // Pedoni bianchi che attaccano sq (vengono dal basso)
        {
            // Se sq è attaccato da un bianco, il bianco deve trovarsi in sq-7 o sq-9
            return (((bit >> 7) & 0x7f7f7f7f7f7f7f7f & enemyPawns) != 0) ||
                   (((bit >> 9) & 0xfefefefefefefefe & enemyPawns) != 0);
        }
        else // Pedoni neri che attaccano sq (vengono dall'alto)
        {
            return (((bit << 7) & 0xfefefefefefefefe & enemyPawns) != 0) ||
                   (((bit << 9) & 0x7f7f7f7f7f7f7f7f & enemyPawns) != 0);
        }
    }

    #endregion

}
