// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 215 Abelshausen Ben
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

using Nancy;

namespace OsmSharp.Service.Tiles
{
    /// <summary>
    /// A nancy module service tiles.
    /// </summary>
    public class TileModule : NancyModule
    {
        /// <summary>
        /// Creates a new instance of the tile module.
        /// </summary>
        public TileModule()
        {
            Get["{instance}/{z}/{x}/{y}.png"] = _ =>
            {
                return this.DoTiles(_, 1);
            };
            Get["{instance}/{z}/{x}/{y}@2x.png"] = _ =>
            {
                return this.DoTiles(_, 2);
            };
        }

        /// <summary>
        /// Executes to get tile request.
        /// </summary>
        /// <param name="_"></param>
        private dynamic DoTiles(dynamic _, int scale)
        {
            string instanceName = _.instance;

            // get instance.
            var instance = ApiBootstrapper.Get(instanceName);
            if(instance == null)
            { // not found!
                return Negotiate.WithStatusCode(HttpStatusCode.NotFound);
            }

            // x,y,z.
            int x = -1, y = -1;
            ushort z = 0;
            if (ushort.TryParse(_.z, out z) &&
                int.TryParse(_.x, out x) &&
                int.TryParse(_.y, out y))
            { // ok, valid stuff!
                var stream = instance.Get(x, y, z, scale);
                if (stream == null)
                {
                    return Negotiate.WithStatusCode(HttpStatusCode.NotFound);
                }
                return Response.FromStream(stream, "image/png");
            }
            return Negotiate.WithStatusCode(HttpStatusCode.NotFound);
        }
    }
}