// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyCopyright("Copyright ©  2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("086bb804-3b8d-47a3-96d2-b46c2069d2a3")]

[assembly: InternalsVisibleTo("BuildUpToDateChecker.Tests" + StrongNamePublicKeys.CloudBuildPublicKey)]
[assembly: InternalsVisibleTo("ToDynamicProxyGenAssembly2" + StrongNamePublicKeys.MoqPublicKey)]
