﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Xml.Serialization;
using Brewmaster.Modules.Ppu;

namespace Brewmaster.EditorWindows.TileMaps
{
	public class TileMap
	{
		public int Width = 1;
		public int Height = 1;
		public List<int> MetaTileResolutions = new List<int> {2, 4};
		public List<Palette> Palettes = new List<Palette>();
		public Size BaseTileSize = new Size(8, 8);
		public Size AttributeSize = new Size(2, 2);
		public Size ScreenSize = new Size(32, 30);
		public int BitsPerPixel = 2;
		public int ColorCount { get { return (int)Math.Pow(2, BitsPerPixel); } }
		public List<List<TileMapScreen>> Screens = new List<List<TileMapScreen>>();

		public SerializableTileMap GetSerializable()
		{
			return new SerializableTileMap
			{
				Width = Width,
				Height = Height,
				ScreenSize = ScreenSize,
				AttributeSize = AttributeSize,
				BitsPerPixel = BitsPerPixel,
				Screens = GetScreenArray(),
				Palettes = Palettes.Select(p => p.Colors).ToList()
			};
		}

		private SerializableScreen[] GetScreenArray()
		{
			var screens = new SerializableScreen[Width * Height];
			for (var y = 0; y < Height; y++)
			for (var x = 0; x < Width; x++)
			{
				if (Screens.Count <= y || Screens[y].Count <= x || Screens[y][x] == null) continue;
				screens[y * Width + x] = new SerializableScreen
				{
					Tiles = Screens[y][x].Tiles,
					ColorAttributes = Screens[y][x].ColorAttributes
				};
			}
			return screens;
		}
	}
	public class TileMapScreen
	{
		private readonly TileMap _map;

		public TileMapScreen(TileMap map)
		{
			_map = map;
			Tiles = new int[map.ScreenSize.Width * map.ScreenSize.Height];
			ColorAttributes = new int[(map.ScreenSize.Width / map.AttributeSize.Width) * (map.ScreenSize.Height / map.AttributeSize.Height)];
			Image = new Bitmap(map.ScreenSize.Width * map.BaseTileSize.Width, map.ScreenSize.Height * map.BaseTileSize.Height);
		}

		public Bitmap Image;
		public int[] Tiles;
		public int[] ColorAttributes;
		public event Action<int, int> TileChanged;
		public event Action EditEnd;

		public void PrintTile(int x, int y, int index)
		{
			Tiles[y * _map.ScreenSize.Width + x] = index;
			if (TileChanged != null) TileChanged(x, y);
		}

		public int GetTile(int x, int y)
		{
			var index = y * _map.ScreenSize.Width + x;
			return Tiles.Length > index ? Tiles[index] : -1;
		}

		public void SetColorAttribute(int x, int y, int paletteIndex)
		{
			var attributeIndex = y * (_map.ScreenSize.Width / _map.AttributeSize.Width) + x;
			ColorAttributes[attributeIndex] = (ColorAttributes[attributeIndex] & 0xF8) | paletteIndex;
			
			if (TileChanged == null) return;
			for (var i = 0; i < _map.AttributeSize.Width; i++)
			for (var j = 0; j < _map.AttributeSize.Height; j++)
			{
				TileChanged(x * _map.AttributeSize.Width + i, y * _map.AttributeSize.Height + j);
			}
		}

		public int GetColorAttribute(int x, int y)
		{
			return ColorAttributes[y * (_map.ScreenSize.Width / _map.AttributeSize.Width) + x] & 0x07;
		}

		public void SetColorTile(int x, int y, int paletteIndex)
		{
			SetColorAttribute(x / _map.AttributeSize.Width, y / _map.AttributeSize.Height, paletteIndex);
		}
		public int GetColorTile(int x, int y)
		{
			return GetColorAttribute(x / _map.AttributeSize.Width, y / _map.AttributeSize.Height);
		}

		public void RefreshTile(int x, int y, MapEditorState state)
		{
			var index = y * _map.ScreenSize.Width + x;
			var attributeIndex = (y / _map.AttributeSize.Height) * (_map.ScreenSize.Width / _map.AttributeSize.Width) + (x / _map.AttributeSize.Width);
			var paletteIndex = ColorAttributes[attributeIndex];
			using (var tile = TilePalette.GetTileImage(state.ChrData, Tiles[index], _map.Palettes[paletteIndex].Colors))
			{
				if (tile == null) return;
				using (var graphics = Graphics.FromImage(Image))
				{
					graphics.CompositingMode = CompositingMode.SourceCopy;
					graphics.DrawImageUnscaled(tile, x * _map.BaseTileSize.Width, y * _map.BaseTileSize.Height);
				}
			}

		}

		public void RefreshAllTiles(MapEditorState state)
		{
			for (var x = 0; x < _map.ScreenSize.Width; x++)
			for (var y = 0; y < _map.ScreenSize.Height; y++)
			{
				RefreshTile(x, y, state);
			}

		}

		public void OnEditEnd()
		{
			if (EditEnd != null) EditEnd();
		}

		public MetaTile GetMetaTile(int x, int y, int size)
		{
			var tiles = new int[size * size];
			var attributes = new int[(size / _map.AttributeSize.Width) * (size / _map.AttributeSize.Height)];
			for (var i = 0; i < tiles.Length; i++)
			{
				var iX = x * size + (i % size); // (xoffset) + (local X)
				var iY = y * size + (i / size); // (yoffset) + (local Y)
				//attributes[((i/size) / _map.AttributeSize.Height) * _map.AttributeSize.Width + ((i % size) / _map.AttributeSize.Width)] = GetColorTile(iX, iY);
				tiles[i] = GetTile(iX, iY);
			}

			for (var i = 0; i < attributes.Length; i++)
			{
				var iX = x * size + (i % (size / _map.AttributeSize.Width)) * _map.AttributeSize.Width;
				var iY = y * size + (i / (size / _map.AttributeSize.Width)) * _map.AttributeSize.Height;
				attributes[i] = GetColorTile(iX, iY);
			}

			return new MetaTile
			{
				Tiles = tiles,
				Attributes = attributes
			};
		}

		public void PrintMetaTile(int x, int y, MetaTile metaTile, int size)
		{
			for (var i = 0; i < metaTile.Tiles.Length; i++)
			{
				var iX = x * size + (i % size);
				var iY = y * size + (i / size);
				PrintTile(iX, iY, metaTile.Tiles[i]);
			}
			for (var i = 0; i < metaTile.Attributes.Length; i++)
			{
				var iX = x * size + (i % (size / _map.AttributeSize.Width)) * _map.AttributeSize.Width;
				var iY = y * size + (i / (size / _map.AttributeSize.Width)) * _map.AttributeSize.Height;
				SetColorTile(iX, iY, metaTile.Attributes[i]);
			}
		}
	}

	[Serializable]
	public class SerializableTileMap
	{
		public Size ScreenSize = new Size(32, 30);
		public Size AttributeSize = new Size(2, 2);
		public int BitsPerPixel;
		public int Width;
		public int Height;
		public string ChrSource;
		public SerializableScreen[] Screens;
		public List<List<Color>> Palettes;

		public TileMap GetMap()
		{
			var map = new TileMap
			{
				Width = Width,
				Height = Height,
				ScreenSize = ScreenSize,
				BitsPerPixel = BitsPerPixel,
				AttributeSize = AttributeSize,
				Palettes = Palettes != null ? Palettes.Select(c => new Palette { Colors = c }).ToList() : new List<Palette>()
			};
			for (var y = 0; y < Height; y++)
			{
				var row = new List<TileMapScreen>();
				map.Screens.Add(row);
				for (var x = 0; x < Width; x++)
				{
					if (Screens.Length <= y * Width + x) break;
					var screenSource = Screens[y * Width + x];
					if (screenSource == null)
					{
						row.Add(null);
						continue;
					}
					var screen = new TileMapScreen(map);
					if (screenSource.Tiles != null) screen.Tiles = screenSource.Tiles;
					if (screenSource.ColorAttributes != null) screen.ColorAttributes = screenSource.ColorAttributes;
					row.Add(screen);
				}
			}
			
			return map;
		}
	}

	[Serializable]
	public class SerializableScreen
	{
		public int[] Tiles;
		public int[] ColorAttributes;
	}

	[Serializable]
	[XmlRoot("tilemap")]
	public class PyxelMap
	{
		[XmlAttribute(AttributeName = "tileswide")]
		public int Width;
		[XmlAttribute(AttributeName = "tileshigh")]
		public int Height;

		[XmlElement(ElementName = "layer")]
		public List<Layer> Layers;

		public class Layer
		{
			[XmlAttribute(AttributeName = "number")]
			public int Number;
			[XmlAttribute(AttributeName = "name")]
			public string Name;

			[XmlElement(ElementName = "tile")]
			public List<Tile> Tiles;
		}

		public class Tile
		{
			[XmlAttribute(AttributeName = "x")]
			public int X;
			[XmlAttribute(AttributeName = "y")]
			public int Y;
			[XmlAttribute(AttributeName = "index")]
			public int Index;
		}
	}
}