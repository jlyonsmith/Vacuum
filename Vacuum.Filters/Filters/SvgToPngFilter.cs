using System;
using System.Collections.Generic;
using ToolBelt;
using System.Linq;
using System.IO;
using Vacuum;

namespace Vacuum.Filters
{
	public class SvgToPngFilter : FilterBase
	{
		#region Construction
		public SvgToPngFilter()
		{
            this.Extensions = new FilterExtension[]
            {
                new FilterExtension(".svg", ".png")
            };
		}
		#endregion

		#region Properties
		[TargetParameter("Width of the bitmap in pixels")]
		public double Width { get; set; }
		
		[TargetParameter("Height of the bitmap in pixels")]
		public double Height { get; set; }
		#endregion
		
		#region IFilter

        public override void Filter()
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

