using Chess;
using System;
using System.Threading.Tasks;
namespace Sandbox.AI;

public class ChessAI
{
	public static async void NextMove(ChessComponent board)
	{
		var gameController = board.Scene.Components.GetInDescendants<GameController>();
		if ( board.IsEndGame )
		{
			gameController.Won(board.EndGame.WonSide);
			return;
		}

		await CheckItems( board );

		var move = GetBestMove( board, 2, true );

		if ( move == null )
		{
			gameController.Won(FigureColor.White);
			return;
		}

		var nextCell = board.Pieces[move.NewPosition.Y, move.NewPosition.X];

		if ( nextCell.Current != null && nextCell.Current.Tags.Has( "white" ) )
		{
			var killedFigure = nextCell.Current.Components.Get<FigureComponent>();
			killedFigure.Kill();
			move.CapturedFigure = killedFigure;
			//cellComponent.Current?.Destroy();	
		}

		if (move.Parameter is not MoveCastle)
			move.Piece.SetCell(nextCell.GameObject);

		var piece = move.Piece.GameObject;

		var pos = piece.Transform.Position;
		var colliderBox = nextCell.Components.Get<BoxCollider>();
		if ( colliderBox is { KeyframeBody: not null } )
		{
			pos.x = colliderBox.KeyframeBody.MassCenter.x;
			pos.y = colliderBox.KeyframeBody.MassCenter.y;	
		}

		var originalPos = piece.Transform.Position;
		
		var hand = board.Scene.Children.First( gameObject => gameObject.Tags.Has( "hand" ) );
		var render = hand.Components.Get<SkinnedModelRenderer>();
		render.SceneModel.SetAnimParameter("hold", true);

		var handPos = new Vector3(330, 14, 40);
		
		var anim = Animation.Play( piece, "figure_move", 2, EasingFunc.EaseInCubic,( gameObject, progress ) =>
		{
			gameObject.Transform.Position = originalPos + ((pos - originalPos) * progress);
			
			var renderer = gameObject.Components.Get<SkinnedModelRenderer>();
			var attach = renderer.GetAttachment( "head" );
			if (attach.HasValue)
				UpdateHandPos(hand, attach.Value.Position.WithX( attach.Value.Position.x + 20 )
					.WithY( attach.Value.Position.y + 20 )
					.WithZ( attach.Value.Position.z - 55 ), handPos, attach.Value.Rotation, Math.Min(progress * 8, 1));
		});

		anim.PlayAfter( 1, ( gameObject, progress ) =>
		{
			render.SceneModel.SetAnimParameter("hold", false);
			
			var renderer = gameObject.Components.Get<SkinnedModelRenderer>();
			var attach = renderer.GetAttachment( "head" );
			if (attach.HasValue)
				UpdateHandPos(hand, handPos, attach.Value.Position.WithX( attach.Value.Position.x + 20 )
					.WithY( attach.Value.Position.y + 20 )
					.WithZ( attach.Value.Position.z - 55 ), attach.Value.Rotation, progress);
		} ).OnComplete( _ =>
		{
			if ( move.OriginalPosition != move.NewPosition )
			{
				board.executedMoves.Add(move);
		
				board.moveIndex = board.executedMoves.Count - 1;

				if ( move.Parameter is MoveEnPassant && move.CapturedFigure != null )
				{
					var killedFigure = move.CapturedFigure.Components.Get<FigureComponent>();
					killedFigure.Kill();
				}
				
				move.Parameter?.Execute(move, board);

				board.HandleKingChecked();

				if ( board.Pieces[board.WhiteKing.Y, board.WhiteKing.X].Current != null )
				{
					var cellOutline = board[board.WhiteKing].GetCell()?.Components.GetOrCreate<HighlightOutline>();
					if ( cellOutline != null )
					{
						if ( move.IsCheck )
						{
							cellOutline.Enabled = true;
							cellOutline.InsideColor = Color.Red.WithAlpha( 0.2f );
						}
						else
						{
							cellOutline.Enabled = false;
							cellOutline.InsideColor = Color.Transparent;
						}
					}	
				}
			}
	    
			board.HandleEndGame();	
			
			if ( !board.IsEndGame && !board.HaveMoves(FigureColor.White))
			{
				board.Won(FigureColor.Black);
			}
			
			if ( board.IsEndGame )
			{
				gameController.Won(board.EndGame.WonSide);
			}
		} );

		await Task.Delay( 500 );
		Sound.Play( "figure_move", piece.Transform.Position );
	}

	public static Move GetBestMove(ChessComponent board, int depth, bool isMaximisingPlayer)
	{
		var moves = GetPossibleMoves( board );
		
		if (moves == null)
			return null;
		
		Move bestMove = null;
		//use any negative large number
		var bestValue = -9999;
		
		foreach ( var move in moves )
		{
			board.DropPieceToNewPosition(move);
			var boardValue = Minimax(depth - 1, board, -10000, 10000, !isMaximisingPlayer);
			board.Cancel();
			if ( boardValue < bestValue )
				continue;
			
			bestValue = boardValue;
			bestMove = move;
		}
		return bestMove;
	}

	public static int Minimax( int depth, ChessComponent board, int alpha, int beta, bool isMaximisingPlayer )
	{
		if (depth == 0) {
			return -CountEvaluation(board);
		}
		
		var newGameMoves = GetPossibleMoves( board );
		
		if (newGameMoves == null)
			return -CountEvaluation(board);

		if (isMaximisingPlayer) {
			var bestMove = -9999;
			foreach ( var newGameMove in newGameMoves )
			{
				board.DropPieceToNewPosition(newGameMove);
				bestMove = Math.Max(bestMove, Minimax(depth - 1, board, alpha,  beta, false));
				board.Cancel();
			}
			alpha = Math.Max(alpha, bestMove);
			if (beta <= alpha) {
				return bestMove;
			}
			return bestMove;
		} else {
			var bestMove = 9999;
			foreach ( var newGameMove in newGameMoves )
			{
				board.DropPieceToNewPosition(newGameMove);
				bestMove = Math.Min(bestMove, Minimax(depth - 1, board,  alpha,  beta, true));
				board.Cancel();
			}
			beta = Math.Min(beta, bestMove);
			if (beta <= alpha) {
				return bestMove;
			}
			return bestMove;
		}
	}
	
	/*public static int CountEvaluation(ChessComponent board)
	{
		var totalEvaluation = 0;
		for (var i = 0; i < 8; i++) {
			for (var j = 0; j < 8; j++)
			{
				totalEvaluation += GetPieceValue(board[i, j]);
			}
		}
		return totalEvaluation;
	}
	*/
	
	static double[,] pawnEvalWhite = {
		{0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
		{5.0,  5.0,  5.0,  5.0,  5.0,  5.0,  5.0,  5.0},
		{1.0,  1.0,  2.0,  3.0,  3.0,  2.0,  1.0,  1.0},
		{0.5,  0.5,  1.0,  2.5,  2.5,  1.0,  0.5,  0.5},
		{0.0,  0.0,  0.0,  2.0,  2.0,  0.0,  0.0,  0.0},
		{0.5, -0.5, -1.0,  0.0,  0.0, -1.0, -0.5,  0.5},
		{0.5,  1.0, 1.0,  -2.0, -2.0,  1.0,  1.0,  0.5}, 
		{0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0}
	};

	static double[,] pawnEvalBlack = {
		{0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
		{0.5,  1.0, 1.0,  -2.0, -2.0,  1.0,  1.0,  0.5}, 
		{0.5, -0.5, -1.0,  0.0,  0.0, -1.0, -0.5,  0.5},
		{0.0,  0.0,  0.0,  2.0,  2.0,  0.0,  0.0,  0.0},
		{0.5,  0.5,  1.0,  2.5,  2.5,  1.0,  0.5,  0.5},
		{1.0,  1.0,  2.0,  3.0,  3.0,  2.0,  1.0,  1.0},
		{5.0,  5.0,  5.0,  5.0,  5.0,  5.0,  5.0,  5.0},
		{0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0}
	};

	static double[,] knightEval =
	{
		{-5.0, -4.0, -3.0, -3.0, -3.0, -3.0, -4.0, -5.0},
		{-4.0, -2.0,  0.0,  0.0,  0.0,  0.0, -2.0, -4.0},
		{-3.0,  0.0,  1.0,  1.5,  1.5,  1.0,  0.0, -3.0},
		{-3.0,  0.5,  1.5,  2.0,  2.0,  1.5,  0.5, -3.0},
		{-3.0,  0.0,  1.5,  2.0,  2.0,  1.5,  0.0, -3.0},
		{-3.0,  0.5,  1.0,  1.5,  1.5,  1.0,  0.5, -3.0},
		{-4.0, -2.0,  0.0,  0.5,  0.5,  0.0, -2.0, -4.0},
		{-5.0, -4.0, -3.0, -3.0, -3.0, -3.0, -4.0, -5.0}
	};

	static double[,] bishopEvalWhite = {
		{ -2.0, -1.0, -1.0, -1.0, -1.0, -1.0, -1.0, -2.0},
		{ -1.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -1.0},
		{ -1.0,  0.0,  0.5,  1.0,  1.0,  0.5,  0.0, -1.0},
		{ -1.0,  0.5,  0.5,  1.0,  1.0,  0.5,  0.5, -1.0},
		{ -1.0,  0.0,  1.0,  1.0,  1.0,  1.0,  0.0, -1.0},
		{ -1.0,  1.0,  1.0,  1.0,  1.0,  1.0,  1.0, -1.0},
		{ -1.0,  0.5,  0.0,  0.0,  0.0,  0.0,  0.5, -1.0},
		{ -2.0, -1.0, -1.0, -1.0, -1.0, -1.0, -1.0, -2.0}
	};

	static double[,] bishopEvalBlack = {
		{ -2.0, -1.0, -1.0, -1.0, -1.0, -1.0, -1.0, -2.0},
		{ -1.0,  0.5,  0.0,  0.0,  0.0,  0.0,  0.5, -1.0},
		{ -1.0,  1.0,  1.0,  1.0,  1.0,  1.0,  1.0, -1.0},
		{ -1.0,  0.0,  1.0,  1.0,  1.0,  1.0,  0.0, -1.0},
		{ -1.0,  0.5,  0.5,  1.0,  1.0,  0.5,  0.5, -1.0},
		{ -1.0,  0.0,  0.5,  1.0,  1.0,  0.5,  0.0, -1.0},
		{ -1.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -1.0},
		{ -2.0, -1.0, -1.0, -1.0, -1.0, -1.0, -1.0, -2.0}
	};

	static double[,] rookEvalWhite = {
		{  0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0},
		{  0.5,  1.0,  1.0,  1.0,  1.0,  1.0,  1.0,  0.5},
		{ -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5},
		{ -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5},
		{ -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5},
		{ -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5},
		{ -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5},
		{  0.0,   0.0, 0.0,  0.5,  0.5,  0.0,  0.0,  0.0}
	};

	static double[,] rookEvalBlack = {
		{  0.0,   0.0, 0.0,  0.5,  0.5,  0.0,  0.0,  0.0},
		{ -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5},
		{ -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5},
		{ -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5},
		{ -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5},
		{ -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5},
		{  0.5,  1.0,  1.0,  1.0,  1.0,  1.0,  1.0,  0.5},
		{  0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0},
	};

	static double[,] evalQueen = {
		{ -2.0, -1.0, -1.0, -0.5, -0.5, -1.0, -1.0, -2.0},
		{ -1.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -1.0},
		{ -1.0,  0.0,  0.5,  0.5,  0.5,  0.5,  0.0, -1.0},
		{ -0.5,  0.0,  0.5,  0.5,  0.5,  0.5,  0.0, -0.5},
		{  0.0,  0.0,  0.5,  0.5,  0.5,  0.5,  0.0, -0.5},
		{ -1.0,  0.5,  0.5,  0.5,  0.5,  0.5,  0.0, -1.0},
		{ -1.0,  0.0,  0.5,  0.0,  0.0,  0.0,  0.0, -1.0},
		{ -2.0, -1.0, -1.0, -0.5, -0.5, -1.0, -1.0, -2.0}
	};

	static double[,] kingEvalWhite = {
		{ -3.0, -4.0, -4.0, -5.0, -5.0, -4.0, -4.0, -3.0},
		{ -3.0, -4.0, -4.0, -5.0, -5.0, -4.0, -4.0, -3.0},
		{ -3.0, -4.0, -4.0, -5.0, -5.0, -4.0, -4.0, -3.0},
		{ -3.0, -4.0, -4.0, -5.0, -5.0, -4.0, -4.0, -3.0},
		{ -2.0, -3.0, -3.0, -4.0, -4.0, -3.0, -3.0, -2.0},
		{ -1.0, -2.0, -2.0, -2.0, -2.0, -2.0, -2.0, -1.0},
		{  2.0,  2.0,  0.0,  0.0,  0.0,  0.0,  2.0,  2.0 },
		{  2.0,  3.0,  1.0,  0.0,  0.0,  1.0,  3.0,  2.0 }
    };

	static double[,] kingEvalBlack = {
		{  2.0,  3.0,  1.0,  0.0,  0.0,  1.0,  3.0,  2.0 },
		{  2.0,  2.0,  0.0,  0.0,  0.0,  0.0,  2.0,  2.0 },
		{ -1.0, -2.0, -2.0, -2.0, -2.0, -2.0, -2.0, -1.0},
		{ -2.0, -3.0, -3.0, -4.0, -4.0, -3.0, -3.0, -2.0},
		{ -3.0, -4.0, -4.0, -5.0, -5.0, -4.0, -4.0, -3.0},
		{ -3.0, -4.0, -4.0, -5.0, -5.0, -4.0, -4.0, -3.0},
		{ -3.0, -4.0, -4.0, -5.0, -5.0, -4.0, -4.0, -3.0},
		{ -3.0, -4.0, -4.0, -5.0, -5.0, -4.0, -4.0, -3.0}
	};

	public static int CountEvaluation(ChessComponent board)
	{
		var totalEvaluation = 0;
		for (var i = 0; i < 8; i++) {
			for (var j = 0; j < 8; j++)
			{
				var piece = board[i, j];
				totalEvaluation += GetPieceValue(piece, j, i);
			}
		}
		return totalEvaluation;
	}

	public static int GetPieceValue(FigureComponent piece, int x, int y)
	{
		if (piece == null) {
			return 0;
		}
		
		var isWhite = piece.Color == FigureColor.White;

		var absoluteValue = 0.0;
		absoluteValue += piece.Type switch
		{
			FigureType.King => 900 + ( isWhite ? kingEvalWhite[y, x] : kingEvalBlack[y, x] ),
			FigureType.Queen => 90 + evalQueen[y, x],
			FigureType.Bishop => 30 + ( isWhite ? bishopEvalWhite[y, x] : bishopEvalBlack[y, x] ),
			FigureType.Knight => 30 + knightEval[y, x],
			FigureType.Rook => 50 + ( isWhite ? rookEvalWhite[y, x] : rookEvalBlack[y, x] ),
			FigureType.Pawn => 10 + ( isWhite ? pawnEvalWhite[y, x] : pawnEvalBlack[y, x] ),
			_ => throw new ArgumentOutOfRangeException()
		};

		return isWhite ? (int) absoluteValue : (int)-absoluteValue;
	}
	
	public static int GetAlivePiecesCount(ChessComponent board)
	{
		return board.Pieces.Cast<CellComponent>().Count( cell => cell.GetPiece() != null && cell.GetPiece().Color == FigureColor.Black );
	}

	public static async Task CheckItems(ChessComponent board)
	{
		var alivePieces = GetAlivePiecesCount( board );

		if ( alivePieces > 12 )
			return;
		
		var chance = Game.Random.Next( 0, 100 );
		if ( chance < 60 )
		{
			return;
		}

		var items = board.Scene.Components.GetAll<ItemComponent>( FindMode.InDescendants ).Where(item => item.PlayerColor == FigureColor.Black).ToList();
		chance = Game.Random.Next( 0, 100 );
		var lowBound = 0;
		ItemComponent item = null;
		foreach ( var itemComponent in items )
		{
			if ( !itemComponent.GameObject.Parent.Enabled || !itemComponent.GameObject.Tags.Has("item"))
			{
				continue;
			}

			var highBound = lowBound + 100 / items.Count;
			if ( chance < highBound )
			{
				item = itemComponent;
				break;
			}

			lowBound = highBound;
		}

		if (item == null)
			return;

		var validCells = board.Pieces.Cast<CellComponent>().Where( cell => item.ValidCell( cell ) ).ToList();
		
		if (!validCells.Any())
			return;

		CellComponent cell = null;
		switch ( item.Type )
		{

			case ItemType.Hammer:
				{
					var highestValue = 0;
					foreach ( var cellComponent in from cellComponent in validCells let pieceValue = GetPieceValue( cellComponent.GetPiece(), cellComponent.Position.X, cellComponent.Position.Y ) where pieceValue > highestValue select cellComponent )
					{
						cell = cellComponent;
						highestValue = GetPieceValue( cellComponent.GetPiece(), cellComponent.Position.X, cellComponent.Position.Y );
					}
					break;
				}
			case ItemType.Crown:
				{
					chance = Game.Random.Next( 0, 100 );
					lowBound = 0;
					foreach ( var validCell in validCells )
					{
						var highBound = lowBound + 100 / validCells.Count;
						if ( chance < highBound )
						{
							cell = validCell;
							break;
						}

						lowBound = highBound;
					}
					break;
				}
			default:
				throw new ArgumentOutOfRangeException();
		}
		
		if (cell == null)
			return;

		var pos = cell.GetPiece().Transform.Position;
		var colliderBox = cell.Components.Get<BoxCollider>();
		pos.x = colliderBox.KeyframeBody.MassCenter.x;
		pos.y = colliderBox.KeyframeBody.MassCenter.y;
		pos.z = colliderBox.KeyframeBody.MassCenter.z + 100;
		var originalPos = item.GameObject.Transform.Position;
		
		item.SelectedItem = item.GameObject.Clone();
		item.SelectedItem.Transform.Position = item.GameObject.Transform.Position;
		item.SelectedItem.Transform.Rotation = item.GameObject.Transform.Rotation;
		item.SelectedItem.Components.Get<ModelRenderer>().Tint = Color.Transparent;
		item.SelectedItem.Tags.Add( "selected_item" );
		item.SelectedItem.Tags.Remove("item");
		item.SelectedItem.Tags.Remove("selected");

		Animation.Play( item.GameObject, "use_item", 2, EasingFunc.EaseInOutCubic, ( gameObject, progress ) =>
		{
			gameObject.Transform.Position = originalPos + ((pos - originalPos) * progress);
		} );
		
		await GameTask.DelaySeconds( 2 );
		item.UseAI( cell.GameObject.Id );
	}
	
	static void UpdateHandPos(GameObject hand, Vector3 pos, Vector3 origPos, Rotation rot, float progress)
	{
		hand.Transform.Position = origPos + (pos - origPos) * progress;
		
		hand.Transform.Rotation = hand.Transform.Rotation.Angles().WithYaw( rot.Angles().yaw );
	}

	static List<Move> GetPossibleMoves(ChessComponent board)
	{
		var blackFigures = board.Pieces.Cast<CellComponent>().Where( cell => cell.GetPiece() != null 
		                                                                     && cell.GetPiece().Color == FigureColor.Black 
		                                                                     && board.Pieces.Cast<CellComponent>().Any( cellComponent => board.IsValidMove( Move.Create()
			                                                                     .SetFigure( cell.GetPiece() )
			                                                                     .SetCell( cell )
			                                                                     .SetNewPosition( cellComponent.Position )
			                                                                     .SetOriginalPosition( cell.Position )
			                                                                     .Build(), false ) && !board.IsCheck(FigureColor.Black, Move.Create()
			                                                                     .SetFigure( cell.GetPiece() )
			                                                                     .SetCell( cell )
			                                                                     .SetNewPosition( cellComponent.Position )
			                                                                     .SetOriginalPosition( cell.Position )
			                                                                     .Build()) && (cellComponent.Current == null || !cellComponent.Current.Tags.Has( "black" )) )).ToList();
		
		
		if ( blackFigures.Count == 0 )
		{
			board.Won( FigureColor.White );
			return null;
		}

		return (from cellComponent in blackFigures
			let validCells = board.Pieces.Cast<CellComponent>()
				.Where( targetCell => board.IsValidMove( Move.Create()
					.SetFigure( cellComponent.GetPiece() )
					.SetCell( cellComponent )
					.SetNewPosition( targetCell.Position )
					.SetOriginalPosition( cellComponent.Position )
					.Build(), false ) && (targetCell.Current == null || !targetCell.Current.Tags.Has( "black" )) )
				.ToList()
			from validCell in validCells
			let move = Move.Create()
				.SetFigure( cellComponent.GetPiece() )
				.SetCell( cellComponent )
				.SetNewPosition( validCell.Position )
				.SetOriginalPosition( cellComponent.Position )
				.Build()
			where board.IsValidMove( move, false ) || validCell.Position == cellComponent.Position
			where validCell.Current == null || !validCell.Current.Tags.Has( "black" ) || validCell.Current == cellComponent.GetPiece().GameObject
			select move).ToList();
	}
}
