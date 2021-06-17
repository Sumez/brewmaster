﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Brewmaster.EditorWindows.TileMaps.Tools;

namespace Brewmaster.EditorWindows.TileMaps
{
	public class ChrTilePalette : UserControl
	{
		private readonly MapEditorState _state;
		private TilePalette _tilePalette;
		private Panel _controlPanel;
		private Panel _mainPanel;
		private CheckBox _highlightButton;
		private Button _removeUnusedButton;
		private Button _mergeButton;
		private Container components;
		private ToolTip _toolTip;
		public event Action<int> HoverTile;
		public event Action UserSelectedTile;
		public int SelectedTile
		{
			get { return _tilePalette.SelectedTile; }
			set { _tilePalette.SelectedTile = value; }
		}


		public ChrTilePalette(MapEditorState state)
		{
			_state = state;
			InitializeComponent();
			_tilePalette.AllowTileDrag = true;

			state.ChrDataChanged += RefreshTileCount;
			state.PaletteChanged += _tilePalette.RefreshImage;

			_tilePalette.Tiles = new List<MetaTile>();
			RefreshTileCount();
			_tilePalette.TileClick += SelectTile;
			_tilePalette.TileDrag += state.MoveChrTile;
			_tilePalette.TileHover += (index) => { if (HoverTile != null) HoverTile(index); };
			_tilePalette.RemoveTile = (index) =>
			{
				var usages = state.GetTileUsage(index);
				if (usages > 0)
				{
					MessageBox.Show(string.Format("Cannot delete tile used in {0} locations", usages), "Delete CHR tile", MessageBoxButtons.OK);
					return;
				}
				state.RemoveChrTile(index);
			};

			_state.TileUsageChanged += DisplayTileUsage;
			_highlightButton.CheckStateChanged += (o, a) => { DisplayTileUsage(); };
			_removeUnusedButton.Click += (o, a) => { _state.RemoveUnusedTiles(); };
			_mergeButton.Click += (o, a) => { _state.MergeIdenticalTiles(); };
		}
		
		private void DisplayTileUsage()
		{
			var tiles = new List<int>();
			if (_highlightButton.Checked)
				for (var i = 0; i < _tilePalette.Tiles.Count; i++)
					if (_state.GetTileUsage(i) == 0) tiles.Add(i);
			
			_tilePalette.HighlightedTiles = tiles;
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}


		private void SelectTile(int index)
		{
			_tilePalette.SelectedTile = index;
			if (UserSelectedTile != null) UserSelectedTile();
		}

		private void RefreshTileCount()
		{
			var tiles = _tilePalette.Tiles;
			var targetTiles = _state.ChrData.Length / TileImage.GetTileDataLength();
			if (targetTiles == tiles.Count)
			{
				_tilePalette.RefreshImage();
				return;
			}

			for (var i = tiles.Count; i < targetTiles; ++i) tiles.Add(new MetaTile { Tiles = new[] { i }, Attributes = new[] { -1 } });
			while (tiles.Count > targetTiles) tiles.RemoveAt(tiles.Count - 1);

			_tilePalette.Tiles = tiles;
			if (_tilePalette.SelectedTile >= targetTiles) SelectTile(targetTiles - 1);
		}

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChrTilePalette));
			this.components = new System.ComponentModel.Container();
			this._tilePalette = new Brewmaster.EditorWindows.TileMaps.TilePalette();
			this._controlPanel = new System.Windows.Forms.Panel();
			this._highlightButton = new System.Windows.Forms.CheckBox();
			this._removeUnusedButton = new System.Windows.Forms.Button();
			this._mergeButton = new System.Windows.Forms.Button();
			this._mainPanel = new System.Windows.Forms.Panel();
			this._controlPanel.SuspendLayout();
			this._toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			// 
			// _tilePalette
			// 
			this._tilePalette.State = _state;
			this._tilePalette.AllowTileDrag = false;
			this._tilePalette.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._tilePalette.Location = new System.Drawing.Point(3, 3);
			this._tilePalette.MetaTileWidth = 1;
			this._tilePalette.Name = "_tilePalette";
			this._tilePalette.SelectedTile = -1;
			this._tilePalette.Size = new System.Drawing.Size(564, 507);
			this._tilePalette.TabIndex = 0;
			this._tilePalette.Text = "tilePalette1";
			this._tilePalette.Zoom = 2;
			// 
			// _controlPanel
			// 
			this._controlPanel.Controls.Add(this._highlightButton);
			this._controlPanel.Controls.Add(this._removeUnusedButton);
			this._controlPanel.Controls.Add(this._mergeButton);
			this._controlPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this._controlPanel.Location = new System.Drawing.Point(0, 1211);
			this._controlPanel.Name = "_controlPanel";
			this._controlPanel.Size = new System.Drawing.Size(570, 24);
			this._controlPanel.TabIndex = 1;
			// 
			// _highlightButton
			// 
			this._highlightButton.Appearance = System.Windows.Forms.Appearance.Button;
			this._highlightButton.Location = new System.Drawing.Point(2, 2);
			this._highlightButton.Margin = new System.Windows.Forms.Padding(0, 2, 2, 2);
			this._highlightButton.Name = "_highlightButton";
			this._highlightButton.Size = new System.Drawing.Size(95, 20);
			this._highlightButton.TabIndex = 1;
			this._toolTip.SetToolTip(this._highlightButton, "Highlight tiles unused in the current map");
			this._highlightButton.Text = "Highlight unused";
			this._highlightButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this._highlightButton.UseVisualStyleBackColor = true;
			// 
			// _removeUnusedButton
			// 
			this._removeUnusedButton.Location = new System.Drawing.Point(99, 2);
			this._removeUnusedButton.Margin = new System.Windows.Forms.Padding(0, 2, 2, 2);
			this._removeUnusedButton.Name = "_removeUnusedButton";
			this._removeUnusedButton.Size = new System.Drawing.Size(95, 20);
			this._removeUnusedButton.TabIndex = 1;
			this._toolTip.SetToolTip(this._removeUnusedButton, "Remove tiles unused in the current map");
			this._removeUnusedButton.Text = "Remove unused";
			this._removeUnusedButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this._removeUnusedButton.UseVisualStyleBackColor = true;
			// 
			// _mergeButton
			// 
			this._mergeButton.Location = new System.Drawing.Point(196, 2);
			this._mergeButton.Margin = new System.Windows.Forms.Padding(0, 2, 2, 2);
			this._mergeButton.Name = "_mergeButton";
			this._mergeButton.Size = new System.Drawing.Size(64, 20);
			this._mergeButton.TabIndex = 1;
			this._toolTip.SetToolTip(this._mergeButton, "Merge identical tiles into a single shared tile");
			this._mergeButton.Text = "Merge";
			this._mergeButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this._mergeButton.UseVisualStyleBackColor = true;
			// 
			// _mainPanel
			// 
			this._mainPanel.Controls.Add(this._tilePalette);
			this._mainPanel.AutoScroll = true;
			this._mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._mainPanel.Location = new System.Drawing.Point(0, 0);
			this._mainPanel.Name = "_mainPanel";
			this.Size = new System.Drawing.Size(570, 513);
			this._mainPanel.TabIndex = 2;
			// 
			// ChrTilePalette
			// 
			this.Controls.Add(this._mainPanel);
			this.Controls.Add(this._controlPanel);
			this.Name = "ChrTilePalette";
			this.Size = new System.Drawing.Size(570, 513);
			this._controlPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
	}
}