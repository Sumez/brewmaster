﻿using System;
using System.Collections.Generic;
using System.IO;
using Brewmaster.ProjectModel;

namespace Brewmaster.Pipeline
{
	public class PipelineSettings
	{
		public static readonly List<PipelineOption> PipelineOptions = new List<PipelineOption>
		{
			new TileMapAsmPipeline(),
			new TileMapBinaryPipeline(),
			new ChrPipeline()
		};


		// DATA ESSENTIAL TO OUR STORED FILE
		public AsmProjectFile File { get; set; }
		public virtual List<string> OutputFiles { get; } = new List<string>();
		public DateTime? LastProcessed { get; protected set; }
		public PipelineOption Type { get; }
		public ProjectModel.Properties GenericSettings { get; } = new ProjectModel.Properties();
		// END

		public PipelineSettings(PipelineOption type, AsmProjectFile file, DateTime? lastProcessed = null)
		{
			File = file;
			LastProcessed = lastProcessed;
			Type = type;
		}

		public void Process()
		{
			Type.Process(this);
			LastProcessed = DateTime.Now;
		}

		public PipelineSettings Clone()
		{
			return Type.Clone(this);
		}

		public virtual void SettingChanged(bool registerProjectChange = true)
		{
			LastProcessed = null;
			if (registerProjectChange) File.Project.Pristine = false;
		}

		public string GetFilePath(int index)
		{
			return Path.Combine(File.Project.Directory.FullName, OutputFiles[index]);
		}
	}
}

