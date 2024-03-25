// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Principal;

namespace DevHome.SetupFlow.Common.Helpers;

#nullable enable

public class Log
{
    private static bool RunningAsAdmin
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }



    // Component names to prepend to log strings
    public static class Component
    {
        public static readonly string Configuration = nameof(Configuration);
        public static readonly string AppManagement = nameof(AppManagement);
        public static readonly string DevDrive = nameof(DevDrive);
        public static readonly string RepoConfig = nameof(RepoConfig);

        public static readonly string Orchestrator = nameof(Orchestrator);
        public static readonly string MainPage = nameof(MainPage);
        public static readonly string Loading = nameof(Loading);
        public static readonly string Review = nameof(Review);
        public static readonly string Summary = nameof(Summary);

        public static readonly string IPCClient = nameof(IPCClient);
        public static readonly string IPCServer = nameof(IPCServer);
        public static readonly string Elevated = nameof(Elevated);

        public static readonly string SetupTarget = nameof(SetupTarget);
        public static readonly string ComputeSystemsListViewModel = nameof(ComputeSystemsListViewModel);
        public static readonly string ComputeSystemCardViewModel = nameof(ComputeSystemCardViewModel);
        public static readonly string ComputeSystemViewModelFactory = nameof(ComputeSystemViewModelFactory);
        public static readonly string ConfigurationTarget = nameof(ConfigurationTarget);
        public static readonly string SDKOpenConfigurationSetResult = nameof(SDKOpenConfigurationSetResult);
    }
}
