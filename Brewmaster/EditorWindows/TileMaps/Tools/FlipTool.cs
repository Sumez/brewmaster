﻿using System;
using System.Drawing;

namespace Brewmaster.EditorWindows.TileMaps.Tools
{
	public class FlipTool : MapEditorTool
	{
		private MapEditorState _state;
		private bool _vertical;

		public FlipTool(MapEditorState state, TileMap map, bool vertical)
		{
			_vertical = vertical;
			_state = state;
			Size = new Size(1, 1);
		}

		public override bool EditsChr { get { return true; } }

		public override void Paint(int x, int y, TileMapScreen screen)
		{
			var tile = screen.GetTile(x, y);

			//screen.Image.SetPixel(x, y, _map.Palettes[palette].Colors[SelectedColor]);

			if (_state.GetTileUsage(tile) > 1)
			{
				// Duplicate tile when more than one is in use
				tile = _state.CopyTile(tile);
				screen.PrintTile(x, y, tile);
			}
			_state.FlipTile(tile, _vertical);
			screen.PrintTile(x, y, tile);
		}
		public override void AfterPaint()
		{
			_state.OnChrDataChanged();
		}
		public override void EyeDrop(int x, int y, TileMapScreen screen)
		{
		}
	}
}