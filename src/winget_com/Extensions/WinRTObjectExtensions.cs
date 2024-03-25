// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WinRT;

namespace DevHome.SetupFlow.Common.Extensions;

public static class WinRTObjectExtensions
{
    public static TOutput GetValueOrDefault<TProjectedClass, TOutput>(
        this TProjectedClass projectedClassInstance,
        Func<TProjectedClass, TOutput> getValueFunc,
        TOutput defaultValue)
        where TProjectedClass : IWinRTObject
    {
        // TODO Use API contract version to check if member is available
        // https://github.com/microsoft/devhome/issues/625
        // Modify the signature to take the current and min version
        try
        {
            return getValueFunc(projectedClassInstance);
        }
        catch
        {
            return defaultValue;
        }
    }
}
