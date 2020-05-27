﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Brewmaster.ProjectExplorer;
using Brewmaster.Properties;

namespace Brewmaster.EditorWindows.TileMaps
{
	public class MapScreenView : Control
	{
		public MapEditorTool Tool { get { return _state.Tool; } }

		private readonly TileMap _map;
		private TileMapScreen _screen;
		private Pen _dotted;
		private Pen _solid;
		private Pen _dashed;
		private Bitmap _grid;
		private int _cursorX = -1;
		private int _cursorY = -1;
		private bool _mouseDown;
		private bool _alteredByTool;
		private SolidBrush _toolBrush;
		private MapEditorState _state;

		public MapScreenView(TileMap map, TileMapScreen screen, MapEditorState state)
		{
			_map = map;
			_screen = screen;
			_state = state;

			DoubleBuffered = true;

			var gridColor = Color.FromArgb(128, 255, 255, 255);
			_dotted = new Pen(gridColor, 1);
			_dotted.DashStyle = DashStyle.Custom;
			_dotted.DashPattern = new float[] { 1, 3 };

			_dashed = new Pen(gridColor, 1);
			_dashed.DashStyle = DashStyle.Custom;
			_dashed.DashPattern = new float[] { 2, 2 };

			_solid = new Pen(gridColor, 1);

			_toolBrush = new SolidBrush(Color.FromArgb(128, 255, 255, 255));

			_state.ZoomChanged += RefreshView;
			RefreshView();

			_screen.TileChanged += RefreshTile;

			var mouseHandler = new OsFeatures.GlobalMouseHandler();
			mouseHandler.MouseUp += MouseButtonUp;
			Application.AddMessageFilter(mouseHandler);
			Disposed += (s, a) =>
			{
				mouseHandler.MouseUp -= MouseButtonUp;
				Application.RemoveMessageFilter(mouseHandler);
			};
			state.ToolChanged += () => { Cursor = state.Tool.Pixel ? new Cursor(new MemoryStream(Resources.pen)) : Cursors.Default; };
		}
		protected override void Dispose(bool disposing)
		{
			_state.ZoomChanged -= RefreshView;
			base.Dispose(disposing);
		}

		private void RefreshView()
		{
			var width = _map.ScreenSize.Width * _map.BaseTileSize.Width * Zoom;
			var height = _map.ScreenSize.Height * _map.BaseTileSize.Height * Zoom;
			if (Width != width || Height != height) Size = new Size(width, height);
			GenerateGrid();
			Invalidate();
		}

		private bool MouseButtonUp(Point location)
		{
			if (_mouseDown && _alteredByTool) _screen.OnEditEnd();

			_mouseDown = false;
			_alteredByTool = false;
			return false;
		}


		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (_cursorX < 0 || _cursorY < 0) return;

			switch (e.Button)
			{
				case MouseButtons.Left:
					_mouseDown = true;
					_alteredByTool = false;
					if (Tool != null) PaintTool();
					break;
				case MouseButtons.Right:
					if (Tool != null) Tool.EyeDrop(_cursorX, _cursorY, _screen);
					break;
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			var x = e.Location.X / ToolWidth;
			if (x < 0 || x >= (_map.ScreenSize.Width * _map.BaseTileSize.Width) / (Tool.Size.Width * (Tool.Pixel ? 1 : _map.BaseTileSize.Width))) x = -1;

			var y = e.Location.Y / ToolHeight;
			if (y < 0 || y >= (_map.ScreenSize.Height * _map.BaseTileSize.Height) / (Tool.Size.Height * (Tool.Pixel ? 1 : _map.BaseTileSize.Height))) y = -1;

			if (_cursorX == x && _cursorY == y) return;
			var oldX = _cursorX;
			var oldY = _cursorY;
			_cursorX = x;
			_cursorY = y;
			InvalidateToolPosition(oldX, oldY);
			InvalidateToolPosition(_cursorX, _cursorY);

			if (_mouseDown && _cursorX >= 0 && _cursorY >= 0) PaintTool();

			base.OnMouseMove(e);
		}

		private void PaintTool()
		{
			Tool.Paint(_cursorX, _cursorY, _screen);
			_alteredByTool = true;
		}

		
		protected override void OnMouseLeave(EventArgs e)
		{
			var oldX = _cursorX;
			var oldY = _cursorY;
			_cursorX = _cursorY = -1;
			base.OnMouseLeave(e);
			InvalidateToolPosition(oldX, oldY);
		}

		private void InvalidateToolPosition(int x, int y)
		{
			if (x < 0 || y < 0) return;
			Invalidate(new Rectangle(x * ToolWidth, y * ToolHeight, ToolWidth + 1, ToolHeight + 1));
		}

		private void GenerateGrid()
		{
			var width = _map.ScreenSize.Width * _map.BaseTileSize.Width * Zoom;
			var height = _map.ScreenSize.Height * _map.BaseTileSize.Height * Zoom;
			var tileWidth = _map.BaseTileSize.Width * Zoom;
			var tileHeight = _map.BaseTileSize.Height * Zoom;

			var grid = new Bitmap(Zoom * _map.ScreenSize.Width * _map.BaseTileSize.Width, Zoom * _map.ScreenSize.Height * _map.BaseTileSize.Height);
			using (var graphics = Graphics.FromImage(grid))
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.CompositingQuality = CompositingQuality.HighSpeed;
				for (var i = 1; i < _map.ScreenSize.Width; i++)
				{
					if (Zoom == 1 && i % 2 == 1) continue;
					var pen = i % 4 == 0 ? _solid : i % 2 == 0 ? _dashed : _dotted;
					graphics.DrawLine(pen, i * tileWidth, 0, i * tileWidth, height);
				}
				for (var i = 1; i < _map.ScreenSize.Height; i++)
				{
					if (Zoom == 1 && i % 2 == 1) continue;
					var pen = i % 4 == 0 ? _solid : i % 2 == 0 ? _dashed : _dotted;
					graphics.DrawLine(pen, 0, i * tileHeight, width, i * tileHeight);
				}
			}

			if (_grid != null) _grid.Dispose();
			_grid = grid;
		}

		public int Zoom
		{
			get { return _state.Zoom; }
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			//var timer = new Stopwatch();
			//timer.Start();
			e.Graphics.CompositingMode = CompositingMode.SourceCopy;
			e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
			e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			e.Graphics.DrawImage(_screen.Image, new Rectangle(0, 0, _screen.Image.Width * Zoom, _screen.Image.Height * Zoom));

			if (_cursorX >= 0 && _cursorY >= 0 && Tool.Image != null)
			{
				var attributeIndex = (_cursorY / (_map.BaseTileSize.Height * _map.AttributeSize.Height)) * (_map.ScreenSize.Width / _map.AttributeSize.Width) + (_cursorX / (_map.BaseTileSize.Width * _map.AttributeSize.Width));
				Tool.RefreshImage(_map.Palettes[_screen.ColorAttributes[attributeIndex] & 0xff]);
				e.Graphics.DrawImage(Tool.Image, new Rectangle(_cursorX * ToolWidth, _cursorY * ToolHeight, Tool.Image.Width * Zoom, Tool.Image.Height * Zoom));
			}

			e.Graphics.CompositingMode = CompositingMode.SourceOver;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.None;
			e.Graphics.DrawImageUnscaled(_grid, 0, 0);

			if (_cursorX >= 0 && _cursorY >= 0)
			{
				if (Tool.Image == null) e.Graphics.FillRectangle(_toolBrush, _cursorX * ToolWidth, _cursorY * ToolHeight, ToolWidth, ToolHeight);
				else if (!Tool.Pixel) e.Graphics.DrawRectangle(Pens.Black, _cursorX * ToolWidth, _cursorY * ToolHeight, ToolWidth, ToolHeight);
			}
			//timer.Stop();
			//Debug.WriteLine("Paint: " + timer.Elapsed.TotalMilliseconds);
		}

		public int ToolWidth { get { return (Tool.Pixel ? 1 : _map.BaseTileSize.Width) * Tool.Size.Width * Zoom; } }
		public int ToolHeight { get { return (Tool.Pixel ? 1 : _map.BaseTileSize.Height) * Tool.Size.Height * Zoom; } }

		public void RefreshTile(int x, int y)
		{
			_screen.RefreshTile(x, y, _state);
			Invalidate(new Rectangle(x * _map.BaseTileSize.Width * Zoom, y * _map.BaseTileSize.Width * Zoom, _map.BaseTileSize.Width * Zoom, _map.BaseTileSize.Height * Zoom));
		}


		public void RefreshAllTiles()
		{
			_screen.RefreshAllTiles(_state);
			Invalidate();
		}
	}
}