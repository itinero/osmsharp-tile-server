OsmSharp.Service.Tiles
======================

### This is unsupported and unmaintained, can serve as inspiration to others!

Serves tiles directly from raw OSM files using the rendering functionality of OsmSharp. Handles for now only sub-country or city-sized OSM-extracts because all data is kept in-memory. Support will be added for vector data files in the future.

<p>
	<img src="https://raw.githubusercontent.com/OsmSharp/OsmSharp.Service.Tiles/master/screenshots/osmsharp_tiles_leaflet.png" width="600"/>
</p>

Setup
-----

Pull this repository, build and run the sample project or the selfhost project. An example configuration file is preloaded but you should be able to customize it:

```xml
﻿<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <configSections>
    <section name="TileApiConfiguration" type="OsmSharp.Service.Tiles.Configurations.TileApiConfiguration,
             OsmSharp.Service.Tiles"/>
  </configSections>
  <TileApiConfiguration>
    <instances>
      <!--A simple instance loading raw osm-data from a pbf file.-->
      <!--Check http://localhost:1234/tiles_{name}/{z}/{x}/{y}.png after starting.-->
      <add name="{name}" data="/path/to/osm-file.osm.pbf" format="osm-pbf" mapcss="path/to/mapcss-file.mapcss"/>
    </instances>
  </TileApiConfiguration>
</configuration>
```
