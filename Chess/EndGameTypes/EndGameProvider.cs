// *****************************************************
// *                                                   *
// * O Lord, Thank you for your goodness in our lives. *
// *     Please bless this code to our compilers.      *
// *                     Amen.                         *
// *                                                   *
// *****************************************************
//                                    Made by Geras1mleo

namespace Chess;

internal class EndGameProvider
{
    private readonly ChessComponent board;
    private readonly List<EndGameRule> rules = new();

    public EndGameProvider(ChessComponent board)
    {
        this.board = board;
    }

    public EndGameInfo GetEndGameInfo()
    {
        EndGameInfo endgameInfo = null;

        if (board.moveIndex >= 0 && board.executedMoves[board.moveIndex].IsMate)
        {
	        endgameInfo = new EndGameInfo(EndgameType.Checkmate, board.Turn == FigureColor.Black ? FigureColor.White : FigureColor.Black);
        }
        
        if (board.BlackKing == new Position())
	        endgameInfo = new EndGameInfo(EndgameType.Checkmate, FigureColor.White);
        
        if (board.WhiteKing == new Position())
	        endgameInfo = new EndGameInfo(EndgameType.Checkmate, FigureColor.Black);

        if (endgameInfo is null)
        {
            endgameInfo = ResolveDrawRules();
        }

        return endgameInfo;
    }

    private EndGameInfo ResolveDrawRules()
    {
        EndGameInfo endgameInfo = null;
        
        for (var i = 0; i < rules.Count && endgameInfo is null; i++)
        {
	        if (rules[i].IsEndGame())
            {
                endgameInfo = new EndGameInfo(rules[i].Type, null);
            }
        }

        return endgameInfo;
    }

    public void UpdateRules()
    {
        rules.Clear();

        if ((board.AutoEndgameRules & AutoEndgameRules.InsufficientMaterial) == AutoEndgameRules.InsufficientMaterial)
            rules.Add(new InsufficientMaterialRule(board));

        if ((board.AutoEndgameRules & AutoEndgameRules.FiftyMoveRule) == AutoEndgameRules.FiftyMoveRule)
            rules.Add(new FiftyMoveRule(board));
    }
}
