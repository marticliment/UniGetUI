import glob
import json
import os

try:
    root_dir = os.path.join(os.path.dirname(__file__), "..")
    os.chdir(os.path.normpath(os.path.join(root_dir, "UniGetUI/Core/Languages")))

    contents = ""

    with open("lang_en.json", "r") as f:
        engfile = json.load(f)

    os.chdir(root_dir)
    print(f"Working on 📂 {os.getcwd()}")

    for codefile in glob.glob("**/*.py", recursive=True):
        print(f"Reading 📄 {codefile}")
        with open(codefile, "r", errors="ignore") as f:
            contents += f.read()
        contents += " ################################ File division #########################################"
    for key in engfile.keys():
        key = key.replace("\n", "\\n")
        if not key in contents:
            if not key.replace("\"", "\\\"") in contents:
                print(f"Unused key 😳: {key}")
    print("Job finished succuessfully! 😎")
except Exception as e:
    print("FAILED:", e)

os.system("pause")
