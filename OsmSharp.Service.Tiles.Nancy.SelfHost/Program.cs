using Nancy.Hosting.Self;
using OsmSharp.Osm.PBF.Streams;
using OsmSharp.WinForms.UI;
using System;
using System.Collections.Generic;
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

            // add a default test instance.
            ApiBootstrapper.AddInstance("default", RenderingInstance.BuildForMapCSS(
                new PBFOsmStreamSource(Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.Service.Tiles.Nancy.SelfHost.kempen-big.osm.pbf")),
                Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.Service.Tiles.Nancy.SelfHost.default.mapcss")));

            // start hosting this!
            using (var host = new NancyHost(new Uri("http://localhost:1234")))
            {
                host.Start();
                Console.ReadLine();
            }
        }
    }
}
