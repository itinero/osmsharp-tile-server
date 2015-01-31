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

using OsmSharp.Math.Geo.Projections;
using OsmSharp.Osm.PBF.Streams;
using OsmSharp.Osm.Streams;
using OsmSharp.Osm.Xml.Streams;
using OsmSharp.Service.Tiles.Configurations;
using OsmSharp.Service.Tiles.Monitoring;
using OsmSharp.UI.Map.Layers;
using OsmSharp.UI.Map.Styles.MapCSS;
using OsmSharp.UI.Map.Styles.Streams;
using OsmSharp.UI.Renderer.Scene;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace OsmSharp.Service.Tiles
{
    /// <summary>
    /// Holds all rendering instances and other static information for the API.
    /// </summary>
    public static class ApiBootstrapper
    {
        /// <summary>
        /// Holds all booted rendering instances.
        /// </summary>
        private static Dictionary<string, RenderingInstance> _instances = new Dictionary<string,RenderingInstance>();

        /// <summary>
        /// Adds a new rendering instance.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="instance"></param>
        public static void AddInstance(string name, RenderingInstance instance)
        {
            _instances[name] = instance;
        }

        /// <summary>
        /// Returns the rendering instance with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static RenderingInstance Get(string name)
        {
            RenderingInstance instance;
            if(_instances.TryGetValue(name, out instance))
            {
                return instance;
            }
            return null;
        }


        ///<summary>
        ///Holds all instance monitors.
        ///</summary>
        private static List<InstanceMonitor> _instanceMonitors = new List<InstanceMonitor>();

        /// <summary>
        /// A delegate to load a new instance configuration.
        /// </summary>
        /// <param name="apiConfiguration"></param>
        /// <param name="instanceConfiguration"></param>
        /// <returns></returns>
        public delegate bool InstanceLoaderDelegate(TileApiConfiguration apiConfiguration, InstanceConfiguration instanceConfiguration);

        /// <summary>
        /// Does the actual bootstrapping.
        /// </summary>
        public static void BootFromConfiguration()
        {
            // get the api configuration.
            var apiConfiguration = (TileApiConfiguration)ConfigurationManager.GetSection("TileApiConfiguration");

            // load all relevant routers.
            foreach (InstanceConfiguration instanceConfiguration in apiConfiguration.Instances)
            {
                var thread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
                {
                    // load instance.
                    if (LoadInstance(apiConfiguration, instanceConfiguration))
                    { // instance loaded correctly.
                        // start monitoring files...
                        if (apiConfiguration.Monitor)
                        { // ...but only when configured as such.
                            var monitor = new InstanceMonitor(apiConfiguration, instanceConfiguration, LoadInstance);
                            _instanceMonitors.Add(monitor);

                            // get data file configuration.
                            var data = instanceConfiguration.Data;
                            if (!string.IsNullOrWhiteSpace(data))
                            { // monitor data.
                                monitor.AddFile(data);
                            }
                            // get mapcss configuration.
                            var mapCSS = instanceConfiguration.MapCSS;
                            if (!string.IsNullOrWhiteSpace(mapCSS))
                            { // monitor mapcss.
                                monitor.AddFile(mapCSS);
                            }
                            monitor.Start();
                        }
                    }
                }));
                thread.Start();
            }
        }

        /// <summary>
        /// Holds a sync object.
        /// </summary>
        private static object _sync = new object();

        /// <summary>
        /// Loads a new instance.
        /// </summary>
        /// <param name="apiConfiguration"></param>
        /// <param name="instanceConfiguration"></param>
        /// <returns></returns>
        private static bool LoadInstance(TileApiConfiguration apiConfiguration, InstanceConfiguration instanceConfiguration)
        {
            // get data file configuration.
            var data = instanceConfiguration.Data;

            // get mapcss configuration.
            var mapCSS = instanceConfiguration.MapCSS;
            if (string.IsNullOrWhiteSpace(mapCSS))
            {

            }

            // get the format.
            var format = instanceConfiguration.Format;

            // get the include file.
            try
            {
                // create routing instance.
                OsmSharp.Logging.Log.TraceEvent("Bootstrapper", OsmSharp.Logging.TraceEventType.Information,
                    string.Format("Creating {0} instance...", instanceConfiguration.Name));
                switch (format.ToLowerInvariant())
                {
                    case "osm-xml":
                        using (var stream = new FileInfo(data).OpenRead())
                        {
                            var streamSource = new XmlOsmStreamSource(stream);
                            using (var cssStream = new FileInfo(mapCSS).OpenRead())
                            {
                                ApiBootstrapper.BuildTileServer("tiles_" +
                                    instanceConfiguration.Name, streamSource, cssStream, apiConfiguration.Cache);
                            }
                        }
                        break;
                    case "osm-pbf":
                        using (var stream = new FileInfo(data).OpenRead())
                        {
                            var streamSource = new PBFOsmStreamSource(stream);
                            using (var cssStream = new FileInfo(mapCSS).OpenRead())
                            {
                                ApiBootstrapper.BuildTileServer("tiles_" +
                                    instanceConfiguration.Name, streamSource, cssStream, apiConfiguration.Cache);
                            }
                        }
                        break;
                    default:
                        throw new Exception(string.Format("Unrecognised raw osm format: {0}", format));
                }
                OsmSharp.Logging.Log.TraceEvent("Bootstrapper", OsmSharp.Logging.TraceEventType.Information,
                    string.Format("Instance {0} created successfully!", instanceConfiguration.Name));
            }
            catch (Exception ex)
            {
                OsmSharp.Logging.Log.TraceEvent("Bootstrapper", OsmSharp.Logging.TraceEventType.Error,
                    string.Format("Exception occured while creating instance {0}:{1}",
                    instanceConfiguration.Name, ex.ToInvariantString()));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Builds a tile server instance based on the given osm source and mapcss styles file.
        /// </summary>
        /// <param name="name">The name of the instance-to-be.</param>
        /// <param name="source">The osm source stream.</param>
        /// <param name="mapCSSfile">The stream containing the mapcss.</param>
        /// <param name="cacheFolder"></param>
        private static void BuildTileServer(string name, OsmStreamSource source, Stream mapCSSfile, string cacheFolder)
        {
            try
            {
                // initialize mapcss interpreter.
                var mapCSSInterpreter = new MapCSSInterpreter(mapCSSfile, new MapCSSDictionaryImageSource());

                var scene = new Scene2D(new OsmSharp.Math.Geo.Projections.WebMercator(), new List<float>(new float[] {
                                    16, 14, 12, 10 }));
                var target = new StyleOsmStreamSceneTarget(
                    mapCSSInterpreter, scene, new WebMercator());
                target.RegisterSource(source);
                target.Pull();

                //var merger = new Scene2DObjectMerger();
                //scene = merger.BuildMergedScene(scene);

                OsmSharp.Service.Tiles.RenderingInstance instance = null;
                if (string.IsNullOrWhiteSpace(cacheFolder))
                { // no cache.
                    instance = new OsmSharp.Service.Tiles.RenderingInstance();
                }
                else
                { // use cache.
                    var instanceCacheFolder = Path.Combine(cacheFolder, name);
                    var instanceCacheDirectoryInfo = new DirectoryInfo(instanceCacheFolder);
                    if (!instanceCacheDirectoryInfo.Exists)
                    { // create the directory if it doesn't exists.
                        instanceCacheDirectoryInfo.Create();
                    }
                    var instanceCache = new OsmSharp.Service.Tiles.Cache.TileCache(new DirectoryInfo(instanceCacheFolder));
                    instanceCache.Clear();
                    instance = new OsmSharp.Service.Tiles.RenderingInstance(instanceCache);
                }
                instance.Map.AddLayer(new LayerScene(scene));

                // add a default test instance.
                OsmSharp.Service.Tiles.ApiBootstrapper.AddInstance(name, instance);
            }
            catch (Exception ex)
            {
                OsmSharp.Logging.Log.TraceEvent("Bootstrapper.BuildTileServer", OsmSharp.Logging.TraceEventType.Error, 
                    "Failed to setup tile service: " + ex.Message);
            }
        }

    }
}