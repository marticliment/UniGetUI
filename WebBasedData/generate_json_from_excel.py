import os, xlrd, json
from urllib.request import urlopen

os.chdir(os.path.dirname(__file__))

try:
    os.remove("screenshot-database.json")
except FileNotFoundError:
    pass
try:
    os.remove("screenshot_database.xlsx")
except FileNotFoundError:
    pass

with open("screenshot_database.xlsx", "wb") as f:
    f.write(urlopen("https://docs.google.com/spreadsheets/d/1Zxgzs1BiTZipC7EiwNEb9cIchistIdr5/export?format=xlsx").read())

workbook = xlrd.open_workbook('screenshot_database.xlsx')
worksheet = workbook.sheet_by_index(0)

jsoncontent = {
    "package_count": {
        "winget": 0,
        "scoop": 0,
        "total": 0,
        "done": 0
    },
    "winget": {},
    "scoop": {}
}

scoopCount = 0
wingetCount = 0
doneCount = 0
arrivedAtTheEnd = False
i = 2
while not arrivedAtTheEnd:
    try:
        data = [worksheet.cell_value(i, 0).lower(), worksheet.cell_value(i, 1), worksheet.cell_value(i, 2), []]
        j = 3
        while worksheet.cell_value(i, j) != "" and worksheet.cell_value(i, j) != None:
            data[3].append(worksheet.cell_value(i, j))
            j += 1
            if j >23:
                break
        assert (type(data) == list)
        assert (len(data) == 4)
        assert (type(data[1]) == str)
        assert (type(data[1]) == str)
        assert (type(data[2]) == str)
        assert (type(data[3]) == list)
        if data[2] != "":
            doneCount += 1
        if data[0] == "winget":
            wingetCount += 1
            jsoncontent["winget"][data[1]] = {
                "icon": data[2],
                "images": data[3]
            }
        elif data[0] == "scoop":
            scoopCount += 1
            jsoncontent["scoop"][data[1]] = {
                "icon": data[2],
                "images": data[3]
            }

        i += 1
        if data[0] == "" or data[0] == None:
            arrivedAtTheEnd = True
    except IndexError:
        arrivedAtTheEnd = True

jsoncontent["package_count"]["winget"] = wingetCount
jsoncontent["package_count"]["scoop"] = scoopCount
jsoncontent["package_count"]["total"] = scoopCount+wingetCount
jsoncontent["package_count"]["done"] = doneCount

with open("screenshot-database.json", "w") as outfile:
    json.dump(jsoncontent, outfile, indent=4)
 


os.system("pause")