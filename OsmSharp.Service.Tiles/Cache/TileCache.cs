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

using OsmSharp.Osm.Tiles;
using System;
using System.IO;

namespace OsmSharp.Service.Tiles.Cache
{
    /// <summary>
    /// Represents a tile cache, a location on disk to store tiles.
    /// </summary>
    public class TileCache
    {
        /// <summary>
        /// Holds the directory to store cached tiles.
        /// </summary>
        private DirectoryInfo _cacheDir;

        /// <summary>
        /// Holds the maximum time a tile can live in cache.
        /// </summary>
        private double _maxAge;

        /// <summary>
        /// Creates a new tile cache.
        /// </summary>
        /// <param name="cacheDir">Cache directory.</param>
        public TileCache(DirectoryInfo cacheDir)
            : this(cacheDir, new TimeSpan(10, 0, 0, 0))
        {

        }

        /// <summary>
        /// Creates a new tile cache.
        /// </summary>
        /// <param name="cacheDir">Cache directory.</param>
        /// <param name="ttl">Time to live.</param>
        public TileCache(DirectoryInfo cacheDir, TimeSpan ttl)
        {
            _cacheDir = cacheDir;
            _maxAge = ttl.TotalMilliseconds;
        }

        /// <summary>
        /// Clears all tiles from this cache.
        /// </summary>
        public void Clear()
        {
            foreach (var directory in _cacheDir.EnumerateDirectories())
            {
                directory.Delete(true);
            }
        }

        /// <summary>
        /// Returns a file info object for a given tile.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="type">The type of image.</param>
        /// <returns></returns>
        private FileInfo GetTileFile(Tile tile, int scale, ImageType type)
        {
            string extension = string.Empty;
            switch (type)
            {
                case ImageType.Png:
                    extension = ".png";
                    break;
                case ImageType.Bmp:
                    extension = ".bmp";
                    break;
                case ImageType.Jpeg:
                    extension = ".jpg";
                    break;
            }
            if(scale != 0)
            { // add scale if needed.
                extension = string.Format("{0}x", scale) + extension;
            }
            return new FileInfo(Path.Combine(_cacheDir.FullName, tile.Zoom.ToString(), tile.X.ToInvariantString(), tile.Y.ToInvariantString() + extension));
        }

        /// <summary>
        /// Returns true if this tiles cache contains the given tile.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="type">The type of image.</param>
        /// <returns></returns>
        public bool Has(Tile tile, int scale, ImageType type)
        {
            var fileInfo = this.GetTileFile(tile, scale, type);
            lock (fileInfo.FullName)
            {
                return fileInfo.Exists && (DateTime.Now - fileInfo.CreationTime).TotalMilliseconds < _maxAge;
            }
        }

        /// <summary>
        /// Tries to get a tile and returns true if successfull.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="scale"></param>
        /// <param name="type"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public bool TryGet(Tile tile, int scale, ImageType type, out Stream image)
        {
            var fileInfo = this.GetTileFile(tile, scale, type);
            lock (fileInfo.FullName)
            {
                image = null;
                if (fileInfo.Exists && (DateTime.Now - fileInfo.CreationTime).TotalMilliseconds < _maxAge)
                {
                    using (var fileStream = fileInfo.OpenRead())
                    {
                        image = new MemoryStream();
                        CopyStream(fileStream, image);
                        image.Seek(0, SeekOrigin.Begin);
                    }
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Writes a new file to cache.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="scale"></param>
        /// <param name="type"></param>
        /// <param name="image"></param>
        public void Write(Tile tile, int scale, ImageType type, Stream image)
        {
            var fileInfo = this.GetTileFile(tile, scale, type);
            lock (fileInfo.FullName)
            {
                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }
                if (!fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }
                using (var outputStream = fileInfo.OpenWrite())
                {
                    CopyStream(image, outputStream);
                }
            }
        }

        /// <summary>
        /// Copies one stream into the other.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[0x1000];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, read);
        }
    }
}
