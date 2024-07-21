using System.Diagnostics;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.ManagerClasses.Classes
{
    public class ManagerLogger
    {
        private readonly PackageManager Manager;
        public List<TaskLogger> Operations = [];

        public ManagerLogger(PackageManager manager)
        {
            Manager = manager;
        }

        public ProcessTaskLogger CreateNew(LoggableTaskType type, Process process)
        {
            if (process.StartInfo == null)
            {
                throw new InvalidOperationException("Given process instance did not have a valid StartInfo value");
            }

            ProcessTaskLogger operation = new(Manager, type, process.StartInfo.FileName, process.StartInfo.Arguments);
            Operations.Add(operation);
            return operation;
        }

        public NativeTaskLogger CreateNew(LoggableTaskType type)
        {
            NativeTaskLogger operation = new(Manager, type);
            Operations.Add(operation);
            return operation;
        }
    }

    public abstract class TaskLogger
    {
        protected DateTime StartTime;
        protected DateTime? EndTime;
        protected bool isComplete;
        protected bool isOpen;
        protected IEnumerable<string>? CachedMessage;
        protected IEnumerable<string>? CachedVerboseMessage;

        private const int RETURNCODE_UNSET = -200;
        protected int ReturnCode = -200;

        public TaskLogger()
        {
            StartTime = DateTime.Now;
            isComplete = false;
            isOpen = true;
        }

        ~TaskLogger()
        {
            if (isOpen)
            {
                Close(RETURNCODE_UNSET);
            }
        }

        public void Close(int returnCode)
        {
            ReturnCode = returnCode;
            EndTime = DateTime.Now;
            isOpen = false;
            isComplete = true;
            CachedMessage = null;
            CachedVerboseMessage = null;
        }

        /// <summary>
        /// Returns the output with a preceeding digit representing the color of the line:
        ///   0. White
        ///   1. Grey
        ///   2. Red
        ///   3. Blue
        ///   4. Green
        ///   5. Yellow
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<string> AsColoredString(bool verbose = false);
    }

    public class ProcessTaskLogger : TaskLogger
    {
        private readonly PackageManager Manager;
        private readonly LoggableTaskType Type;

        private readonly string Executable;
        private readonly string Arguments;
        private readonly List<string> StdIn = [];
        private readonly List<string> StdOut = [];
        private readonly List<string> StdErr = [];

        public ProcessTaskLogger(PackageManager manager, LoggableTaskType type, string executable, string arguments)
        {
            Type = type;
            Manager = manager;
            Executable = executable;
            Arguments = arguments;
        }

        public void AddToStdIn(string? line)
        {
            if (line != null)
            {
                AddToStdIn(line.Split('\n'));
            }
        }

        public void AddToStdIn(IEnumerable<string> lines)
        {
            if (!isOpen)
            {
                throw new InvalidOperationException("Attempted to write log into an already-closed ProcessTaskLogger");
            }

            foreach (string line in lines)
            {
                if (line != "")
                {
                    StdIn.Add(line);
                }
            }
        }

        public void AddToStdOut(string? line)
        {
            if (line != null)
            {
                AddToStdOut(line.Split('\n'));
            }
        }

        public void AddToStdOut(IEnumerable<string> lines)
        {
            if (!isOpen)
            {
                throw new InvalidOperationException("Attempted to write log into an already-closed ProcessTaskLogger");
            }

            foreach (string line in lines)
            {
                if (line != "")
                {
                    StdOut.Add(line);
                }
            }
        }

        public void AddToStdErr(string? line)
        {
            if (line != null)
            {
                AddToStdErr(line.Split('\n'));
            }
        }

        public void AddToStdErr(IEnumerable<string> lines)
        {
            if (!isOpen)
            {
                throw new InvalidOperationException("Attempted to write log into an already-closed ProcessTaskLogger");
            }

            foreach (string line in lines)
            {
                if (line != "")
                {
                    StdErr.Add(line);
                }
            }
        }

        public override IEnumerable<string> AsColoredString(bool verbose = false)
        {
            if (!verbose && CachedMessage != null && isComplete)
            {
                return CachedMessage;
            }

            if (verbose && CachedVerboseMessage != null && isComplete)
            {
                return CachedVerboseMessage;
            }

            List<string> result =
            [
                $"0Logged subprocess-based task on manager {Manager.Name}. Task type is {Type}",
                $"0Subprocess executable: \"{Executable}\"",
                $"0Command-line arguments: \"{Arguments}\"",
                $"0Process start time: {StartTime}",
                EndTime == null ? "2Process end time:   UNFINISHED" : $"0Process end time:   {EndTime}",
            ];

            if (StdIn.Count > 0)
            {
                result.Add("0");
                result.Add("0-- Process STDIN");
                if (verbose)
                {
                    foreach (string line in StdIn)
                    {
                        result.Add("3  " + line);
                    }
                }
                else
                {
                    result.Add("1 ...");
                }
            }
            if (StdOut.Count > 0)
            {
                result.Add("0");
                result.Add("0-- Process STDOUT");
                if (verbose)
                {
                    foreach (string line in StdOut)
                    {
                        result.Add("1  " + line);
                    }
                }
                else
                {
                    result.Add("1 ...");
                }
            }
            if (StdErr.Count > 0)
            {
                result.Add("0");
                result.Add("0-- Process STDERR");
                foreach (string line in StdErr)
                {
                    result.Add("2  " + line);
                }
            }
            result.Add("0");
            if (!isComplete)
            {
                result.Add("5Return code: Process has not finished yet");
            }
            else if (ReturnCode == -200)
            {
                result.Add("5Return code: UNSPECIFIED");
            }
            else if (ReturnCode == 0)
            {
                result.Add("4Return code: SUCCESS (0)");
            }
            else
            {
                result.Add($"2Return code: FAILED ({ReturnCode})");
            }

            result.Add("0");
            result.Add("0——————————————————————————————————————————");
            result.Add("0");

            if (verbose)
            {
                return CachedVerboseMessage = result;
            }

            return CachedMessage = result;
        }
    }

    public class NativeTaskLogger : TaskLogger
    {
        private readonly PackageManager Manager;
        private readonly LoggableTaskType Type;

        private readonly List<string> Info = [];
        private readonly List<string> Errors = [];

        public NativeTaskLogger(PackageManager manager, LoggableTaskType type)
        {
            Type = type;
            Manager = manager;
        }

        public void Log(IEnumerable<string> lines)
        {
            if (!isOpen)
            {
                throw new InvalidOperationException("Attempted to write log into an already-closed NativeTaskLogger");
            }

            foreach (string line in lines)
            {
                if (line != "")
                {
                    Info.Add(line);
                }
            }
        }

        public void Log(string? line)
        {
            if (line != null)
            {
                Log(line.Split('\n'));
            }
        }

        public void Error(IEnumerable<string> lines)
        {
            if (!isOpen)
            {
                throw new InvalidOperationException("Attempted to write log into an already-closed NativeTaskLogger");
            }

            foreach (string line in lines)
            {
                if (line != "")
                {
                    Errors.Add(line);
                }
            }
        }

        public void Error(string? line)
        {
            if (line != null)
            {
                Error(line.Split('\n'));
            }
        }

        public void Error(Exception? e)
        {
            if (e != null)
            {
                Error(e.ToString().Split('\n'));
            }
        }

        public override IEnumerable<string> AsColoredString(bool verbose = false)
        {
            if (!verbose && CachedMessage != null && isComplete)
            {
                return CachedMessage;
            }

            if (verbose && CachedVerboseMessage != null && isComplete)
            {
                return CachedVerboseMessage;
            }

            List<string> result =
            [
                $"0Logged native task on manager {Manager.Name}. Task type is {Type}",
                $"0Process start time: {StartTime}",
                EndTime == null ? "2Process end time:   UNFINISHED" : $"0Process end time:   {EndTime}",
            ];

            if (Info.Count > 0)
            {
                result.Add("0");
                result.Add("0-- Task information");
                if (verbose)
                {
                    foreach (string line in Info)
                    {
                        result.Add("1  " + line);
                    }
                }
                else
                {
                    result.Add("1 ...");
                }
            }
            if (Errors.Count > 0)
            {
                result.Add("0");
                result.Add("0-- Task errors");
                foreach (string line in Errors)
                {
                    result.Add("2  " + line);
                }
            }
            result.Add("0");
            if (!isComplete)
            {
                result.Add("5The task has not finished yet");
            }
            else if (ReturnCode == -200)
            {
                result.Add("5The task did not report a finish status");
            }
            else if (ReturnCode == 0)
            {
                result.Add("4The task reported success");
            }
            else
            {
                result.Add($"2The task reported a failure ({ReturnCode})");
            }

            result.Add("0");
            result.Add("0——————————————————————————————————————————");
            result.Add("0");

            if (verbose)
            {
                return CachedVerboseMessage = result;
            }

            return CachedMessage = result;
        }
    }
}
