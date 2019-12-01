using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// make the BusinessLayer, SchedulingLayer and Report Tool friends
[assembly: InternalsVisibleTo("BusinessLayer"),
           InternalsVisibleTo(assemblyName: "BusinessLayerTestDriver"),
           InternalsVisibleTo("SchedulingLayer"),
           InternalsVisibleTo("ServiceLayer")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("771d4d55-8cd3-4af4-8c28-ea094925d68a")]
