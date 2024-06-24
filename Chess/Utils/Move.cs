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
/// Move on chess board
/// </summary>
public class Move
{
    /// <summary>
    /// Whether Positions are initialized
    /// </summary>
    public bool HasValue => OriginalPosition.HasValue && NewPosition.HasValue;

    /// <summary>
    /// Original Cell
    /// </summary>
    public CellComponent Cell { get; internal set; }
    
    /// <summary>
    /// Moved Piece
    /// </summary>
    public FigureComponent Piece { get; internal set; }

    /// <summary>
    /// Original position of moved piece
    /// </summary>
    public Position OriginalPosition { get; internal set; }

    /// <summary>
    /// New Position of moved piece
    /// </summary>
    public Position NewPosition { get; internal set; }

    /// <summary>
    /// Captured piece (if exist) or null
    /// </summary>
    public FigureComponent? CapturedFigure { get; internal set; }
    
    /// <summary>
    /// Original piece (if exist) or null
    /// </summary>
    public FigureComponent? OriginalFigure { get; internal set; }
    
    
    /// <summary>
    /// Move additional parameter   
    /// </summary>
    public IMoveParameter? Parameter { get; internal set; }

    /// <summary>
    /// Move places opponent's king in check? => true
    /// </summary>
    public bool IsCheck { get; internal set; }

    /// <summary>
    /// Move places opponent's king in checkmate => true
    /// </summary>
    public bool IsMate { get; internal set; }
    
    /// <summary>
    /// Initializes new Move object by given positions
    /// </summary>
    public Move(Position originalPosition, Position newPosition)
    {
        OriginalPosition = originalPosition;
        NewPosition = newPosition;
    }
    
    internal Move(Move source)
    {
	    if (source.Piece is not null)
		    Piece = source.Piece;

	    OriginalPosition = new Position(source.OriginalPosition.X, source.OriginalPosition.Y);
	    NewPosition = new Position(source.NewPosition.X, source.NewPosition.Y);

	    if (source.CapturedFigure is not null)
		    CapturedFigure = source.CapturedFigure;

	    if (source.Parameter is not null)
		    Parameter = IMoveParameter.FromString(source.Parameter.ShortStr);

	    IsCheck = source.IsCheck;
	    IsMate = source.IsMate;
    }

    /// <summary>
    /// Needed to Generate move from SAN in ChessConversions
    /// </summary>
    internal Move()
    {
        OriginalPosition = new();
        NewPosition = new();
    }

    public static Builder Create()
    {
	    return new Builder();
    }

    public class Builder
    {
	    Move move;
	    
	    public Builder()
	    {
		    move = new Move();
	    }

	    public Builder SetOriginalPosition( Position position )
	    {
		    move.OriginalPosition = position;
		    return this;
	    }
	    
	    public Builder SetNewPosition( Position position )
	    {
		    move.NewPosition = position;
		    return this;
	    }
	    
	    public Builder SetCell( CellComponent cell )
	    {
		    move.Cell = cell;
		    return this;
	    }
	    
	    public Builder SetFigure( FigureComponent figure )
	    {
		    move.Piece = figure;
		    return this;
	    }

	    public Move Build()
	    {
		    return move;
	    }
    }
}
