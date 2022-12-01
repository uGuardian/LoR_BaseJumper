using System;
using System.Reflection;
using System.Security.Permissions;

[assembly: System.Reflection.AssemblyCompanyAttribute("uGuardian")]
#if DEBUG
#warning DEBUG
[assembly: System.Reflection.AssemblyConfigurationAttribute("Debug")]
#else
[assembly: System.Reflection.AssemblyConfigurationAttribute("Release")]
#endif
[assembly: System.Reflection.AssemblyFileVersionAttribute(BaseJumperAPI.Globals.Version)]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute(BaseJumperAPI.Globals.Version)]
[assembly: System.Reflection.AssemblyProductAttribute("BaseJumper")]
[assembly: System.Reflection.AssemblyTitleAttribute("BaseJumperCore")]
[assembly: System.Reflection.AssemblyVersionAttribute(BaseJumperAPI.Globals.Version)]
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618