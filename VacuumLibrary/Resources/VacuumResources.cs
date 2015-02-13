//
// This file genenerated by the Buckle tool on 2/13/2015 at 8:27 AM. 
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
    public static string FileNotFound(object param0)
    {
        string format = ResourceManager.GetString("FileNotFound", CultureInfo.CurrentUICulture);
        return string.Format(CultureInfo.CurrentCulture, format, param0);
    }
}
}
