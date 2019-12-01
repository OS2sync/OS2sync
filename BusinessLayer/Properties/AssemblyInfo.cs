using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// make the BusinessLayer, SchedulingLayer and Report Tool friends
[assembly: InternalsVisibleTo(assemblyName: "BusinessLayerTestDriver"),
           InternalsVisibleTo("SchedulingLayer"),
           InternalsVisibleTo("ServiceLayer")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("45f7dedd-5e65-4902-94d6-e8299b2a324c")]
