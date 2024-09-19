import os
import glob

os.chdir(os.path.join(os.path.dirname(__file__), ".."))  # move to root project

try:
    floatval = input("Enter version code (X.XXX)  : ")

    if floatval == "":
        print("Version changer script aborted")
        exit()

    versionCode = float(floatval)
    versionName = str(input("Enter version name (string) : "))
    versionISS = str(input("Enter version     (X.X.X.X) : "))

    def fileReplaceLinesWith(filename: str, list: dict[str, str], encoding="utf-8"):
        with open(filename, "r+", encoding=encoding, errors="ignore") as f:
            data = ""
            for line in f.readlines():
                match = False
                for key, value in list.items():
                    if (key in line):
                        data += f"{key}{value}"
                        match = True
                        continue
                if (not match):
                    data += line
            f.seek(0)
            f.write(data)
            f.truncate()

    fileReplaceLinesWith("src/UniGetUI.Core.Data/CoreData.cs", {
        "        public const string VersionName = ": f" \"{versionName}\"; // Do not modify this line, use file scripts/apply_versions.py\n",
        "        public const double VersionNumber = ": f" {versionCode}; // Do not modify this line, use file scripts/apply_versions.py\n",
    }, encoding="utf-8-sig")

    fileReplaceLinesWith("src/SharedAssemblyInfo.cs", {
        "[assembly: AssemblyVersion(\"": f"{versionISS}\")]\n",
        "[assembly: AssemblyFileVersion(\"": f"{versionISS}\")]\n",
        "[assembly: AssemblyInformationalVersion(\"": f"{versionName}\")]\n",
        "[assembly: AssemblyInformationalVersionAttribute(\"": f"{versionName}\")]\n",
        "[assembly: AssemblyVersionAttribute(\"": f"{versionISS}\")]\n",
        # Your replacement dictionary here
    }, encoding="utf-8-sig")

    fileReplaceLinesWith("UniGetUI.iss", {
        "#define MyAppVersion": f" \"{versionName}\"\n",
        "VersionInfoVersion=": f"{versionISS}\n",
    }, encoding="utf-8-sig")

    fileReplaceLinesWith("src/UniGetUI/app.manifest", {
        "	  version=": f" \"{versionISS}\"\n",
    }, encoding="utf-8-sig")

    print("done!")
except FileNotFoundError as e:
    print(f"Error: {e.strerror}: {e.filename}")
    os.system("pause")
except Exception as e:
    print(f"Error: {str(e)}")
    os.system("pause")
