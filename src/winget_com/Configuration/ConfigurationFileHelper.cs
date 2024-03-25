// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Exceptions;
using DevHome.SetupFlow.Common.Helpers;
using Microsoft.Management.Configuration;
using Windows.Storage;
using Windows.Storage.Streams;

namespace DevHome.SetupFlow.Common.Configuration;

/// <summary>
/// Helper for applying a configuration file. This exists so that we can
/// use it in an elevated or non-elevated context.
/// </summary>
public class ConfigurationFileHelper
{
    public class ApplicationResult
    {
        public ApplyConfigurationSetResult Result
        {
            get;
        }

        public bool Succeeded => Result.ResultCode == null;

        public bool RequiresReboot => Result.UnitResults.Any(result => result.RebootRequired);

        public Exception ResultException => Result.ResultCode;

        public ApplicationResult(ApplyConfigurationSetResult result)
        {
            Result = result;
        }
    }

    private const string PowerShellHandlerIdentifier = "pwsh";
    private readonly Guid _activityId;
    private ConfigurationProcessor _processor;
    private ConfigurationSet _configSet;

    public ConfigurationFileHelper(Guid activityId)
    {
        _activityId = activityId;
    }

    /// <summary>
    /// Open configuration set from the provided <paramref name="content"/>.
    /// </summary>
    /// <param name="filePath">DSC configuration file path</param>
    /// <param name="content">DSC configuration file content</param>
    public async Task OpenConfigurationSetAsync(string filePath, string content)
    {
        try
        {
            _processor = await CreateConfigurationProcessorAsync();
            _configSet = await OpenConfigurationSetInternalAsync(_processor, filePath, content);
        }
        catch
        {
            _processor = null;
            _configSet = null;
            throw;
        }
    }

    private async Task<ConfigurationSet> OpenConfigurationSetInternalAsync(ConfigurationProcessor processor, string filePath, string content)
    {
        var file = await StorageFile.GetFileFromPathAsync(filePath);
        var parentDir = await file.GetParentAsync();
        var inputStream = StringToStream(content);

        var openConfigResult = processor.OpenConfigurationSet(inputStream);
        var configSet = openConfigResult.Set ?? throw new OpenConfigurationSetException(openConfigResult.ResultCode, openConfigResult.Field, openConfigResult.Value);

        // Set input file path in the configuration set to inform the
        // processor about the working directory when applying the
        // configuration
        configSet.Name = file.Name;
        configSet.Origin = parentDir.Path;
        configSet.Path = file.Path;
        return configSet;
    }

    public async Task<ApplicationResult> ApplyConfigurationAsync()
    {
        if (_processor == null || _configSet == null)
        {
            throw new InvalidOperationException();
        }

        var result = await _processor.ApplySetAsync(_configSet, ApplyConfigurationSetFlags.None);
        return new ApplicationResult(result);
    }

    /// <summary>
    /// Create and configure the configuration processor.
    /// </summary>
    /// <returns>Configuration processor</returns>
    private async Task<ConfigurationProcessor> CreateConfigurationProcessorAsync()
    {
        ConfigurationStaticFunctions config = new();
        var factory = await config.CreateConfigurationSetProcessorFactoryAsync(PowerShellHandlerIdentifier).AsTask();

        // Create and configure the configuration processor.
        var processor = config.CreateConfigurationProcessor(factory);
        processor.Caller = nameof(DevHome);
        processor.Diagnostics += LogConfigurationDiagnostics;
        processor.MinimumLevel = DiagnosticLevel.Verbose;
        return processor;
    }

    /// <summary>
    /// Log configuration diagnostics event handler
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="diagnosticInformation">Diagnostic information</param>
    private void LogConfigurationDiagnostics(object sender, IDiagnosticInformation diagnosticInformation)
    {
    }

    /// <summary>
    /// Convert a string to an input stream
    /// </summary>
    /// <param name="str">Target string</param>
    /// <returns>Input stream</returns>
    private InMemoryRandomAccessStream StringToStream(string str)
    {
        InMemoryRandomAccessStream result = new();
        using (DataWriter writer = new(result))
        {
            writer.UnicodeEncoding = UnicodeEncoding.Utf8;
            writer.WriteString(str);
            writer.StoreAsync().AsTask().Wait();
            writer.DetachStream();
        }

        result.Seek(0);
        return result;
    }
}
