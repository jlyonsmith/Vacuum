//
// This file genenerated by the Buckle tool on 11/26/2012 at 9:05 PM. 
//
// Contains strongly typed wrappers for resources in VacuumResources.resx
//

namespace Vacuum {
using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Globalization;


/// <summary>
/// Strongly typed resource wrappers generated from VacuumResources.resx.
/// </summary>
public class VacuumResources
{
    internal static readonly ResourceManager ResourceManager = new ResourceManager(typeof(VacuumResources));

    /// <summary>
    /// File '{0}' does not exist
    /// </summary>
    public static ToolBelt.Message FileNotFound(object param0)
    {
        Object[] o = { param0 };
        return new ToolBelt.Message("FileNotFound", typeof(VacuumResources), ResourceManager, o);
    }
}
}
