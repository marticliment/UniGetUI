

def ExportSettingsToFile(filename: str):
    try:
        rawstr = ""
        for file in glob.glob(os.path.join(os.path.expanduser("~"), ".wingetui/*")):
            if "Running" not in file and "png" not in file and "PreferredLanguage" not in file and "json" not in file:
                sName = file.replace("\\", "/").split("/")[-1]
                rawstr += sName + "|@|" + \
                    getSettingsValue(sName).replace("|~|", "").replace("|@|", "") + "|~|"
        oFile = open(filename, "w", encoding="utf-8", errors="ignore")
        oFile.write(rawstr)
        oFile.close()
        subprocess.run(
            "explorer /select,\"" + filename.replace('/', '\\') + "\"", shell=True)
    except Exception as e:
        report(e)

def ImportSettingsFromFile(file: str):
    try:
        iFile = open(file, "r")
        rawstr = iFile.read()
        iFile.close()
        ResetSettings()
        for element in rawstr.split("|~|"):
            pairValue = element.split("|@|")
            if len(pairValue) == 2:
                setSettings(pairValue[0], True)
                if pairValue[1] != "":
                    setSettingsValue(pairValue[0], pairValue[1])
    except Exception as e:
        report(e)

def ResetSettings():
    ResetCache()
    for file in glob.glob(os.path.join(os.path.expanduser("~"), ".wingetui/**/*"), recursive=True):
        if "Running" not in file:
            try:
                os.remove(file)
            except Exception:
                pass

def ResetCache():
    for file in glob.glob(os.path.join(os.path.expanduser("~"), "AppData/Local/WingetUI/**/*"), recursive=True):
        if "Running" not in file:
            try:
                os.remove(file)
            except Exception:
                pass


def ConvertMarkdownToHtml(content: str) -> str:
    try:
        content = content.replace("\n\r", "<br>")
        content = content.replace("\n", "<br>")
        firsttext = "<br>" + content
        content = ""
        for line in firsttext.split("<br>"):
            if line:
                content += line + "<br>"
        content = "<br>" + content

        # Convert headers
        for match in re.findall(r"<br>[ ]*#{3,4}[^\>\<]*<br>", content):
            match: str
            content = content.replace(
                match, f'<br><b>{match.replace("#", "").strip()}</b>')

        for match in re.findall(r"<br>[ ]*##[^\>\<]*<br>", content):
            match: str
            content = content.replace(
                match, f'<br><b style="font-size:12.5pt;">{match.replace("#", "").strip()}</b>')

        for match in re.findall(r"<br>#[^\>\<]*<br>", content):
            match: str
            content = content.replace(
                match, f'<br><b style="font-size:14pt;">{match.replace("#", "").strip()}</b>')

        # Convert linked images to URLs
        for match in re.findall(r"\[!\[[^\[\]]*\]\([^\(\)]*\)\]\([^\(\)]*\)", content):
            match: str
            content = content.replace(
                match, f'<a style="color:{blueColor}" href="{match.split("(")[-1][:-1]}">{match.split("]")[0][3:]}</a>')

        # Convert unlinked images to URLs
        for match in re.findall(r"!\[[^\[\]]*\]\([^\(\)]*\)", content):
            match: str
            content = content.replace(
                match, f'<a style="color:{blueColor}" href="{match.split("]")[1][1:-1]}">{match.split("]")[0][2:]}</a>')

        # Convert URLs to <a href=></a> tags
        for match in re.findall(r"\[[^\[\]]*\]\([^\(\)]*\)", content):
            match: str
            content = content.replace(
                match, f'<a style="color:{blueColor}" href="{match.split("]")[1][1:-1]}">{match.split("]")[0][1:]}</a>')

        i = 0
        linelist = content.split("<br>")
        while i < len(linelist):
            line = linelist[i]

            for match in re.findall(r"\*\*[^ ][^\*]+[^ ]\*\*", line):
                line = line.replace(match, "<b>" + match[2:-2] + "</b>")

            for match in re.findall(r"\*[^ ][^\*]+[^ ]\*", line):
                line = line.replace(match, "<i>" + match[1:-1] + "</i>")

            for match in re.findall(r"`[^ ][^`]+[^ ]`", line):
                line = line.replace(
                    match, f"<span style='font-family: \"Consolas\";background-color:{'#303030' if isDark() else '#eeeeee'}'>" + match[1:-1] + "</span>")

            linelist[i] = line
            i += 1

        content = "<br>".join(linelist)

        content = content.replace("<br></b>", "</b><br>")

        # Convert unordered lists
        content = content.replace("<br>- ", f"<br>{'&nbsp;'*4}● ")
        content = content.replace("<br> - ", f"<br>{'&nbsp;'*4}● ")
        content = content.replace("<br>  - ", f"<br>{'&nbsp;'*8}○ ")
        content = content.replace("<br>   - ", f"<br>{'&nbsp;'*8}○ ")
        content = content.replace("<br>    - ", f"<br>{'&nbsp;'*12}□ ")
        content = content.replace("<br>     - ", f"<br>{'&nbsp;'*12}□ ")
        content = content.replace("<br>* ", f"<br>{'&nbsp;'*4}● ")
        content = content.replace("<br> * ", f"<br>{'&nbsp;'*4}● ")
        content = content.replace("<br>  * ", f"<br>{'&nbsp;'*8}○ ")
        content = content.replace("<br>   * ", f"<br>{'&nbsp;'*8}○ ")
        content = content.replace("<br>    * ", f"<br>{'&nbsp;'*12}□ ")
        content = content.replace("<br>     * ", f"<br>{'&nbsp;'*12}□ ")

        # Convert numbered lists
        for number in range(0, 20):
            content = content.replace(
                f"<br>{number}. ", f"<br>{'&nbsp;'*4}{number}. ")
            content = content.replace(
                f"<br> {number}. ", f"<br>{'&nbsp;'*4}{number}. ")

        # Filter empty newlines
        content = content.replace(
            "<br><br>", "<br>").replace("<br><br>", "<br>")
        print(content)
        return content
    except Exception as e:
        report(e)
        return content

