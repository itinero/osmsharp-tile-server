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

using System;
using System.IO;

namespace OsmSharp.Service.Tiles
{
    /// <summary>
    /// Facade with implementing rendering functionalities.
    /// </summary>
    public class RenderingFacade
    {
        /// <summary>
        /// Gets a tile from the given instance.
        /// </summary>
        /// <param name="instanceName"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="zoom"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public Stream Get(string instanceName, uint x, uint y, ushort zoom, ImageType image = ImageType.Png)
        {
            throw new NotImplementedException();
        }
    }
}