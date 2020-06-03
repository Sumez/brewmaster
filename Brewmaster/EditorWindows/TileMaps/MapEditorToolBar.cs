﻿using System;
using System.Windows.Forms;
using Brewmaster.Properties;

namespace Brewmaster.EditorWindows.TileMaps
{
	public class MapEditorToolBar : ToolStrip
	{
		public Action ImportImage { get; set; }
		public Action ImportChr { get; set; }
		public Action ImportMap { get; set; }
		public Action ImportPalette { get; set; }
		public Action ImportJsonSession { get; set; }

		public Action TileTool { get; set; }
		public Action ColorTool { get; set; }
		public Action PixelTool { get; set; }
		public Action MetaTool { get; set; }

		public Action ToggleGrid { get; set; }
		public Action ToggleMetaValues { get; set; }

		public MapEditorToolBar()
		{
			GripStyle = ToolStripGripStyle.Hidden;

			Items.AddRange(new[] {
				new ToolStripButton("import image", Resources.image, (s, a) => ImportImage()),
				new ToolStripButton("import CHR", Resources.image, (s, a) => ImportChr()),
				new ToolStripButton("import map", Resources.macro, (s, a) => ImportMap()),
				new ToolStripButton("import Palette", Resources.image, (s, a) => ImportPalette()),
				new ToolStripButton("import JSON", Resources.macro, (s, a) => ImportJsonSession()),

				new ToolStripButton("Tile", Resources.image, (s, a) => TileTool()),
				new ToolStripButton("Color", Resources.data, (s, a) => ColorTool()),
				new ToolStripButton("Pen", Resources.data, (s, a) => PixelTool()),
				new ToolStripButton("Collisions", Resources.chip, (s, a) => MetaTool()),

				GridButton = new ToolStripButton("Show grid", Resources.chip, (s, a) => ToggleGrid()) { CheckOnClick = true },
				CollisionButton = new ToolStripButton("Show Collisions", Resources.chip, (s, a) => ToggleMetaValues()) { CheckOnClick = true }	
			});
		}

		public ToolStripButton CollisionButton { get; set; }
		public ToolStripButton GridButton { get; set; }
	}
}
