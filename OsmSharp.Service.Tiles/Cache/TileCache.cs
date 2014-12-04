using OsmSharp.Osm.Tiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Holds the object used to synchronize.
        /// </summary>
        private object _sync = new object();

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
            lock (_sync)
            {
                foreach (var directory in _cacheDir.EnumerateDirectories())
                {
                    directory.Delete(true);
                }
            }
        }

        /// <summary>
        /// Returns a file info object for a given tile.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <param name="type">The type of image.</param>
        /// <returns></returns>
        private FileInfo GetTileFile(Tile tile, ImageType type)
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
            return new FileInfo(Path.Combine(_cacheDir.FullName, tile.Zoom.ToString(), tile.X.ToInvariantString(), tile.Y.ToInvariantString() + extension));
        }

        /// <summary>
        /// Returns true if this tiles cache contains the given tile.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <param name="type">The type of image.</param>
        /// <returns></returns>
        public bool Has(Tile tile, ImageType type)
        {
            var fileInfo = this.GetTileFile(tile, type);
            lock (_sync)
            {
                return fileInfo.Exists && (DateTime.Now - fileInfo.CreationTime).TotalMilliseconds < _maxAge;
            }
        }

        /// <summary>
        /// Tries to get a tile and returns true if successfull.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="type"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public bool TryGet(Tile tile, ImageType type, out Stream image)
        {
            var fileInfo = this.GetTileFile(tile, type);
            image = null;
            lock (_sync)
            {
                if (fileInfo.Exists && (DateTime.Now - fileInfo.CreationTime).TotalMilliseconds < _maxAge)
                {
                    var fileStream = fileInfo.OpenRead();
                    var memoryStream = new MemoryStream();
                    CopyStream(fileStream, memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Writes a new file to cache.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="type"></param>
        /// <param name="image"></param>
        public void Write(Tile tile, ImageType type, Stream image)
        {
            lock (_sync)
            {
                var fileInfo = this.GetTileFile(tile, type);
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
