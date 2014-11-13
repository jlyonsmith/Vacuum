using System;

namespace Vacuum
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public abstract class ParameterAttribute : Attribute
	{
		public ParameterAttribute()
		{
			Description = String.Empty;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ParameterAttribute" />.
		/// </summary>
		/// <param name="description">Description of the content filter</param>
		public ParameterAttribute(string description) : this()
		{
			Description = description;
		}
		
		#region Properties
		/// <summary>
		/// Gets or sets the description of the parameter
		/// </summary>
		public string Description { get; set; }
		
		/// <summary>
		/// Gets or sets whether parameter is required
		/// </summary>
        public bool Required { get { return this.Default == null; } }

        /// <summary>
        /// Gets or sets the default value for the parameter
        /// </summary>
        /// <value>The default value.</value>
        public object Default { get; set; }
		
		#endregion	
	}

    public class FilterParameterAttribute : ParameterAttribute
    {
        public FilterParameterAttribute() : base()
        {
        }

        public FilterParameterAttribute(string description) : base(description)
        {
        }
    }

    public class TargetParameterAttribute : ParameterAttribute
    {
        public TargetParameterAttribute() : base()
        {
        }

        public TargetParameterAttribute(string description) : base(description)
        {
        }
    }
}

