using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Drawing;
using ToolBelt;
using MsgPack.Serialization;
using Vacuum;

namespace Vacuum.Filters
{
    public class PinboardToDataFilter : FilterBase
    {
        public PinboardToDataFilter()
        {
            this.Extensions = new FilterExtension[]
            {
                new FilterExtension(".pinboard", ".data")
            };
        }

		#region IFilter

        public override void Filter()
		{
			if (Target.InputPaths.Count != 1)
				throw new ContentFileException(Target.TargetNode.Name, "One input file expected");
			
			if (Target.OutputPaths.Count != 1)
				throw new ContentFileException(Target.TargetNode.Name, "One output file expected");
			
			ParsedPath pinboardPath = Target.InputPaths[0];
            ParsedPath dataPath = Target.OutputPaths[0];
			PinboardFileV1 pinboard = PinboardFileCache.Load(pinboardPath);
			Rectangle[] rectangles = new Rectangle[pinboard.RectInfos.Count + 1];

			rectangles[0] = new Rectangle(pinboard.ScreenRectInfo.X, pinboard.ScreenRectInfo.Y, pinboard.ScreenRectInfo.Width, pinboard.ScreenRectInfo.Height);

			for (int i = 0; i < pinboard.RectInfos.Count; i++)
			{
				rectangles[i + 1] = new Rectangle(pinboard.RectInfos[i].X, pinboard.RectInfos[i].Y, pinboard.RectInfos[i].Width, pinboard.RectInfos[i].Height);
			}

			if (!Directory.Exists(dataPath.VolumeAndDirectory))
			{
				Directory.CreateDirectory(dataPath.VolumeAndDirectory);
			}

            using (FileStream stream = new FileStream(dataPath, FileMode.Create))
            {
                var serializer = SerializationContext.Default.GetSerializer<Rectangle[]>();

                serializer.Pack(stream, rectangles);
            }
        }

        #endregion
    }
}
