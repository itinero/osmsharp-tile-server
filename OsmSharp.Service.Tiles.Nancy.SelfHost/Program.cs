using Nancy.Hosting.Self;
using OsmSharp.Math.Geo.Projections;
using OsmSharp.Osm.PBF.Streams;
using OsmSharp.Osm.Streams.Filters;
using OsmSharp.Osm.Xml.Streams;
using OsmSharp.UI.Map.Layers;
using OsmSharp.UI.Map.Styles.MapCSS;
using OsmSharp.UI.Map.Styles.Streams;
using OsmSharp.UI.Renderer.Scene;
using OsmSharp.UI.Renderer.Scene.Simplification;
using OsmSharp.WinForms.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OsmSharp.Service.Tiles.Nancy.SelfHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Native.Initialize();

            // enable logging and use the console as output.
            OsmSharp.Logging.Log.Enable();
            OsmSharp.Logging.Log.RegisterListener(
                new OsmSharp.WinForms.UI.Logging.ConsoleTraceListener());

            // initialize mapcss interpreter.
            var mapCSSInterpreter = new MapCSSInterpreter(Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.Service.Tiles.Nancy.SelfHost.custom.mapcss"),
                new MapCSSDictionaryImageSource());

            var scene = new Scene2D(new OsmSharp.Math.Geo.Projections.WebMercator(), new List<float>(new float[] {
                16, 14, 12, 10 }));
            var target = new StyleOsmStreamSceneTarget(
                mapCSSInterpreter, scene, new WebMercator());
            var source = new PBFOsmStreamSource(Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.Service.Tiles.Nancy.SelfHost.kempen-big.osm.pbf"));
            var progress = new OsmStreamFilterProgress();
            progress.RegisterSource(source);
            target.RegisterSource(progress);
            target.Pull();

            var merger = new Scene2DObjectMerger();
            scene = merger.BuildMergedScene(scene);

            var instance = new RenderingInstance();
            instance.Map.AddLayer(new LayerScene(scene));

            // add a default test instance.
            ApiBootstrapper.AddInstance("default", instance);

            // start hosting this!
            using (var host = new NancyHost(new Uri("http://localhost:1234")))
            {
                host.Start();
                Console.ReadLine();
            }
        }
    }
}
