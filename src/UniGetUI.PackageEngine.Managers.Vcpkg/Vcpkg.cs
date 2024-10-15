using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.VcpkgManager {
    public class Vcpkg : PackageManager
    {
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
				// TODO: Sources
				DefaultSource = new ManagerSource(this, DefaultTriplet, new Uri("https://vcpkg.io/")),
                KnownSources = [
					new ManagerSource(this, "arm-neon-android", new Uri("https://vcpkg.io/")),
					new ManagerSource(this, "arm64-android", new Uri("https://vcpkg.io/")),
					new ManagerSource(this, "arm64-uwp", new Uri("https://vcpkg.io/")),
					new ManagerSource(this, "arm64-windows", new Uri("https://vcpkg.io/")),
					new ManagerSource(this, "x64-android", new Uri("https://vcpkg.io/")),
					new ManagerSource(this, "x64-linux", new Uri("https://vcpkg.io/")),
					new ManagerSource(this, "x64-osx", new Uri("https://vcpkg.io/")),
					new ManagerSource(this, "x64-uwp", new Uri("https://vcpkg.io/")),
					new ManagerSource(this, "x64-windows-static", new Uri("https://vcpkg.io/")),
					new ManagerSource(this, "x64-windows", new Uri("https://vcpkg.io/")),
					new ManagerSource(this, "x86-windows", new Uri("https://vcpkg.io/"))
				],
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
			return [];
            throw new NotImplementedException();
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