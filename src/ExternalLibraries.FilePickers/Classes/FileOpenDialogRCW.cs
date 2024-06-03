using ExternalLibraries.Pickers.Guids;
using System.Runtime.InteropServices;

namespace ExternalLibraries.Pickers.Classes;

// ---------------------------------------------------
// .NET classes representing runtime callable wrappers
[ComImport,
ClassInterface(ClassInterfaceType.None),
TypeLibType(TypeLibTypeFlags.FCanCreate),
Guid(CLSIDGuid.FileOpenDialog)]
internal class FileOpenDialogRCW
{
}