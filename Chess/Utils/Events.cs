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

public delegate void ChessEventHandler(object sender, ChessEventArgs e);
public delegate void ChessCheckedChangedEventHandler(object sender, CheckEventArgs e);
public delegate void ChessEndGameEventHandler(object sender, EndgameEventArgs e);
public delegate void ChessCaptureEventHandler(object sender, CaptureEventArgs e);
public delegate void ChessPromotionResultEventHandler(object sender, PromotionEventArgs e);

public abstract class ChessEventArgs : EventArgs
{
    public ChessComponent ChessBoard { get; }

    protected ChessEventArgs(ChessComponent chessBoard)
    {
        ChessBoard = chessBoard;
    }
}
public class CaptureEventArgs : ChessEventArgs
{
    /// <summary>
    /// Piece that has been captured
    /// </summary>
    public FigureComponent CapturedPiece { get; }

    /// <summary>
    /// List of captured pieces where color == White
    /// </summary>
    public FigureComponent[] WhiteCapturedPieces { get; set; }

    /// <summary>
    /// List of captured pieces where color == Black
    /// </summary>
    public FigureComponent[] BlackCapturedPieces { get; set; }

    public CaptureEventArgs(ChessComponent chessBoard, FigureComponent capturedPiece, FigureComponent[] whiteCapturedPieces, FigureComponent[] blackCapturedPieces) : base(chessBoard)
    {
        CapturedPiece = capturedPiece;
        WhiteCapturedPieces = whiteCapturedPieces;
        BlackCapturedPieces = blackCapturedPieces;
    }
}

public class EndgameEventArgs : ChessEventArgs
{
    /// <summary>
    /// End game additional info
    /// </summary>
    public EndGameInfo EndgameInfo { get; }

    public EndgameEventArgs(ChessComponent chessBoard, EndGameInfo endgameInfo) : base(chessBoard)
    {
        EndgameInfo = endgameInfo;
    }
}

public class CheckEventArgs : ChessEventArgs
{
    /// <summary>
    /// Position of checked king
    /// </summary>
    public Position KingPosition { get; }

    /// <summary>
    /// Checked state
    /// </summary>
    public bool IsChecked { get; }

    public CheckEventArgs(ChessComponent chessBoard, Position kingPosition, bool isChecked) : base(chessBoard)
    {
        KingPosition = kingPosition;
        IsChecked = isChecked;
    }
}

public class PromotionEventArgs : ChessEventArgs
{
    /// <summary>
    /// Specified by user promotion result
    /// </summary>
    public PromotionType PromotionResult { get; set; } = PromotionType.Default;

    public PromotionEventArgs(ChessComponent chessBoard) : base(chessBoard) { }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
