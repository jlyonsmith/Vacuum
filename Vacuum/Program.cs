using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ToolBelt;
using Vacuum;
using System.Xml;

namespace Vacuum
{
    class Program
    {
        public static int Main(string[] args)
        {
            VacuumTool tool = new VacuumTool();

            try
            {
		        ((IProcessCommandLine)tool).ProcessCommandLine(args);

				tool.Execute();

                return tool.HasOutputErrors ? 1 : 0;
            }
            catch (Exception e)
            {
                ConsoleUtility.WriteMessage(MessageType.Error, e.ToString());
                return 1;
            }

        }
    }
}
