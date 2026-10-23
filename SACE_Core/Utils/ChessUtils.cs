using SACE_Core.Classes;
using SACE_Core.Enums;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SACE_Core.Utils;

internal static class ChessUtils
{
    public static readonly string FEN_START_POSITION = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    /// <summary>
    /// Valuta se la stringa passata come parametro è una posizione FEN valida
    /// </summary>

    public static bool IsValidFen(string fen)
    {
        if (string.IsNullOrWhiteSpace(fen)) return false;

        string[] parts = fen.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // La FEN standard ha 6 parti, ma molti motori accettano anche solo le prime 4 
        // (Posizione, Turno, Arrocco, En Passant)
        if (parts.Length < 4 || parts.Length > 6) return false;

        // Validazione Posizione Pezzi
        string[] ranks = parts[0].Split('/');
        if (ranks.Length != 8) return false;

        foreach (string rank in ranks)
        {
            int count = 0;
            foreach (char c in rank)
            {
                if (char.IsDigit(c))
                {
                    int val = c - '0';
                    if (val == 0 || val > 8) return false;
                    count += val;
                }
                else if ("pnbrqkPNBRQK".Contains(c))
                {
                    count++;
                }
                else return false;
            }
            if (count != 8) return false;
        }

        // Validazione Turno
        if (parts[1] != "w" && parts[1] != "b") return false;

        // Validazione Arrocco
        // Deve essere "-" oppure una combinazione di KQkq senza ripetizioni
        string castling = parts[2];
        if (castling != "-")
        {
            if (castling.Length > 4 || castling.Length == 0) return false;
            // Verifica che contenga solo caratteri validi e senza duplicati
            if (castling.Any(c => !"KQkq".Contains(c)) || castling.Distinct().Count() != castling.Length)
                return false;
        }

        // Validazione En Passant
        if (parts[3] != "-")
        {
            // Deve essere una coordinata valida
            if (!Regex.IsMatch(parts[3], @"^[a-h][1-8]$")) return false;
        }

        // Validazione Halfmove e Fullmove (potrebbero mancare nella stringa)
        if (parts.Length > 4)
        {
            if (!int.TryParse(parts[4], out int halfmove) || halfmove < 0) return false;
        }

        if (parts.Length > 5)
        {
            if (!int.TryParse(parts[5], out int fullmove) || fullmove < 0) return false;
        }

        return true;
    }


    /// <summary>
    /// Ritorna true se l'indice passato in input è una casa dove promuovo il pedone
    /// </summary>
    public static bool IsPromotionSquare(PieceColor pieceColor, short index)
    {
        if (pieceColor == PieceColor.PC_WHITE) return index >= 56 && index <= 63;
        else return index >= 0 && index <= 7;
    }

    /// <summary>
    /// Ritorna true se la casa indicata è la casa di partenza di un pedone
    /// </summary>
    public static bool IsPawnStartingSquare(PieceColor pieceColor, short index)
    {
        if (pieceColor == PieceColor.PC_WHITE) return index >= 8 && index <= 15;
        else return index >= 48 && index <= 55;
    }

    /// <summary>
    /// Ritorna il nome della casa a partire dall'indice
    /// </summary>
    public static string IndexToSquareName(int index)
    {
        if (!BitUtils.IsUlongIndexValid((short)index)) return "";

        char colonna = (char)('a' + (index % 8));
        char riga = (char)('1' + (index / 8));

        return $"{colonna}{riga}";
    }

    /// <summary>
    /// Ritorna il tipo ed il colore del pezzo in base al carattere passato come parametro (trascodifica)
    /// </summary>
    public static (PieceColor color, PieceType type) CharToPiece(char c)
    {
        PieceColor color = char.IsUpper(c) ? PieceColor.PC_WHITE : PieceColor.PC_BLACK;
        char lowerC = char.ToLower(c);

        PieceType type = lowerC switch
        {
            'p' => PieceType.PT_PAWN,
            'n' => PieceType.PT_KNIGHT,
            'b' => PieceType.PT_BISHOP,
            'r' => PieceType.PT_ROOK,
            'q' => PieceType.PT_QUEEN,
            'k' => PieceType.PT_KING,
            _ => throw new System.ArgumentException("Carattere FEN non valido")
        };

        return (color, type);
    }

    /// <summary>
    /// Stampa le caselle occupate
    /// </summary>
    public static void PrintOccupiedSquares(Board b)
    {
        for (int rank = 7; rank >= 0; rank--)
        {
            string row = "";
            for (int file = 0; file < 8; file++)
            {
                ulong mask = 1UL << (rank * 8 + file);
                bool occupies = (b.GetOccupied() & mask) != 0;
                row += occupies ? "X " : ". ";
            }
            Debug.WriteLine(row + "  " + (rank + 1));
        }
        Debug.WriteLine("a b c d e f g h");
    }

    /// <summary>
    /// Ritorna la notazione algebrica della mossa
    /// </summary>
    public static string MoveToAlgebraic(Move move)
    {
        // TODO : completare per bene

        if (move.From == 0 && move.To == 0) return "none";

        string from = ChessUtils.IndexToSquareName(move.From);
        string to = ChessUtils.IndexToSquareName(move.To);

        string promo = "";
        if ((int)move.Flag >= 8 && (int)move.Flag <= 11) promo = "q";

        return from + to + promo;
    }
}
