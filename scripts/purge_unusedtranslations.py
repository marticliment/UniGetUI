import os, glob, json

try:
    os.chdir("../wingetui/lang/")

    contents = ""

    with open("lang_en.json", "r") as f:
        engfile = json.load(f)

    os.chdir("../../")
    print(f"Working on 📂 {os.getcwd()}")

    for codefile in glob.glob("**/*.py", recursive=True):
        print(f"Reading 📄 {codefile}")
        with open(codefile, "r", errors="ignore") as f:
            contents += f.read()
        contents += " ################################ File division #########################################"
    for key in engfile.keys():
        key = key.replace("\n", "\\n")
        if not key in contents:
            print("Unused key 😳: "+str(key))
    print("Job finished succuessfully! 😎")
except Exception as e:
    print("FAILED:", e)
        
os.system("pause")