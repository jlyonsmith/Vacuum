using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolBelt;

namespace Vacuum
{
    public interface IContentFilter
    {
        IList<FilterExtension> Extensions { get; }
        VacuumContext Context { get; set; }
        VacuumTarget Target { get; set; }
        void Compile();
    }
}
