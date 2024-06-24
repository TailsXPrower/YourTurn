using Sandbox.Network;
using System;
using System.Threading.Tasks;
namespace Sandbox;

public enum GameType
{
	Artificial,
	Player
}

public sealed class GameController : Component, Component.INetworkListener
{
	public static bool IsMultiplayer { get; set; }
	
	public Dictionary<FigureColor, int> WinsCounter { get; set; } = new();

	[Property] public GameObject Camera { get; set; }
	[Property] public GameObject CameraBlackPos { get; set; }
	[Property] public GameObject CameraWhitePos { get; set; }
	[Property] public GameObject CameraCounterPos { get; set; }
	[Property] public GameObject CameraLeftPos { get; set; }
	[Property] public GameObject CameraRightPos { get; set; }

	[HostSync] public GameType GameType { get; set; } = GameType.Artificial;

	[HostSync] public NetDictionary<FigureColor, string> PlayersName { get; set; } = new();
	[HostSync] public NetDictionary<Guid, FigureColor> Players { get; set; } = new();

	bool _isPaused;
	public bool IsPaused
	{
		get
		{
			return _isPaused;
		}
		set
		{
			_isPaused = value;
			
			var figureComponent = Scene.Components.GetAll<FigureComponent>(FindMode.InDescendants);
			foreach ( var component in figureComponent )
			{
				component.IsUsable = !value;
			}
		}
	}

	public Timer BlackTimer { get; set; }
	public Timer WhiteTimer { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		WinsCounter.Add(FigureColor.White, 0);
		WinsCounter.Add(FigureColor.Black, 0);

		var cameraPos = GetCameraMainPos();
		Camera.Transform.Position = cameraPos.Transform.Position;
		Camera.Transform.Rotation = cameraPos.Transform.Rotation;
		
		if (IsMultiplayer || GameType == GameType.Player)
			return;

		var chess = Scene.Components.GetInDescendants<ChessComponent>();
		chess.Start();
	}

	public void OnReset()
	{
		RemoveTimers();
		WinsCounter.Clear();
		
		WinsCounter.Add(FigureColor.White, 0);
		WinsCounter.Add(FigureColor.Black, 0);
		
		var cameraPos = GetCameraMainPos();
		Camera.Transform.Position = cameraPos.Transform.Position;
		Camera.Transform.Rotation = cameraPos.Transform.Rotation;
		
		foreach ( var sceneChild in Scene.Children.Where( sceneChild => sceneChild.Tags.Has("items") ) )
		{
			foreach ( var item in sceneChild.Children )
			{
				var renderer = item.Components.Get<ModelRenderer>();
				renderer.Tint = renderer.Tint.WithAlpha(1);

				item.Tags.Remove("item");

				Animation.Play( item, item.Id.ToString(), 2, EasingFunc.Linear, ( gameObject, progress ) =>
				{
					renderer.Tint = renderer.Tint.WithAlpha(1 - progress);
				} );
			}
		}
	}
	
	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor )
			return;

		if ( IsMultiplayer && !GameNetworkSystem.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			GameNetworkSystem.CreateLobby();
		}
	}

	public bool IsLoaded;

	TimeSince _timeSinceSecond = 0;
	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( Players.ContainsKey( Connection.Local.Id ) && !IsLoaded )
		{
			IsLoaded = true;
			OnConnected();
		}

		/*if ( Players.Count <= 1 && Players.ContainsKey( Connection.Local.Id ) && Players[Connection.Local.Id] == FigureColor.Black )
		{
			GameNetworkSystem.Disconnect();
			Scene.LoadFromFile( "Scenes/mainmenu.scene" );
		}*/

		if (IsPaused)
			return;

		if ( _timeSinceSecond > 1 )
		{
			_timeSinceSecond = 0;
			
			var chess = Scene.Components.GetInDescendants<ChessComponent>();
			switch ( chess.Turn )
			{
				case FigureColor.Black:
					BlackTimer?.Update();
					break;
				case FigureColor.White:
					WhiteTimer?.Update();
					break;
				case FigureColor.None:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	public void CreateTimers()
	{
		BlackTimer = new Timer( 4*60, _ =>
		{
			BlackTimer = null;
			WhiteTimer = null;
			Won( FigureColor.White );
		}, (timer) =>
		{
			var monitor = Scene.Components.GetInDescendants<InfoMonitor>();
			monitor.UpdateTimer( FigureColor.Black, timer.Duration - timer.Progress );
		} );
		
		WhiteTimer = new Timer( 4*60, _ =>
		{
			BlackTimer = null;
			WhiteTimer = null;
			Won( FigureColor.Black );
		}, (timer) =>
		{
			var monitor = Scene.Components.GetInDescendants<InfoMonitor>();
			monitor.UpdateTimer( FigureColor.White, timer.Duration - timer.Progress );
		} );
	}
	
	public void RemoveTimers()
	{
		BlackTimer = null;
		WhiteTimer = null;
	}
	
	public void Won( FigureColor? color )
	{
		Won(color ?? FigureColor.None);
	}
	
	[Broadcast]
	public void Won( FigureColor color )
	{
		AssignVictory(color);
	}

	public GameObject GetCameraMainPos()
	{
		if ( !Players.ContainsKey( Connection.Local.Id) )
			return CameraWhitePos;
		
		return Players[Connection.Local.Id] switch
		{
			FigureColor.White => CameraWhitePos,
			FigureColor.Black => CameraBlackPos,
			_ => CameraWhitePos
		};
	}
	
	public GameObject GetCameraDirPos()
	{
		if ( !Players.ContainsKey( Connection.Local.Id) )
			return CameraLeftPos;
		
		return Players[Connection.Local.Id] switch
		{
			FigureColor.White => CameraLeftPos,
			FigureColor.Black => CameraRightPos,
			_ => CameraLeftPos
		};
	}

	public async void AssignVictory(FigureColor color)
	{
		var chess = Scene.Components.GetInDescendants<ChessComponent>();
		var monitor = Scene.Components.GetInDescendants<InfoMonitor>();
		
		if ( color != FigureColor.None )
		{
			WinsCounter[color]++;
		
			monitor.UpdateDeath(color == FigureColor.White ? FigureColor.Black : FigureColor.White, WinsCounter[color]);
		}

		Services.Stats.Increment( "total_played", 1 );
		
		IsPaused = true;

		var camera = Scene.Components.GetInDescendants<CameraComponent>().GameObject;
		camera.Transform.Position = CameraCounterPos.Transform.Position;
		camera.Transform.Rotation = CameraCounterPos.Transform.Rotation;

		if ( WinsCounter.TryGetValue( color, out var value ) && value == 3)
		{
			if ( GameType == GameType.Player )
			{
				if (Players[Connection.Local.Id] == color )
					Services.Stats.Increment( "wins", 1 );
			} else
			{
				Services.Stats.Increment( "wins_single", 1 );
			}
			
			await Task.DelaySeconds( 3f );
			
			monitor.Win( color );
			
			await Task.DelaySeconds( 3f );
		
			OnEndGame(color);
		} 
		else if ( WinsCounter[FigureColor.White] == 2 || WinsCounter[FigureColor.Black] == 2 )
		{
			await Task.DelaySeconds( 4f );

			CreateTimers();
			
			await Task.DelaySeconds( 1f );

			camera.Transform.Position = GetCameraMainPos().Transform.Position;
			camera.Transform.Rotation = GetCameraMainPos().Transform.Rotation;
			
			chess.Restart();
		}
		else
		{
			await Task.DelaySeconds( 3f );
		
			camera.Transform.Position = GetCameraMainPos().Transform.Position;
			camera.Transform.Rotation = GetCameraMainPos().Transform.Rotation;
		
			await Task.DelaySeconds( 1 );
			
			chess.Restart();
		}

		SpawnItems();
	}

	async void OnEndGame(FigureColor winner)
	{
		Camera.Transform.Position = GetCameraMainPos().Transform.Position;
		Camera.Transform.Rotation = GetCameraMainPos().Transform.Rotation;

		var ui = Scene.Components.GetInDescendants<GameUIComponent>();
		ui.Close();

		await GameTask.DelaySeconds( 3 );
		GameNetworkSystem.Disconnect();
		Scene.LoadFromFile( "Scenes/mainmenu.scene" );
	}

	async void SpawnItems()
	{
		if (WinsCounter[FigureColor.Black] >= 3 || WinsCounter[FigureColor.White] >= 3)
			return;
		
		if (WinsCounter[FigureColor.White] < 1 && WinsCounter[FigureColor.Black] < 1)
			return;
		
		await Task.DelaySeconds( 0.5f );
		RotateCameraLeft();
			
		await Task.DelaySeconds( 0.5f );
		
		foreach ( var sceneChild in Scene.Children.Where( sceneChild => sceneChild.Tags.Has("items") ) )
		{
			sceneChild.Enabled = true;
			Sound.Play( "bell", sceneChild.Transform.Position );	
			
			foreach ( var item in sceneChild.Children )
			{
				var renderer = item.Components.Get<ModelRenderer>();
				renderer.Tint = renderer.Tint.WithAlpha(0);

				item.Tags.Add("item");

				Animation.Play( item, item.Id.ToString(), 2, EasingFunc.Linear, ( gameObject, progress ) =>
				{
					renderer.Tint = renderer.Tint.WithAlpha(progress);
				} );
			}
		}
			
		await Task.DelaySeconds( 2f );
			
		RotateCameraRight();
	}
	
	public void RotateCameraLeft()
	{
		var camera = Scene.Components.GetInDescendants<CameraComponent>().GameObject;

		if (Animation.HasAnimation(camera, "right_rotation") || 
		    Animation.HasAnimation(camera, "left_rotation"))
			return;
		
		if (camera.Transform.Rotation == GetCameraDirPos().Transform.Rotation)
			return;

		
		var origAngles = camera.Transform.Rotation.Angles();
		var endAngles = GetCameraDirPos().Transform.Rotation.Angles();
		Animation.Play( camera, "left_rotation", 2.5f, ( gameObject, progress ) =>
		{
			//var vector = new Vector3();
			//camera.Transform.Rotation = Rotation.SmoothDamp( camera.Transform.Rotation, gameController.CameraLeftPos.Transform.Rotation, ref vector, 0.4f, Time.Delta);
			var angles = camera.Transform.Rotation.Angles();
			angles.yaw = origAngles.yaw + (endAngles.yaw - origAngles.yaw) * progress;
			angles.pitch = origAngles.pitch + (endAngles.pitch - origAngles.pitch) * progress;
			angles.roll = origAngles.roll + (endAngles.roll - origAngles.roll) * progress;
			camera.Transform.Rotation = Rotation.From( angles );
		} );
	}
	
	public void RotateCameraRight()
	{
		var gameController = Scene.Components.GetInDescendants<GameController>();
		var camera = Scene.Components.GetInDescendants<CameraComponent>().GameObject;

		if (Animation.HasAnimation(camera, "right_rotation") || 
		    Animation.HasAnimation(camera, "left_rotation"))
			return;
		
		if (camera.Transform.Rotation == gameController.GetCameraMainPos().Transform.Rotation)
			return;

		var origAngles = camera.Transform.Rotation.Angles();
		var endAngles = gameController.GetCameraMainPos().Transform.Rotation.Angles();
		Animation.Play( camera, "right_rotation", 2.5f, ( gameObject, progress ) =>
		{
			//var vector = new Vector3();
			//camera.Transform.Rotation = Rotation.SmoothDamp( camera.Transform.Rotation, gameController.CameraMainPos.Transform.Rotation, ref vector, 0.4f, Time.Delta);
			var angles = camera.Transform.Rotation.Angles();
			angles.yaw = origAngles.yaw + (endAngles.yaw - origAngles.yaw) * progress;
			angles.pitch = origAngles.pitch + (endAngles.pitch - origAngles.pitch) * progress;
			angles.roll = origAngles.roll + (endAngles.roll - origAngles.roll) * progress;
			camera.Transform.Rotation = Rotation.From( angles );
		} );
	}
	
	public void OnConnected()
	{
		var cameraPos = GetCameraMainPos();
		Camera.Transform.Position = cameraPos.Transform.Position;
		Camera.Transform.Rotation = cameraPos.Transform.Rotation;

		var leaderboard = Scene.Components.GetInDescendants<Leaderboard>();
		if ( Players[Connection.Local.Id] == FigureColor.Black )
		{
			leaderboard.GameObject.Transform.Position = leaderboard.GameObject.Transform.Position.WithX(leaderboard.GameObject.Transform.Position.x + 250);
		}
		
		if ( Players.Count == 2 )
		{
			var chess = Scene.Components.GetInDescendants<ChessComponent>();
			chess.Start();
		}
	}

	/// <summary>
	/// Called on the host when someone successfully joins the server (including the local player)
	/// </summary>
	public void OnActive( Connection connection )
	{
		// Spawn a player for this client
		//var player = PlayerPrefab.Clone( SpawnPoint.Transform.World );

		// Find the NameTag component and set their name correctly
		/*var nameTag = player.Components.Get<NameTagPanel>( FindMode.EverythingInSelfAndDescendants );
		if ( nameTag is not null )
		{
			nameTag.Name = connection.DisplayName;
		}*/
		
		GameType = GameType.Player;

		var color = PlayersName.ContainsKey( FigureColor.White ) ? FigureColor.Black : FigureColor.White;
		
		PlayersName.Add(color, connection.DisplayName);
		Players.Add(connection.Id, color);

		if ( Players.Count == 2 )
		{
			var chess = Scene.Components.GetInDescendants<ChessComponent>();
			chess.Start();
		}

		foreach ( var figureComponent in Scene.Components.GetAll<FigureComponent>( FindMode.InDescendants ) )
		{
			if (figureComponent.Color != color)
				continue;
			
			figureComponent.GameObject.Network.AssignOwnership( connection );
		}
		
		foreach ( var itemComponent in Scene.Components.GetAll<ItemComponent>( FindMode.InDescendants ) )
		{
			if (itemComponent.PlayerColor != color)
				continue;
			
			itemComponent.GameObject.Network.AssignOwnership( connection );
		}

		Log.Info($"Player {connection.DisplayName} with ID {connection.Id} successfully connected!");
		// Spawn it on the network, assign connection as the owner
		//player.NetworkSpawn( connection );
	}

	public void OnBecameHost( Connection conn )
	{
		Log.Info($"Host {conn.DisplayName} with ID {conn.Id} disconnected!");
		
		if (!Players.ContainsKey(conn.Id))
			return;
		
		var color = Players[conn.Id];
		
		foreach ( var figureComponent in Scene.Components.GetAll<FigureComponent>( FindMode.InDescendants ) )
		{
			if (figureComponent.Color != color)
				continue;
			
			figureComponent.GameObject.Network.DropOwnership();
		}
		
		foreach ( var itemComponent in Scene.Components.GetAll<ItemComponent>( FindMode.InDescendants ) )
		{
			if (itemComponent.PlayerColor != color)
				continue;
			
			itemComponent.GameObject.Network.DropOwnership();
		}
		
		Players.Remove( conn.Id );
		PlayersName.Remove( color );

		var board = Scene.Components.GetInDescendants<ChessComponent>();
		board.Stop();
	}

	public void OnDisconnected( Connection conn )
	{
		Log.Info($"Player {conn.DisplayName} with ID {conn.Id} disconnected!");
		
		if (!Players.ContainsKey(conn.Id))
			return;
		
		var color = Players[conn.Id];
		
		foreach ( var figureComponent in Scene.Components.GetAll<FigureComponent>( FindMode.InDescendants ) )
		{
			if (figureComponent.Color != color)
				continue;
			
			figureComponent.GameObject.Network.DropOwnership();
		}
		
		foreach ( var itemComponent in Scene.Components.GetAll<ItemComponent>( FindMode.InDescendants ) )
		{
			if (itemComponent.PlayerColor != color)
				continue;
			
			itemComponent.GameObject.Network.DropOwnership();
		}
		
		Players.Remove( conn.Id );
		PlayersName.Remove( color );

		var board = Scene.Components.GetInDescendants<ChessComponent>();
		board.Stop();
	}
}

public class Timer
{
	public int Duration;
	public int Progress;
	public Action<Timer> OnEnd;
	public Action<Timer> OnUpdate;
	
	public Timer(int duration, Action<Timer> onEnd, Action<Timer> onUpdate)
	{
		Duration = duration;
		OnEnd = onEnd;
		OnUpdate = onUpdate;
		
		onUpdate.Invoke(this);
	}
	
	public bool IsEnded()
	{
		return Progress >= Duration;
	}

	public void Update()
	{
		if (IsEnded())
			return;
		
		Progress++;
		OnUpdate.Invoke(this);

		if ( IsEnded() )
		{
			OnEnd.Invoke(this);
		}
	}
}
