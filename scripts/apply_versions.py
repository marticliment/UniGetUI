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

    # Get a list of files in the current directory
    files = list(glob.iglob(glob.escape(".") + '/**/*', recursive=True))

    # Filter the list to only include .csproj files
    csproj_files = [file for file in files if file.endswith(".csproj")]

    # Iterate over each .csproj file
    for csproj_file in csproj_files:
        # Perform your desired operations on each .csproj file
        # For example, you can call the `fileReplaceLinesWith` function here
        fileReplaceLinesWith(csproj_file, {
            "<FileVersion>": f"{versionISS}</FileVersion>\n",
            "<InformationalVersion>": f"{versionName}</InformationalVersion>\n",
            "<ApplicationVersion>": f"{versionName}</ApplicationVersion>\n",
            # Your replacement dictionary here
        }, encoding="utf-8-sig")

    fileReplaceLinesWith("WingetUI.iss", {
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
