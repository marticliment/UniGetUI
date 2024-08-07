﻿using ExternalLibraries.Pickers.Classes;
using ExternalLibraries.Pickers.Guids;
using System.Runtime.InteropServices;

namespace ExternalLibraries.Pickers.Interfaces;

// ---------------------------------------------------------
// Coclass interfaces - designed to "look like" the object 
// in the API, so that the 'new' operator can be used in a 
// straightforward way. Behind the scenes, the C# compiler
// morphs all 'new CoClass()' calls to 'new CoClassWrapper()'
[ComImport,
Guid(IIDGuid.IFileSaveDialog),
CoClass(typeof(FileSaveDialogRCW))]
internal interface FileSaveDialog : IFileSaveDialog
{
}
