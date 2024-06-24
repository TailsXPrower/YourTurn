// *****************************************************
// *                                                   *
// * O Lord, Thank you for your goodness in our lives. *
// *     Please bless this code to our compilers.      *
// *                     Amen.                         *
// *                                                   *
// *****************************************************
//                                    Made by Geras1mleo

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Chess;

/// <summary>
/// Special move Parameter<br/>
/// Castle<br/>
/// EnPassant<br/>
/// Pawn Promotion
/// </summary>
public interface IMoveParameter
{
    /// <summary>
    /// Parameter as short(SAN/LAN) string
    /// </summary>
    public string ShortStr { get; }

    /// <summary>
    /// Parameter as long(Comment) string
    /// </summary>
    public string LongStr { get; }

    /// <summary>
    /// Special Move dropping implementation
    /// </summary>
    internal void Execute(Move move, ChessComponent board);

    internal void Undo(Move move, ChessComponent board);

    internal static IMoveParameter FromString(string parameter)
    {
        return parameter.ToLower() switch
        {
            "o-o" => new MoveCastle(CastleType.King),
            "o-o-o" => new MoveCastle(CastleType.Queen),
            "e.p." => new MoveEnPassant(),
            "=" => new MovePromotion(PromotionType.Default),
            "=q" => new MovePromotion(PromotionType.ToQueen),
            "=r" => new MovePromotion(PromotionType.ToRook),
            "=b" => new MovePromotion(PromotionType.ToBishop),
            "=n" => new MovePromotion(PromotionType.ToKnight),
            _ => new MovePromotion( PromotionType.Default ),
        };
    }
}

public class MoveCastle : IMoveParameter
{
    public CastleType CastleType { get; private set; }

    public string ShortStr
    {
        get
        {
            return CastleType switch
            {
                CastleType.King => "O-O",
                CastleType.Queen => "O-O-O",
                _ => throw new ChessArgumentException(null, nameof(CastleType), nameof(MoveCastle.ShortStr))
            };
        }
    }

    public string LongStr
    {
        get
        {
            return CastleType switch
            {
                CastleType.King => "King Side Castle",
                CastleType.Queen => "Queen Side Castle",
                _ => throw new ChessArgumentException(null, nameof(CastleType), nameof(MoveCastle.LongStr))
            };
        }
    }

    void IMoveParameter.Execute(Move move, ChessComponent board)
    {
        var y = move.NewPosition.Y;
        switch (CastleType)
        {
            case CastleType.King:
	            board[y, 4].Cell = board.Pieces[y, 6].GameObject.Id;
                board[y, 7].Cell = board.Pieces[y, 5].GameObject.Id;
                break;
            case CastleType.Queen:
	            board[y, 4].Cell = board.Pieces[y, 2].GameObject.Id;
	            board[y, 0].Cell = board.Pieces[y, 3].GameObject.Id;
                break;
            default:
                throw new ChessArgumentException(board, nameof(CastleType), nameof(IMoveParameter.Execute));
        }
    }

    void IMoveParameter.Undo(Move move, ChessComponent board)
    {
        var y = move.NewPosition.Y;
        switch (CastleType)
        {
            case CastleType.King:
	            board.Pieces[y, 6].GetPiece().Cell = board.Pieces[y, 4].GameObject.Id;
                board.Pieces[y, 5].GetPiece().Cell = board.Pieces[y, 7].GameObject.Id;
                break;
            case CastleType.Queen:
	            board.Pieces[y, 2].GetPiece().Cell = board.Pieces[y, 4].GameObject.Id;
                board.Pieces[y, 3].GetPiece().Cell = board.Pieces[y, 0].GameObject.Id;
                break;
            default:
                throw new ChessArgumentException(board, nameof(CastleType), nameof(IMoveParameter.Undo));
        }
    }

    internal MoveCastle(CastleType castleType)
    {
        CastleType = castleType;
    }
}

public class MoveEnPassant : IMoveParameter
{
    public Position CapturedPawnPosition { get; internal set; }

    public string ShortStr => "e.p.";

    public string LongStr => "En Passant";

    void IMoveParameter.Execute(Move move, ChessComponent board)
    {
        //ChessComponent.DropPiece(move, board);

        board.Pieces[CapturedPawnPosition.Y, CapturedPawnPosition.X].Current = null;
    }

    void IMoveParameter.Undo(Move move, ChessComponent board)
    {
        //ChessComponent.RestorePiece(move, board);

        if (move.CapturedFigure == null)
	        return;
        
        board.Pieces[CapturedPawnPosition.Y, CapturedPawnPosition.X].Current = move.CapturedFigure.GameObject;
    }
}

public class MovePromotion : IMoveParameter
{
    public PromotionType PromotionType { get; internal set; }

    public string ShortStr
    {
        get
        {
            return PromotionType switch
            {
                PromotionType.Default => "=Q",
                PromotionType.ToQueen => "=Q",
                PromotionType.ToRook => "=R",
                PromotionType.ToBishop => "=B",
                PromotionType.ToKnight => "=N",
                _ => throw new ChessArgumentException(null, nameof(PromotionType), nameof(MovePromotion.ShortStr))
            };
        }
    }

    public string LongStr
    {
        get
        {
            return PromotionType switch
            {
                PromotionType.Default => "Default Promotion",
                PromotionType.ToQueen => "Promotion To Queen",
                PromotionType.ToRook => "Promotion To Rook",
                PromotionType.ToBishop => "Promotion To Bishop",
                PromotionType.ToKnight => "Promotion To Knight",
                _ => throw new ChessArgumentException(null, nameof(PromotionType), nameof(MovePromotion.LongStr))
            };
        }
    }

    void IMoveParameter.Execute(Move move, ChessComponent board)
    {
        //ChessComponent.DropPiece(move, board);

        // Making sure original type(pawn) is saved

        move.Piece.Type = PromotionType switch
        {
            PromotionType.ToQueen or PromotionType.Default => FigureType.Queen,
            PromotionType.ToRook => FigureType.Rook,
            PromotionType.ToBishop => FigureType.Bishop,
            PromotionType.ToKnight => FigureType.Knight,
            _ => throw new ChessArgumentException(board, nameof(PromotionType), nameof(IMoveParameter.Execute)),
        };
    }

    void IMoveParameter.Undo(Move move, ChessComponent board)
    {
        //ChessComponent.RestorePiece(move, board);

        move.Piece.Type = FigureType.Pawn;
    }

    internal MovePromotion(PromotionType promotionType)
    {
        PromotionType = promotionType;
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
