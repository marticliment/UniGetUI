// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Management.Configuration;

namespace DevHome.SetupFlow.Common.Exceptions;

public class OpenConfigurationSetException : WinGetConfigurationException
{
    /// <summary>
    /// Gets the <see cref="OpenConfigurationSetResult.ResultCode"/>
    /// </summary>
    public Exception ResultCode
    {
        get;
    }

    /// <summary>
    /// Gets the field that is missing/invalid, if appropriate for the specific ResultCode.
    /// </summary>
    public string Field
    {
        get;
    }

    /// <summary>
    /// Gets the value of the field, if appropriate for the specific ResultCode.
    /// </summary>
    public string Value
    {
        get;
    }

    public OpenConfigurationSetException(Exception resultCode, string field, string value)
    {
        ResultCode = resultCode;
        Field = field;
        Value = value;
    }
}
