// *****************************************************
// *                                                   *
// * O Lord, Thank you for your goodness in our lives. *
// *     Please bless this code to our compilers.      *
// *                     Amen.                         *
// *                                                   *
// *****************************************************
//                                    Made by Geras1mleo

namespace Chess;

/// <summary>
/// https://www.chessprogramming.org/Draw
/// </summary>
internal abstract class EndGameRule
{
    protected ChessComponent board;

    internal abstract EndgameType Type { get; }

    internal EndGameRule(ChessComponent board)
    {
        this.board = board;
    }

    internal abstract bool IsEndGame();
}

