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

            Game.RenderSystem.DisplayBoundsChanged += (s, e) =>
            {
                masterView.Resize(Game.RenderSystem.DisplayBounds.Width, Game.RenderSystem.DisplayBounds.Height);
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

            Game.Mouse.Scroll += (sender, args) =>
            {
                graph.Camera.Zoom(args.WheelDelta > 0 ? -0.1f : 0.1f);
            };

            Game.Mouse.Move += (sender, args) =>
            {
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
            var zone = -1;
            var currentChr = "";
            var zoneIndex = -1;
            var lines = File.ReadAllLines(path);
            var index = 0;
            foreach (var line in lines)
            {
                var info = line.Split(',');
                int id = index++;
                Log.Message(info[0] + ' ' + id);

                var chr = info[0].Split(':')[0];
                if (!chr.Equals(currentChr))
                {
                    zoneIndex++;
                    currentChr = chr;
                    Log.Message("zone" + zoneIndex);
                }

                if (!allVertices.ContainsKey(id))
                {
                    if (zone == zoneIndex && id != 0)
                    {
                        try
                        {
                                var type = 1;
                               // var id = i;
                                var stock = id - 1;


                                Graph.Link link = new Graph.Link()
                                {
                                    SourceID = id,
                                    StockID = stock,
                                    Length = 10,
                                    Force = 0,
                                    Orientation = Vector3.Zero,
                                    Weight = graph.cfg.MaxLinkWidth,
                                    LinkType = type + 1,
                                    Color = paletteByZone.ElementAt(zone).ToVector4(),
                                    Width = 10,
                                    LifeTime = 0,
                                    TotalLifeTime = 0,
                                };
                                allEdges.Add(link);
                            }
                        
                        catch (Exception e)
                        {
                            Log.Message(e.StackTrace);
                        }
                    }
                    else
                    {
                        zone = zoneIndex;
                    }
                    Graph.Vertice node = new Graph.Vertice()
                    {
                        Position = new Vector3(float.Parse(info[1]), float.Parse(info[2]), float.Parse(info[3])) * 1000,//gcConfig.LinkSize,
                        Velocity = Vector3.Zero,
                        Color = paletteByZone.ElementAt(zone).ToVector4(),//to bechange
                        Size = float.Parse(info[4]) * 1000,
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
            //try
            //{
            //    for (int i = 0; i < lines.Count() - 1; i++)
            //    {
            //        var type = 1;
            //        var id = i;
            //        var stock = i + 1;


            //        Graph.Link link = new Graph.Link()
            //        {
            //            SourceID = id,
            //            StockID = stock,
            //            Length = 10,
            //            Force = 0,
            //            Orientation = Vector3.Zero,
            //            Weight = graph.cfg.MaxLinkWidth,
            //            LinkType = type + 1,
            //            Color = ColorConstant.paletteWhite.ElementAt(type + 1).ToVector4(),
            //            Width = 10,
            //            LifeTime = 0,
            //            TotalLifeTime = 0,
            //        };
            //        allEdges.Add(link);
            //    }
            //}
            //catch (Exception e)
            //{
            //    Log.Message(e.StackTrace);
            //}

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
            graph.staticMode = true;
            graph.Pause();
            graph.AddMaxParticles();



            viewLayer.GraphLayers.Add(graph);
        }

        void LoadContent()
        {

        }

        public readonly List<Color> paletteByZone = new List<Color>()
        {
            new Color(255,176,46),
            new Color(205,127,50),
            new Color(144,0,32),
            new Color(69,22,28),
            new Color(52,59,41),
new Color(213,113,63),
new Color(100,149,237),
new Color(154,206,235),
new Color(218,216,113),
new Color(52,201,36),
new Color(222,76,138),
new Color(0,255,127),
new Color(236,234,190),
new Color(167,252,0),
new Color(189,51,164),
new Color(112,41,99),
new Color(94,33,41),
new Color(65,72,51),
new Color(145,30,66),
new Color(37,109,123),
new Color(0,149,182),
new Color(255,207,72),
new Color(204,85,0),
new Color(184,183,153),
new Color(220,220,220),
new Color(223,115,255),
new Color(243,165,5),
new Color(115,66,34),
new Color(201,160,220),
new Color(193,84,193),
new Color(66,94,23),
new Color(181,121,0),
new Color(0,84,31),
new Color(89,51,21),
new Color(202,55,103),
new Color(21,96,189),
new Color(255,67,164),
new Color(252,108,133),
new Color(162,173,208),
new Color(245,245,245),
new Color(244,169,0),
new Color(253,188,180),
new Color(237,255,33),
new Color(225,204,79),
new Color(154,205,50),
new Color(197,227,132),

        };


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
                graph.Helper.NodeSelection();
            }
        }

        //public struct Zone
        //{
        //    Dictionary<int, Graph.Vertice> nodes;
        //    public string Name;
        //    public Vector4 color;
        //}
        //Dictionary<int, Zone> zones;

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
