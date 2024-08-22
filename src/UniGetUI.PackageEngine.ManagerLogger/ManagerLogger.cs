using System.Diagnostics;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.ManagerLogger
{
    public class ManagerLogger
    {
        public enum OperationType
        {
            SearchPackages,
            CheckUpdates,
            ListInstalledPackages,
            RefreshIndexes,
            ListSources,
            GetPackageDetails,
            GetPackageVersions,
        }

        public class OperationLog
        {
            ManagerLogger Logger;
            OperationType Type;

            string Executable;
            string Arguments;
            int ReturnCode;
            DateTime StartTime;
            DateTime? EndTime;
            List<string> StdOut = new();
            List<string> StdErr = new();
            bool isComplete = false;
            bool isOpen = false;

            public OperationLog(ManagerLogger logger, OperationType type, string executable, string arguments)
            {
                Type = type;
                Logger = logger;
                Executable = executable;
                Arguments = arguments;
                StartTime = DateTime.Now;
                isComplete = false;
                isOpen = true;
            }

            public void AddStdoutLine(string? line)
            {
                if (!isOpen) throw new Exception("Attempted to write log into an already-closed OperationLog");
                if (line != null)
                    StdOut.Add(line);
            }

            public void AddStdoutLines(IEnumerable<string> lines)
            {
                if (!isOpen) throw new Exception("Attempted to write log into an already-closed OperationLog");
                foreach (string line in lines)
                    StdOut.Add(line);
            }

            public void AddStderrLine(string? line)
            {
                if (!isOpen) throw new Exception("Attempted to write log into an already-closed OperationLog");
                if (line != null)
                    StdErr.Add(line);
            }

            public void AddStderrLines(IEnumerable<string> lines)
            {
                if (!isOpen) throw new Exception("Attempted to write log into an already-closed OperationLog");
                foreach (string line in lines)
                    StdErr.Add(line);
            }

            public void End()
            {
                EndTime = DateTime.Now;
                isComplete = true;
                isOpen = false;
                Logger.LoadOperation(this);
            }
        }

        PackageManager Manager;
        List<OperationLog> Operations = new();


        public ManagerLogger(PackageManager manager)
        {
            Manager = manager;
        }

        private void LoadOperation(OperationLog operation)
        {
            Operations.Add(operation);
        }

        public OperationLog CreateOperationLog(OperationType type, Process process)
        {
            if (process.StartInfo == null)
                throw new Exception("Process instance did not have a valid StartInfo value");

            return new OperationLog(this, type, process.StartInfo.FileName, process.StartInfo.Arguments);
        }

        public OperationLog CreateNativeOperation(OperationType type, string ExtraArgument = "N/A")
        {
            return new OperationLog(this, type, $"{Manager.Name} native operation", ExtraArgument);
        }
    }
}
