using SACE_Core.Enums;
using SACE_Core.Utils;

namespace SACE_Core.Classes;

internal static class MoveGenerator
{
    internal static readonly ulong[] KnightMoves = new ulong[64];
    internal static readonly ulong[] KingMoves = new ulong[64];

    // Direzioni alfieri e torri
    private static readonly int[] BishopOffsets = { 7, 9, -7, -9 };
    private static readonly int[] RookOffsets = { 8, -8, 1, -1 };

    static MoveGenerator()
    {
        PrecomputeNonSlidingPieces();
    }
    private static void PrecomputeNonSlidingPieces() // Look up tables per cavalli e re
    {
        for (int i = 0; i < 64; i++)
        {
            ulong knight = 1UL << i;
            // Calcolo salti cavallo (controllo di non saltare da una parte all'altra della scacchiera)
            ulong l1 = (knight >> 1) & 0x7f7f7f7f7f7f7f7f;
            ulong l2 = (knight >> 2) & 0x3f3f3f3f3f3f3f3f;
            ulong r1 = (knight << 1) & 0xfefefefefefefefe;
            ulong r2 = (knight << 2) & 0xfcfcfcfcfcfcfcfc;

            KnightMoves[i] = (l1 << 16) | (l1 >> 16) | (r1 << 16) | (r1 >> 16) |
                             (l2 << 8) | (l2 >> 8) | (r2 << 8) | (r2 >> 8);

            // Calcolo mosse Re
            ulong king = 1UL << i;
            ulong k_r = (king << 1) & 0xfefefefefefefefe;
            ulong k_l = (king >> 1) & 0x7f7f7f7f7f7f7f7f;
            ulong k_u = (king | k_r | k_l) << 8;
            ulong k_d = (king | k_r | k_l) >> 8;
            KingMoves[i] = k_r | k_l | k_u | k_d;
        }
    }

    public static List<Move> GeneratePseudoLegalMoves(Board board, bool getCapturesOnly = false)
    {
        var moves = new List<Move>();
        PieceColor us = board.SideToMove;
        PieceColor them = (us == PieceColor.PC_WHITE) ? PieceColor.PC_BLACK : PieceColor.PC_WHITE;

        ulong occupied = board.GetOccupied();
        ulong friendlyPieces = board.GetAllPiecesBitboard(us);
        ulong enemyPieces = board.GetAllPiecesBitboard(them);

        GenerateKnightMoves(board, moves, us, friendlyPieces, enemyPieces);
        GenerateKingMoves(board, moves, us, friendlyPieces, enemyPieces);
        GeneratePawnMoves(board, moves, us, occupied, enemyPieces);
        GenerateSlidingMoves(board, moves, PieceType.PT_BISHOP, us, friendlyPieces, enemyPieces);
        GenerateSlidingMoves(board, moves, PieceType.PT_ROOK, us, friendlyPieces, enemyPieces);
        GenerateSlidingMoves(board, moves, PieceType.PT_QUEEN, us, friendlyPieces, enemyPieces);

        if (getCapturesOnly) moves = moves.Where(m => m.Flag == MoveFlag.Capture || m.Flag > MoveFlag.EnPassant || (int)m.Flag >= 12).ToList();

        return moves;
    }

    public static List<Move> GetLegalMoves(Board board, bool getCapturesOnly = false)
    {
        // Genero le mosse pseudo legali per prima cosa
        List<Move> pseudoMoves = GeneratePseudoLegalMoves(board,getCapturesOnly);
        List<Move> legalMoves = new List<Move>();

        PieceColor us = board.SideToMove;
        PieceColor enemy = (us == PieceColor.PC_WHITE) ? PieceColor.PC_BLACK : PieceColor.PC_WHITE;

        foreach (var move in pseudoMoves)
        {
            board.MakeMove(move);

            // Trovo dove si trova il mio Re dopo la mossa per vedere se è attaccato
            int kingSq = BitUtils.GetLSB(board.PieceBitboards[(int)us][(int)PieceType.PT_KING]);
            if (!IsSquareAttacked(board, kingSq, enemy))
            {
                legalMoves.Add(move);
            }

            // Annullo la mossa per tornare alla posizione originale
            board.UnmakeMove(move);
        }

        return legalMoves;
    }

    #region Mosse pezzi

    public static void GenerateSlidingMoves(Board board, List<Move> moves, PieceType type, PieceColor us, ulong friendly, ulong enemy)
    {
        ulong pieces = board.PieceBitboards[(int)us][(int)type];
        int[] offsets = (type == PieceType.PT_BISHOP) ? BishopOffsets : RookOffsets;

        // Se è una Regina uso entrambi gli offset
        if (type == PieceType.PT_QUEEN)
            offsets = BishopOffsets.Concat(RookOffsets).ToArray();

        while (pieces != 0)
        {
            int from = BitUtils.PopLSB(ref pieces);

            foreach (int offset in offsets)
            {
                int currentSq = from;

                while (true)
                {
                    int oldRank = currentSq / 8;
                    int oldFile = currentSq % 8;

                    currentSq += offset;

                    // Controllo fuori scacchiera
                    if (currentSq < 0 || currentSq >= 64) break;

                    // Controllo salto del bordo laterale
                    int newRank = currentSq / 8;
                    int newFile = currentSq % 8;
                    if (Math.Abs(newRank - oldRank) > 1 || Math.Abs(newFile - oldFile) > 1) break;

                    // Controllo collisioni
                    if (BitUtils.GetBit(friendly, currentSq))
                    {
                        // Pezzo alleato: bloccato, non aggiunge mossa
                        break;
                    }

                    if (BitUtils.GetBit(enemy, currentSq))
                    {
                        // Pezzo nemico: cattura e si ferma
                        moves.Add(new Move(from, currentSq, MoveFlag.Capture));
                        break;
                    }

                    // Casa vuota: aggiungi mossa e continua a scorrere
                    moves.Add(new Move(from, currentSq, MoveFlag.Quiet));
                }
            }
        }
    }

    #region Mosse Re
    public static void GenerateKingMoves(Board board, List<Move> moves, PieceColor us, ulong friendly, ulong enemy)
    {
        ulong king = board.PieceBitboards[(int)us][(int)PieceType.PT_KING];
        if (king == 0) return; // Non dovrebbe succedere in una partita vera...

        int from = BitUtils.PopLSB(ref king);

        // Mosse Standard (spostamenti di 1 casa e Catture)
        ulong attacks = KingMoves[from] & ~friendly;

        while (attacks != 0)
        {
            int to = BitUtils.PopLSB(ref attacks);
            MoveFlag flag = BitUtils.GetBit(enemy, to) ? MoveFlag.Capture : MoveFlag.Quiet;
            moves.Add(new Move(from, to, flag));
        }

        // Arrocchi
        GenerateCastlingMoves(board, moves, us, from);
    }
    private static void GenerateCastlingMoves(Board board, List<Move> moves, PieceColor us, int kingFrom)
    {
        /*
         * É possibile arroccare solo se:
         * - Il re non è sotto scacco
         * - Re e torre non si sono mai mossi ( questa cosa è gestita dalle variabili Can***Castle )
         * - Non si finisce sotto scacco dopo l'arrocco
         * - Non si passa attraverso uno scacco (una casa attaccata dal nemico)
         */

        ulong occupied = board.GetOccupied();
        PieceColor enemy = (us == PieceColor.PC_WHITE) ? PieceColor.PC_BLACK : PieceColor.PC_WHITE;

        if (IsSquareAttacked(board, kingFrom, enemy)) return;

        if (us == PieceColor.PC_WHITE)
        {
            if (board.CanWhiteShortCastle)
            {
                ulong blockers = (1UL << 5) | (1UL << 6);
                if ((occupied & blockers) == 0) // Case vuote
                {
                    if (!IsSquareAttacked(board, 5, enemy) && !IsSquareAttacked(board, 6, enemy))
                    {
                        moves.Add(new Move(kingFrom, 6, MoveFlag.ShortCastle));
                    }
                }
            }
            if (board.CanWhiteLongCastle)
            {
                ulong blockers = (1UL << 1) | (1UL << 2) | (1UL << 3);
                if ((occupied & blockers) == 0) // Case vuote
                {
                    if (!IsSquareAttacked(board, 3, enemy) && !IsSquareAttacked(board, 2, enemy))
                    {
                        moves.Add(new Move(kingFrom, 2, MoveFlag.LongCastle));
                    }
                }
            }
        }
        else // Nero
        {
            if (board.CanBlackShortCastle)
            {
                ulong blockers = (1UL << 61) | (1UL << 62);
                if ((occupied & blockers) == 0)
                {
                    if (!IsSquareAttacked(board, 61, enemy) && !IsSquareAttacked(board, 62, enemy))
                    {
                        moves.Add(new Move(kingFrom, 62, MoveFlag.ShortCastle));
                    }
                }
            }
            if (board.CanBlackLongCastle)
            {
                ulong blockers = (1UL << 57) | (1UL << 58) | (1UL << 59);
                if ((occupied & blockers) == 0)
                {
                    if (!IsSquareAttacked(board, 59, enemy) && !IsSquareAttacked(board, 58, enemy))
                    {
                        moves.Add(new Move(kingFrom, 58, MoveFlag.LongCastle));
                    }
                }
            }
        }
    }
    #endregion

    private static void GenerateKnightMoves(Board board, List<Move> moves, PieceColor us, ulong friendly, ulong enemy)
    {
        ulong knights = board.PieceBitboards[(int)us][(int)PieceType.PT_KNIGHT];

        while (knights != 0)
        {
            int from = BitUtils.PopLSB(ref knights);
            // Escludo le case occupate dai pezzi amici
            ulong attacks = KnightMoves[from] & ~friendly;

            while (attacks != 0)
            {
                int to = BitUtils.PopLSB(ref attacks);
                // Se la casa di arrivo è occupata da un nemico allora è una cattura
                MoveFlag flag = BitUtils.GetBit(enemy, to) ? MoveFlag.Capture : MoveFlag.Quiet;
                moves.Add(new Move(from, to, flag));
            }
        }
    }

    #region Mosse Pedoni
    public static void GeneratePawnMoves(Board board, List<Move> moves, PieceColor us, ulong occupied, ulong enemy)
    {
        ulong pawns = board.PieceBitboards[(int)us][(int)PieceType.PT_PAWN];
        int direction = (us == PieceColor.PC_WHITE) ? 8 : -8;
        int startRank = (us == PieceColor.PC_WHITE) ? 1 : 6;
        int promoRank = (us == PieceColor.PC_WHITE) ? 7 : 0;

        while (pawns != 0)
        {
            int from = BitUtils.PopLSB(ref pawns);
            int fRank = from / 8;
            int fFile = from % 8;

            // --- Spostamento verticale ---
            int toNext = from + direction;
            if (toNext >= 0 && toNext < 64 && !BitUtils.GetBit(occupied, toNext))
            {
                // Spinta singola e Promozione
                if (toNext / 8 == promoRank)
                {
                    AddPromotionMoves(from, toNext, false, moves);
                }
                else
                {
                    moves.Add(new Move(from, toNext, MoveFlag.Quiet));

                    // Doppia spinta (solo se la casa davanti è libera e siamo sulla riga di partenza)
                    if (fRank == startRank)
                    {
                        int toDouble = from + (direction * 2);
                        if (!BitUtils.GetBit(occupied, toDouble))
                            moves.Add(new Move(from, toDouble, MoveFlag.DoublePawnPush));
                    }
                }
            }

            // --- Catture (Spostamento diagonale) ---
            // Direzioni di cattura in base al colore
            int[] captureOffsets = (us == PieceColor.PC_WHITE) ? new[] { 7, 9 } : new[] { -7, -9 };

            foreach (int offset in captureOffsets)
            {
                int toCapture = from + offset;
                if (toCapture < 0 || toCapture >= 64) continue;

                int tFile = toCapture % 8;

                // Controlle se il pedone è in colonna A non può catturare a sinistra (-1 file)
                // Se è in colonna H non può catturare a destra (+1 file)
                if (Math.Abs(tFile - fFile) != 1) continue;

                // Cattura Normale
                if (BitUtils.GetBit(enemy, toCapture))
                {
                    if (toCapture / 8 == promoRank)
                        AddPromotionMoves(from, toCapture, true, moves);
                    else
                        moves.Add(new Move(from, toCapture, MoveFlag.Capture));
                }
                // Cattura En Passant
                // board.EnPassantTarget è un ulong con un solo bit acceso (o zero)
                else if (BitUtils.GetBit(board.EnPassantTarget, toCapture))
                {
                    moves.Add(new Move(from, toCapture, MoveFlag.EnPassant));
                }
            }
        }
    }

    private static void AddPromotionMoves(int from, int to, bool isCapture, List<Move> moves)
    {
        if (isCapture)
        {
            moves.Add(new Move(from, to, MoveFlag.PromoteToQueenCapture));
            moves.Add(new Move(from, to, MoveFlag.PromoteToKnightCapture));
            moves.Add(new Move(from, to, MoveFlag.PromoteToBishopCapture));
            moves.Add(new Move(from, to, MoveFlag.PromoteToRookCapture));
        }
        else
        {
            moves.Add(new Move(from, to, MoveFlag.PromoteToQueen));
            moves.Add(new Move(from, to, MoveFlag.PromoteToKnight));
            moves.Add(new Move(from, to, MoveFlag.PromoteToBishop));
            moves.Add(new Move(from, to, MoveFlag.PromoteToRook));
        }
    }
    #endregion

    #endregion

    #region Case attaccate
    public static bool IsSquareAttacked(Board board, int square, PieceColor attackerColor)
    {
        int us = (int)attackerColor;
        ulong occupied = board.GetOccupied();

        // Attacchi dai Pedoni
        ulong pawns = board.PieceBitboards[us][(int)PieceType.PT_PAWN];
        if (attackerColor == PieceColor.PC_BLACK)
        {
            if (CanPawnAttack(square, 7, pawns) || CanPawnAttack(square, 9, pawns)) return true;
        }
        else // Attaccante bianco
        {
            if (CanPawnAttack(square, -7, pawns) || CanPawnAttack(square, -9, pawns)) return true;
        }

        // Attacchi dai Cavalli
        ulong knights = board.PieceBitboards[us][(int)PieceType.PT_KNIGHT];
        if ((KnightMoves[square] & knights) != 0) return true;

        // Attacchi dal Re (per evitare che i Re si avvicinino troppo)
        ulong king = board.PieceBitboards[us][(int)PieceType.PT_KING];
        if ((KingMoves[square] & king) != 0) return true;

        // Attacchi dai pezzi a scorrimento (Torri, Alfieri, Regine)
        if (IsAttackedBySliding(board, square, PieceType.PT_BISHOP, attackerColor, occupied)) return true;
        if (IsAttackedBySliding(board, square, PieceType.PT_ROOK, attackerColor, occupied)) return true;

        return false;
    }

    private static bool CanPawnAttack(int square, int offset, ulong pawns)
    {
        int target = square + offset;
        if (target < 0 || target >= 64) return false;
        if (Math.Abs((target % 8) - (square % 8)) != 1) return false;
        return BitUtils.GetBit(pawns, target);
    }

    private static bool IsAttackedBySliding(Board board, int square, PieceType type, PieceColor attackerColor, ulong occupied)
    {
        int[] offsets = (type == PieceType.PT_BISHOP) ? BishopOffsets : RookOffsets;
        ulong enemySliders = board.PieceBitboards[(int)attackerColor][(int)type] |
                             board.PieceBitboards[(int)attackerColor][(int)PieceType.PT_QUEEN];

        foreach (int offset in offsets)
        {
            int currentSq = square;
            while (true)
            {
                int oldFile = currentSq % 8;
                currentSq += offset;
                if (currentSq < 0 || currentSq >= 64 || Math.Abs((currentSq % 8) - oldFile) > 1) break;

                if (BitUtils.GetBit(enemySliders, currentSq)) return true;
                if (BitUtils.GetBit(occupied, currentSq)) break; // Pezzo bloccante (amico o nemico)
            }
        }
        return false;
    }
    #endregion



    #region Perft
    public static long Perft(Board board, int depth)
    {
        if (depth == 0) return 1;

        long nodes = 0;
        List<Move> moves = MoveGenerator.GetLegalMoves(board);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            nodes += Perft(board, depth - 1);
            board.UnmakeMove(move);
        }

        return nodes;
    }
    #endregion
}

