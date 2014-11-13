using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolBelt;

namespace Vacuum
{
    public interface IFilter
    {
        IList<FilterExtension> Extensions { get; set; }
        VacuumContext Context { get; set; }
        VacuumTarget Target { get; set; }
        void Setup();
        void Filter();
        void CleanUp();
    }
}
