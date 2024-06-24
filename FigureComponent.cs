using Sandbox;
using System;

public enum FigureColor
{
	Black,
	White,
	None
}

public enum FigureType
{
	King,
	Queen,
	Bishop,
	Knight,
	Rook,
	Pawn
}

public sealed class FigureComponent : Component
{
	[Property] public bool IsUsable { get; set; }
	
	public GameObject OriginalPosition { get; set; }
	
	Guid _cell { get; set; }
	[Sync] public Guid Cell
	{
		get
		{
			return _cell;
		}
		set
		{
			CellComponent cellComponent;

			var cell = GetCell();
			if ( cell != null )
			{
				cellComponent = cell.Components.Get<CellComponent>();
				if (cellComponent.Current == GameObject)
					cellComponent.Current = null;	
			}
			
			_cell = value;

			var newCell = Scene?.Directory.FindByGuid( value );
			
			if (newCell == null)
				return;
			
			cellComponent = newCell.Components.Get<CellComponent>();
			cellComponent.Current = GameObject;

			var pos = GameObject.Transform.Position;
			var colliderBox = newCell.Components.Get<BoxCollider>();
			if ( colliderBox is { KeyframeBody: not null } )
			{
				pos.x = colliderBox.KeyframeBody.MassCenter.x;
				pos.y = colliderBox.KeyframeBody.MassCenter.y;	
			}

			GameObject.Transform.Position = pos;
		}
	}

	[Property] public FigureColor Color { get; set; }

	FigureType _type;
	[Sync][Property] public FigureType Type
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;

			var renderer = GameObject.Components.Get<SkinnedModelRenderer>();
			var collider = GameObject.Components.Get<ModelCollider>();
			
			if (renderer == null)
				return;
			
			renderer.Model = _type switch
			{
				FigureType.King => Model.Load( "models/statue/statue_wolf.vmdl" ),
				FigureType.Queen => Model.Load( "models/statue/statue_lynx.vmdl" ),
				FigureType.Bishop => Model.Load( "models/statue/statue_fox.vmdl" ),
				FigureType.Knight => Model.Load( "models/statue/statue_horse.vmdl" ),
				FigureType.Rook => Model.Load( "models/statue/statue_bear.vmdl" ),
				FigureType.Pawn => Model.Load( "models/statue/statue_boar.vmdl" ),
				_ => throw new ArgumentOutOfRangeException()
			};

			if (collider != null)
				collider.Model = renderer.Model;
		}
	}

	public void SetCell( GameObject cell )
	{
		CellComponent cellComponent;
		if ( GetCell() != null )
		{
			cellComponent = GetCell().Components.Get<CellComponent>();
			cellComponent.Current = null;	
		}
			
		cellComponent = cell.Components.Get<CellComponent>();
		cellComponent.Current = GameObject;
		
		_cell = cell.Id;
	}

	public void Kill()
	{
		var color = GameObject.Tags.Has( "white" ) ? "white" : "black";
		var cells = Scene.Components.GetAll<CellComponent>( FindMode.InDescendants ).Where( cell => cell.GameObject.Tags.Has("death_cell") && 
		                                                                                            cell.GameObject.Tags.Has(color) && 
		                                                                                            cell.Current == null );
		GameObject.Tags.Add("dead");
		var cell = cells.First();
		
		if (Cell != cell.GameObject.Id)
			Cell = cell.GameObject.Id;
		
		var pos = GameObject.Transform.Position;
		var colliderBox = GetCell().Components.Get<BoxCollider>();
		if ( colliderBox is { KeyframeBody: not null } )
		{
			pos.z = colliderBox.KeyframeBody.MassCenter.z;
		}
		GameObject.Transform.Position = pos;
	}
	
	public void Revive(CellComponent newCell)
	{
		GameObject.Tags.Remove("dead");
		Cell = newCell.GameObject.Id;
		
		var pos = GameObject.Transform.Position;
		var colliderBox = GetCell().Components.Get<BoxCollider>();
		if ( colliderBox is { KeyframeBody: not null } )
		{
			pos.z = colliderBox.KeyframeBody.MassCenter.z;
		}
		GameObject.Transform.Position = pos;
	}

	public GameObject GetCell()
	{
		return Scene.Directory.FindByGuid(Cell);
	}
	
	public CellComponent GetCellComponent()
	{
		return GetCell().Components.Get<CellComponent>();
	}

	public FigureColor GetOppositeColor()
	{
		return Color switch
		{
			FigureColor.Black => FigureColor.White,
			FigureColor.White => FigureColor.Black,
			FigureColor.None => FigureColor.None,
			_ => FigureColor.None
		};
	}

	public string GetTranslatedType()
	{
		return Type switch
		{
			FigureType.King => "Wolf",
			FigureType.Queen => "Lynx",
			FigureType.Bishop => "Fox",
			FigureType.Knight => "Horse",
			FigureType.Rook => "Bear",
			FigureType.Pawn => "Boar",
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	RealTimeSince timeSinceCellUpdate;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( timeSinceCellUpdate > 2 )
		{
			var cell = GetCellComponent();
			if ( cell.Current != GameObject )
			{
				cell.Current = GameObject;
			}
			timeSinceCellUpdate = 0;
		}
	}

	protected override void OnStart()
	{
		base.OnStart();

		OriginalPosition = GetCell();

		if (Color == FigureColor.Black)
			GameObject.Tags.Add("ai");
	}
}
