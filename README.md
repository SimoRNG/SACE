# Chess Engine (Studio Bitboard & Alpha-Beta)

Sviluppato per scopi di studio. Clonabile in Visual Studio. Progetto di avvio: SACE_Engine

# Obiettivi
* Bitboard: Sostituire la classica rappresentazione a matrice 8x8 con manipolazioni di interi a 64 bit
* Alpha-Beta Pruning: Implementazione dell'algoritmo di ricerca minimax ottimizzato con il taglio alfa-beta
* Testing con xUnit: Utilizzo di test automatici (in particolare test di generazione delle mosse / perft)

# Stato attuale
* Funzionamento: Il programma è avviabile e permette di giocare una partita usando la notazione standard UCI, ma il bot è debole
* Bug / Crash: Il programma in determinati scenari può inchiodarsi o fallire (da investigare)
