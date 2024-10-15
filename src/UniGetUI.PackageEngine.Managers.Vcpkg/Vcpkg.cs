using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using Windows.Security.Authentication.Web.Core;

namespace UniGetUI.PackageEngine.Managers.VcpkgManager {
    public class Vcpkg : PackageManager
    {
        public Dictionary<string, ManagerSource> TripletSourceMap;
        public Uri URI_VCPKG_IO = new Uri("https://vcpkg.io/");

		public Vcpkg()
		{
			Capabilities = new ManagerCapabilities
			{
				CanRunAsAdmin = false, // TODO: check this; should this be true since we need admin to install to protected directories?
				SupportsCustomSources = true, // TODO: check this; are different triplets "different sources"?
			};

			string DefaultTriplet = Environment.GetEnvironmentVariable("VCPKG_DEFAULT_TRIPLET") ?? "";

			if (DefaultTriplet == "") {
				if (RuntimeInformation.OSArchitecture == Architecture.X64) DefaultTriplet = "x64-";
				else if (RuntimeInformation.OSArchitecture == Architecture.X86) DefaultTriplet = "x86-";
				else if (RuntimeInformation.OSArchitecture == Architecture.Arm64) DefaultTriplet = "arm64-";

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) DefaultTriplet += "windows";
				if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) DefaultTriplet += "osx";
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) DefaultTriplet += "linux";
			}

            TripletSourceMap = new Dictionary<string, ManagerSource>
            {
                { "arm-neon-android",   new ManagerSource(this, "arm-neon-android", URI_VCPKG_IO) },
                { "arm64-android",      new ManagerSource(this, "arm64-android", URI_VCPKG_IO) },
                { "arm64-uwp",          new ManagerSource(this, "arm64-uwp", URI_VCPKG_IO) },
                { "arm64-windows",      new ManagerSource(this, "arm64-windows", URI_VCPKG_IO) },
                { "x64-android",        new ManagerSource(this, "x64-android", URI_VCPKG_IO) },
                { "x64-linux",          new ManagerSource(this, "x64-linux", URI_VCPKG_IO) },
                { "x64-osx",            new ManagerSource(this, "x64-osx", URI_VCPKG_IO) },
                { "x64-uwp",            new ManagerSource(this, "x64-uwp", URI_VCPKG_IO) },
                { "x64-windows-static", new ManagerSource(this, "x64-windows-static", URI_VCPKG_IO) },
                { "x64-windows",        new ManagerSource(this, "x64-windows", URI_VCPKG_IO) },
                { "x86-windows",        new ManagerSource(this, "x86-windows", URI_VCPKG_IO) }
            };

            Properties = new ManagerProperties
            {
                Name = "vcpkg",
                Description = CoreTools.Translate("A popular C/C++ library manager. Full of C/C++ libraries and other C/C++-related utilities<br>Contains: <b>C/C++ libraries and related utilities</b>"),
                IconId = IconType.Package, // What I got from discussion #2826 is that for a custom vcpkg icon, Marti has to do it, so this one seems the most 
                ColorIconId = "vcpkg_color",
                ExecutableFriendlyName = "vcpkg",
                InstallVerb = "install",
                UninstallVerb = "remove",
                UpdateVerb = "update",
                ExecutableCallArgs = "",
                DefaultSource = new ManagerSource(this, DefaultTriplet, URI_VCPKG_IO),
                KnownSources = [.. TripletSourceMap.Values],
            };

            SourceProvider = new VcpkgSourceProvider(this);
            OperationProvider = new VcpkgOperationProvider(this);
        }

        protected override IEnumerable<Package> FindPackages_UnSafe(string query)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Package> GetAvailableUpdates_UnSafe()
        {
			return [];
            throw new NotImplementedException();
        }

        protected override IEnumerable<Package> GetInstalledPackages_UnSafe()
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " list",
                    // vcpkg has an --x-json flag that would list installed packages in JSON, but it's expiremental
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListInstalledPackages, p);
            string? line;
            List<Package> Packages = [];

            p.Start();

            Dictionary<string, string> PackageVersions = new();
            while ((line = p.StandardOutput.ReadLine()) is not null)
            {
                logger.AddToStdOut(line);

                // Sample line:
                // name:triplet (source)             version     description
                // curl:x64-mingw-dynamic            8.1.2#2     A library for transferring data with URLs
                // curl[non-http]:x64-mingw-dynamic              Enables protocols beyond HTTP/HTTPS/HTTP2
                // (note that the suboptions, with the `name[build-option]` syntax have no version)

                //                                           to get rid of many spaces of padding
                string[] PackageData = Regex.Replace(line, @"\s+", " ").Split(' ');
                string PackageId = PackageData[0];
                string PackageName = PackageId.Split(':')[0],
                    PackageTriplet = PackageId.Split(':')[1],
                    PackageVersion = PackageData[1];
                if (PackageId.Contains('[') /* meaning its a suboption, and thus has no version */)
                {
                    PackageVersion = PackageVersions[PackageName[..PackageName.IndexOf("[")]];
                }
                else
                {
                    PackageVersions[PackageName] = PackageVersion;
                }

                if (!TripletSourceMap.TryGetValue(PackageTriplet, out ManagerSource? value))
                {
                    value = new ManagerSource(this, PackageTriplet, URI_VCPKG_IO);
                    TripletSourceMap[PackageTriplet] = value;
                }

                Packages.Add(new Package(CoreTools.FormatAsName(PackageName), PackageId, PackageVersion, value, this));
            }

            return Packages;
        }

        protected override ManagerStatus LoadManager()
        {
			var (found, path) = CoreTools.Which("vcpkg");

			ManagerStatus status = new ManagerStatus
			{
				Found = found,
				ExecutablePath = path,
			};

			if (!status.Found)
            {
                return status;
            }

			Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };
			process.Start();
			status.Version = (process.StandardOutput.ReadLine() ?? "Unknown").Replace("vcpkg package management program version", "").Trim();

			return status;
        }
    }
}