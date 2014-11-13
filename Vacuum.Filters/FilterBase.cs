using System;
using System.Collections.Generic;

namespace Vacuum.Filters
{
    public abstract class FilterBase : IFilter
    {
        public FilterBase()
        {
            this.Extensions = new FilterExtension[0];
        }

        public virtual IList<FilterExtension> Extensions { get; set; }
        public virtual VacuumContext Context { get; set; }
        public virtual VacuumTarget Target { get; set; }

        public virtual void Setup()
        {
        }

        public virtual void CleanUp()
        {
        }

        public abstract void Filter();
    }
}

