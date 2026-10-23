using SACE_Core.Enums;
using SACE_Core.Utils;

namespace SACE_Core.Classes;

internal class Board
{
    public ulong[][] PieceBitboards = new ulong[2][];
    #region Bitboard combinati
    public ulong GetAllPiecesBitboard(PieceColor pieceColor)
    {
        ulong result = 0;
        int colorIdx = (int)pieceColor;
        for (int i = 0; i < 6; i++)
        {
            result |= PieceBitboards[colorIdx][i];
        }
        return result;
    }
    public ulong GetOccupied() => GetAllPiecesBitboard(PieceColor.PC_WHITE) | GetAllPiecesBitboard(PieceColor.PC_BLACK);
    #endregion

    public PieceColor SideToMove = PieceColor.PC_WHITE;
    Stack<UndoInfo> _history = new(); // Storico per tornare indietro con le mosse

    // Arrocchi
    internal bool CanWhiteShortCastle { get; set; } = false;
    internal bool CanBlackShortCastle { get; set; } = false;
    internal bool CanWhiteLongCastle { get; set; } = false;
    internal bool CanBlackLongCastle { get; set; } = false;
    // --------
    // Regole di gioco e altro
    internal ulong EnPassantTarget; // Bitboard con la casella di en passant, se esiste
    internal int HalfMoves;
    internal int FullMoves;
    // --------

    public Board(string fenPosition = "")
    {
        if (ChessUtils.IsValidFen(fenPosition)) LoadFromFEN(fenPosition);
        else LoadFromFEN(ChessUtils.FEN_START_POSITION);
    }

    private void ResetBitboards()
    {
        PieceBitboards[(int)PieceColor.PC_WHITE] = new ulong[6]; // Bianchi
        PieceBitboards[(int)PieceColor.PC_BLACK] = new ulong[6]; // Neri
    }
    private void SetCastlingFlagsByColor(bool value = false, PieceColor pieceColor = PieceColor.PC_WHITE)
    {
        if (pieceColor == PieceColor.PC_WHITE)
        {
            CanWhiteLongCastle = value;
            CanWhiteShortCastle = value;
        }
        else
        {
            CanBlackLongCastle = value;
            CanBlackShortCastle = value;
        }
    }

    private void ResetBoard()
    {
        ResetBitboards();
        SetCastlingFlagsByColor(false, PieceColor.PC_WHITE);
        SetCastlingFlagsByColor(false, PieceColor.PC_BLACK);

        SideToMove = PieceColor.PC_WHITE;
        HalfMoves = 0; FullMoves = 0;
        EnPassantTarget = 0;


    }


    /// <summary>
    /// Imposta la posizione in base alla stringa FEN
    /// </summary>
    private void LoadFromFEN(string fenPosition)
    {
        //Azzero tutti i bitboard
        ResetBoard();

        //...e poi inizio ad impostare TUTTO
        string[] fenSections = fenPosition.Split(' ');

        //Ciclo sulle diverse sezioni
        //Dovrebbero essere sempre 6, ma potrebbe mancare qualche parte. Le parti che però ci sono allora seguono il formato standard
        for (int i = 0; i < fenSections.Length; i++)
        {
            //Posizione dei pezzi
            if (i == 0)
            {
                string[] rows = fenSections[i].Split('/');
                for (int rank = 7; rank >= 0; rank--) // Dalla traversa 8 (7) alla 1 (0)
                {
                    string currentRow = rows[7 - rank];
                    int file = 0;

                    foreach (char c in currentRow)
                    {
                        if (char.IsDigit(c))
                        {
                            file += (c - '0');
                        }
                        else
                        {
                            var piece = ChessUtils.CharToPiece(c);
                            int colorIdx = (int)piece.color;
                            int pieceIdx = (int)piece.type;

                            int squareIndex = rank * 8 + file;

                            PieceBitboards[colorIdx][pieceIdx] |= (1UL << squareIndex);

                            file++;
                        }
                    }
                }
            }

            //Colore attivo
            else if (i == 1)
            {
                if (fenSections[i] == "w") SideToMove = PieceColor.PC_WHITE;
                else SideToMove = PieceColor.PC_BLACK;
            }

            //Possibilità di arrocco
            else if (i == 2)
            {
                if (fenSections[i].Contains("q")) CanBlackLongCastle = true;
                if (fenSections[i].Contains("Q")) CanWhiteLongCastle = true;
                if (fenSections[i].Contains("k")) CanBlackShortCastle = true;
                if (fenSections[i].Contains("K")) CanWhiteShortCastle = true;
            }

            //En Passant
            else if (i == 3)
            {
                if (fenSections[3] != "-")
                {
                    int file = fenSections[3][0] - 'a';
                    int rank = int.Parse(fenSections[3][1].ToString()) - 1;
                    EnPassantTarget = 1UL << (rank * 8 + file);
                }
            }

            //Mezze mosse
            else if (i == 4)
            {
                HalfMoves = int.Parse(fenSections[4]);
            }

            //Mosse complete
            else if (i == 5)
            {
                FullMoves = int.Parse(fenSections[5]);
            }
        }

    }

    #region Esecuzione mossa
    /// <summary>
    /// Esegue una mossa
    /// </summary>
    public void MakeMove(Move move)
    {
        // Salvo lo stato attuale nello storico
        UndoInfo undo = new UndoInfo
        {
            WhiteShortCastle = CanWhiteShortCastle,
            WhiteLongCastle = CanWhiteLongCastle,
            BlackShortCastle = CanBlackShortCastle,
            BlackLongCastle = CanBlackLongCastle,
            EnPassantTarget = EnPassantTarget,
            HalfMoves = HalfMoves,
            CapturedPiece = PieceType.PT_PAWN // Default, verrà sovrascritto se c'è cattura
        };

        int from = move.From;
        int to = move.To;
        MoveFlag flag = move.Flag;
        int us = (int)SideToMove;
        int them = 1 - us;
        HalfMoves++;

        // Pezzo mosso
        PieceType movedPiece = GetPieceAtSquare(from);

        // Cattura
        if (flag == MoveFlag.Capture || IsPromotionCapture(flag))
        {
            undo.CapturedPiece = GetPieceAtSquare(to);
            BitUtils.SetBitToValue(ref PieceBitboards[them][(int)undo.CapturedPiece], to, false);
            HalfMoves = 0; // Reset regola 50 mosse
        }
        else if (flag == MoveFlag.EnPassant)
        {
            int capSq = (SideToMove == PieceColor.PC_WHITE) ? to - 8 : to + 8;
            BitUtils.SetBitToValue(ref PieceBitboards[them][(int)PieceType.PT_PAWN], capSq, false);
            undo.CapturedPiece = PieceType.PT_PAWN;
            HalfMoves = 0; // Reset regola 50 mosse
        }

        // Muovo effettivamente il pezzo
        // (prima rimuovo, poi rimetto nel bitboard corrispondente, ma controllo prima se è una promozione)
        BitUtils.SetBitToValue(ref PieceBitboards[us][(int)movedPiece], from, false);

        if (IsPromotion(flag))
        {
            PieceType promo = GetPromoPiece(flag);
            BitUtils.SetBitToValue(ref PieceBitboards[us][(int)promo], to, true);
        }
        else
        {
            BitUtils.SetBitToValue(ref PieceBitboards[us][(int)movedPiece], to, true);
        }

        // Muovo anche la torre se è un Arrocco
        if (flag == MoveFlag.ShortCastle) MoveRookForCastle(us, true);
        if (flag == MoveFlag.LongCastle) MoveRookForCastle(us, false);

        // Aggiornamento diritti arrocco e En Passant
        UpdateCastlingRights(movedPiece, from, to);
        EnPassantTarget = (flag == MoveFlag.DoublePawnPush) ? (1UL << (from + (to - from) / 2)) : 0;

        // Cambio turno
        SideToMove = (SideToMove == PieceColor.PC_WHITE) ? PieceColor.PC_BLACK : PieceColor.PC_WHITE;
        _history.Push(undo);
    }

    private void UpdateCastlingRights(PieceType movedPiece, int from, int to)
    {
        // Se si muove il Re addio arrocchi per quel colore
        if (movedPiece == PieceType.PT_KING)
        {
            if (SideToMove == PieceColor.PC_WHITE)
            {
                CanWhiteShortCastle = false;
                CanWhiteLongCastle = false;
            }
            else
            {
                CanBlackShortCastle = false;
                CanBlackLongCastle = false;
            }
        }

        // Se si muove una Torre o viene catturata una Torre
        // controllo le 4 case d'angolo originali
        if (from == 0 || to == 0) CanWhiteLongCastle = false;  // a1
        if (from == 7 || to == 7) CanWhiteShortCastle = false; // h1
        if (from == 56 || to == 56) CanBlackLongCastle = false; // a8
        if (from == 63 || to == 63) CanBlackShortCastle = false; // h8
    }

    private void MoveRookForCastle(int us, bool isShort, bool undo = false)
    {
        int rookFrom, rookTo;

        if (us == (int)PieceColor.PC_WHITE)
        {
            rookFrom = isShort ? 7 : 0; // h1 o a1
            rookTo = isShort ? 5 : 3;   // f1 o d1
        }
        else
        {
            rookFrom = isShort ? 63 : 56; // h8 o a8
            rookTo = isShort ? 61 : 59;   // f8 o d8
        }

        // Se sto annullando (undo), inverto le case
        if (undo)
        {
            int temp = rookFrom;
            rookFrom = rookTo;
            rookTo = temp;
        }

        // Aggiorno il bitboard della Torre per il colore 'us'
        BitUtils.SetBitToValue(ref PieceBitboards[us][(int)PieceType.PT_ROOK], rookFrom, false);
        BitUtils.SetBitToValue(ref PieceBitboards[us][(int)PieceType.PT_ROOK], rookTo, true);
    }

    private PieceType GetPromoPiece(MoveFlag flag)
    {
        return flag switch
        {
            MoveFlag.PromoteToQueen or MoveFlag.PromoteToQueenCapture => PieceType.PT_QUEEN,
            MoveFlag.PromoteToRook or MoveFlag.PromoteToRookCapture => PieceType.PT_ROOK,
            MoveFlag.PromoteToBishop or MoveFlag.PromoteToBishopCapture => PieceType.PT_BISHOP,
            MoveFlag.PromoteToKnight or MoveFlag.PromoteToKnightCapture => PieceType.PT_KNIGHT,
            _ => throw new ArgumentException("Il flag non è una promozione valida")
        };
    }

    private bool IsPromotionCapture(MoveFlag flag)
    {
        return (int)flag >= 12;
    }

    private bool IsPromotion(MoveFlag flag)
    {
        return (int)flag >= 8;
    }

    public PieceType GetPieceAtSquare(int square)
    {
        ulong bit = 1UL << square;

        for (int color = 0; color < 2; color++)
        {
            for (int type = 0; type < 6; type++)
            {
                if ((PieceBitboards[color][type] & bit) != 0)
                {
                    return (PieceType)type;
                }
            }
        }

        throw new Exception($"Nessun pezzo trovato alla casa {square}");
    }

    public void UnmakeMove(Move move)
    {
        if (_history.Count == 0) return;

        // Recupero l'ultimo stato salvato
        UndoInfo undo = _history.Pop();

        // Cambio il turno (torno al colore che ha appena mosso)
        SideToMove = (SideToMove == PieceColor.PC_WHITE) ? PieceColor.PC_BLACK : PieceColor.PC_WHITE;

        int us = (int)SideToMove;
        int them = 1 - us;
        int from = move.From;
        int to = move.To;
        MoveFlag flag = move.Flag;

        // Identifico il pezzo che si trova attualmente in 'to'
        // Se la mossa era una promozione, in 'to' c'è un pezzo pesante (Regina, ecc.)
        // ma in 'from' deve tornare un PEDONE.
        PieceType pieceOnTo = IsPromotion(flag) ? GetPromoPiece(flag) : GetPieceAtSquare(to);

        // Rimuovo il pezzo da 'to'
        BitUtils.SetBitToValue(ref PieceBitboards[us][(int)pieceOnTo], to, false);

        // Ripristino il pezzo in 'from'
        // Se era una promozione, rimetto un pedone, altrimenti il pezzo originale
        PieceType pieceToRestore = IsPromotion(flag) ? PieceType.PT_PAWN : pieceOnTo;
        BitUtils.SetBitToValue(ref PieceBitboards[us][(int)pieceToRestore], from, true);

        // Ripristino l'eventuale pezzo catturato
        if (flag == MoveFlag.Capture || IsPromotionCapture(flag))
        {
            // Rimettiamo il pezzo catturato nella casa 'to'
            BitUtils.SetBitToValue(ref PieceBitboards[them][(int)undo.CapturedPiece], to, true);
        }
        else if (flag == MoveFlag.EnPassant)
        {
            // En Passant, il pedone catturato non era in 'to'
            int capSq = (SideToMove == PieceColor.PC_WHITE) ? to - 8 : to + 8;
            BitUtils.SetBitToValue(ref PieceBitboards[them][(int)PieceType.PT_PAWN], capSq, true);
        }

        // Ripristino la Torre se era un Arrocco
        if (flag == MoveFlag.ShortCastle) MoveRookForCastle(us, true, undo: true);
        if (flag == MoveFlag.LongCastle) MoveRookForCastle(us, false, undo: true);

        // Ripristino i metadati della scacchiera dallo stato 'undo'
        CanWhiteShortCastle = undo.WhiteShortCastle;
        CanWhiteLongCastle = undo.WhiteLongCastle;
        CanBlackShortCastle = undo.BlackShortCastle;
        CanBlackLongCastle = undo.BlackLongCastle;
        EnPassantTarget = undo.EnPassantTarget;
        HalfMoves = undo.HalfMoves;

        // FullMoves va decrementato solo se il Nero ha appena annullato la mossa
        if (SideToMove == PieceColor.PC_BLACK) FullMoves--;
    }

    #endregion

}
