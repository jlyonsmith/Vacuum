using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ToolBelt;
using TsonLibrary;
using Vacuum;

namespace Vacuum
{
	internal class AttributedProperty
	{
		public AttributedProperty(ParameterAttribute attribute, PropertyInfo propInfo)
		{
			this.Attribute = attribute;
			this.PropInfo = propInfo;
		}
		
        public ParameterAttribute Attribute;
		public PropertyInfo PropInfo;
	}

	public class FilterClass
    {
		internal FilterClass(TsonStringNode assemblyNode, Assembly assembly, Type type, Type interfaceType)
		{
			this.Assembly = assembly;
			this.Type = type;
			this.Interface = interfaceType;
			this.Instance = Activator.CreateInstance(this.Type);

			this.FilterParameters = new List<AttributedProperty>();
			this.TargetParameters = new List<AttributedProperty>();

			foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
			{
				if (propertyInfo.Name == "Target")
				{
					this.TargetProperty = propertyInfo;
				}
				else if (propertyInfo.Name == "Context")
				{
					this.ContextProperty = propertyInfo;
				}
				else if (propertyInfo.Name == "Extensions")
				{
					this.ExtensionsProperty = propertyInfo;
				}
				else
				{
                    var filterAttrs = propertyInfo.GetCustomAttributes<FilterParameterAttribute>(true);
					
                    if (filterAttrs.Count() > 0)
                    {
    					if (!(propertyInfo.CanRead && propertyInfo.CanWrite))
    						throw new ContentFileException(
    							assemblyNode, "Filter property '{0}' on '{1}' filter must be read/write".CultureFormat(propertyInfo.Name, this.Name));

                        this.FilterParameters.Add(new AttributedProperty(filterAttrs.First(), propertyInfo));
                    }
                    else
                    {
                        var targetAttrs = propertyInfo.GetCustomAttributes<TargetParameterAttribute>(true);

                        if (targetAttrs.Count() > 0)
                        {
                            if (!(propertyInfo.CanRead && propertyInfo.CanWrite))
                                throw new ContentFileException(
                                    assemblyNode, "Target property '{0}' on '{1}' filter must be read/write".CultureFormat(propertyInfo.Name, this.Name));

                            this.TargetParameters.Add(new AttributedProperty(targetAttrs.First(), propertyInfo));
                        }
                    }
				}
			}
		}

        public Assembly Assembly { get; private set; }
        public Type Type { get; private set; }
        public string Name { get { return this.Type.FullName; } }
		public IList<FilterExtension> Extensions { get { return (IList<FilterExtension>)Interface.GetProperty("Extensions").GetValue(this.Instance, null); } }
		internal Type Interface { get; private set; }
		internal Object Instance { get; private set; }
		internal MethodInfo FilterMethod { get { return Interface.GetMethod("Filter"); } }
		internal PropertyInfo ContextProperty { get; private set; }
		internal PropertyInfo TargetProperty { get; private set; }
		internal PropertyInfo ExtensionsProperty { get; private set; }
        internal List<AttributedProperty> FilterParameters { get; set; }
        internal List<AttributedProperty> TargetParameters { get; set; }
	}
}
