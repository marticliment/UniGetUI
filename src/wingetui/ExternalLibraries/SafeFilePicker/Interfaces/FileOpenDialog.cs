using System.Runtime.InteropServices;
using Pickers.Classes;
using Pickers.Guids;

namespace Pickers.Interfaces;

// ---------------------------------------------------------
// Coclass interfaces - designed to "look like" the object 
// in the API, so that the 'new' operator can be used in a 
// straightforward way. Behind the scenes, the C# compiler
// morphs all 'new CoClass()' calls to 'new CoClassWrapper()'
[ComImport,
Guid(IIDGuid.IFileOpenDialog),
CoClass(typeof(FileOpenDialogRCW))]
internal interface FileOpenDialog : IFileOpenDialog
{
}