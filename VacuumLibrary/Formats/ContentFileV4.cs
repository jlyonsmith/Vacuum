using System;
using System.IO;
using System.Collections.Generic;
using ToolBelt;
using TsonLibrary;

namespace Vacuum
{
    public class ContentFileV4 : TsonTypedObjectNode
    {
        public class Target : TsonTypedObjectNode
        {
            [TsonNotNull]
            public TsonStringNode Name { get; set; }
            [TsonNotNull]
            public TsonArrayNode<TsonStringNode> Inputs { get; set; }
            [TsonNotNull]
            public TsonArrayNode<TsonStringNode> Outputs { get; set; }
            public TsonStringNode Filter { get; set; }
            public TsonObjectNode Parameters { get; set; }
        }

		public class FilterSetting : TsonTypedObjectNode
		{
            [TsonNotNull]
            public TsonStringNode Name { get; set; }
            [TsonNotNull]
            public TsonArrayNode<ContentFileV4.FilterExtension> Extensions { get; set; }
            public TsonObjectNode Parameters { get; set; }
		}

		public class FilterExtension : TsonTypedObjectNode
		{
            [TsonNotNull]
            public TsonArrayNode<TsonStringNode> Inputs { get; set; }
            [TsonNotNull]
            public TsonArrayNode<TsonStringNode> Outputs { get; set; }
		}

        public class NameAndString : TsonTypedObjectNode
        {
            [TsonNotNull]
            public TsonStringNode Name { get; set; }
            public TsonStringNode Value { get; set; }
        }

        public TsonArrayNode<TsonStringNode> FilterAssemblies { get; set; }
        public TsonArrayNode<ContentFileV4.FilterSetting> FilterSettings { get; set; }
        public TsonArrayNode<ContentFileV4.NameAndString> Properties { get; set; }
        public TsonArrayNode<ContentFileV4.Target> Targets { get; set; }

        public static ContentFileV4 Load(ParsedPath contentPath)
		{
			try
			{
                return Tson.ToObjectNode<ContentFileV4>(File.ReadAllText(contentPath));
			}
			catch (Exception e)
			{
                TsonParseException tpe = e as TsonParseException;
                TsonFormatException tfe = e as TsonFormatException;

				if (tpe != null)
				{
                    throw new ContentFileException("Invalid TSON", tpe);
				}
                else if (tfe != null)
                {
                    throw new ContentFileException("Invalid content file", tfe);
                }
				else
				{
					throw;
				}
			}
		}
	}
}
