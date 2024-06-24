using Chess;
using Sandbox;
using System;

public enum ItemType
{
	Hammer,
	Crown
}

public sealed class ItemComponent : Component
{
	[Property] public ItemType Type { get; set; } = ItemType.Hammer;
	[Property] public FigureColor PlayerColor { get; set; } = FigureColor.White;

	public GameObject SelectedItem;
	
	protected override void OnUpdate()
	{

	}

	protected override void OnStart()
	{
		base.OnStart();
		GameObject.Tags.Remove("item");
		var render = GameObject.Components.Get<SkinnedModelRenderer>();
		render.Tint = render.Tint.WithAlpha(0);
	}

	public void OnSelect()
	{
		if (Scene.Components.GetInDescendants<GameController>().IsPaused)
			return;
		
		var chess = Scene.Components.GetInDescendants<ChessComponent>();
		var validCells = chess.Pieces.Cast<CellComponent>().Where( cellComponent => ValidCell( cellComponent ));
			
		foreach ( var cellComponent in validCells )
		{
			var cellOutline = cellComponent.Components.GetOrCreate<HighlightOutline>();
			cellOutline.Enabled = true;
			cellOutline.InsideColor = Color.Green.WithAlpha( 0.2f );
		}

		SelectedItem = GameObject.Clone();
		SelectedItem.Transform.Position = GameObject.Transform.Position;
		SelectedItem.Transform.Rotation = GameObject.Transform.Rotation;
		SelectedItem.Components.Get<ModelRenderer>().Tint = Color.Transparent;
		SelectedItem.Tags.Add( "selected_item" );
		SelectedItem.Tags.Remove("item");
		SelectedItem.Tags.Remove("selected");

		var cloneOutline = SelectedItem.Components.GetOrCreate<HighlightOutline>();
		cloneOutline.Enabled = true;
		cloneOutline.Color = Color.Cyan;
	}

	public bool ValidCell( CellComponent cellComponent )
	{
		var figure = cellComponent.Current;
		
		if (figure == null)
			return false;
		
		var figureComponent = cellComponent.GetPiece();
		var board = Scene.Components.GetInDescendants<ChessComponent>();
		
		switch ( Type )
		{
			case ItemType.Hammer:
				if (figureComponent.Color != (PlayerColor == FigureColor.White ? FigureColor.Black : FigureColor.White) )
					return false;

				var kingPos = PlayerColor == FigureColor.White ? board.BlackKing : board.WhiteKing;
				if (Math.Abs(cellComponent.Position.X - kingPos.X) < 2 && Math.Abs(cellComponent.Position.Y - kingPos.Y) < 2)
				{
					return false;
				}
				
				foreach ( var cell in board.Pieces )
				{
					var piece = cell.GetPiece();
					if (piece == null || piece.Color == figureComponent.Color) 
						continue;

					var move = Move.Create().SetNewPosition( cellComponent.Position )
						.SetOriginalPosition( cell.Position )
						.SetFigure( piece )
						.SetCell( cell )
						.Build();
					
					if ( ChessComponent.IsKingCheckedValidation( move, figureComponent.Color, board ) && ChessComponent.IsValidMove(move, board, false, false))
					{
						return false;
					}
				}

				return figureComponent.Type is not (FigureType.King or FigureType.Queen);
			case ItemType.Crown:
				if (figureComponent.Color != PlayerColor)
					return false;

				if ( ChessComponent.QueenValidation( Move.Create()
					.SetOriginalPosition( cellComponent.Position )
					.SetNewPosition( PlayerColor == FigureColor.White ? board.BlackKing : board.WhiteKing )
					.SetCell( cellComponent ).Build(), board.Pieces ) )
					return false;

				return figureComponent.Type == FigureType.Pawn;
			default:
				return false;
		}
	}

	[Broadcast]
	public void Use(Guid cell)
	{
		var cellComponent = Scene.Directory.FindByGuid(cell).Components.Get<CellComponent>();

		if (!ValidCell(cellComponent))
			return;

		var chess = Scene.Components.GetInDescendants<ChessComponent>();
		foreach ( var piece in chess.Pieces )
		{
			if (piece.Components.TryGet<HighlightOutline>( out var cellOutline ))
			{
				cellOutline.Enabled = false;	
			}
		}
		
		var figureComponent = cellComponent.GetPiece();
		var gameUi = Scene.Components.GetInDescendants<GameUIComponent>();
		gameUi.SelectedObject = null;
		
		if (Rpc.CallerId == Connection.Local.Id)
			gameUi.UsedItem = true;
		
		switch ( Type )
		{
			case ItemType.Hammer:
				OnHammerUse(figureComponent);
				break;
			case ItemType.Crown:
				OnCrownUse( figureComponent );
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
	
	public void UseAI(Guid cell)
	{
		var cellComponent = Scene.Directory.FindByGuid(cell).Components.Get<CellComponent>();

		if (!ValidCell(cellComponent))
			return;

		var chess = Scene.Components.GetInDescendants<ChessComponent>();
		foreach ( var piece in chess.Pieces )
		{
			if (piece.Components.TryGet<HighlightOutline>( out var cellOutline ))
			{
				cellOutline.Enabled = false;	
			}
		}
		
		var figureComponent = cellComponent.GetPiece();

		switch ( Type )
		{
			case ItemType.Hammer:
				OnHammerUse(figureComponent);
				break;
			case ItemType.Crown:
				OnCrownUse( figureComponent );
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	public async void OnHammerUse(FigureComponent figureComponent)
	{
		var render = GameObject.Components.Get<SkinnedModelRenderer>();
		render.SceneModel.SetAnimParameter( "use", true );
		await Task.DelaySeconds( 0.75f );
		Sound.Play( "hammer_use", figureComponent.Transform.Position );
		figureComponent.Kill();

		GameObject.Tags.Remove("item");

		var anim = Animation.Play( GameObject, GameObject.Id.ToString(), 1, EasingFunc.Linear, ( gameObject, progress ) =>
		{
			render.Tint = render.Tint.WithAlpha(1 - progress);
		} );
		anim.OnStop( gameObject =>
		{
			render.Tint = render.Tint.WithAlpha(0);
			
			if (gameObject == null || SelectedItem == null || gameObject.IsProxy)
				return;
			
			gameObject.Transform.Position = SelectedItem.Transform.Position;
			gameObject.Transform.Rotation = SelectedItem.Transform.Rotation;
			SelectedItem?.Destroy();
		} );
	}
	
	public async void OnCrownUse(FigureComponent figureComponent)
	{
		var render = GameObject.Components.Get<SkinnedModelRenderer>();
		render.SceneModel.SetAnimParameter( "use", true );
		Sound.Play( "crown_use", figureComponent.Transform.Position );
		GameObject.Tags.Remove("item");
		if (GameObject.Components.TryGet<HighlightOutline>( out var cellOutline ))
		{
			cellOutline.Enabled = false;	
		}
		await Task.DelaySeconds( 0.25f );
		var anim = Animation.Play( GameObject, GameObject.Id.ToString(), 1f, EasingFunc.Linear, ( gameObject, progress ) =>
		{
			render.Tint = render.Tint.WithAlpha(1 - progress);
		} );
		anim.OnStop( gameObject =>
		{
			render.Tint = render.Tint.WithAlpha(0);
			
			if (SelectedItem == null)
				return;
			
			gameObject.Transform.Position = SelectedItem.Transform.Position;
			gameObject.Transform.Rotation = SelectedItem.Transform.Rotation;
			SelectedItem?.Destroy();
		} );
		await Task.DelaySeconds( 0.5f );
		figureComponent.Type = FigureType.Queen;
	}
	
	public void ProcessSelection(GameObject camera)
	{
		var component = camera.Components.Get<CameraComponent>();
		var ray = component.ScreenPixelToRay( Mouse.Position );
		var trace = Scene.Trace.Ray( ray, 900 ).WithoutTags("white", "black").IgnoreGameObject( GameObject ).Run();

		var pos = GameObject.Transform.Position;

		// Outline
		var selectedObjectOutline = GameObject.Components.GetOrCreate<HighlightOutline>();
		if ( trace.Hit && trace.GameObject.Tags.Has("cell") )
		{
			var cell = trace.GameObject;
			var cellComponent = cell.Components.Get<CellComponent>();

			if (ValidCell(cellComponent))
			{
				var colliderBox = cell.Components.Get<BoxCollider>();
				pos.x = colliderBox.KeyframeBody.MassCenter.x;
				pos.y = colliderBox.KeyframeBody.MassCenter.y;
				pos.z = colliderBox.KeyframeBody.MassCenter.z + 100;
			
				selectedObjectOutline.Color = Color.Green;	
			}
			else
			{
				pos.x = trace.EndPosition.x;
				pos.y = trace.EndPosition.y;
				pos.z = trace.HitPosition.z;
				
				selectedObjectOutline.Color = Color.Red;
			}
		} else if (trace.Hit && trace.GameObject.Tags.Has("selected_item"))
		{
			pos = trace.GameObject.Transform.Position;

			selectedObjectOutline.Color = Color.Green;
		}
		else
		{
			pos.x = trace.EndPosition.x;
			pos.y = trace.EndPosition.y;
			pos.z = trace.EndPosition.z;
			
			selectedObjectOutline.Color = Color.Red;
		}
		
		GameObject.Transform.Position = pos;
	}
	
	public void OnDeselect(GameObject camera)
	{
		var component = camera.Components.Get<CameraComponent>();
		var ray = component.ScreenPixelToRay( Mouse.Position );
		var trace = Scene.Trace.Ray( ray, 900 ).WithoutTags( "white" ).IgnoreGameObject(GameObject).Run();

		switch ( trace.Hit )
		{
			case true when trace.GameObject.Tags.Has( "cell" ):
				{
					var cell = trace.GameObject;
					Use(cell.Id);
					break;
				}
			case true when trace.GameObject.Tags.Has( "selected_item" ):
				{
					GameObject.Transform.Position = trace.GameObject.Transform.Position;
					GameObject.Transform.Rotation = trace.GameObject.Transform.Rotation;
					
					var chess = Scene.Components.GetInDescendants<ChessComponent>();
					foreach ( var piece in chess.Pieces )
					{
						if (piece.Components.TryGet<HighlightOutline>( out var cellOutline ))
						{
							cellOutline.Enabled = false;	
						}
					}
					
					trace.GameObject.Destroy();
					
					var gameUi = Scene.Components.GetInDescendants<GameUIComponent>();
					var selectedObjectOutline = GameObject.Components.GetOrCreate<HighlightOutline>();
					selectedObjectOutline.Color = gameUi.HitObject == gameUi.SelectedObject ? Color.Cyan : Color.Transparent;
					selectedObjectOutline.Enabled = gameUi.HitObject == gameUi.SelectedObject;
					gameUi.SelectedObject = null;
					
					break;
				}
		}
	}
}
