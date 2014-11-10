using System;
using ToolBelt;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using System.IO;
using Vacuum;

namespace Vacuum.Filters
{
	public class SvgAndPinboardToPngFilter : IContentFilter
	{
		#region Fields
		private FilterExtension[] extensions = new FilterExtension[]
		{
			new FilterExtension(".svg:.pinboard", ".png")
		};
		#endregion 

		#region Properties
		[ContentFilterParameterAttribute("Rectangle name to use to size the bitmap", Optional = false)]
		public string Rectangle { get; set; }

		[ContentFilterParameterAttribute("Rotation to apply to the bitmap.  Can be None, Left, Right or UpsideDown", Optional = true)]
		public string Rotation { get; set; }
		#endregion

		#region Construction
		public SvgAndPinboardToPngFilter()
		{
			Rotation = "None";
		}
		#endregion

		#region IContentFilter

		public IList<FilterExtension> Extensions { get { return extensions; } }
		public VacuumContext Context { get; set; }
		public VacuumTarget Target { get; set; }

		public void Compile()
		{
			IEnumerable<ParsedPath> svgPaths = Target.InputPaths.Where(f => f.Extension == ".svg");
			ParsedPath pinboardPath = Target.InputPaths.Where(f => f.Extension == ".pinboard").First();
			ParsedPath pngPath = Target.OutputPaths.Where(f => f.Extension == ".png").First();
			PinboardFileV1 pinboardFile = PinboardFileCache.Load(pinboardPath);
			List<ImagePlacement> placements = new List<ImagePlacement>();
			string[] rectangleNames = this.Rectangle.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);

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

