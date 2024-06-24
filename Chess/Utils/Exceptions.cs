// *****************************************************
// *                                                   *
// * O Lord, Thank you for your goodness in our lives. *
// *     Please bless this code to our compilers.      *
// *                     Amen.                         *
// *                                                   *
// *****************************************************
//                                    Made by Geras1mleo

using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Chess;

public class ChessException : Exception
{
    public ChessComponent? Board { get; }
    public ChessException(ChessComponent? board, string message) : base(message) => Board = board;
}

public class ChessGameEndedException : ChessException
{
    public EndGameInfo EndgameInfo { get; }
    public ChessGameEndedException(ChessComponent board, EndGameInfo endgameInfo)
        : this(board, "This game is already ended.", endgameInfo) { }
    public ChessGameEndedException(ChessComponent board, string message, EndGameInfo endgameInfo) : base(board, message) => EndgameInfo = endgameInfo;
}


public class ChessInvalidMoveException : ChessException
{
    public Move Move { get; }
    public ChessInvalidMoveException(ChessComponent board, Move move)
        : this(board, $"Given move: {move} is invalid for current pieces positions.", move) { }
    public ChessInvalidMoveException(ChessComponent board, string message, Move move) : base(board, message) => Move = move;
}

public class ChessPieceNotFoundException : ChessException
{
    public Position Position { get; }
    public ChessPieceNotFoundException(ChessComponent board, Position position)
        : this(board, $"Piece on given position: {position} has been not found in current chess board", position) { }
    public ChessPieceNotFoundException(ChessComponent board, string message, Position position) : base(board, message) => Position = position;
}

public class ChessSanNotFoundException : ChessException
{
    public string SanMove { get; set; }
    public ChessSanNotFoundException(ChessComponent board, string san)
        : this(board, $"Given SAN move: {san} has been not found with current board positions.", san) { }
    public ChessSanNotFoundException(ChessComponent board, string message, string san) : base(board, message) => SanMove = san;
}

public class ChessSanTooAmbiguousException : ChessException
{
    public string SanMove { get; set; }
    public Move[] Moves { get; }
    public ChessSanTooAmbiguousException(ChessComponent board, string san, Move[] moves)
        : this(board, $"Given SAN move: {san} is too ambiguous between moves: {string.Join(", ", moves.Select(m => m.ToString()))}", san, moves) { }
    public ChessSanTooAmbiguousException(ChessComponent board, string message, string san, Move[] moves) : base(board, message)
    {
        SanMove = san;
        Moves = moves;
    }
}

public class ChessArgumentException : ChessException
{
    public ChessArgumentException(ChessComponent? board, string argument, string method)
        : this(board, $"An argument: {argument} in method: {method} is not valid...") { }
    public ChessArgumentException(ChessComponent? board, string message) : base(board, message) { }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
