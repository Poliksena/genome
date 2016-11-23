using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Graphics;
using Fusion.Core;
using Fusion.Core.Configuration;
using Fusion.Framework;
using Fusion.Build;
using Fusion.Engine.Graphics.GIS;
using Fusion.Engine.Graphics.GIS.GlobeMath;
using Fusion.Engine.Frames;
using Fusion;
using Fusion.Core.Shell;
using System.IO;
using Fusion.Engine.Media;
using Fusion.Engine.Audio;
using Fusion.Engine.Graphics.Graph;


namespace SignalingSample
{
	[Command("refreshServers", CommandAffinity.Default)]
	public class RefreshServerList : NoRollbackCommand
	{

		public RefreshServerList(Invoker invoker) : base(invoker)
		{
		}

		public override void Execute()
		{
			Invoker.Game.GameInterface.StartDiscovery(4, new TimeSpan(0, 0, 10));
		}

	}

	[Command("stopRefresh", CommandAffinity.Default)]
	public class StopRefreshServerList : NoRollbackCommand
	{

		public StopRefreshServerList(Invoker invoker) : base(invoker)
		{
		}

		public override void Execute()
		{
			Invoker.Game.GameInterface.StopDiscovery();
		}

	}




	class CustomGameInterface : Fusion.Engine.Common.UserInterface
	{

		[GameModule("Console", "con", InitOrder.Before)]
		public GameConsole Console { get { return console; } }
		public GameConsole console;


		[GameModule("GUI", "gui", InitOrder.Before)]
		public FrameProcessor FrameProcessor { get { return userInterface; } }
		FrameProcessor userInterface;


		RenderWorld masterView;
		RenderLayer viewLayer;
		SoundWorld soundWorld;
		DiscTexture debugFont;

		private Vector2 prevMousePos;
		private Vector2 mouseDelta;


		private GraphLayer graph;

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="engine"></param>
		public CustomGameInterface(Game game) : base(game)
		{
			console = new GameConsole(game, "conchars");
			userInterface = new FrameProcessor(game, @"Fonts\textFont");
		}



		float angle = 0;


		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			debugFont = Game.Content.Load<DiscTexture>("conchars");

			var bounds = Game.RenderSystem.DisplayBounds;
			masterView = Game.RenderSystem.RenderWorld;


			Game.RenderSystem.RemoveLayer(masterView);

			viewLayer = new RenderLayer(Game);
			Game.RenderSystem.AddLayer(viewLayer);

			Game.RenderSystem.DisplayBoundsChanged += (s,e) => {
				masterView.Resize( Game.RenderSystem.DisplayBounds.Width, Game.RenderSystem.DisplayBounds.Height );
			};

			viewLayer.SpriteLayers.Add(console.ConsoleSpriteLayer);


			Game.Keyboard.KeyDown += Keyboard_KeyDown;

			LoadContent();

			Game.Reloading += (s, e) => LoadContent();


			Game.Touch.Tap += args => System.Console.WriteLine("You just perform tap gesture at point: " + args.Position);
			Game.Touch.DoubleTap += args => System.Console.WriteLine("You just perform double tap gesture at point: " + args.Position);
			Game.Touch.SecondaryTap += args => System.Console.WriteLine("You just perform secondary tap gesture at point: " + args.Position);
			Game.Touch.Manipulate += args => System.Console.WriteLine("You just perform touch manipulation: " + args.Position + "	" + args.ScaleDelta + "	" + args.RotationDelta + " " + args.IsEventBegin + " " + args.IsEventEnd);


			graph = new GraphLayer(Game);
			graph.Camera = new GreatCircleCamera();

			Game.Mouse.Scroll += (sender, args) => {
				graph.Camera.Zoom(args.WheelDelta > 0 ? -0.1f : 0.1f);
			};

			Game.Mouse.Move += (sender, args) => {
				if (Game.Keyboard.IsKeyDown(Keys.LeftButton))
				{
					graph.Camera.RotateCamera(mouseDelta);
				}
				if (Game.Keyboard.IsKeyDown(Keys.MiddleButton))
				{
					graph.Camera.MoveCamera(mouseDelta);
				}
			};

			var g = new Graph();
			// Здесь должно быть чтение из файла и запись в узлы
			string path = @"Data/Hela_sonic_LMNA_interactions_diploid_10071362_iter_final.csv";
			Dictionary<int, Graph.Vertice> allVertices = new Dictionary<int, Graph.Vertice>();
			List<Graph.Link> allEdges = new List<Graph.Link>();

			var lines = File.ReadAllLines(path);
            var index = 0;
			foreach(var line in lines)
			{
				var info = line.Split(',');
				int id = index++;
				Log.Message(info[0] + ' ' + id);

				if (!allVertices.ContainsKey(id))
				{
                    int zone = 2;							

                    Graph.Vertice node = new Graph.Vertice()
                    {
						Position = new Vector3(float.Parse(info[1]), float.Parse(info[2]), float.Parse(info[3])) * 1000,//gcConfig.LinkSize,
						Velocity = Vector3.Zero,
						Color = ColorConstant.paletteByGroup.ElementAt(zone).ToVector4(),
						Size = float.Parse(info[4])*1000,
						Acceleration = Vector3.Zero,
						Mass = 0,
						Information = 1,
						Id = id,
						Group = zone,
						Charge = 1,
						Cluster = 0,
						ColorType = zone * 100 + graph.cfg.MinParticleRadius * 5,
					};

					allVertices.Add(id, node);
				}
			}
            try
            {
                for (int i = 0; i < lines.Count() - 1; i++)
                {
                    var type = 1;
                    var id = i;
                    var stock = i + 1;


                    Graph.Link link = new Graph.Link()
                    {
                        SourceID = id,
                        StockID = stock,
                        Length = 5,
                        Force = 0,
                        Orientation = Vector3.Zero,
                        Weight = graph.cfg.MaxLinkWidth,
                        LinkType = type + 1,
                        Color = ColorConstant.paletteWhite.ElementAt(type + 1).ToVector4(),
                        Width = 0.2f,
                        LifeTime = 0,
                        TotalLifeTime = 0,
                    };
                    allEdges.Add(link);
                }
            }
            catch (Exception e)
            {
                Log.Message(e.StackTrace);
            }

            foreach (var pair in allVertices)
			{
				var node = pair.Value;
				//node.ColorType = 0;
				g.nodes.Add(node);
			}
			g.NodesCount = g.nodes.Count;
			g.links = allEdges;

			graph.SetGraph(g);
			graph.cfg.GraphLayout = @"Graph/Signaling";
			graph.Initialize();
			graph.staticMode = false;
			graph.AddMaxParticles();
          
			

			viewLayer.GraphLayers.Add(graph);
		}

		void LoadContent()
		{

		}


		void Keyboard_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Keys.F5)
			{
				Builder.SafeBuild();
				Game.Reload();
			}


			if (e.Key == Keys.LeftShift)
			{
				viewLayer.GlobeCamera.ToggleViewToPointCamera();
			}

			if (e.Key == Keys.LeftButton)
			{
				//graph.Helper.NodeSelection();
			}
		}



		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{

				masterView.Dispose();
				viewLayer.Dispose();
			}
			base.Dispose(disposing);
		}


		public override void RequestToExit()
		{
			Game.Exit();
		}


		/// <summary>
		/// Updates internal state of interface.
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update(GameTime gameTime)
		{
			console.Update(gameTime);

			graph.Camera.Update(gameTime);


			//mouseDelta = Game.Mouse.Position - prevMousePos;
			//prevMousePos = Game.Mouse.Position;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="serverInfo"></param>
		public override void DiscoveryResponse(System.Net.IPEndPoint endPoint, string serverInfo)
		{
			Log.Message("DISCOVERY : {0} - {1}", endPoint.ToString(), serverInfo);
		}
	}
}
