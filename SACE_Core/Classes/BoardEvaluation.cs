using SACE_Core.Enums;
using SACE_Core.Utils;

namespace SACE_Core.Classes;
// Questa classe si occupa del valutare una posizione
internal static class BoardEvaluation
{
    // Valore dei pezzi in base alla fase della partita (apertura , mediogioco, finale)
    public static readonly int[] PawnValue = { 100, 100, 120 };
    public static readonly int[] KnightValue = { 300, 300, 310 };
    public static readonly int[] BishopValue = { 300, 300, 330 };
    public static readonly int[] RookValue = { 500, 500, 570 };
    public static readonly int[] QueenValue = { 900, 900, 1000 };



    /*
     * La valutazione di una posizione deve tenere conto di: (da tenere in considerazione specialmente le prime cose)
     * - Quantità e valore dei pezzi in gioco (il valore dovrebbe essere dinamico in base alla fase della partita)
     * - Controllo del centro / Posizione dei pezzi in generale
     * - Pezzi lasciati in presa (male)
     * - Pedoni prossimi alla promozione
     * - Pedoni passati (ancora meglio se protetti)
     * - Pedoni arretrati e non difendibili da altri pedoni danno un malus perchè sono difficili da difendere e generalmente creano debolezze strutturali
     * - Posizione del re in base alla fase della partita (all'inizio protetto, verso il finale vicino al centro)
     * - Scacchi e pezzi avversari inchiodati solitamente sono punti a favore
     */
    internal static int Evaluate(Board currentPosition)
    {
        // Risultato > 0 : bianco in vantaggio
        // < 0 : nero in vantaggio
        // == 0 : pareggio

        // Fase della partita - può essere utile sapere in che fase siamo per dare valori dinamici
        GamePhase gamePhase = GetGamePhase(currentPosition);

        int result = 0;

        /* Non qui, GetLegalMoves è troppo pesante per essere chiamato qua
        
        List<Move> legalMoves = new List<Move>(MoveGenerator.GetLegalMoves(currentPosition));
        //Se non ho mosse disponibili è scacco matto
        if (legalMoves.Count == 0)
        {
            if (currentPosition.SideToMove == PieceColor.PC_WHITE) return double.MinValue; // Vince il nero
            return double.MaxValue; // Vince il bianco
        }

        */


        #region Quantità e qualità dei pezzi

        result += EvaluateColor(currentPosition, PieceColor.PC_WHITE, gamePhase);
        result -= EvaluateColor(currentPosition, PieceColor.PC_BLACK, gamePhase);

        #endregion

        #region Attività dei pezzi
        // I pezzi che controllano o occupano il centro (e magari sono anche sostenuti da pezzi alleati)
        // generalmente sono buoni da avere
        // Il re all'inizio e nel mediogioco è meglio se sta al riparo, mentre meglio offensivo verso il finale quando ci sono pochi pezzi

        // Creo delle maschere che funzionano da "bonus"
        // Se un pezzo occupa una certa casella in una certa fase della partita può essere valutato di più o di meno
        result += EvaluateActivity(currentPosition, PieceColor.PC_WHITE, gamePhase);
        result -= EvaluateActivity(currentPosition, PieceColor.PC_BLACK, gamePhase);

        #endregion

        #region Pezzi lasciati in presa
        // Applico un malus se ho dei pezzi lasciati in presa
        int hangingPiecesMalus = EvaluateHangingPieces(currentPosition, currentPosition.SideToMove);
        result -= (currentPosition.SideToMove == PieceColor.PC_WHITE) ? hangingPiecesMalus : -hangingPiecesMalus;
        #endregion

        // Do un piccolo vantaggio a chi ha l'iniziativa (tempo di vantaggio)
        result += (currentPosition.SideToMove == PieceColor.PC_WHITE) ? 10 : -10;


        // Punteggio come int, a video meglio dividere per 1000
        return result;
    }

    private static int EvaluateHangingPieces(Board position, PieceColor color)
    {
        int malus = 0;
        PieceColor enemyColor = (color == PieceColor.PC_WHITE) ? PieceColor.PC_BLACK : PieceColor.PC_WHITE;

        ulong myPieces = position.GetAllPiecesBitboard(color);
        while (myPieces != 0)
        {
            int sq = BitUtils.PopLSB(ref myPieces);
            PieceType myType = position.GetPieceAtSquare(sq);

            var smallestAttacker = BoardEvaluationHelper.GetSmallestAttacker(position, sq, enemyColor);
            if (smallestAttacker != null)
            {
                var friendlyDefender = BoardEvaluationHelper.GetSmallestAttacker(position, sq, color);
                bool isDefended = false;
                if (friendlyDefender != null) isDefended = true;

                if (!isDefended)
                {
                    // Pezzo gratis
                    malus += GetBasePieceValue(myType) / 3;
                }
                else if (GetBasePieceValue(smallestAttacker.Value.type) < GetBasePieceValue(myType))
                {
                    // Scambio sfavorevole 
                    malus += (GetBasePieceValue(myType) - GetBasePieceValue(smallestAttacker.Value.type)) / 4;
                }

            }
        }
        return malus;
    }
    private static int GetBasePieceValue(PieceType type, GamePhase gamePhase = GamePhase.GP_OPENING)
    {
        // Ritorno il valore base - come se fossimo in apertura
        switch (type)
        {
            case PieceType.PT_PAWN:
                return PawnValue[(int)gamePhase];
            case PieceType.PT_KNIGHT:
                return KnightValue[(int)gamePhase];
            case PieceType.PT_BISHOP:
                return BishopValue[(int)gamePhase];
            case PieceType.PT_ROOK:
                return RookValue[(int)gamePhase];
            case PieceType.PT_QUEEN:
                return QueenValue[(int)gamePhase];
            case PieceType.PT_KING:
                return int.MaxValue;
            default:
                throw new Exception("Pezzo non gestito nell'ottenimento del valore base");
        }
    }

    //private static bool IsAttackedByCheaperPiece(Board position, int sq, PieceType myType, PieceColor enemyColor)
    //{
    //    if (myType == PieceType.PT_PAWN) return false; // Un pedone è il pezzo più scrauso

    //    var attacker = GetSmallestAttacker(position, sq, enemyColor);

    //    if (attacker.type == PieceType.PT_NONE) return false;

    //    // Se il valore dell'attaccante è minore del mio, allora sono in pericolo
    //    return GetBasePieceValue(attacker.type) < GetBasePieceValue(myType);
    //}

    private static int EvaluateColor(Board position, PieceColor evaluatedColor, GamePhase gamePhase)
    {
        // Prendo la quantità di ogni pezzo e la moltiplico per il loro valore

        int pawnsCount = BitUtils.CountBits(position.PieceBitboards[(int)evaluatedColor][(int)PieceType.PT_PAWN]);
        int bishopCount = BitUtils.CountBits(position.PieceBitboards[(int)evaluatedColor][(int)PieceType.PT_BISHOP]);
        int queenCount = BitUtils.CountBits(position.PieceBitboards[(int)evaluatedColor][(int)PieceType.PT_QUEEN]);
        int rookCount = BitUtils.CountBits(position.PieceBitboards[(int)evaluatedColor][(int)PieceType.PT_ROOK]);
        int knightCount = BitUtils.CountBits(position.PieceBitboards[(int)evaluatedColor][(int)PieceType.PT_KNIGHT]);

        int pawnsValue = pawnsCount * PawnValue[(int)gamePhase];
        int bishopValue = bishopCount * BishopValue[(int)gamePhase];
        int queenValue = queenCount * QueenValue[(int)gamePhase];
        int rookValue = rookCount * RookValue[(int)gamePhase];
        int knightValue = knightCount * KnightValue[(int)gamePhase];

        // Nelle posizioni chiuse il cavallo vale un po' di più, mentre in campo aperto è meglio l'alfiere
        if (gamePhase == GamePhase.GP_MIDDLEGAME)
        {
            if (BitUtils.CountBits(position.GetOccupied()) > 20)
                knightValue += (10 * knightCount); // 10 punti di bonus per ogni cavallo
            else
                bishopValue += (10 * bishopCount); // 10 punti di bonus per ogni alfiere
        }

        return (pawnsValue + bishopValue + queenValue + rookValue + knightValue);
    }
    private static int EvaluateActivity(Board position, PieceColor color, GamePhase phase)
    {
        int activityScore = 0;
        int cIdx = (int)color;

        // Maschera Cavalli
        ulong knights = position.PieceBitboards[cIdx][(int)PieceType.PT_KNIGHT];
        while (knights != 0)
        {
            int sq = BitUtils.PopLSB(ref knights);
            activityScore += BoardEvaluationHelper.GetPstValue(BoardEvaluationHelper.KnightPST, sq, color);
        }

        // Maschera Re
        int kingSq = BitUtils.GetLSB(position.PieceBitboards[cIdx][(int)PieceType.PT_KING]);
        int[] kingPst = (phase == GamePhase.GP_ENDGAME) ? BoardEvaluationHelper.KingEndgamePST : BoardEvaluationHelper.KingOpeningPST;
        activityScore += BoardEvaluationHelper.GetPstValue(kingPst, kingSq, color);



        return activityScore;
    }


    /// <summary>
    /// Fase della partita (apertura, mediogioco, finale)
    /// </summary>
    private static GamePhase GetGamePhase(Board position)
    {
        // L'apertura sono generalmente le prime mosse
        if (position.FullMoves <= 5) return GamePhase.GP_OPENING;


        // Negli altri casi meno pezzi ci sono in partita e più siamo vicini al finale (escludo re e pedoni)
        int materialInGame = 0;
        int whiteBB = (int)PieceColor.PC_WHITE, blackBB = (int)PieceColor.PC_BLACK;

        // Cavalli
        materialInGame += BitUtils.CountBits(position.PieceBitboards[whiteBB][(int)PieceType.PT_KNIGHT]) * 1;
        materialInGame += BitUtils.CountBits(position.PieceBitboards[blackBB][(int)PieceType.PT_KNIGHT]) * 1;

        // Alfieri
        materialInGame += BitUtils.CountBits(position.PieceBitboards[whiteBB][(int)PieceType.PT_BISHOP]) * 1;
        materialInGame += BitUtils.CountBits(position.PieceBitboards[blackBB][(int)PieceType.PT_BISHOP]) * 1;

        // Torri
        materialInGame += BitUtils.CountBits(position.PieceBitboards[whiteBB][(int)PieceType.PT_ROOK]) * 1;
        materialInGame += BitUtils.CountBits(position.PieceBitboards[blackBB][(int)PieceType.PT_ROOK]) * 1;

        // Regine
        materialInGame += BitUtils.CountBits(position.PieceBitboards[whiteBB][(int)PieceType.PT_QUEEN]) * 1;
        materialInGame += BitUtils.CountBits(position.PieceBitboards[blackBB][(int)PieceType.PT_QUEEN]) * 1;

        // Se il materiale è alto (> 18-20), siamo in Opening/Middlegame
        // Se è basso (< 6), siamo in Finale
        if (materialInGame <= 6) return GamePhase.GP_ENDGAME;

        return GamePhase.GP_MIDDLEGAME;
    }



}
