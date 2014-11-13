using System;
using System.IO;
using ToolBelt;
using System.Linq;
using System.Collections.Generic;
using Vacuum;

namespace Vacuum.Filters
{
	public class CopyFilter : FilterBase 
	{
		#region IFilter
		
		public override void Filter()
		{
			IList<ParsedPath> fromPaths = Target.InputPaths;
			IList<ParsedPath> toPaths = Target.OutputPaths;

			List<FileStream> toStreams = new List<FileStream>();

			try
			{
				foreach (ParsedPath toPath in toPaths)
				{
					if (!Directory.Exists(toPath.VolumeAndDirectory))
						Directory.CreateDirectory(toPath.VolumeAndDirectory);

					toStreams.Add(new FileStream(toPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
				}
				
				foreach (ParsedPath fromPath in fromPaths)
				{
					foreach (var toStream in toStreams)
					{
						byte[] fromData = File.ReadAllBytes(fromPath);
				
						toStream.Write(fromData, 0, fromData.Length);
					}
				}
			}
			finally
			{
				foreach (FileStream toStream in toStreams)
					toStream.Close();
			}
		}
		
		#endregion
	}
}

