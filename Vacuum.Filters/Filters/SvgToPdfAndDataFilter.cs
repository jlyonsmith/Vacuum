using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolBelt;
using System.IO;
using System.Xml;
using MsgPack.Serialization;
using Vacuum;

namespace Vacuum.Filters
{
	public class SvgToPdfAndDataFilter : FilterBase
    {
        public SvgToPdfAndDataFilter()
        {
            this.Extensions = new FilterExtension[]
            {
                new FilterExtension(".svg", ".pdf:.data")
            };
        }

		#region Properties
        [TargetParameter("Number of rows of images. Used for compound images.", Default = (object)1)]
		public int Rows { get; set; }
		
        [TargetParameter("Number of columns of images.  Used for compound images", Default = (object)1)]
		public int Columns { get; set; }
		
		[TargetParameter("Name of the pinboard to use for the rectangle")]
		public string Pinboard { get; set; }
		
		[TargetParameter("Name of the rectangle to use to size the image")]
		public string Rectangle { get; set; }

		#endregion

		#region IFilter

        public override void Filter()
		{
			IEnumerable<ParsedPath> svgPaths = Target.InputPaths.Where(f => f.Extension == ".svg");
			ParsedPath pdfPath = Target.OutputPaths.Where(f => f.Extension == ".pdf").First();
			ParsedPath dataPath = Target.OutputPaths.Where(f => f.Extension == ".data").First();

			ParsedPath nUpSvgPath = null;

			try
			{
				if (svgPaths.Count() > 1)
				{
					nUpSvgPath = pdfPath.WithFileAndExtension(
                        String.Format("{0}_{1}x{2}.svg", pdfPath.File, Rows, Columns));

					CreateNupSvg(svgPaths, nUpSvgPath, Rows, Columns);
				}

				if (!Directory.Exists(pdfPath.VolumeAndDirectory))
				{
					Directory.CreateDirectory(pdfPath.VolumeAndDirectory);
				}
				
				ImageTools.SvgToPdfWithInkscape(nUpSvgPath == null ? svgPaths.First() : nUpSvgPath, pdfPath);
			}
			finally
			{
				if (nUpSvgPath != null)
					File.Delete(nUpSvgPath);
			}

			List<string> pdfInfo = new List<string>();
			
			pdfInfo.Add(this.Pinboard);
			pdfInfo.Add(this.Rectangle);
			pdfInfo.Add(Rows.ToString());
			pdfInfo.Add(Columns.ToString());

            if (!Directory.Exists(dataPath.VolumeAndDirectory))
			{
				Directory.CreateDirectory(dataPath.VolumeAndDirectory);
			}

            // Write the output file
            using (var stream = new FileStream(dataPath, FileMode.Create))
            {
                var serializer = SerializationContext.Default.GetSerializer<List<string>>();

                serializer.Pack(stream, pdfInfo);
            }
		}

		void CreateNupSvg(IEnumerable<ParsedPath> svgPaths, ParsedPath nUpSvgPath, int numRows, int numCols)
		{
			int numSvgPaths = svgPaths.Count();

			if (numRows * numCols != numSvgPaths)
				throw new ContentFileException("Number of SVG's ({0}) does not match number of cells ({1})"
					.CultureFormat(numSvgPaths, numCols * numRows));

			// Load the first SVG element and determine it's width & height
			ParsedPath svgPath = svgPaths.First();
			double width;
			double height;

			GetSvgWidthAndHeight(svgPath, out width, out height);

			double unitWidth = width;
			double unitHeight = height;
			double totalWidth = unitWidth * numCols;
			double totalHeight = unitHeight * numRows;

			using (StreamWriter wr = new StreamWriter(nUpSvgPath))
			{
				wr.WriteLine(@"<svg
    viewBox=""0 0 {0} {1}"" 
    preserveAspectRation=""none""
    version=""1.1""
    xmlns=""http://www.w3.org/2000/svg"">",
 					totalWidth, // 0
					totalHeight // 1
		            );

				IEnumerator<ParsedPath> e = (IEnumerator<ParsedPath>)svgPaths.GetEnumerator();

				e.MoveNext();

				for (int row = 0; row < numRows; row++)
				{
					for (int col = 0; col < numCols; col++)
					{
						svgPath = e.Current;

						GetSvgWidthAndHeight(svgPath, out width, out height);

						wr.WriteLine(@"<g transform=""translate({0},{1}) scale({2},{3})"">",
			            	unitWidth * col, // 0
				            unitHeight * row, // 1
						    width / unitWidth, // 2
					        height / unitHeight // 3
			            );
						wr.WriteLine(ReadAllXmlWithoutHeader(svgPath));
						wr.WriteLine(@"</g>");

						e.MoveNext();
					}
				}

				wr.WriteLine(@"</svg>");
			}
		}

		private string ReadAllXmlWithoutHeader(ParsedPath svgPath)
		{
			using (XmlReader reader = XmlReader.Create(svgPath))
			{
				reader.MoveToContent();
				return reader.ReadOuterXml();
			}
		}

		private void GetSvgWidthAndHeight(ParsedPath svgPath, out double width, out double height)
		{
			using (XmlReader reader = XmlReader.Create(svgPath))
			{
				reader.MoveToContent();
				
				if (reader.NodeType != XmlNodeType.Element || reader.Name != "svg")
					throw new XmlException("Expected svg as first element in file '{0}'".CultureFormat(svgPath));
				
				width = double.Parse(reader.GetAttribute("width"));
				height = double.Parse(reader.GetAttribute("height"));
			}
		}
		#endregion
    }
}
