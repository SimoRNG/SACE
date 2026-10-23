/*
 * Progetto dedicato all'interpretare comandi, rigirarli al motore di gioco
 * (il progetto core) ed inviarli via interfaccia UCI (Universal Chess Interface)
 */

using SACE_Core.Classes;
using SACE_Core.Utils;

namespace SACE_Engine;

public class Program
{
    private static Board _board = new Board();
    private static int _defaultDepth = 3;

    static void Main(string[] args)
    {
        Console.WriteLine("Ready.");

        Console.SetIn(new System.IO.StreamReader(Console.OpenStandardInput()));

        while (true)
        {
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;

            string[] tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string command = tokens[0].ToLower();

            switch (command)
            {
                case "uci":
                    Console.WriteLine("id name SACE v1.0");
                    Console.WriteLine("id author SR");
                    Console.WriteLine("uciok");
                    break;
                case "isready":
                    Console.WriteLine("readyok");
                    break;
                case "ucinewgame":
                    _board = new Board();
                    break;
                case "position":
                    HandlePosition(tokens);
                    break;
                case "go":
                    HandleGo();
                    break;
                case "quit":
                    return;
            }
        }
    }

    private static void HandlePosition(string[] tokens)
    {
        // UCI format: position [startpos | fen <fenstring>] (moves <move1> ... <moveN>)
        int moveStartIndex = -1;

        if (tokens[1] == "startpos")
        {
            _board = new Board(); // Reset alla posizione iniziale
            moveStartIndex = 2;
        }
        else if (tokens[1] == "fen")
        {
            moveStartIndex = 8; 
        }

        // Se ci sono mosse dopo "moves" vanno applicate alla scacchiera
        if (moveStartIndex < tokens.Length && tokens[moveStartIndex] == "moves")
        {
            for (int i = moveStartIndex + 1; i < tokens.Length; i++)
            {
                ApplyUciMove(tokens[i]);
            }
        }
    }

    private static void ApplyUciMove(string moveStr)
    {
        var legalMoves = MoveGenerator.GetLegalMoves(_board);
        foreach (var move in legalMoves)
        {
            string algebraic = ChessUtils.IndexToSquareName(move.From) +
                              ChessUtils.IndexToSquareName(move.To);

            // Gestione promozione e7e8q
            if ((int)move.Flag >= 8 && (int)move.Flag <= 11) algebraic += "q";

            if (algebraic == moveStr)
            {
                _board.MakeMove(move);
                return;
            }
        }
    }

    private static void HandleGo()//string[] tokens)
    {
        Move bestMove = SearchEngine.GetBestMove(_board, _defaultDepth);

        string moveStr = ChessUtils.IndexToSquareName(bestMove.From) +
                         ChessUtils.IndexToSquareName(bestMove.To);

        // Logica riduttiva al momento
        if ((int)bestMove.Flag >= 8 && (int)bestMove.Flag <= 11) moveStr += "q";

        Console.WriteLine($"bestmove {moveStr}");
    }
}