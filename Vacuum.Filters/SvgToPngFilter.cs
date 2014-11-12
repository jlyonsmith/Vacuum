using System;
using System.Collections.Generic;
using ToolBelt;
using System.Linq;
using System.IO;
using Vacuum;

namespace Vacuum.Filters
{
	public class SvgToPngFilter : IContentFilter
	{
		#region Construction
		public SvgToPngFilter()
		{
		}
		#endregion

		#region Fields
		private FilterExtension[] extensions = new FilterExtension[]
		{
			new FilterExtension(".svg", ".png")
		};
		#endregion 
		
		#region Properties
		[ContentFilterParameter("Width of the bitmap in pixels", Optional = false)]
		public double Width { get; set; }
		
		[ContentFilterParameter("Height of the bitmap in pixels", Optional = false)]
		public double Height { get; set; }
		#endregion
		
		#region IContentFilter
		public IList<FilterExtension> Extensions { get { return extensions; } }
		public VacuumContext Context { get; set; }
		public VacuumTarget Target { get; set; }

		public void Compile()
		{
			ParsedPath svgFileName = Target.InputPaths.Where(f => f.Extension == ".svg").First();
			ParsedPath pngFileName = Target.OutputPaths.Where(f => f.Extension == ".png").First();

			if (!Directory.Exists(pngFileName.VolumeAndDirectory))
			{
				Directory.CreateDirectory(pngFileName.VolumeAndDirectory);
			}

            ImageTools.SvgToPngWithInkscape(svgFileName, pngFileName, (int)this.Width, (int)this.Height);
		}

		#endregion
	}
}

