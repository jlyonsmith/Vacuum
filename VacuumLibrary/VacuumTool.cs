using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolBelt;
using System.Xml;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using TsonLibrary;

namespace Vacuum
{
	[CommandLineTitle("Vacuum Content Filter")]
	[CommandLineDescription("A tool for filtering raw game and application content into processed form")]
	[CommandLineCopyright("Copyright (c) 2013, Jamoki LLC")]
	[CommandLineCommandDescription("filter", Description = "Filters content in a .contents file")]
	[CommandLineCommandDescription("clean", Description = "Cleans content in a .contents file")]
	[CommandLineCommandDescription("new", Description = "Creates a new bare bones .contents file")]
	[CommandLineCommandDescription("help", Description = "Displays help for this tool ")]
	public class VacuumTool : ToolBase
	{
		#region Fields
		private VacuumContext context = null;

        #endregion

        #region Construction
		public VacuumTool()
		{
		}

        #endregion

		[CommandCommandLineArgument(Description = "Mode to execute in.  Can be filter, clean, help, new.", Commands = "help,filter,clean,new")]
		public string Command { get; set; }

		[DefaultCommandLineArgument(
			Description = "Input .content data file", ValueHint = "<content-file>",
			Commands = "filter,clean,new")]
		public ParsedFilePath ContentPath { get; set; }

		[CommandLineArgument(
			"properties", ShortName = "p", Description = "Additional properties to set", 
			ValueHint = "<prop1=val1;prop2=val2>",
			Commands = "filter,clean")]
		public string Properties { get; set; }

		[CommandLineArgument(
			"force", ShortName = "f", Description = "Force a filter where every source file is out of date.",
			Commands = "filter")]
		public bool Force { get; set; }

		[CommandLineArgument("debug", ShortName = "d", Description="Show property information while building", Commands="filter")]
		public bool ShowProperties { get; set; }
		
		[CommandLineArgument("help", ShortName = "?", Description="Displays this help", Commands="filter,clean,new")]
		public bool ShowHelp { get; set; }
		
		[CommandLineArgument("nologo", Description = "Suppress display of logo/banner", Commands = "filter,clean,new")]
		public bool NoLogo { get; set; }

		public override void Execute()
		{
			try
			{
				if (!NoLogo)
					WriteMessage(Parser.LogoBanner);
				
				bool hasContentFile = !String.IsNullOrEmpty(ContentPath);

                if (String.IsNullOrEmpty(this.Command) || this.Command == "help")
                {
                    if (!hasContentFile)
                        WriteMessage(Parser.Usage);
                    else
                        WriteMessage(Parser.GetUsage(ContentPath));

                    return;
                }
				
				if (!hasContentFile)
				{
					WriteError("A .content file must be specified");
					return;
				}
				
                this.ContentPath = new ParsedFilePath(this.ContentPath.MakeFullPath());

				if (this.Command == "new")
				{
					CreateContentFileFromTemplate();
					return;
				} 

				if (!File.Exists(this.ContentPath))
				{
					WriteError("Content file '{0}' does not exist", this.ContentPath);
					return;
				}
				
				context = new VacuumContext(this.Properties, this.ContentPath);

				ApplyFilterSettings();

				if (this.Command == "help")
				{
					WriteContentFilterUsage();
					return;
				}

				List<VacuumTarget> vacuumTargets;

				PrepareVacuumTargets(out vacuumTargets);

				if (this.Command == "clean")
				{
					Clean(vacuumTargets);
				}
				else
				{
					Filter(vacuumTargets);
				}

				WriteMessage("Done");
			}
			catch (Exception e)
			{
                TextLocation? location = null;

				do
				{
                    ContentFileException cfe = e as ContentFileException;
                    TsonParseException tpe = e as TsonParseException;
                    TsonFormatException tfe = e as TsonFormatException;

                    if (cfe != null)
                    {
                        location = cfe.ErrorLocation;
                    } 
                    else if (tpe != null)
                    {
                        location = tpe.ErrorLocation;
                    }
                    else if (tfe != null)
                    {
                        location = tfe.ErrorLocation;
                    }

					// If we started showing content file errors, keep going... 
                    if (location.HasValue && location != TextLocation.None)
                    {
                        WriteErrorWithLine(location.Value, e.Message);
                    }
					else
						ConsoleUtility.WriteMessage(MessageType.Error, e.Message);
#if DEBUG
					// Gotta have this in debug builds
					WriteMessage(e.StackTrace);
#endif
				}
				while ((e = e.InnerException) != null);
			}
		}

        #region Private Methods

        private void WriteErrorWithLine(TextLocation location, string message)
        {
            ConsoleUtility.WriteMessage(MessageType.Error, "{0}({1},{2}): {3}", this.ContentPath, location.Line, location.Column, message);
        }

        private void WriteWarningWithLine(TextLocation location, string message)
        {
            ConsoleUtility.WriteMessage(MessageType.Warning, "{0}({1},{2}): {3}", this.ContentPath, location.Line, location.Column, message);
        }

		private void CreateContentFileFromTemplate()
		{
			// Get this type's assembly
			Assembly assem = this.GetType().Assembly;
			
			// Load the resource using a namespace
            using (StreamReader reader = new StreamReader(assem.GetManifestResourceStream("Vacuum.Resources.Template.content")))
			{
				File.WriteAllText(this.ContentPath, reader.ReadToEnd());
			}
		}

		private void WriteContentFilterUsage()
		{
			foreach (var filterClass in context.FilterClasses)
			{
				WriteMessage("\nFilter '{0}':", filterClass.Name);

				WriteMessage("  Extensions:");

				if (filterClass.Extensions.Count == 0)
				{
					WriteMessage ("    None");
					continue;
				}

				foreach (var extension in filterClass.Extensions)
				{
					WriteMessage("    {0} -> {1}", extension.Input, extension.Output);
				}

				if (filterClass.FilterParameters.Count > 0)
				{
					WriteMessage("  Filter Parameters:");
					WriteParameters(filterClass.FilterParameters);
				}

				if (filterClass.TargetParameters.Count > 0)
				{
					WriteMessage("  Target Paramaters:");
					WriteParameters(filterClass.TargetParameters);
				}
			}
		}

        private void WriteParameters(List<AttributedProperty> attrProps)
        {
            foreach (var attrProp in attrProps)
            {
                var sb = new StringBuilder();

                sb.AppendFormat("{0} (", attrProp.Attribute.Description);

                if (!attrProp.Attribute.Required)
                {
                    sb.AppendFormat("optional, default=\"{0}\")", attrProp.Attribute.Default);
                }
                else
                {
                    sb.Append("required)");
                }

                var description = sb.ToString();

                WriteMessage(
                    "    {0,-15}{1,-15}", 
                    attrProp.PropInfo.Name, 
                    attrProp.PropInfo.PropertyType.Name);

                int indent = 4 + 15 + 15;
                string[] lines = description.WordWrap(79 - indent);
                int i = 0;

                WriteMessage(lines[i++]);

                for (; i < lines.Length; i++)
                {
                    WriteMessage(new String(' ', indent) + lines[i]);
                }
            }
        }

		private void WriteProperties(PropertyCollection properties)
		{
			WriteMessage("  Properties:");

			foreach (KeyValuePair<string, string> pair in properties)
			{
				WriteMessage("    {0} = {1}", pair.Key, pair.Value);
			}
		}

		private void PrepareVacuumTargets(out List<VacuumTarget> vacuumTargets)
		{
			vacuumTargets = new List<VacuumTarget>();
			
			foreach (var rawTarget in context.ContentFile.Targets)
			{
				try
				{
					vacuumTargets.Add(new VacuumTarget(rawTarget, context));
				}
				catch (Exception e)
				{
					throw new ContentFileException(rawTarget.Name, "Error preparing to filter targets", e);
				}
			}
			
			vacuumTargets = TopologicallySortVacuumTargets(vacuumTargets);
		}
		
		private List<VacuumTarget> TopologicallySortVacuumTargets(List<VacuumTarget> targets)
		{
			// Create a dictionary of paths -> targets for which they are an input to speed up building the graph
			Dictionary<ParsedPath, List<VacuumTarget>> inputPaths = new Dictionary<ParsedPath, List<VacuumTarget>>();
			
			foreach (var target in targets)
			{
				foreach (var path in target.InputPaths)
				{
					List<VacuumTarget> inputTargets;
					
					if (!inputPaths.TryGetValue(path, out inputTargets))
					{
						inputTargets = new List<VacuumTarget>();
						inputPaths.Add(path, inputTargets);
					}
					
					inputTargets.Add(target);
				}
			}
			
			// Create an adjacency list to represent the graph of from -> to targets
			Dictionary<VacuumTarget, HashSet<VacuumTarget>> graph = new Dictionary<VacuumTarget, HashSet<VacuumTarget>>();
			Dictionary<VacuumTarget, int> inputEdgeCounts = new Dictionary<VacuumTarget, int>();
			
			targets.ForEach(item => graph.Add(item, new HashSet<VacuumTarget>()));
			targets.ForEach(item => inputEdgeCounts[item] = 0);
			
			foreach (var fromTarget in targets)
			{
				foreach (var outputPath in fromTarget.OutputPaths)
				{
					List<VacuumTarget> outputTargets;
					
					if (inputPaths.TryGetValue(outputPath, out outputTargets))
					{
						// The from target has an output path which is an input path to other target(s),
						// so add edges from the from target to each of those other targets
						foreach (var outputTarget in outputTargets)
						{
							HashSet<VacuumTarget> toTargets = graph[fromTarget];
							toTargets.Add(outputTarget);
							inputEdgeCounts[outputTarget]++;
						}
					}
				}
			}
			
			Queue<VacuumTarget> rootTargets = new Queue<VacuumTarget>();
			List<VacuumTarget> orderedTargets = new List<VacuumTarget>();
			
			foreach (var buildTarget in targets)
			{
				if (inputEdgeCounts[buildTarget] == 0)
					rootTargets.Enqueue(buildTarget);
			}
			
			// Do the sort
			while (rootTargets.Count != 0)
			{
				VacuumTarget fromTarget = rootTargets.Dequeue();
				
				orderedTargets.Add(fromTarget);
				
				HashSet<VacuumTarget> toTargets = graph[fromTarget];
				
				graph.Remove(fromTarget);
				
				foreach (var toTarget in toTargets)
				{
					inputEdgeCounts[toTarget]--;
					
					if (inputEdgeCounts[toTarget] == 0)
					{
						rootTargets.Enqueue(toTarget);
					}
				}
			}
			
			if (graph.Count != 0)
			{
				throw new ArgumentException("A circular target dependency exists starting at target '{0}'".CultureFormat(graph.First().Key.Name));
			}
			
			return orderedTargets;
		}

		private void ApplyFilterSettings()
		{
			foreach (var filterClass in context.FilterClasses)
			{
                var filterSettings = context.ContentFile.FilterSettings.FirstOrDefault(s => filterClass.Name.EndsWith(s.Name.Value));
				
				// If there are extensions in the settings try and set the Extensions property
				if (filterSettings == null)
                {
                    ApplyFilterParameters(filterClass, null, context.ContentFile);
                    continue;
                }

                if (filterSettings.Extensions != null)
                {
    				List<FilterExtension> extensions = new List<FilterExtension>();

                    for (int i = 0; i < filterSettings.Extensions.Count; i++)
    				{
                        var extensionsNode = filterSettings.Extensions[i];

    					extensions.Add(new FilterExtension(extensionsNode.Inputs, extensionsNode.Outputs));

                        try
                        {
                            filterClass.ExtensionsProperty.SetValue(filterClass.Instance, extensions, null);
                        }
                        catch (Exception)
                        {
                            throw new ContentFileException(filterSettings.Name, "Invalid extension values specified for '{0}' filter".CultureFormat(filterClass.Name));
                        }
    				}
                }

                ApplyFilterParameters(filterClass, filterSettings.Parameters, filterSettings);
            }
		}
		
        private void ApplyFilterParameters(FilterClass filterClass, TsonObjectNode parameterNode, TsonNode parentNode)
		{
            var required = new HashSet<string>();
			
            // Set gather required parameters and set default values for all optional parameters
            foreach (var attrProp in filterClass.FilterParameters)
            {
                if (attrProp.Attribute.Required)
                    required.Add(attrProp.PropInfo.Name);
                else
                    attrProp.PropInfo.SetValue(filterClass.Instance, attrProp.Attribute.Default);
            }

            if (parameterNode != null)
            {
                foreach (var keyValue in parameterNode)
    			{
    				var keyNode = keyValue.Key;
    				var valueNode = keyValue.Value;
    				var attrProp = filterClass.FilterParameters.Find(p => p.PropInfo.Name == keyNode.Value);
    				
    				if (attrProp == null)
    				{
                        WriteWarningWithLine(parameterNode.Token.Location, "Supplied filter parameter '{0}' is not applicable to the '{1}' filter".CultureFormat(keyNode.Value, filterClass.Name));
    					continue;
    				}

                    if (!attrProp.PropInfo.CanWrite)
                        throw new ContentFileException(parameterNode, "Unable to write to the '{0}' property of '{1}'".CultureFormat(keyNode.Value, filterClass.Name));

                    object obj = GetParameterValueFromTsonNode(attrProp.PropInfo.PropertyType, valueNode);

                    if (obj == null)
    				{
    					throw new ContentFileException(
    						parameterNode, 
                            "Filter parameter '{0}' for filter '{1}' must be bool, int, double or string".CultureFormat(keyNode.Value, filterClass.Name));
    				}

                    attrProp.PropInfo.SetValue(filterClass.Instance, obj, null);

                    required.Remove(attrProp.PropInfo.Name);
                }
            }

            if (required.Count != 0)
                throw new ContentFileException(
                    parameterNode == null ? parentNode : parameterNode, 
                    "Required filter parameter '{0}' of filter '{1}' was not set".CultureFormat(required.First(), filterClass.Name));
		}

        private void ApplyTargetParameters(FilterClass filterClass, TsonObjectNode parameterNode, TsonNode targetNode)
        {
            var required = new HashSet<string>();

            // Set gather required parameters and set default values for all optional parameters
            foreach (var attrProp in filterClass.TargetParameters)
            {
                if (attrProp.Attribute.Required)
                    required.Add(attrProp.PropInfo.Name);
                else
                    attrProp.PropInfo.SetValue(filterClass.Instance, attrProp.Attribute.Default);
            }

            if (parameterNode != null)
            {
                foreach (var keyValue in parameterNode)
                {
                    var keyNode = keyValue.Key;
                    var valueNode = keyValue.Value;
                    var attrProp = filterClass.TargetParameters.Find(p => p.PropInfo.Name == keyNode.Value);

                    if (attrProp == null)
                    {
                        WriteWarningWithLine(parameterNode.Token.Location, "Supplied target parameter '{0}' is not applicable to the '{1}' filter".CultureFormat(keyNode.Value, filterClass.Name));
                        continue;
                    }

                    if (!attrProp.PropInfo.CanWrite)
                        throw new ContentFileException(parameterNode, "Unable to write to the '{0}' property of '{1}'".CultureFormat(keyNode.Value, filterClass.Name));

                    object obj = GetParameterValueFromTsonNode(attrProp.PropInfo.PropertyType, valueNode);

                    if (obj == null)
                    {
                        throw new ContentFileException(
                            parameterNode, 
                            "Target parameter '{0}' for filter '{1}' must be bool, int, double or string".CultureFormat(keyNode.Value, filterClass.Name));
                    }

                    attrProp.PropInfo.SetValue(filterClass.Instance, obj, null);

                    required.Remove(attrProp.PropInfo.Name);
                }
            }

            if (required.Count != 0)
                throw new ContentFileException(
                    parameterNode == null ? targetNode : parameterNode, 
                    "Required target parameter '{0}' of filter '{1}' was not set".CultureFormat(required.First(), filterClass.Name));
        }

        private object GetParameterValueFromTsonNode(Type propertyType, TsonNode valueNode)
        {
            if (propertyType == typeof(double))
            {
                var numberNode = valueNode as TsonNumberNode;

                if (numberNode == null)
                    throw new ContentFileException(valueNode, "Value is not a number");

                return numberNode.Value;
            }
            else if (propertyType == typeof(int))
            {
                var numberNode = valueNode as TsonNumberNode;

                if (numberNode == null)
                    throw new ContentFileException(valueNode, "Value is not a number");

                return (object)(int)numberNode.Value;
            }
            else if (propertyType == typeof(string))
            {
                var numberNode = valueNode as TsonStringNode;

                if (numberNode == null)
                    throw new ContentFileException(valueNode, "Value is not a string");

                return numberNode.Value;
            }
            else if (propertyType == typeof(bool))
            {
                var boolNode = valueNode as TsonBooleanNode;

                if (boolNode == null)
                    throw new ContentFileException(valueNode, "Value is not boolean");

                return boolNode.Value;
            }
            else
                return null;
        }

		private void Clean(List<VacuumTarget> vacuumTargets)
		{
			foreach (var buildTarget in vacuumTargets)
			{
				WriteMessage("Cleaning target '{0}'", buildTarget.Name);

				foreach (var outputPath in buildTarget.OutputPaths)
				{
					if (File.Exists(outputPath))
					{
						File.Delete(outputPath);
						WriteMessage("\tDeleted '{0}'", outputPath);
					}
				}
			}

			string hashPath = context.ContentFileHashesPath;

			if (File.Exists(hashPath))
			{
				File.Delete(hashPath);
				WriteMessage("Deleted content hash file '{0}'", hashPath);
			}
		}

		private void Filter(List<VacuumTarget> vacuumTargets)
		{
			string oldGlobalHash;
			HashSet<string> oldTargetHashes;

			ReadOldContentFileHashes(out oldGlobalHash, out oldTargetHashes);

			if (ShowProperties)
			{
				WriteProperties(context.Properties);
			}

			foreach (var buildTarget in vacuumTargets)
			{
				foreach (var inputPath in buildTarget.InputPaths)
				{
					if (!File.Exists(inputPath))
					{
						throw new ContentFileException("Required input file '{0}' does not exist".CultureFormat(inputPath));
					}
				}

				if (!IsFilteringRequired(buildTarget, oldGlobalHash, oldTargetHashes))
					continue;

				FilterClass filterClass = buildTarget.FilterClass;
				string msg = String.Format("Vacuuming target '{0}' with '{1}' filter", buildTarget.Name, filterClass.Name);

				foreach (var input in buildTarget.InputPaths)
				{
					msg += Environment.NewLine + "  " + input;
				}
				msg += Environment.NewLine + "  ->";
				foreach (var output in buildTarget.OutputPaths)
				{
					msg += Environment.NewLine + "  " + output;
				}
				WriteMessage(msg);

				if (ShowProperties)
				{
					WriteProperties(buildTarget.Properties);
				}

				// Set the Context and Target properties on the Filter class instance
				filterClass.ContextProperty.SetValue(filterClass.Instance, context, null);
				filterClass.TargetProperty.SetValue(filterClass.Instance, buildTarget, null);

				// Set target parameters
                ApplyTargetParameters(filterClass, buildTarget.TargetNode.Parameters, buildTarget.TargetNode);

				try
				{
					filterClass.FilterMethod.Invoke(filterClass.Instance, null);
				}
				catch (TargetInvocationException e)
				{
					throw new ContentFileException(
						buildTarget.TargetNode.Name, "Unable to filter target '{0}'".CultureFormat(buildTarget.Name), e.InnerException);
				}

				// Ensure that the output files were generated
				foreach (var outputFile in buildTarget.OutputPaths)
				{
					if (!File.Exists(outputFile))
					{
						throw new ContentFileException(
                            buildTarget.TargetNode.Name, "Output file '{0}' was not generated".CultureFormat(outputFile));
					}
				}
			}

			WriteNewContentFileHashes(vacuumTargets);
		}

		private void ReadOldContentFileHashes(out string oldGlobalHash, out HashSet<string> oldTargetHashes)
		{
			oldGlobalHash = String.Empty;
			oldTargetHashes = new HashSet<string>();

			if (File.Exists(context.ContentFileHashesPath))
			{
				try
				{
                    var hashes = Tson.ToObjectNode<ContentFileHashesFile>(
                        File.ReadAllText(context.ContentFileHashesPath));
					
					oldGlobalHash = hashes.Global.Value;

					foreach (var hash in hashes.Targets)
					{
						oldTargetHashes.Add(hash.Value);
					}
				}
				catch
				{
					// Bad file, don't use it again
					File.Delete(context.ContentFileHashesPath);
				}
			}
		}

		private void WriteNewContentFileHashes(List<VacuumTarget> vacuumTargets)
		{
            ContentFileHashesFile hashesFile = new ContentFileHashesFile()
			{
                Global = new TsonStringNode(context.GlobalHash),
                Targets = new TsonArrayNode<TsonStringNode>(vacuumTargets.Select(t => new TsonStringNode(t.Hash)))
			};

			try
			{
                var tson = Tson.Format(hashesFile, TsonFormatStyle.Compact);
				
                File.WriteAllText(context.ContentFileHashesPath, tson);
			}
			catch
			{
				WriteWarning("Unable to write content hash file '{0}'".CultureFormat(context.ContentFileHashesPath));
			}
		}

		private bool IsFilteringRequired(VacuumTarget buildTarget, string oldGlobalHash, HashSet<string> oldTargetHashes)
		{
			if (this.Force)
				return true;

			DateTime lastWriteTime;
			DateTime newestInputFile = context.NewestAssemblyWriteTime;

			foreach (var inputPath in buildTarget.InputPaths)
			{
				lastWriteTime = File.GetLastWriteTime(inputPath);

				if (lastWriteTime > newestInputFile)
					newestInputFile = lastWriteTime;
			}

			DateTime oldestOutputFile = DateTime.MaxValue;

			foreach (var outputPath in buildTarget.OutputPaths)
			{
				lastWriteTime = File.GetLastWriteTime(outputPath);

				if (lastWriteTime < oldestOutputFile)
					oldestOutputFile = lastWriteTime;
			}

			// And last but not least, if the content file is newer than all inputs so far and this targets hash 
			// is not present in the hash file then the definition changed or was added,
			// OR if the global hash has changed then consider the content file write time.

			if (context.ContentFileWriteTime > newestInputFile && 
				(oldGlobalHash != context.GlobalHash || !oldTargetHashes.Contains(buildTarget.Hash)))
			{
				newestInputFile = context.ContentFileWriteTime;
			}

			return newestInputFile > oldestOutputFile;
		}

        #endregion
	}
}
