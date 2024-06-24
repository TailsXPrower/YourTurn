// *****************************************************
// *                                                   *
// * O Lord, Thank you for your goodness in our lives. *
// *     Please bless this code to our compilers.      *
// *                     Amen.                         *
// *                                                   *
// *****************************************************
//                                    Made by Geras1mleo

using System;
using Chess;
using System.Threading;

namespace Sandbox;

public partial class ChessComponent
{
    /// <summary>
    /// Raises when trying to make or validate move but after the move would have been made, king would have been checked
    /// </summary>
    public event ChessCheckedChangedEventHandler OnInvalidMoveKingChecked = delegate { };
    /// <summary>
    /// Raises when white king is (un)checked
    /// </summary>
    public event ChessCheckedChangedEventHandler OnWhiteKingCheckedChanged = delegate { };
    /// <summary>
    /// Raises when black king is (un)checked
    /// </summary>
    public event ChessCheckedChangedEventHandler OnBlackKingCheckedChanged = delegate { };
    /// <summary>
    /// Raises when user has to choose promotion action
    /// </summary>
    public event ChessPromotionResultEventHandler OnPromotePawn = delegate { };
    /// <summary>
    /// Raises when it's end of game
    /// </summary>
    public event ChessEndGameEventHandler OnEndGame = delegate { };
    /// <summary>
    /// Raises when any piece has been captured
    /// </summary>
    public event ChessCaptureEventHandler OnCaptured = delegate { };

    private void OnWhiteKingCheckedChangedEvent(CheckEventArgs e)
    {
	    OnWhiteKingCheckedChanged(this, e);
    }

    private void OnBlackKingCheckedChangedEvent(CheckEventArgs e)
    {
	    OnBlackKingCheckedChanged(this, e);
    }

    private void OnInvalidMoveKingCheckedEvent(CheckEventArgs e)
    {
	    OnInvalidMoveKingChecked(this, e);
    }

    private void OnPromotePawnEvent(PromotionEventArgs e)
    {
	    OnPromotePawn(this, e);
    }

    private void OnEndGameEvent()
    {
	    OnEndGame(this, new EndgameEventArgs(this, EndGame));
    }

    private void OnCapturedEvent(FigureComponent piece)
    {
	    OnCaptured(this, new CaptureEventArgs(this, piece, CapturedWhite, CapturedBlack));
    }
}
