from UniGetUI.Core.Languages.LangData import languageReference

lang = {}
englang = {}
languages = {}  # will be auto-generated

# auto-generate map of files
for key in languageReference.keys():
    if key != "default":
        languages[key] = f"lang_{key}.json"

debugLang = False
