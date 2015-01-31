// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2015 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.

using Nancy.Hosting.Self;
using OsmSharp.Math.Geo.Projections;
using OsmSharp.Osm.PBF.Streams;
using OsmSharp.Osm.Streams.Filters;
using OsmSharp.UI.Map.Layers;
using OsmSharp.UI.Map.Styles.MapCSS;
using OsmSharp.UI.Map.Styles.Streams;
using OsmSharp.UI.Renderer.Scene;
using OsmSharp.UI.Renderer.Scene.Simplification;
using OsmSharp.WinForms.UI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OsmSharp.Service.Tiles.Sample.SelfHost
{
    class Program
    {
        public static void Main(string[] args)
        {
            Native.Initialize();

            // enable logging and use the console as output.
            OsmSharp.Logging.Log.Enable();
            OsmSharp.Logging.Log.RegisterListener(
                new OsmSharp.WinForms.UI.Logging.ConsoleTraceListener());

            // initialize mapcss interpreter.
            var mapCSSInterpreter = new MapCSSInterpreter(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.Service.Tiles.Sample.SelfHost.custom.mapcss"),
                new MapCSSDictionaryImageSource());

            var scene = new Scene2D(new OsmSharp.Math.Geo.Projections.WebMercator(), new List<float>(new float[] {
                16, 14, 12, 10 }));
            var target = new StyleOsmStreamSceneTarget(
                mapCSSInterpreter, scene, new WebMercator());
            var source = new PBFOsmStreamSource(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.Service.Tiles.Sample.SelfHost.kempen.osm.pbf"));
            var progress = new OsmStreamFilterProgress();
            progress.RegisterSource(source);
            target.RegisterSource(progress);
            target.Pull();

            var merger = new Scene2DObjectMerger();
            scene = merger.BuildMergedScene(scene);

            // create a new instance (with a cache).
            var instance = new RenderingInstance();
            instance.Map.AddLayer(new LayerScene(scene));

            // add a default test instance.
            ApiBootstrapper.AddInstance("default", instance);

            // start hosting this!
            using (var host = new NancyHost(new Uri("http://localhost:1234")))
            {
                host.Start();

                OsmSharp.Logging.Log.TraceEvent("Program", OsmSharp.Logging.TraceEventType.Information, "Nancyhost now listening @ http://localhost:1234");
                System.Diagnostics.Process.Start("http://localhost:1234/default");
                Console.ReadLine();
            }
        }
    }
}
