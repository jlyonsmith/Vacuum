using System;

namespace Vacuum
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class ContentFilterParameterAttribute : Attribute
	{
		#region Instance Constructors
		
		public ContentFilterParameterAttribute()
		{
			Description = String.Empty;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ContentFilterSettingAttribute" />.
		/// </summary>
		/// <param name="description">Description of the content compiler</param>
		public ContentFilterParameterAttribute(string description) : this()
		{
			Description = description;
		}
		
		#endregion
		
		#region Instance Properties
		/// <summary>
		/// Gets or sets the description of the command line tool.
		/// </summary>
		public string Description { get; set; }
		
		/// <summary>
		/// Gets or sets the description of the command line tool.
		/// </summary>
		public bool Optional { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Vacuum.ContentFilterParameterAttribute"/> is for the compiler 
		/// </summary>
		/// <value><c>true</c> if for the compiler; otherwise, <c>false</c>.</value>
		public bool ForFilter { get; set; }
		
		/// <summary>
		/// Private resource reader
		/// </summary>
		public Type ResourceReader { get; set; }
		
		#endregion	
	}
}

