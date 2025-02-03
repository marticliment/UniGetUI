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
        UBUNDLE,
        JSON,
        YAML,
        XML,
    }

    public enum OperationVeredict
    {
        Success,
        Failure,
        Canceled,
        // RestartRequired,
        AutoRetry,
    }

    public enum OperationStatus
    {
        InQueue,
        Running,
        Succeeded,
        Failed,
        Canceled
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
        /// <summary>
        /// Installs a required dependency for a Package Manager
        /// </summary>
        InstallManagerDependency,
        /// <summary>
        /// Searches for packages with a specific query
        /// </summary>
        FindPackages,
        /// <summary>
        /// Lists all the available updates
        /// </summary>
        ListUpdates,
        /// <summary>
        /// Lists the installed packages
        /// </summary>
        ListInstalledPackages,
        /// <summary>
        /// Refreshes the package indexes
        /// </summary>
        RefreshIndexes,
        /// <summary>
        /// Lists the available sources for the manager
        /// </summary>
        ListSources,
        /// <summary>
        /// Loads the package details for a specific package
        /// </summary>
        LoadPackageDetails,
        /// <summary>
        /// Loads the available versions for a specific package
        /// </summary>
        LoadPackageVersions,
        /// <summary>
        /// Other, specific task
        /// </summary>
        OtherTask
    }
}
