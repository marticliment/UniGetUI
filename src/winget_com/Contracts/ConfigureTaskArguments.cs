// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.SetupFlow.Common.Contracts;

/// <summary>
/// Class representing a configuration task's arguments passed to the elevated process.
/// </summary>
/// <remarks>
/// <code>ElevatedProcess.exe --config-file file --config-content content</code>
/// </remarks>
public class ConfigureTaskArguments : ITaskArguments
{
    private const string ConfigFile = "--config-file";
    private const string ConfigContent = "--config-content";

    /// <summary>
    /// Gets or sets the configuration file path
    /// </summary>
    public string FilePath
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the configuration file content
    /// </summary>
    public string Content
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
    public static bool TryReadArguments(IList<string> argumentList, ref int index, out ConfigureTaskArguments result)
    {
        result = null;

        // --config-file <file>      --config-content <content>
        // [   index   ] [index + 1] [   index + 2  ] [index + 3]
        const int TaskArgListCount = 4;
        if (index + TaskArgListCount <= argumentList.Count &&
            argumentList[index] == ConfigFile &&
            argumentList[index + 2] == ConfigContent)
        {
            result = new ConfigureTaskArguments
            {
                FilePath = argumentList[index + 1],
                Content = argumentList[index + 3],
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
            ConfigFile, FilePath,      // --config-file <file>
            ConfigContent, Content,    // --config-content <content>
        };
    }
}
