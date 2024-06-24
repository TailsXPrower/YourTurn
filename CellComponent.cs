using Chess;

public sealed class CellComponent : Component
{
	[Property, TextArea] public string CellName { get; set; }

	[Property] public GameObject Current { get; set; }
	
	[Property] public FigureColor Color { get; set; }

	public Position Position;

	protected override void OnEnabled()
	{
		base.OnEnabled();
		CellName = GameObject.Name;

		if (Tags.Has("death_cell"))
			return;
		
		var modelRender = GameObject.Components.GetOrCreate<ModelRenderer>();
		modelRender.Tint = global::Color.Transparent;
		
		Position = new Position();
		foreach ( var s in CellName )
		{
			if ( int.TryParse( s.ToString(), out var x ) )
			{
				Position.Y = (short)(x - 1);
				continue;
			}

			Position.X = s switch
			{
				'A' => 0,
				'B' => 1,
				'C' => 2,
				'D' => 3,
				'E' => 4,
				'F' => 5,
				'G' => 6,
				'H' => 7,
				_ => Position.X
			};
		}

		var chess = Scene.Components.GetInDescendants<ChessComponent>();
		chess.Pieces[Position.Y, Position.X] = this;
	}

	protected override void OnStart()
	{
		base.OnStart();
		
		if (Current == null)
			return;

		var figure = Current.Components.Get<FigureComponent>();
		figure.SetCell( GameObject );
		
		var pos = Current.Transform.Position;
		var colliderBox = GameObject.Components.Get<BoxCollider>();
		pos.x = colliderBox.KeyframeBody.MassCenter.x;
		pos.y = colliderBox.KeyframeBody.MassCenter.y;
		Current.Transform.Position = pos;
	}

	public FigureComponent GetPiece()
	{
		return Current?.Components.Get<FigureComponent>();
	}
}
