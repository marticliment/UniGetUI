using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using UniGetUI.Core.Data;
using UniGetUI.Core.Language;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.Tools
{
    public static class CoreTools
    {

        public static readonly HttpClientHandler HttpClientConfig = new()
        {
            AutomaticDecompression = DecompressionMethods.All
        };

        private static LanguageEngine LanguageEngine = new();

        /// <summary>
        /// Translate a string to the current language
        /// </summary>
        /// <param name="text">The string to translate</param>
        /// <returns>The translated string if available, the original string otherwise</returns>
        public static string Translate(string text)
        {
            return LanguageEngine.Translate(text);
        }

        public static string Translate(string text, Dictionary<string, object?> dict)
        {
            return LanguageEngine.Translate(text, dict);
        }

        public static string Translate(string text, params object[] values)
        {
            Dictionary<string, object?> dict = [];
            foreach ((object item, int index) in values.Select((item, index) => (item, index)))
            {
                dict.Add(index.ToString(), item);
            }

            return Translate(text, dict);
        }

        public static void ReloadLanguageEngineInstance(string ForceLanguage = "")
        {
            LanguageEngine = new LanguageEngine(ForceLanguage);
        }

        /// <summary>
        /// Dummy function to capture the strings that need to be translated but the translation is handled by a custom widget
        /// </summary>
        public static string AutoTranslated(string text)
        {
            return text;
        }

        /// <summary>
        /// Launches the self executable on a new process and kills the current process
        /// </summary>
        public static void RelaunchProcess()
        {
            Logger.Debug("Launching process: " + Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            Process.Start(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            Logger.Warn("About to kill process");
            Environment.Exit(0);
        }

        /// <summary>
        /// Finds an executable in path and returns its location
        /// </summary>
        /// <param name="command">The executable alias to find</param>
        /// <returns>A tuple containing: a boolean hat represents whether the path was found or not; the path to the file if found.</returns>
        public static async Task<Tuple<bool, string>> WhichAsync(string command)
        {
            return await Task.Run(() => Which(command));
        }

        public static Tuple<bool, string> Which(string command, bool updateEnv = true)
        {
            command = command.Replace(";", "").Replace("&", "").Trim();
            Logger.Debug($"Begin \"which\" search for command {command}");
            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Join(Environment.SystemDirectory, "where.exe"),
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = CodePagesEncodingProvider.Instance.GetEncoding(CoreData.CODE_PAGE),
                    StandardErrorEncoding = CodePagesEncodingProvider.Instance.GetEncoding(CoreData.CODE_PAGE),
                }
            };
            if (updateEnv)
            {
                process.StartInfo = UpdateEnvironmentVariables(process.StartInfo);
            }

            try
            {


                process.Start();
                string? line = process.StandardOutput.ReadLine();
                string output;

                if (line is null) output = "";
                else output = line.Trim();

                process.WaitForExit();

                if (process.ExitCode != 0 || output == "")
                {
                    Logger.ImportantInfo($"Command {command} was not found on the system");
                    return new Tuple<bool, string>(false, "");
                }

                Logger.Debug($"Command {command} was found on {output}");
                return new Tuple<bool, string>(File.Exists(output), output);
            }
            catch
            {
                if (updateEnv) return Which(command, false);
                throw;
            }
        }

        /// <summary>
        /// Formats a given package id as a name, capitalizing words and replacing separators with spaces
        /// </summary>
        /// <param name="name">A string containing the Id of a package</param>
        /// <returns>The formatted string</returns>
        public static string FormatAsName(string name)
        {
            name = name.Replace(".install", "").Replace(".portable", "").Replace("-", " ").Replace("_", " ").Split("/")[^1];
            string newName = "";
            for (int i = 0; i < name.Length; i++)
            {
                if (i == 0 || name[i - 1] == ' ')
                {
                    newName += name[i].ToString().ToUpper();
                }
                else
                {
                    newName += name[i];
                }
            }
            return newName;
        }

        /// <summary>
        /// Generates a random string composed of alphanumeric characters and numbers
        /// </summary>
        /// <param name="length">The length of the string</param>
        /// <returns>A string</returns>
        public static string RandomString(int length)
        {
            Random random = new();
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            IEnumerable<char> chars = Enumerable.Range(0, length)
                .Select(_ => pool[random.Next(0, pool.Length)]);
            return new string(chars.ToArray());
        }

        public static void ReportFatalException(Exception e)
        {
            string LangName = "Unknown";
            try
            {
                LangName = LanguageEngine.Locale;
            }
            catch
            {
                // ignored
            }

            string Error_String = $@"
                        OS: {Environment.OSVersion.Platform}
                   Version: {Environment.OSVersion.VersionString}
           OS Architecture: {Environment.Is64BitOperatingSystem}
          APP Architecture: {Environment.Is64BitProcess}
                  Language: {LangName}
               APP Version: {CoreData.VersionName}
                Executable: {Environment.ProcessPath}

Crash Message: {e.Message}

Crash Traceback:
{e.StackTrace}";

            Console.WriteLine(Error_String);

            string ErrorBody = "https://www.marticliment.com/error-report/?appName=UniGetUI^&errorBody=" + Uri.EscapeDataString(Error_String.Replace("\n", "{l}"));

            Console.WriteLine(ErrorBody);

            using Process cmd = new();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine("start " + ErrorBody);
            cmd.StandardInput.WriteLine("exit");
            cmd.WaitForExit();
            Environment.Exit(1);
        }

        /// <summary>
        /// Launches a .bat or .cmd file for the given filename
        /// </summary>
        /// <param name="path">The path of the batch file</param>
        /// <param name="WindowTitle">The title of the window</param>
        /// <param name="RunAsAdmin">Whether the batch file should be launched elevated or not</param>
        public static async void LaunchBatchFile(string path, string WindowTitle = "", bool RunAsAdmin = false)
        {
            Process p = new();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/C start \"" + WindowTitle + "\" \"" + path + "\"";
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Verb = RunAsAdmin ? "runas" : "";
            p.Start();
            await p.WaitForExitAsync();
        }

        /// <summary>
        /// Checks whether the current process has administrator privileges
        /// </summary>
        /// <returns>True if the process has administrator privileges</returns>
        public static bool IsAdministrator()
        {
            try
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent())
                          .IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception e)
            {
                Logger.Warn("Could not check if user is administrator");
                Logger.Warn(e);
                return false;
            }
        }

        /// <summary>
        /// Returns the size (in MB) of the file at the given URL
        /// </summary>
        /// <param name="url">a valid Uri object containing a URL to a file</param>
        /// <returns>a double representing the size in MBs, 0 if the process fails</returns>
        public static async Task<double> GetFileSizeAsync(Uri? url)
        {
            return await GetFileSizeAsyncAsLong(url) / 1048576d;
        }

        /// <summary>
        /// Returns the size (in MB) of the file at the given URL
        /// </summary>
        /// <param name="url">a valid Uri object containing a URL to a file</param>
        /// <returns>a double representing the size in MBs, 0 if the process fails</returns>
        public static double GetFileSize(Uri? url)
        {
            return GetFileSizeAsyncAsLong(url).GetAwaiter().GetResult() / 1048576d;
        }

        public static async Task<long> GetFileSizeAsyncAsLong(Uri? url)
        {
            if (url is null)
            {
                return 0;
            }

            try
            {
#pragma warning disable SYSLIB0014 // Type or member is obsolete
                WebRequest req = WebRequest.Create(url);
#pragma warning restore SYSLIB0014 // Type or member is obsolete
                req.Method = "HEAD";
                WebResponse resp = await req.GetResponseAsync();
                if (long.TryParse(resp.Headers.Get("Content-Length"), out long ContentLength))
                {
                    return ContentLength;
                }

            }
            catch (Exception e)
            {
                Logger.Warn($"Could not load file size for url={url}");
                Logger.Warn(e);
            }
            return 0;
        }

        /// <summary>
        /// Converts a string into a double floating-point number.
        /// </summary>
        /// <param name="Version">Any string</param>
        /// <returns>The best approximation of the string as a Version</returns>
        public static double GetVersionStringAsFloat(string Version)
        {
            try
            {
                string _ver = "";
                bool _dotAdded = false;
                foreach (char _char in Version)
                {
                    if (char.IsDigit(_char))
                    {
                        _ver += _char;
                    }
                    else if (_char == '.')
                    {
                        if (!_dotAdded)
                        {
                            _ver += _char;
                            _dotAdded = true;
                        }
                    }
                }
                double res = -1;
                if (_ver is not "" and not ".")
                {
                    try
                    {
                        double val = double.Parse(_ver, CultureInfo.InvariantCulture);
                        return val;
                    }
                    catch
                    {
                        // ignored
                    }
                }

                return res;
            }
            catch
            {
                Logger.Warn($"Failed to parse version {Version} to float");
                return -1;
            }
        }

        /// <summary>
        /// Returns the query that can be safely passed as a command-line parameter
        /// </summary>
        /// <param name="query">The query to make safe</param>
        /// <returns>The safe version of the query</returns>
        public static string EnsureSafeQueryString(string query)
        {
            return query.Replace(";", string.Empty)
                        .Replace("&", string.Empty)
                        .Replace("|", string.Empty)
                        .Replace(">", string.Empty)
                        .Replace("<", string.Empty)
                        .Replace("%", string.Empty)
                        .Replace("\"", string.Empty)
                        .Replace("~", string.Empty)
                        .Replace("?", string.Empty)
                        .Replace("/", string.Empty)
                        .Replace("'", string.Empty)
                        .Replace("\\", string.Empty)
                        .Replace("`", string.Empty);
        }

        /// <summary>
        /// Returns null if the string is empty
        /// </summary>
        /// <param name="value">The string to check</param>
        /// <returns>a string? instance</returns>
        public static string? GetStringOrNull(string? value)
        {
            if (value == "")
            {
                return null;
            }

            return value;
        }

        /// <summary>
        /// Returns a new Uri if the string is not empty. Returns null otherwise
        /// </summary>
        /// <param name="url">The null, empty or valid string</param>
        /// <returns>an Uri? instance</returns>
        public static Uri? GetUriOrNull(string? url)
        {
            if (url is "" or null)
            {
                return null;
            }

            return new Uri(url);
        }

        /// <summary>
        /// Enables GSudo cache for the current process
        /// </summary>
        public static async Task CacheUACForCurrentProcess()
        {
            Logger.Info("Caching admin rights for process id " + Environment.ProcessId);
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = CoreData.GSudoPath,
                    Arguments = "cache on --pid " + Environment.ProcessId + " -d 1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                }
            };
            p.Start();
            await p.WaitForExitAsync();
        }

        /// <summary>
        /// Reset UAC cache for the current process
        /// </summary>
        public static async Task ResetUACForCurrentProcess()
        {
            Logger.Info("Resetting administrator rights cache for process id " + Environment.ProcessId);
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = CoreData.GSudoPath,
                    Arguments = "cache off --pid " + Environment.ProcessId,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                }
            };
            p.Start();
            await p.WaitForExitAsync();
        }

        /// <summary>
        /// Returns the hash of the given string in the form of a long integer.
        /// The long integer is built with the first half of the MD5 sum of the given string
        /// </summary>
        /// <param name="inputString">A non-empty string</param>
        /// <returns>A long integer containing the first half of the bytes resulting from MD5suming inputString</returns>
        public static long HashStringAsLong(string inputString)
        {
            byte[] bytes = MD5.HashData(Encoding.UTF8.GetBytes(inputString));
            return BitConverter.ToInt64(bytes, 0);
        }

        /// <summary>
        /// Creates a symbolic link between directories
        /// </summary>
        /// <param name="linkPath">The location of the link to be created</param>
        /// <param name="targetPath">The location of the real folder where to point</param>
        public static void CreateSymbolicLinkDir(string linkPath, string targetPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mklink /D \"{linkPath}\" \"{targetPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process? p = Process.Start(startInfo);
            if (p is not null)
            {
                p.WaitForExit();
            }

            if (p is null || p.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"The operation did not complete successfully: \n{p?.StandardOutput.ReadToEnd()}\n{p?.StandardError.ReadToEnd()}\n");
            }
        }

        /// <summary>
        /// Will check whether the given folder is a symbolic link
        /// </summary>
        /// <param name="path">The folder to check</param>
        /// <exception cref="FileNotFoundException"></exception>
        public static bool IsSymbolicLinkDir(string path)
        {
            if (!Directory.Exists(path) && !File.Exists(path))
            {
                throw new FileNotFoundException("The specified path does not exist.", path);
            }

            var attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }

        /// <summary>
        /// Returns the updated environment variables on a new ProcessStartInfo object
        /// </summary>
        /// <returns></returns>
        public static ProcessStartInfo UpdateEnvironmentVariables()
        {
            return UpdateEnvironmentVariables(new ProcessStartInfo());
        }


        /// <summary>
        /// Returns the updated environment variables on the returned ProcessStartInfo object
        /// </summary>
        /// <returns></returns>
        public static ProcessStartInfo UpdateEnvironmentVariables(ProcessStartInfo info)
        {
            foreach (DictionaryEntry env in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine))
            {
                info.Environment[env.Key?.ToString() ?? "UNKNOWN"] = env.Value?.ToString();
            }
            foreach (DictionaryEntry env in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User))
            {
                string key = env.Key.ToString() ?? "";
                string newValue = env.Value?.ToString() ?? "";
                if (info.Environment.TryGetValue(key, out string? oldValue) && oldValue is not null && oldValue.Contains(';') && newValue != "")
                {
                    info.Environment[key] = oldValue + ";" + newValue;
                }
                else
                {
                    info.Environment[key] = newValue;
                }
            }
            return info;
        }
    }
}
