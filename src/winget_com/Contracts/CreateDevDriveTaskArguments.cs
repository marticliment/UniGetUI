// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.SetupFlow.Common.Contracts;

/// <summary>
/// Class representing a dev drive's task arguments passed to the elevated process.
/// </summary>
/// <remarks>
/// <code>ElevatedProcess.exe --devdrive-path path --devdrive-size size --devdrive-letter letter --devdrive-label label</code>
/// </remarks>
public class CreateDevDriveTaskArguments : ITaskArguments
{
    private const string DevDrivePath = "--devdrive-path";
    private const string DevDriveSize = "--devdrive-size";
    private const string DevDriveLetter = "--devdrive-letter";
    private const string DevDriveLabel = "--devdrive-label";

    /// <summary>
    /// Gets or sets the drive's virtual disk path
    /// </summary>
    public string VirtDiskPath
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the drive's size in bytes
    /// </summary>
    public ulong SizeInBytes
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the drive's letter
    /// </summary>
    public char NewDriveLetter
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the drive's label
    /// </summary>
    public string DriveLabel
    {
        get; set;
    }

    /// <summary>
    /// Try to read and parse argument list into an object.
    /// </summary>
    /// <param name="argumentList">Argument list</param>
    /// <param name="index">Index to start reading arguments from</param>
    /// <param name="result">Output object</param>
    /// <returns>True if reading arguments succeeded. False otherwise.</returns>
    public static bool TryReadArguments(IList<string> argumentList, ref int index, out CreateDevDriveTaskArguments result)
    {
        result = null;

        // --devdrive-path <path>      --devdrive-size <size>      --devdrive-letter <letter>    --devdrive-label <label>
        // [    index    ] [index + 1] [  index + 2  ] [index + 3] [   index + 4   ] [index + 5] [   index + 6  ] [index + 7]
        const int TaskArgListCount = 8;
        if (index + TaskArgListCount <= argumentList.Count &&
            argumentList[index] == DevDrivePath &&
            argumentList[index + 2] == DevDriveSize &&
            argumentList[index + 4] == DevDriveLetter &&
            argumentList[index + 6] == DevDriveLabel)
        {
            var virtDiskPath = argumentList[index + 1];
            var sizeInBytesStr = argumentList[index + 3];
            var newDriveLetterStr = argumentList[index + 5];
            var driveLabel = argumentList[index + 7];

            if (!ulong.TryParse(sizeInBytesStr, out var sizeInBytes) ||
                !char.TryParse(newDriveLetterStr, out var letter))
            {
                return false;
            }

            result = new CreateDevDriveTaskArguments
            {
                VirtDiskPath = virtDiskPath,
                SizeInBytes = sizeInBytes,
                NewDriveLetter = letter,
                DriveLabel = driveLabel,
            };
            index += TaskArgListCount;
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public IList<string> ToArgumentList()
    {
        return new List<string>()
        {
            DevDrivePath, VirtDiskPath,            // --devdrive-path <path>
            DevDriveSize, $"{SizeInBytes}",        // --devdrive-size <size>
            DevDriveLetter, $"{NewDriveLetter}",   // --devdrive-letter <letter>
            DevDriveLabel, DriveLabel,             // --devdrive-label <label>
        };
    }
}
