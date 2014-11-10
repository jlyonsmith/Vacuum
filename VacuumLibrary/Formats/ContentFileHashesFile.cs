using System;
using System.Collections.Generic;
using TsonLibrary;

namespace Vacuum
{
	public class ContentFileHashesFile : TsonTypedObjectNode
	{
		public TsonStringNode Global { get; set; }
        public TsonArrayNode<TsonStringNode> Targets { get; set; }
	}
}

