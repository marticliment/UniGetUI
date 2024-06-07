namespace UniGetUI.PackageEngine.Enums
{
    /// <summary>
    /// Represents the installation scope of a package
    /// </summary>
    public enum PackageScope
    {
        Global = 1,
        Machine = 1,
        Local = 0,
        User = 0,
    }

    public enum DeserializedPackageStatus
    {
        ManagerNotFound,
        ManagerNotEnabled,
        ManagerNotReady,
        SourceNotFound,
        IsAvailable
    }

    public enum BundleFormatType
    {
        JSON,
        YAML,
        XML
    }


    public enum OperationVeredict
    {
        Succeeded,
        Failed,
        AutoRetry,
    }
    public enum OperationStatus
    {
        Pending,
        Running,
        Succeeded,
        Failed,
        Cancelled
    }

    public enum OperationType
    {
        Install,
        Update,
        Uninstall,
        None
    }

    public enum LoggableTaskType
    {
        SearchPackages,
        CheckForUpdates,
        ListInstalledPackages,
        RefreshIndexes,
        ListSources,
        GetPackageDetails,
        GetPackageVersions,
    }
}
