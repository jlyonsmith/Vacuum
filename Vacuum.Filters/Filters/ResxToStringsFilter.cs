using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolBelt;
using System.Resources;
using System.Collections;
using System.ComponentModel.Design;
using System.Xml;
using Vacuum;
using TsonLibrary;
using System.IO;

namespace Vacuum.Filters
{
    public class ResxToStringsFilter : FilterBase
    {
        public ResxToStringsFilter()
        {
            this.Extensions = new FilterExtension[]
            {
                new FilterExtension(".resx", ".strings")
            };
        }

		#region Classes
		private class ResourceItem
		{
			#region Fields
			private Type dataType;
			private string name;
			private string valueString;

			#endregion

			#region Constructors
			public ResourceItem(string name, string stringResourceValue)
				: this(name, typeof(string))
			{
				this.valueString = stringResourceValue;
			}

			public ResourceItem(string name, Type dataType)
			{
				this.name = name;
				this.dataType = dataType;
			}

			#endregion

			#region Properties
			public Type DataType
			{
				get
				{
					return this.dataType;
				}
			}

			public string Name
			{
				get
				{
					return this.name;
				}
			}

			public string ValueString
			{
				get
				{
					return this.valueString;
				}
			}
			#endregion
		}

		#endregion

        #region IFilter

        public override void Filter()
        {
            var resxPath = Target.InputPaths.Where(f => f.Extension == ".resx").First();
            var stringsPath = Target.OutputPaths.Where(f => f.Extension == ".strings").First();
			var resources = ReadResources(resxPath);
            var node = new TsonObjectNode();

            foreach (ResourceItem resource in resources)
            {
                if (resource.DataType == typeof(string))
                {
                    node.Add(resource.Name, resource.ValueString);
                }
            }

            File.WriteAllText(stringsPath, Tson.Format(node, TsonFormatStyle.Pretty));
        }

		private List<ResourceItem> ReadResources(string resxFileName)
		{
			List<ResourceItem> resources = new List<ResourceItem>();
			XmlDocument document = new XmlDocument();
			
            document.Load(resxFileName);

            Dictionary<string, string> assemblyDict = new Dictionary<string, string>();

            foreach (XmlElement element in document.DocumentElement.SelectNodes("assembly"))
            {
                assemblyDict.Add(element.GetAttribute("alias"), element.GetAttribute("name"));
            }

			foreach (XmlElement element in document.DocumentElement.SelectNodes("data"))
			{
				string attribute = element.GetAttribute("name");
				if ((attribute == null) || (attribute.Length == 0))
				{
                    this.Context.WriteWarning("Resource skipped. Empty name attribute: {0}".CultureFormat(element.OuterXml));
					continue;
				}
				
                Type dataType = null;
				string typeName = element.GetAttribute("type");

                if ((typeName != null) && (typeName.Length != 0))
				{
                    string[] parts = typeName.Split(',');

                    // Replace assembly alias with full name
                    typeName = parts[0] + ", " + assemblyDict[parts[1].Trim()];

					try
					{
						dataType = Type.GetType(typeName, true);
					}
					catch (Exception exception)
					{
						this.Context.WriteWarning("Resource skipped. Could not load type {0}: {1}".CultureFormat(typeName, exception.Message));
						continue;
					}
				}
				
                ResourceItem item = null;
				
                // String resources typically have no type name
                if ((dataType == null) || (dataType == typeof(string)))
				{
					string stringResourceValue = null;
					XmlNode node = element.SelectSingleNode("value");
					if (node != null)
					{
						stringResourceValue = node.InnerXml;
					}
					if (stringResourceValue == null)
					{
						this.Context.WriteWarning("Resource skipped.  Empty value attribute: {0}".CultureFormat(element.OuterXml));
						continue;
					}
					item = new ResourceItem(attribute, stringResourceValue);
				}
                else
                {
                    item = new ResourceItem(attribute, dataType);
                }

				resources.Add(item);
			}

			return resources;
		}

        #endregion
    }
}
