
public sealed class StatuesComponent : Component
{
	protected override void OnStart()
	{
		foreach ( var renderer in from gameObjectChild in GameObject.Children select gameObjectChild.Components.Get<SkinnedModelRenderer>() )
		{
			renderer.Tint = renderer.Tint.WithAlpha(0);
		}
	}

	public void Restore()
	{
		if (Scene == null)
			return;
		
		foreach ( var figure in from gameObjectChild in GameObject.Children select gameObjectChild.Components.Get<FigureComponent>() )
		{
			figure.Cell = figure.OriginalPosition.Id;
			figure.GameObject.Transform.Position = figure.GameObject.Transform.Position.WithZ( figure.OriginalPosition.Transform.Position.z + 100 );

			var component = figure.GetCellComponent();
			if ( component.Position.Y is 1 or 6 )
				figure.Type = FigureType.Pawn;
		}
	}

	public async void Show()
	{
		if (Scene == null)
			return;

		var children = new List<GameObject>(GameObject.Children);
		var controller = Scene.Components.GetInDescendants<GameController>();
		foreach ( var gameObjectChild in children )
		{
			if (children.Count != GameObject.Children.Count)
				return;
			
			var renderer = gameObjectChild.Components.Get<SkinnedModelRenderer>();
			var figure = gameObjectChild.Components.Get<FigureComponent>();

			var position = gameObjectChild.Transform.LocalPosition;
			var vector = new Vector3( 0, 0, 100 );
			
			var anim = Animation.Play( gameObjectChild, gameObjectChild.Id.ToString(), 2, EasingFunc.EaseInCubic, ( gameObject, progress ) =>
			{
				renderer.Tint = renderer.Tint.WithAlpha(progress);

				if (controller.GameType == GameType.Artificial || !gameObject.IsProxy)
					gameObject.Transform.LocalPosition = position - vector * progress;
			} );
			anim.OnStop( gameObject =>
			{
				var sound = Sound.Play( "figure_use", position );
				sound.Volume = 0.1f;
				if (controller.GameType == GameType.Artificial || !gameObject.IsProxy)
					gameObject.Transform.LocalPosition = gameObject.Transform.LocalPosition.WithZ( 74.355f );
			} );
			await GameTask.Delay( 200 );
		}

		if (Scene == null)
			return;
		
		await GameTask.Delay( 1000 );
		
		controller.IsPaused = false;
	}
	
	public async void Hide()
	{
		if (Scene == null)
			return;
		
		var controller = Scene.Components.GetInDescendants<GameController>();
		controller.IsPaused = true;
		
		foreach ( var gameObjectChild in GameObject.Children )
		{
			gameObjectChild.Tags.Remove("dead");
		}
		
		foreach ( var gameObjectChild in GameObject.Children )
		{
			var renderer = gameObjectChild.Components.Get<SkinnedModelRenderer>();

			var position = gameObjectChild.Transform.LocalPosition;
			var vector = new Vector3( 0, 0, 100 );

			Animation.Play( gameObjectChild, gameObjectChild.Id.ToString(), 2, EasingFunc.EaseInCubic, ( gameObject, progress ) =>
			{
				renderer.Tint = renderer.Tint.WithAlpha(1 - progress);

				gameObject.Transform.LocalPosition = position + vector * progress;
			} );
			await GameTask.Delay( 200 );
		}
	}
}
