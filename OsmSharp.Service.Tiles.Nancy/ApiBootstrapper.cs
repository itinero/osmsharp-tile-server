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

using System.Collections.Generic;

namespace OsmSharp.Service.Tiles.Nancy
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
    }
}