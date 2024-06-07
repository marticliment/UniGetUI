using System.Diagnostics;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Enums;

namespace UniGetUI.PackageEngine.ManagerClasses.Classes
{
    public class ManagerLogger
    {
        

        public class TaskLogger
        {
            ManagerLogger Logger;
            LoggableTaskType Type;

            string Executable;
            string Arguments;
            int ReturnCode;
            DateTime StartTime;
            DateTime? EndTime;
            List<string> StdIn = new();
            List<string> StdOut = new();
            List<string> StdErr = new();
            bool isComplete = false;
            bool isOpen = false;

            public TaskLogger(ManagerLogger logger, LoggableTaskType type, string executable, string arguments)
            {
                Type = type;
                Logger = logger;
                Executable = executable;
                Arguments = arguments;
                StartTime = DateTime.Now;
                isComplete = false;
                isOpen = true;
            }

            ~TaskLogger() {
                if (isOpen)
                    throw new Exception("A TaskLogger instance went out of scope without being closed");
            }

            public void AddToStdIn(string? line)
            {
                if (line != null) AddToStdIn(line.Split('\n'));
            }

            public void AddToStdIn(IEnumerable<string> lines)
            {
                if (!isOpen) throw new Exception("Attempted to write log into an already-closed OperationLog");
                foreach (string line in lines)
                    StdErr.Add(line);
            }

            public void AddToStdOut(string? line)
            {
                if (line != null) AddToStdOut(line.Split('\n'));
            }

            public void AddToStdOut(IEnumerable<string> lines)
            {
                if (!isOpen) throw new Exception("Attempted to write log into an already-closed OperationLog");
                foreach (string line in lines)
                    StdOut.Add(line);
            }

            public void AddToStdErr(string? line)
            {
                if (line != null) AddToStdErr(line.Split('\n'));
            }

            public void AddToStdErr(IEnumerable<string> lines)
            {
                if (!isOpen) throw new Exception("Attempted to write log into an already-closed OperationLog");
                foreach (string line in lines)
                    StdErr.Add(line);
            }

            public void Close()
            {
                EndTime = DateTime.Now;
                isComplete = true;
                isOpen = false;
                Logger.LoadOperation(this);
            }
        }

        PackageManager Manager;
        List<TaskLogger> Operations = new();


        public ManagerLogger(PackageManager manager)
        {
            Manager = manager;
        }

        private void LoadOperation(TaskLogger operation)
        {
            Operations.Add(operation);
        }

        public TaskLogger CreateNew(LoggableTaskType type, Process process)
        {
            if (process.StartInfo == null)
                throw new Exception("Process instance did not have a valid StartInfo value");

            return new TaskLogger(this, type, process.StartInfo.FileName, process.StartInfo.Arguments);
        }

        public TaskLogger CreateNew(LoggableTaskType type, string ExtraArgument = "N/A")
        {
            return new TaskLogger(this, type, $"{Manager.Name} native operation", ExtraArgument);
        }
    }
}
