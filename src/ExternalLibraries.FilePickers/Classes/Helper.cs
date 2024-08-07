﻿using ExternalLibraries.Pickers.Enums;
using ExternalLibraries.Pickers.Interfaces;
using ExternalLibraries.Pickers.Structures;
using System.Runtime.InteropServices;

namespace ExternalLibraries.Pickers.Classes;

internal static class Helper
{
    /// <summary>
    /// Shows FileOpenDialog.
    /// </summary>
    /// <param name="windowHandle">Window handle where dialog should appear.</param>
    /// <param name="fos">File open dialog options.</param>
    /// <param name="typeFilters">List of extensions applied on dialog.</param>
    /// <returns>Path to selected file, folder or empty string.</returns>
    internal static string ShowOpen(nint windowHandle, FOS fos, List<string>? typeFilters = null)
    {
        FileOpenDialog dialog = new();
        try
        {
            dialog.SetOptions(fos);

            if (typeFilters != null)
            {
                typeFilters.Insert(0, string.Join("; ", typeFilters));
                COMDLG_FILTERSPEC[] filterSpecs = typeFilters.Select(f => new COMDLG_FILTERSPEC(f)).ToArray();

                dialog.SetFileTypes((uint)filterSpecs.Length, filterSpecs);
            }

            if (dialog.Show(windowHandle) != 0)
            {
                return string.Empty;
            }

            dialog.GetResult(out IShellItem item);
            item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out string path);
            return path;
        }
        finally
        {
#pragma warning disable CA1416 
            Marshal.ReleaseComObject(dialog);
#pragma warning restore CA1416
        }
    }

    internal static string ShowSave(nint windowHandle, FOS fos, List<string>? typeFilters = null, string name = "")
    {
        FileSaveDialog dialog = new();
        try
        {
            dialog.SetOptions(fos);

            if (typeFilters != null)
            {
                COMDLG_FILTERSPEC[] filterSpecs = typeFilters.Select(f => new COMDLG_FILTERSPEC(f)).ToArray();

                dialog.SetFileTypes((uint)filterSpecs.Length, filterSpecs);
            }

            if (!string.IsNullOrEmpty(name))
            {
                dialog.SetFileName(name);
            }

            if (dialog.Show(windowHandle) != 0)
            {
                return string.Empty;
            }

            dialog.GetResult(out IShellItem item);
            item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out string path);
            return path;
        }
        finally
        {
#pragma warning disable CA1416 
            Marshal.ReleaseComObject(dialog);
#pragma warning restore CA1416
        }
    }
}
