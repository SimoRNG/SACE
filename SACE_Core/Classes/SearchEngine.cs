using SACE_Core.Enums;
using SACE_Core.Utils;

namespace SACE_Core.Classes;

internal class SearchEngine
{
    private const int Infinity = 1000000;
    private const int MateValue = 900000;

    public static Move GetBestMove(Board board, int depth)
    {
        Move bestMove = default;
        int bestScore = -Infinity;

        List<Move> moves = MoveGenerator.GetLegalMoves(board);

        // Ordino le mosse per avere prima quelle più promettenti
        moves = OrderMoves(board, moves);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            // AlphaBeta per il colore avversario
            int score = -AlphaBeta(board, depth - 1, -Infinity, Infinity);
            board.UnmakeMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private static List<Move> OrderMoves(Board board, List<Move> moves)
    {
        return moves.OrderByDescending(m =>
        {
            int score = 0;

            // Priorità 1: Catture (MVV-LVA: Most Valuable Victim - Least Valuable Attacker)
            if (m.Flag == MoveFlag.Capture || (int)m.Flag >= 12)
            {
                PieceType victim = board.GetPieceAtSquare(m.To);
                PieceType attacker = board.GetPieceAtSquare(m.From);
                score = 1000 + ((int)victim * 10) - (int)attacker;
            }
            // Priorità 2: Promozioni
            if ((int)m.Flag >= 8 && (int)m.Flag <= 11) score += 800;

            return score;
        }).ToList();
    }

    public static int AlphaBeta(Board board, int depth, int alpha, int beta)
    {
        // Caso base: profondità raggiunta
        if (depth == 0)
        {
            //return BoardEvaluation.Evaluate(board)  * (board.SideToMove == PieceColor.PC_WHITE ? 1 : -1);
            return QuiescenceSearch(board, alpha, beta); // Controllo le catture nella situazione statica
        }

        List<Move> moves = MoveGenerator.GetLegalMoves(board);

        // Scacco Matto / Stallo
        if (moves.Count == 0)
        {
            int kingSq = BitUtils.GetLSB(board.PieceBitboards[(int)board.SideToMove][(int)PieceType.PT_KING]);
            PieceColor enemy = (board.SideToMove == PieceColor.PC_WHITE) ? PieceColor.PC_BLACK : PieceColor.PC_WHITE;

            if (MoveGenerator.IsSquareAttacked(board, kingSq, enemy))
            {
                // Scacco matto
                // Restituisco un valore alto ma influenzato dalla profondità
                // (per preferire il matto più veloce)
                return -MateValue - depth;
            }
            return 0; // Stallo
        }

        // Alpha-Beta (Negamax)
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int score = -AlphaBeta(board, depth - 1, -beta, -alpha);
            board.UnmakeMove(move);

            // Trovata una mossa migliore per il giocatore attuale
            if (score >= beta)
            {
                return beta; // Beta-cutoff - l'avversario non mi permetterà di arrivare qui
            }

            if (score > alpha)
            {
                alpha = score; // Aggiorno il miglior punteggio garantito
            }
        }

        return alpha;
    }

    private static int QuiescenceSearch(Board board, int alpha, int beta, int qsDepth = 1)
    {

        // Valutazione statica come base
        int standPat = (int)(BoardEvaluation.Evaluate(board)) * (board.SideToMove == PieceColor.PC_WHITE ? 1 : -1);

        if (standPat >= beta) return beta;
        if (alpha < standPat) alpha = standPat;

        // Profondità massima
        if (qsDepth <= 0) return standPat;

        // Genera solo mosse di cattura
        var allMoves = MoveGenerator.GetLegalMoves(board, getCapturesOnly: true);
        foreach (var move in allMoves)
        {
            // Filtro manuale per le catture (molto più veloce di .Where)
            if (move.Flag != MoveFlag.Capture && (int)move.Flag < 12) continue;

            board.MakeMove(move);
            int score = -QuiescenceSearch(board, -beta, -alpha, qsDepth - 1);
            board.UnmakeMove(move);

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }

        return alpha;
    }
}
