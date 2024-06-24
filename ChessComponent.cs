using Chess;
using System;
namespace Sandbox;

public partial class ChessComponent : Component
{
	[Property] public GameObject WhiteFigures { get; set; }
	[Property] public GameObject BlackFigures { get; set; }
	
	/// <summary>
	/// Board y-dimension
	/// </summary>
	public const int MAX_ROWS = 8;

	/// <summary>
	/// Board x-dimension
	/// </summary>
	public const int MAX_COLS = 8;
	
	internal CellComponent[,] Pieces = new CellComponent[8, 8];
	
	/// <summary>
	/// Returns Piece on given position
	/// </summary>
	/// <param name="x">0->8</param>
	/// <param name="y">0->8</param>
	public FigureComponent this[int x, int y] => Pieces[x, y].GetPiece();
	
	/// <summary>
	/// Returns Piece on given position
	/// </summary>
	/// <param name="pos">Position on chess board</param>
	public FigureComponent this[Position pos] => Pieces[pos.Y, pos.X].GetPiece();

	public FigureColor Turn => DisplayedMoves.Count % 2 == 0 ? FigureColor.White : FigureColor.Black;
	
	private bool whiteKingChecked = false;

    /// <summary>
    /// Returns the state of White king (Checked or not)
    /// </summary>
    public bool WhiteKingChecked
    {
        get => whiteKingChecked;
        private set
        {
            if (value != whiteKingChecked)
            {
                whiteKingChecked = value;
                //OnWhiteKingCheckedChangedEvent(new CheckEventArgs(this, WhiteKing, value));
            }
        }
    }

    private bool blackKingChecked = false;

    /// <summary>
    /// Returns the state of Black king (Checked or not)
    /// </summary>
    public bool BlackKingChecked
    {
        get => blackKingChecked;
        private set
        {
	        if (value != blackKingChecked)
            {
                blackKingChecked = value;
                //OnBlackKingCheckedChangedEvent(new CheckEventArgs(this, BlackKing, value));
            }
        }
    }

    /// <summary>
    /// Returns White king position on chess board
    /// </summary>
    public Position WhiteKing => GetKingPosition(FigureColor.White, this);

    /// <summary>
    /// Returns Black king position on chess board
    /// </summary>
    public Position BlackKing => GetKingPosition(FigureColor.Black, this);

    /// <summary>
    /// White pieces that has been captured by black player
    /// </summary>
    public FigureComponent[] CapturedWhite
    {
        get
        {
            var captured = new List<FigureComponent>();

            captured.AddRange(DisplayedMoves.Where(m => m.CapturedFigure?.Color == FigureColor.White)
                                  .Select(m => m.CapturedFigure));

            return captured.ToArray();
        }
    }

    /// <summary>
    /// Black pieces that has been captured by white player
    /// </summary>
    public FigureComponent[] CapturedBlack
    {
        get
        {
            var captured = new List<FigureComponent>();

            captured.AddRange(DisplayedMoves.Where(m => m.CapturedFigure?.Color == FigureColor.Black)
                                  .Select(m => m.CapturedFigure));

            return captured.ToArray();
        }
    }
    
    private EndGameInfo endGame;

    /// <summary>
    /// Represents End of game state(or null), won side(or null if draw) and type of end game
    /// </summary>
    public EndGameInfo EndGame
    {
	    get => endGame;
	    private set
	    {
		    endGame = value;
		    if (value is not null)
			    OnEndGameEvent();
	    }
    }
    
    /// <summary>
    /// https://www.chessprogramming.org/Irreversible_Moves
    /// </summary>
    public int LastIrreversibleMoveIndex
    {
	    get
	    {
		    int index = moveIndex;
		    bool moveFound = false;

		    while (index >= 0 && !moveFound)
		    {
			    if (executedMoves[index].CapturedFigure is not null
			        || executedMoves[index].Piece.Type == FigureType.Pawn)
			    {
				    moveFound = true;
			    }

			    index--;
		    }

		    return moveFound ? index + 1 : index;
	    }
    }
    
    private readonly AutoEndgameRules autoEndgameRules = AutoEndgameRules.None;
    /// <summary>
    /// This property keeps track of auto-draw (endgame) rules that will be used to check for endgame
    /// </summary>
    public AutoEndgameRules AutoEndgameRules
    {
	    get => autoEndgameRules;
	    init
	    {
		    autoEndgameRules = value;
		    endGameProvider.UpdateRules();
	    }
    }

    /// <summary>
    /// When true => use: EndGame. for more info on endgame(type, won side)
    /// </summary>
    public bool IsEndGame => EndGame is not null;
    
    private readonly EndGameProvider endGameProvider;
    private bool standardiseCastlingPositions;
    internal int moveIndex = -1;
    internal readonly List<Move> executedMoves;
    
    /// <summary>
    /// Index of displayed move on this chess board
    /// </summary>
    public int MoveIndex
    {
	    get => moveIndex;
	    set
	    {
		    if (value < executedMoves.Count && value >= -1)
			    DisplayMoves(executedMoves.GetRange(0, value + 1));
		    else
			    throw new IndexOutOfRangeException("Move not found");
	    }
    }
    
    /// <summary>
    /// Whether to standardise move like {e1 - h1} into {e1 - g1} during validation
    /// </summary>
    public bool StandardiseCastlingPositions
    {
	    get => standardiseCastlingPositions;
	    set => standardiseCastlingPositions = value;
    }
    
    /// <summary>
    /// Executed moves on this chess board<br/>
    /// </summary>
    public IReadOnlyList<Move> ExecutedMoves => new List<Move>(executedMoves);
    
    /// <summary>
    /// Whether last move is displayed on this chess board<br/>
    /// False after Previous() / First() / MoveIndex = ...
    /// </summary>
    public bool IsLastMoveDisplayed => moveIndex == executedMoves.Count - 1;

    internal List<Move> DisplayedMoves => executedMoves.GetRange(0, moveIndex + 1);

    public ChessComponent()
    {
	    executedMoves = new List<Move>();
	    endGameProvider = new EndGameProvider(this);
	    AutoEndgameRules = AutoEndgameRules.All;
    }
    
    /// <summary>
    /// Clears board and sets begin positions
    /// </summary>
    public void Clear()
    {
	    executedMoves.Clear();
	    moveIndex = -1;
	    endGame = null;
	    WhiteKingChecked = false;
	    BlackKingChecked = false;
    }
    
    /// <summary>
    /// Checking if there one of kings are checked
    /// and updating checked states
    /// </summary>
    internal void HandleKingChecked()
    {
	    WhiteKingChecked = false;
	    BlackKingChecked = false;
    }
    
    /// <summary>
    /// Checking if there is a checkmate or stalemate
    /// and updating end game state
    /// </summary>
    internal void HandleEndGame()
    {
	    EndGame = endGameProvider.GetEndGameInfo();
    }
    
    private void DisplayMoves(List<Move> moves)
    {
	    foreach (var move in moves)
		    DropPieceToNewPosition(move);

	    moveIndex = moves.Count - 1;

	    HandleKingChecked();
    }
    
    internal void DropPieceToNewPosition(Move move)
    {
	    executedMoves.Add(move);
	    moveIndex = executedMoves.Count - 1;

	    if ( move.Parameter is MoveCastle )
	    {
		    move.Parameter?.Execute(move, this);
	    }
	    else
	    {
		    DropPiece(move, this);
	    
		    move.Parameter?.Execute(move, this);
	    }
    }
    
    /// <summary>
    /// Default dropping piece implementation<br/>
    /// Clearing old, copy to new...
    /// </summary>
    internal static void DropPiece(Move move, ChessComponent board)
    {
	    // Moving piece to its new position
	    board.Pieces[move.OriginalPosition.Y, move.OriginalPosition.X].Current = null;
	    board.Pieces[move.NewPosition.Y, move.NewPosition.X].Current = move.Piece.GameObject;
    }
    
    /// <summary>
    /// Displaying first move(if possible)
    /// </summary>
    public void First() => MoveIndex = 0;

    /// <summary>
    /// Displaying last move(if possible)
    /// </summary>
    public void Last() => MoveIndex = executedMoves.Count - 1;

    /// <summary>
    /// Displaying next move(if possible)
    /// </summary>
    public void Next() => MoveIndex++;

    /// <summary>
    /// Displaying previous move(if possible)
    /// </summary>
    public void Previous() => MoveIndex--;
    
    /// <summary>
    /// Declares resign of one of sides
    /// </summary>
    /// <param name="resignedSide">Resigned side</param>
    /// <exception cref="ChessGameEndedException"></exception>
    public void Resign(FigureColor resignedSide)
    {
	    if (IsEndGame)
		    throw new ChessGameEndedException(this, EndGame);

	    EndGame = new EndGameInfo(EndgameType.Resigned, resignedSide == FigureColor.Black ? FigureColor.White : FigureColor.Black);
    }

    /// <summary>
    /// Declares draw in current chess game
    /// </summary>
    /// <exception cref="ChessGameEndedException"></exception>
    public void Draw()
    {
	    if (IsEndGame)
		    throw new ChessGameEndedException(this, EndGame);

	    EndGame = new EndGameInfo(EndgameType.DrawDeclared, null);
    }
    
    /// <summary>
    /// Declares win in current chess game
    /// </summary>
    /// <exception cref="ChessGameEndedException"></exception>
    public void Won(FigureColor color)
    {
	    if (IsEndGame)
		    throw new ChessGameEndedException(this, EndGame);
	    
	    EndGame = new EndGameInfo(EndgameType.Checkmate, color);
    }
    
    /// <summary>
    /// Cancel last move and display previous pieces positions
    /// </summary>
    public void Cancel()
    {
	    if (IsLastMoveDisplayed && executedMoves.Count > 0)
	    {
		    var move = executedMoves[^1];

		    if ( move.Parameter is MoveCastle )
		    {
			    move.Parameter?.Undo(move, this);
		    }
		    else
		    {
			    RestorePiece(move, this);
		    
			    move.Parameter?.Undo(move, this);
		    }

		    executedMoves.RemoveAt(executedMoves.Count - 1);
		    moveIndex = executedMoves.Count - 1;

		    HandleKingChecked();
		    EndGame = null;
	    }
    }
    
    internal static void RestorePiece(Move move, ChessComponent board)
    {
	    // Moving piece to its original position
	    board.Pieces[move.OriginalPosition.Y, move.OriginalPosition.X].Current = move.Piece.GameObject;
	    board.Pieces[move.NewPosition.Y, move.NewPosition.X].Current = null;
	    
	    if (move.OriginalFigure != null) 
		    board.Pieces[move.NewPosition.Y, move.NewPosition.X].Current = move.OriginalFigure.GameObject;
	    
	    // Clearing new position / or setting captured piece back
	    if (move.CapturedFigure != null)
		    board.Pieces[move.NewPosition.Y, move.NewPosition.X].Current = move.CapturedFigure.GameObject;
    }
    
    internal int GetHalfMovesCount()
    {
	    int index = LastIrreversibleMoveIndex;

	    if (index >= 0)
		    return moveIndex - index;
	    else
		    return moveIndex + 1;
    }

    internal int GetFullMovesCount()
    {
	    return (moveIndex + 3) / 2;
    }
    
    public bool HaveMoves(FigureColor color)
    {
	    var board = Scene.Components.GetInDescendants<ChessComponent>();
	    var figures = board.Pieces.Cast<CellComponent>().Where( cell => cell.GetPiece() != null 
	                                                                    && cell.GetPiece().Color == color 
	                                                                    && board.Pieces.Cast<CellComponent>().Any( cellComponent => board.IsValidMove( Move.Create()
		                                                                    .SetFigure( cell.GetPiece() )
		                                                                    .SetCell( cell )
		                                                                    .SetNewPosition( cellComponent.Position )
		                                                                    .SetOriginalPosition( cell.Position )
		                                                                    .Build(), false ) && !board.IsCheck(color, Move.Create()
		                                                                    .SetFigure( cell.GetPiece() )
		                                                                    .SetCell( cell )
		                                                                    .SetNewPosition( cellComponent.Position )
		                                                                    .SetOriginalPosition( cell.Position )
		                                                                    .Build()) && (cellComponent.Current == null || !cellComponent.Current.Tags.Has( color.ToString().ToLower() )) )).ToList();
	    return figures.Count > 0;
    }

    public async void Start()
    {
	    var render = GameObject.Components.Get<SkinnedModelRenderer>();
	    render.SceneModel.SetAnimParameter( "opened", true );

	    var whiteFigures = WhiteFigures.Components.Get<StatuesComponent>();
	    var blackFigures = BlackFigures.Components.Get<StatuesComponent>();
	    
	    var controller = Scene.Components.GetInDescendants<GameController>();
	    controller.IsPaused = true;
	    
	    foreach ( var gameObjectChild in WhiteFigures.Children.Concat(BlackFigures.Children) )
	    {
		    var renderer = gameObjectChild.Components.Get<SkinnedModelRenderer>();

		    renderer.Tint = renderer.Tint.WithAlpha(0);
			
		    var position = gameObjectChild.Transform.LocalPosition;
		    position.z = 174.355f;
		    gameObjectChild.Transform.LocalPosition = position;
	    }
		
	    await GameTask.Delay( 3000 );
	    
	    whiteFigures.Show();
	    blackFigures.Show();
    }
    
    public async void Restart()
    {
	    Clear();
	    
	    var whiteFigures = WhiteFigures.Components.Get<StatuesComponent>();
	    var blackFigures = BlackFigures.Components.Get<StatuesComponent>();

	    whiteFigures.Hide();
	    blackFigures.Hide();

	    await Task.DelaySeconds(6);
	    
	    whiteFigures.Restore();
	    blackFigures.Restore();

	    whiteFigures.Show();
	    blackFigures.Show();
    }
    
    public async void Stop()
    {
	    Clear();

	    var infoMonitor = Scene.Components.GetInDescendants<InfoMonitor>();
	    infoMonitor.Reset();

	    var controller = Scene.Components.GetInDescendants<GameController>();
	    controller.OnReset();

	    var render = GameObject.Components.Get<SkinnedModelRenderer>();

	    var whiteFigures = WhiteFigures.Components.Get<StatuesComponent>();
	    var blackFigures = BlackFigures.Components.Get<StatuesComponent>();
	    
	    whiteFigures.Hide();
	    blackFigures.Hide();
	    
	    await Task.DelaySeconds(6);
	    
	    render.SceneModel.SetAnimParameter( "opened", false );
	    
	    whiteFigures.Restore();
	    blackFigures.Restore();
    }

    protected override void OnUpdate()
    {
	    base.OnUpdate();
	    
	    /*var controller = Scene.Components.GetInDescendants<GameController>();
	    if ( blackKingChecked )
	    {
		    if ( controller.GameType == GameType.Player && controller.Players.ContainsKey(Connection.Local.Id))
		    {
			    if ( controller.Players[Connection.Local.Id] == FigureColor.Black )
			    {
				    var cellOutline = this[BlackKing].GetCell()?.Components.GetOrCreate<HighlightOutline>();
				        
				    if ( cellOutline != null )
				    {
					    cellOutline.Enabled = true;
					    cellOutline.InsideColor = Color.Red.WithAlpha( 0.2f );
				    }
			    }
		    }   
	    }
	    else
	    {
		    var cellOutline = this[BlackKing].GetCell()?.Components.GetOrCreate<HighlightOutline>();

		    if ( cellOutline != null && cellOutline.Color == Color.Red.WithAlpha( 0.2f ))
		    {
			    cellOutline.InsideColor = Color.Transparent;
			    cellOutline.Enabled = false;   
		    }
	    }
	    
	    if ( whiteKingChecked )
	    {
		    if ( controller.GameType == GameType.Player && controller.Players.ContainsKey(Connection.Local.Id))
		    {
			    if ( controller.Players[Connection.Local.Id] == FigureColor.White )
			    {
				    var cellOutline = this[WhiteKing].GetCell()?.Components.GetOrCreate<HighlightOutline>();
				        
				    if ( cellOutline != null )
				    {
					    cellOutline.Enabled = true;
					    cellOutline.InsideColor = Color.Red.WithAlpha( 0.2f );
				    }
			    }
		    }
		    else
		    {
			    var cellOutline = this[WhiteKing].GetCell()?.Components.GetOrCreate<HighlightOutline>();
				        
			    if ( cellOutline != null )
			    {
				    cellOutline.Enabled = true;
				    cellOutline.InsideColor = Color.Red.WithAlpha( 0.2f );
			    }
		    }
	    }
	    else
	    {
		    var cellOutline = this[WhiteKing].GetCell()?.Components.GetOrCreate<HighlightOutline>();

		    if ( cellOutline != null && cellOutline.Color == Color.Red.WithAlpha( 0.2f ))
		    {
			    cellOutline.InsideColor = Color.Transparent;
			    cellOutline.Enabled = false;   
		    }
	    }*/
    }
}
