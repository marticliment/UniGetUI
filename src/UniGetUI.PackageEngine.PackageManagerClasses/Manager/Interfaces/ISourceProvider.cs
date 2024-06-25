using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;

namespace UniGetUI.PackageEngine.Classes.Manager.Interfaces
{
    internal interface ISourceProvider
    {
        /// <summary>
        /// Returns the command-line parameters required to add the given source to the manager.
        /// </summary>
        /// <param name="source">The source to add</param>
        /// <returns>An array containing the parameters to pass to the manager executable</returns>
        public abstract string[] GetAddSourceParameters(ManagerSource source);
        
        /// <summary>
        /// Returns the command-line parameters required to remove the given source from the manager.
        /// </summary>
        /// <param name="source">The source to remove</param>
        /// <returns>An array containing the parameters to pass to the manager executable</returns>
        public abstract string[] GetRemoveSourceParameters(ManagerSource source);

        /// <summary>
        /// Checks the result of attempting to add a source
        /// </summary>
        /// <param name="source">The added (or not) source</param>
        /// <param name="ReturnCode">The returncode of the operation</param>
        /// <param name="Output">the command-line output of the operation</param>
        /// <returns>An OperationVeredict value</returns>
        public abstract OperationVeredict GetAddSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output);

        /// <summary>
        /// Checks the result of attempting to remove a source
        /// </summary>
        /// <param name="source">The removed (or not) source</param>
        /// <param name="ReturnCode">The returncode of the operation</param>
        /// <param name="Output">the command-line output of the operation</param>
        /// <returns>An OperationVeredict value</returns>
        public abstract OperationVeredict GetRemoveSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output);

        /// <summary>
        /// Returns the available sources
        /// </summary>
        /// <returns>An array of ManagerSource objects</returns>
        public abstract Task<ManagerSource[]> GetSources();
    }
}
