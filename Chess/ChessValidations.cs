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
using Menu;

namespace Sandbox;

public partial class ChessComponent
{
	/// <summary>
    /// Checks if given move is valid for current pieces positions
    /// </summary>
    /// <param name="move">Move to be checked</param>
    /// <returns>Whether given move is valid</returns>
    public bool IsValidMove(Move move)
    {
        return IsValidMove(move, this, false, true);
    }
	
	public bool IsValidMove(Move move, bool checkTurn)
	{
		return IsValidMove(move, this, false, checkTurn);
	}

	public bool IsCheck( FigureColor side )
	{
		return IsKingChecked( side, this );
	}
	
	public bool IsCheck( FigureColor side, Move move )
	{
		return IsKingCheckedValidation( move, side, this );
	}
	
	public bool IsCheckmate( FigureColor side )
	{
		return IsCheckmate( side, this );
	}
	
	public bool IsStalemate( FigureColor side )
	{
		return IsStalemate( side, this );
	}

    private static bool IsCheckmate(FigureColor side, ChessComponent board)
    {
        return IsKingChecked(side, board) && !PlayerHasMoves(side, board);
    }

    private static bool IsStalemate(FigureColor side, ChessComponent board)
    {
	    return !IsKingChecked(side, board) && !PlayerHasMoves(side, board);
    }

    private static Position GetKingPosition(FigureColor side, ChessComponent board)
    {
        var kingPos = new Position();
        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
	            var piece = board[i, j];
	            if ( piece?.Color != side || piece?.Type != FigureType.King )
		            continue;
	            
	            kingPos = new Position { Y = i, X = j, };
                return kingPos;
            }
        }
        return kingPos;
    }

    // TODO: populateProperties = false/true
    internal static bool IsValidMove(Move move, ChessComponent board, bool raise, bool checkTurn)
    {
	    if (move is null || !move.HasValue)
		    throw new ArgumentNullException(nameof(move));
	    
	    var piece = board[move.OriginalPosition.Y, move.OriginalPosition.X];

	    if (piece == null)
		    throw new ChessPieceNotFoundException(board, move.OriginalPosition);

	    if ( checkTurn && piece.Color != board.Turn )
		    return false;

	    if (move.OriginalPosition == move.NewPosition)
		    return false;

	    MovePromotion? promParams = null;
	    // Promotion result can be already specified so we dont need to invoke event again to ask for prom result
	    if (move.Parameter is MovePromotion p)
		    promParams = new MovePromotion(p.PromotionType);

	    ResetMoveProperties(move, piece);

	    bool isValid = IsValidMove(move, board);
	    // If is not valid => don't validate further
	    bool isChecked = !isValid || IsKingCheckedValidation(move, move.Piece.Color, board);

	    if (!isChecked)
	    {
		    ValidMoveSetProperties(move, board, raise, promParams);

		    return true;
	    }
	    else
	    {
		    if (isValid && raise)
		    {
			    //board.OnInvalidMoveKingCheckedEvent(new CheckEventArgs(board, move.Piece.Color == PieceColor.White ? board.WhiteKing : board.BlackKing, true));
		    }
		    return false;
	    }
    }

    private static void ResetMoveProperties(Move move, FigureComponent piece)
    {
	    move.Piece = piece;
        move.IsCheck = false;
        move.IsMate = false;
    }

    private static void ValidMoveSetProperties(Move move, ChessComponent board, bool raise, MovePromotion? promParams)
    {
        // If capturing some pieces
        var capturedPiece = board[move.NewPosition.Y, move.NewPosition.X];
        if (capturedPiece != null && capturedPiece.Color != move.Piece.Color)
        {
            move.CapturedFigure = capturedPiece;
        }

        // Promotion: Invoke event only when raises == true AND promotion parameters has been not specified yet
        if (move.Parameter is MovePromotion promotion)
        {
            if (promParams != null && promParams.PromotionType != PromotionType.Default)
            {
                move.Parameter = promParams;
            }
            else if (raise)
            {
                var args = new PromotionEventArgs(board);
                //board.OnPromotePawnEvent(args);
                promotion.PromotionType = args.PromotionResult;
            }
        }

        move.IsCheck = IsKingCheckedValidation(move, move.Piece.GetOppositeColor(), board); // Whether there is a check on opposite king
        move.IsMate = !PlayerHasMovesValidation(move, move.Piece.GetOppositeColor(), board); // Whether the opposite king is in (stale)mate
    }

    internal static bool IsValidMove(Move move, ChessComponent board)
    {
        return move.Piece.Type switch
        {
            FigureType.Pawn => PawnValidation(move, board),
            FigureType.Rook => RookValidation(move, board.Pieces),
            FigureType.Knight => KnightValidation(move, board.Pieces),
            FigureType.Bishop => BishopValidation(move, board.Pieces),
            FigureType.Queen => QueenValidation(move, board.Pieces),
            FigureType.King => KingValidation(move, board),
            _ => false
        };
    }

    /// <summary>
    /// Basically checking if after the move has been executed
    /// the next move onto position of king is valid for one of pieces of opponent
    /// </summary>
    internal static bool IsKingCheckedValidation(Move move, FigureColor side, ChessComponent board)
    {
	    // If validating castle move
        if (move.Parameter is MoveCastle castle && move.Piece.Color == side && move.Piece is not null)
            return IsKingCheckedWhileCastling(side, board, castle);

        //Log.Info("board[y, 4] 1.0: "+ (board[7, 4] == null));
        
        move.OriginalFigure = board[move.NewPosition];
        board.DropPieceToNewPosition(move);
        var isChecked = IsKingChecked(side, board);
        board.Cancel();
        move.OriginalFigure = null;

        return isChecked;
    }

    private static bool IsKingCheckedWhileCastling(FigureColor side, ChessComponent board, MoveCastle castle)
    {
	    var isCheck = false;
        var kingPos = GetKingPosition(side, board);
        var step = (short)(castle.CastleType == CastleType.King ? 1 : -1);

        var i = kingPos.X;
        while (i is < MAX_COLS - 1 and > 1 && !isCheck)
        {
            isCheck = IsKingCheckedValidation(new Move(kingPos, new Position { Y = kingPos.Y, X = i }) { Piece = board[kingPos], Cell = board[kingPos].GetCellComponent() }, side, board);
            i += step;
        }

        return isCheck;
    }

    private static bool IsKingChecked(FigureColor side, ChessComponent board)
    {
        var kingPos = GetKingPosition(side, board);

        // move in Validation => King is being captured!
        if (!kingPos.HasValue)
            return false;

        for (short i = 0; i < MAX_ROWS; i++)
        {
            for (short j = 0; j < MAX_COLS; j++)
            {
                var piece = board[i, j];
                if (piece == null || piece.Color == side)
                    continue;
                if (kingPos.X == j && kingPos.Y == i)
                    continue;

                if (IsValidMove(new Move(new Position { Y = i, X = j }, kingPos) { Piece = piece, Cell = piece.GetCellComponent() }, board))
                    return true;
            }
        }

        return false;
    }

    private static bool PlayerHasMovesValidation(Move move, FigureColor side, ChessComponent board)
    {
	    move.OriginalFigure = board[move.NewPosition];
	    board.DropPieceToNewPosition(move);
	    var playerHasMoves = PlayerHasMoves(side, board);
	    board.Cancel();
	    move.OriginalFigure = null;
	    return playerHasMoves;
    }
    
    internal static bool PlayerHasMoves(FigureColor side, ChessComponent board)
    {
        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                var piece = board[i, j];
                if (piece == null || piece.Color != side)
                    continue;

                var fromPosition = new Position { Y = i, X = j };

                foreach (var position in GeneratePositions(fromPosition, board))
                {
                    var move = new Move(fromPosition, position) { Piece = piece };

                    if (piece.Type == FigureType.King)
                        KingValidation(move, board); // Needed to specify castling options that are required in the IsKingCheckedValidation

                    if (!IsKingCheckedValidation(move, side, board))
                        return true;
                }
            }
        }

        return false;
    }

    private static bool PawnValidation(Move move, ChessComponent board)
    {
	    var isValid = false;

        var verticalDifference = (short)(move.NewPosition.Y - move.OriginalPosition.Y);
        var horizontalDifference = (short)(move.NewPosition.X - move.OriginalPosition.X);

        var verticalStep = Math.Abs(verticalDifference);
        var horizontalStep = Math.Abs(horizontalDifference);

        var pieceColor = move.Piece.Color;

        // If the pawn is moving forward
        if ( (pieceColor != FigureColor.Black || verticalDifference <= 0) && (pieceColor != FigureColor.White || verticalDifference >= 0) )
	        return false;
        
        // Check if moving 1 step forward
        if (horizontalStep == 0 && verticalStep == 1 && board[move.NewPosition.Y, move.NewPosition.X] == null)
        {
	        HandlePotentialPromotion(move);
	        isValid = true;
        }
        // Check if moving 2 steps forward at the beginning
        else if (horizontalStep == 0 && verticalStep == 2
                                     && ((move.OriginalPosition.Y == 1 && board[2, move.NewPosition.X] == null &&
                                          board[3, move.NewPosition.X] == null)
                                         || (move.OriginalPosition.Y == 6 && board[5, move.NewPosition.X] == null &&
                                             board[4, move.NewPosition.X] == null)))
        {
	        isValid = true;
        }
        // Check if taking piece of opponent
        else if (verticalStep == 1 && horizontalStep == 1
                                   && board[move.NewPosition.Y, move.NewPosition.X] != null
                                   && pieceColor != board[move.NewPosition.Y, move.NewPosition.X].Color)
        {
	        HandlePotentialPromotion(move);
	        isValid = true;
        }
        // Check if the move is valid en passant
        else if (IsValidEnPassant(move, board, verticalDifference, horizontalDifference))
        {
	        if (true)
		        Log.Info("[Debug] EN PASSANT");
	       
	        HandleEnPassant(move, verticalDifference, board);
	        isValid = true;
	    }

        return isValid;
    }

    public static void HandlePotentialPromotion(Move move)
    {
        if (move.NewPosition.Y % (MAX_ROWS - 1) == 0)
        {
            move.Parameter = new MovePromotion(PromotionType.Default);
        }
    }

    private static void HandleEnPassant(Move move, short verticalDifference, ChessComponent board)
    {
        move.Parameter = new MoveEnPassant()
        {
            CapturedPawnPosition = new Position()
            {
                Y = (short)(move.NewPosition.Y - verticalDifference),
                X = move.NewPosition.X
            }
        };
        move.CapturedFigure = board[(move.Parameter as MoveEnPassant).CapturedPawnPosition];
    }

    private static bool IsValidEnPassant(Move move, ChessComponent board, short v, short h)
    {
        if (Math.Abs(v) == 1 && Math.Abs(h) == 1) // Capture attempt
        {
            var piece = board[move.NewPosition.Y - v, move.NewPosition.X];

            // if on given new (position.y => one back) is a pawn with opposite color
            if (piece is not null && piece.Color != move.Piece.Color && piece.Type == FigureType.Pawn)
            {
                return LastMoveEnPassantPosition(board) == move.NewPosition;
            }
        }

        return false;
    }

    public static bool QueenValidation(Move move, CellComponent[,] pieces)
    {
        // For queen just using validation of bishop OR rook
        return BishopValidation(move, pieces) || RookValidation(move, pieces);
    }

    private static bool RookValidation(Move move, CellComponent[,] pieces)
    {
        int verticalDiff = move.NewPosition.Y - move.OriginalPosition.Y;
        int horizontalDiff = move.NewPosition.X - move.OriginalPosition.X;

        // If not moving straight
        if (verticalDiff != 0 && horizontalDiff != 0)
            return false;

        int stepVertical = Math.Sign(verticalDiff);
        int stepHorizontal = Math.Sign(horizontalDiff);

        int i = move.OriginalPosition.Y + stepVertical;
        int j = move.OriginalPosition.X + stepHorizontal;

        while (i != move.NewPosition.Y || j != move.NewPosition.X)
        {
	        if ( pieces[i, j].GetPiece() != null )
	        {
		        return false;
	        }

	        i += stepVertical;
            j += stepHorizontal;
        }

        return true;
    }

    private static bool KnightValidation(Move move, CellComponent[,] pieces)
    {
	    int verticalDiff = Math.Abs(move.NewPosition.X - move.OriginalPosition.X);
        int horizontalDiff = Math.Abs(move.NewPosition.Y - move.OriginalPosition.Y);

        // Check if the move is in a L-shape: 2 steps horizontally and 1 step vertically, or vice versa
        if ((verticalDiff == 2 && horizontalDiff == 1) || (verticalDiff == 1 && horizontalDiff == 2))
            return pieces[move.NewPosition.Y, move.NewPosition.X].Color != move.Cell.Color;

        return false;
    }

    private static bool BishopValidation(Move move, CellComponent[,] pieces)
    {
        var verticalDiff = move.NewPosition.Y - move.OriginalPosition.Y;
        var horizontalDiff = move.NewPosition.X - move.OriginalPosition.X;

        // If not moving diagonal
        if (Math.Abs(verticalDiff) != Math.Abs(horizontalDiff))
            return false;

        var stepVertical = Math.Sign(verticalDiff);
        var stepHorizontal = Math.Sign(horizontalDiff);

        int i = move.OriginalPosition.Y + stepVertical;
        int j = move.OriginalPosition.X + stepHorizontal;

        while (i != move.NewPosition.Y && j != move.NewPosition.X)
        {
            if (pieces[i, j].GetPiece() != null)
                return false;

            i += stepVertical;
            j += stepHorizontal;
        }

        return pieces[i, j].Color == move.Cell.Color;
    }

    private static bool KingValidation(Move move, ChessComponent board)
    {
        if (Math.Abs(move.NewPosition.X - move.OriginalPosition.X) < 2 && Math.Abs(move.NewPosition.Y - move.OriginalPosition.Y) < 2)
        {
            // Piece(if exist) has different color than king
            return board.Pieces[move.NewPosition.Y, move.NewPosition.X].GetPiece()?.Color != move.Piece.Color;
        }

        // Castle validation:

        var kingMovesHorizontally = move.OriginalPosition.Y == move.NewPosition.Y;
        var kingOnBeginPos = move.OriginalPosition.X == 4 && move.OriginalPosition.Y % 7 == 0;

        if (!kingOnBeginPos || !kingMovesHorizontally)
            return false;

        var kingMoves2Tiles = Math.Abs(move.NewPosition.X - move.OriginalPosition.X) == 2;
        var kingMovesOnRook = move.NewPosition.X % 7 == 0;

        if (!kingMovesOnRook && !kingMoves2Tiles)
            return false;

        // Standardise x-pos for checks
        var x = kingMovesOnRook ? (move.NewPosition.X == 0 ? 2 : 6) : move.NewPosition.X;

        var isKingSideCastle = x == 6;
        var isQueenSideCastle = x == 2;

        var moveCastle = isKingSideCastle ? new MoveCastle(CastleType.King) : new MoveCastle(CastleType.Queen);
        move.Parameter = moveCastle;

        int y = move.NewPosition.Y;

        var hasObstacles = true;

        if (isQueenSideCastle)
            hasObstacles = board.Pieces[y, 1].GetPiece() != null || board.Pieces[y, 2].GetPiece() != null || board.Pieces[y, 3].GetPiece() != null;
        else if (isKingSideCastle)
            hasObstacles = board.Pieces[y, 5].GetPiece() != null || board.Pieces[y, 6].GetPiece() != null;

        var isValid = !hasObstacles && HasRightToCastle(move.Piece.Color, moveCastle.CastleType, board);

        // Standardise castling position
        if (board.StandardiseCastlingPositions && isValid && kingMovesOnRook)
            move.NewPosition = new Position((short)(move.NewPosition.X == 0 ? 2 : 6), move.NewPosition.Y);

        return isValid;
    }

    internal static bool HasRightToCastle(FigureColor side, CastleType castleType, ChessComponent board)
    {
	    return ValidByMoves();

        bool ValidByMoves()
        {
	        Position kingpos = new(4, (short)(side == FigureColor.White ? 7 : 0)); // "e1" : "e8"

	        var rookpos = castleType switch
            {
                CastleType.King => new Position(7, (short)(side == FigureColor.White ? 7 : 0)), // "h1" : "h8"
                CastleType.Queen => new Position(0, (short)(side == FigureColor.White ? 7 : 0)), // "a1" : "a8"
                _ => new Position(),
            };

            var rook = board[rookpos.Y, rookpos.X];
            return rook != null
                && rook.Type == FigureType.Rook
                && rook.Color == side
                && !PieceEverMoved(kingpos, board) && !PieceEverMoved(rookpos, board);
        }
    }

    private static bool PieceEverMoved(Position piecePos, ChessComponent board)
    {
        return board.DisplayedMoves.Any(p => p.OriginalPosition == piecePos);
    }

    /// <summary>
    /// Returns Valid(if not => X = -1,Y = -1) En passant move new position
    /// </summary>
    internal static Position LastMoveEnPassantPosition(ChessComponent board)
    {
        Position pos = new();

        if (board.moveIndex >= 0) // If there are moves made on board
        {
            var lastMove = board.DisplayedMoves.Last();

            bool isPawn = lastMove.Piece.Type == FigureType.Pawn;
            bool moving2Tiles = Math.Abs(lastMove.NewPosition.Y - lastMove.OriginalPosition.Y) == 2;

            if (isPawn && moving2Tiles)
            {
                pos = new Position
                {
                    X = lastMove.NewPosition.X,
                    Y = (short)((lastMove.NewPosition.Y + lastMove.OriginalPosition.Y) / 2)
                };
            }
        }
        return pos;
    }
}
