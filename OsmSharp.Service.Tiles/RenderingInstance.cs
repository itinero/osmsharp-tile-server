// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2014 Abelshausen Ben
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
using OsmSharp.Osm.Data.Memory;
using OsmSharp.Osm.Streams;
using OsmSharp.Osm.Tiles;
using OsmSharp.Service.Tiles.Cache;
using OsmSharp.UI.Map;
using OsmSharp.UI.Map.Layers;
using OsmSharp.UI.Map.Styles;
using OsmSharp.UI.Map.Styles.MapCSS;
using OsmSharp.WinForms.UI.Renderer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace OsmSharp.Service.Tiles
{
    /// <summary>
    /// A rendering instance.
    /// </summary>
    public class RenderingInstance
    {
        /// <summary>
        /// Holds the target per scale.
        /// </summary>
        private Dictionary<int, Tuple<Bitmap, Graphics>> _targetsPerScale;

        /// <summary>
        /// Holds the renderer.
        /// </summary>
        private MapRenderer<Graphics> _renderer;

        /// <summary>
        /// Holds the map to render.
        /// </summary>
        private Map _map;

        /// <summary>
        /// Holds the tile cache.
        /// </summary>
        private TileCache _cache;

        /// <summary>
        /// Creates a new rendering instance.
        /// </summary>
        public RenderingInstance()
            : this(null)
        {

        }

        /// <summary>
        /// Creates a new rendering instance.
        /// </summary>
        /// <param name="cache">The cache.</param>
        public RenderingInstance(TileCache cache)
        {
            _targetsPerScale = new Dictionary<int, Tuple<Bitmap, Graphics>>();
            _renderer = new MapRenderer<Graphics>(new GraphicsRenderer2D());
            _map = new Map(new WebMercator());
            _cache = cache;
        }

        /// <summary>
        /// Gets the map for this instance.
        /// </summary>
        public Map Map
        {
            get
            {
                return _map;
            }
        }

        /// <summary>
        /// Returns a tile for the given zoom, x and y.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="zoom"></param>
        /// <param name="scale">Scale parameter, 1 = 256, 2 = 512, ...</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual Stream Get(int x, int y, ushort zoom, int scale, ImageType type = ImageType.Png)
        {
            Stream cachedImage;
            var tile = new Tile(x, y, zoom);
            if (_cache != null &&
                _cache.TryGet(tile, scale, type, out cachedImage))
            { // read from cache.
                return cachedImage;
            }
            var renderedImage = this.Render(x, y, scale, zoom, type);
            if(_cache != null)
            { // cache image.
                _cache.Write(tile, scale, type, renderedImage);
                renderedImage.Seek(0, SeekOrigin.Begin);
            }
            return renderedImage;
        }

        /// <summary>
        /// Renders a tile for the given zoom, x and y.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="zoom"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual Stream Render(int x, int y, int scale, ushort zoom, ImageType type)
        {
            var tile = new Tile(x, y, zoom);
            var projection = _map.Projection;
            var zoomFactor = (float)projection.ToZoomFactor(zoom);
            var center = tile.Box.Center;
            var sizeInPixels = scale * 256;

            // get target/image.
            Bitmap image = null;
            Graphics target = null;
            lock(_targetsPerScale)
            {
                Tuple<Bitmap, Graphics> tuple;
                if(!_targetsPerScale.TryGetValue(scale, out tuple))
                { // not there yet!
                    // build the target to render to.
                    image = new Bitmap(sizeInPixels, sizeInPixels);
                    target = Graphics.FromImage(image);
                    target.SmoothingMode = SmoothingMode.HighQuality;
                    target.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    target.CompositingQuality = CompositingQuality.HighQuality;
                    target.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    tuple = new Tuple<Bitmap, Graphics>(image, target);
                    _targetsPerScale[scale] = tuple;
                }
                target = tuple.Item2;
                image = tuple.Item1;
            }

            var stream = new MemoryStream();
            lock (target)
            {
                target.FillRectangle(Brushes.White, 0, 0, sizeInPixels, sizeInPixels);
                var visibleView = _renderer.Create(256, 256, _map, zoomFactor, center, false, true);
                var renderingView = _renderer.Create(256 * 3, 256 * 3, _map, zoomFactor, center, false, true);
                _map.ViewChanged(zoomFactor, center, renderingView, renderingView);
                _renderer.Render(target, _map, visibleView, renderingView, (float)_map.Projection.ToZoomFactor(zoom));

                switch (type)
                {
                    case ImageType.Png:
                        image.Save(stream, ImageFormat.Png);
                        break;
                    case ImageType.Bmp:
                        image.Save(stream, ImageFormat.Bmp);
                        break;
                    case ImageType.Jpeg:
                        image.Save(stream, ImageFormat.Jpeg);
                        break;
                }
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        #region Static Instance Builders

        /// <summary>
        /// Builds a rendering instance.
        /// </summary>
        /// <param name="streamSource">Source data.</param>
        /// <param name="interpreter">The style interpreter.</param>
        /// <returns></returns>
        public static RenderingInstance Build(OsmStreamSource streamSource, StyleInterpreter interpreter)
        {
            var instance = new RenderingInstance();

            // load data into memory.
            var dataSource = MemoryDataSource.CreateFrom(streamSource);

            // add layer to map.
            instance.Map.AddLayer(new LayerOsm(dataSource, interpreter, instance.Map.Projection));

            return instance;
        }

        /// <summary>
        /// Builds a rendering instance for mapCSS.
        /// </summary>
        /// <param name="streamSource"></param>
        /// <param name="mapCSS"></param>
        /// <returns></returns>
        public static RenderingInstance BuildForMapCSS(OsmStreamSource streamSource, string mapCSS)
        {
            var instance = new RenderingInstance();

            // load data into memory.
            var dataSource = MemoryDataSource.CreateFrom(streamSource);

            // create mapCSS interpreter.
            var interpreter = new MapCSSInterpreter(mapCSS);

            // add layer to map.
            instance.Map.AddLayer(new LayerOsm(dataSource, interpreter, instance.Map.Projection));

            return instance;
        }

        /// <summary>
        /// Builds a rendering instance for a mapCSS file.
        /// </summary>
        /// <param name="streamSource"></param>
        /// <param name="mapCSSFile"></param>
        /// <returns></returns>
        public static RenderingInstance BuildForMapCSS(OsmStreamSource streamSource, Stream mapCSSFile)
        {
            var instance = new RenderingInstance();

            // load data into memory.
            var dataSource = MemoryDataSource.CreateFrom(streamSource);

            // create mapCSS interpreter.
            var interpreter = new MapCSSInterpreter(mapCSSFile, new MapCSSDictionaryImageSource());

            // add layer to map.
            instance.Map.AddLayer(new LayerOsm(dataSource, interpreter, instance.Map.Projection));

            return instance;
        }

        #endregion
    }
}