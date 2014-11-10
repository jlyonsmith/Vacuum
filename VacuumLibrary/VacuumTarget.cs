using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using ToolBelt;
using System.IO;
using TsonLibrary;

namespace Vacuum
{
    public sealed class VacuumTarget
    {
		public FilterClass FilterClass { get; private set; }
		public string Name { get; private set; }
		public IList<ParsedPath> InputPaths { get; private set; }
		public IList<ParsedPath> OutputPaths { get; private set; }
		public FilterExtension Extension { get; private set; }
		public string Hash { get; private set; }
		public PropertyCollection Properties { get; private set; }
		public ContentFileV4.Target TargetNode{ get; set; }

		public VacuumTarget(
			ContentFileV4.Target rawTarget, 
			VacuumContext vacuumContext)
		{
            this.TargetNode = rawTarget;
            this.Name = TargetNode.Name.Value;

			if (TargetNode.Inputs.Count == 0)
				throw new ContentFileException(TargetNode.Name, "Target must have at least one input");

			this.Properties = new PropertyCollection();
			this.Properties.Set("TargetName", this.Name);

			List<ParsedPath> inputPaths = new List<ParsedPath>();

			foreach (var rawInputFile in TargetNode.Inputs)
			{
				ParsedPath pathSpec = null; 
				string s;
				
				try
				{
					s = vacuumContext.Properties.ExpandVariables(rawInputFile.Value, false);
					s = this.Properties.ExpandVariables(s);
				}
				catch (Exception e)
				{
					throw new ContentFileException(rawInputFile, e);
				}

				try
				{
					pathSpec = new ParsedPath(s, PathType.File).MakeFullPath();
				}
				catch (Exception e)
				{
					throw new ContentFileException("Bad path '{0}'".CultureFormat(s), e);
				}
				
				if (pathSpec.HasWildcards)
				{
					if (!Directory.Exists(pathSpec.VolumeAndDirectory))
					{
						throw new ContentFileException("Directory '{0}' does not exist".CultureFormat(pathSpec.VolumeAndDirectory));
					}
					
					IList<ParsedPath> files = DirectoryUtility.GetFiles(pathSpec, SearchScope.DirectoryOnly);
					
					if (files.Count == 0)
					{
						throw new ContentFileException("Wildcard input refers to no files after expansion");
					}
					
					inputPaths = new List<ParsedPath>(inputPaths.Concat(files));
				}
				else
				{
					inputPaths.Add(pathSpec);
				}
			}

			inputPaths.Sort();
			this.InputPaths = inputPaths;
			
			List<ParsedPath> outputPaths = new List<ParsedPath>();
			
			if (TargetNode.Outputs.Count == 0)
				throw new ContentFileException(TargetNode.Name, "Target must have at least one output");

			foreach (var rawOutputFile in TargetNode.Outputs)
			{
				string s;

				try
				{
					s = vacuumContext.Properties.ExpandVariables(rawOutputFile.Value, false);
					s = this.Properties.ExpandVariables(s);
				}
				catch (Exception e)
				{
					throw new ContentFileException(rawOutputFile, e);
				}
				
				try
				{
					ParsedPath outputFile = new ParsedPath(s, PathType.File).MakeFullPath();
					
					outputPaths.Add(outputFile);
				}
				catch (Exception e)
				{
					throw new ContentFileException("Bad path '{0}'".CultureFormat(s), e);
				}
			}

			outputPaths.Sort();
			this.OutputPaths = outputPaths;

			this.Extension = new FilterExtension(this.InputPaths.AsEnumerable(), this.OutputPaths.AsEnumerable());

			if (TargetNode.Filter == null || TargetNode.Filter.Value.Length == 0)
			{
				IEnumerator<FilterClass> e = vacuumContext.FilterClasses.GetEnumerator();
				
				while (e.MoveNext())
				{
					foreach (FilterExtension extension in e.Current.Extensions)
					{
						if (extension.Equals(this.Extension))
						{
							this.FilterClass = e.Current;
							break;
						}
					}

					if (this.FilterClass != null)
						break;
				}
				
				if (this.FilterClass == null)
				{
					throw new ArgumentException(
						"No compiler found for target '{0}' handling extensions '{1}'".CultureFormat(this.Name, this.Extension.ToString()));
				}
			}
			else
			{
				// Search for the compiler based on the supplied name and validate it handles the extensions
				foreach (var filterClass in vacuumContext.FilterClasses)
				{
					if (filterClass.Name.EndsWith(TargetNode.Filter.Value, StringComparison.OrdinalIgnoreCase))
					{
						this.FilterClass = filterClass;
						break;
					}
				}

				if (this.FilterClass == null)
					throw new ArgumentException("Supplied compiler '{0}' was not found".CultureFormat(TargetNode.Filter));
			}

			SHA1 sha1 = SHA1.Create();
			StringBuilder sb = new StringBuilder();

			sb.Append(TargetNode.Inputs);
			sb.Append(TargetNode.Outputs);
			
			if (TargetNode.Filter != null)
				sb.Append(TargetNode.Filter);

			if (TargetNode.Parameters != null)
			{
                foreach (var keyValue in TargetNode.Parameters)
				{
					sb.Append(keyValue.Key.Value);
                    sb.Append(keyValue.Value.ToString());
				}
			}

			sha1.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
			Hash = BitConverter.ToString(sha1.Hash).Replace("-", "");
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
    }
}
