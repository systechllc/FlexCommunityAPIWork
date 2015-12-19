using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Cat")]
[assembly: AssemblyDescription("SmartSDR CAT translator")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("FlexRadio Systems")]
[assembly: AssemblyProduct("Cat")]
[assembly: AssemblyCopyright("Copyright © 2012-2015 FlexRadio Systems. All rights reserved.")]
[assembly: AssemblyTrademark("FlexRadio Systems")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d97ce640-0430-4da8-8c45-24ec88c6ca96")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.4.24.0")]
[assembly: AssemblyFileVersion("1.4.24.0")]
//[assembly: log4net.Config.XmlConfiguratorAttribute(ConfigFile = @"Cfg\Libs\CatLog4net.xml", Watch = true)]
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("CatTests")]
