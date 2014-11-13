using System;
using ToolBelt;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using System.IO;
using Vacuum;

namespace Vacuum.Filters
{
	public class SvgAndPinboardToPngFilter : FilterBase
	{
        public SvgAndPinboardToPngFilter()
        {
            this.Extensions = new FilterExtension[]
            {
                new FilterExtension(".svg:.pinboard", ".png")
            };
        }

		#region Properties
		[TargetParameter("List of rectangle names to use to size the SVG files")]
		public string Rectangles { get; set; }

        [TargetParameter("Rotation to apply to the bitmap.  Can be None, Left, Right or UpsideDown", Default = "None")]
		public string Rotation { get; set; }
		#endregion

		#region IFilter

        public override void Filter()
		{
			IEnumerable<ParsedPath> svgPaths = Target.InputPaths.Where(f => f.Extension == ".svg");
			ParsedPath pinboardPath = Target.InputPaths.Where(f => f.Extension == ".pinboard").First();
			ParsedPath pngPath = Target.OutputPaths.Where(f => f.Extension == ".png").First();
			PinboardFileV1 pinboardFile = PinboardFileCache.Load(pinboardPath);
			List<ImagePlacement> placements = new List<ImagePlacement>();
            string[] rectangleNames = this.Rectangles.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);

			if (svgPaths.Count() != rectangleNames.Length)
				throw new ContentFileException("Number of .svg files ({0}) does match number of RectangleNames ({1})"
					.CultureFormat(svgPaths.Count(), rectangleNames.Length));

			ImageRotation rotation;

			if (!Enum.TryParse(this.Rotation, out rotation))
				throw new ContentFileException("Invalid value '{0}' for given for rotation.  Valid are None, Left, Right, UpsideDown".CultureFormat(this.Rotation));

			int i = 0;

			try
			{
				if (!Directory.Exists(pngPath.VolumeAndDirectory))
				{
					Directory.CreateDirectory(pngPath.VolumeAndDirectory);
				}

				foreach (var svgPath in svgPaths)
				{
					PinboardFileV1.RectangleInfo rectInfo = pinboardFile.GetRectangleInfoByName(rectangleNames[i]);
					ParsedPath tempPngPath = pngPath.WithFileAndExtension(String.Format("{0}_{1}.png", pngPath.File, i));
					
					if (rectInfo == null)
					{
						throw new ContentFileException("Rectangle '{0}' not found in pinboard file '{1}'"
	                    	.CultureFormat(rectangleNames[i], pinboardFile)); 
					}

					ImageTools.SvgToPngWithInkscape(svgPath, tempPngPath, rectInfo.Width, rectInfo.Height);

					placements.Add(new ImagePlacement(
						tempPngPath, new Cairo.Rectangle(rectInfo.X, rectInfo.Y, rectInfo.Width, rectInfo.Height)));

					i++;
				}

				ImageTools.CombinePngs(placements, pngPath);
				ImageTools.RotatePng(pngPath, rotation);
			}
			finally
			{
				foreach (var placement in placements)
				{
					if (File.Exists(placement.ImageFile))
						File.Delete(placement.ImageFile);
				}
			}
		}

		#endregion
	}
}

