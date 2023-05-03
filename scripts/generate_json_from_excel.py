import os, xlrd, json
from urllib.request import urlopen

root_dir = os.path.join(os.path.dirname(__file__), "..")
os.chdir(os.path.join(root_dir, "WebBasedData"))

try:
    os.remove("screenshot-database-v2.json")
except FileNotFoundError:
    pass
try:
    os.remove("screenshot_database.xlsx")
except FileNotFoundError:
    pass

with open("screenshot_database.xlsx", "wb") as f:
    f.write(urlopen("https://docs.google.com/spreadsheets/d/1Zxgzs1BiTZipC7EiwNEb9cIchistIdr5/export?format=xlsx").read())

try:
    workbook = xlrd.open_workbook('screenshot_database.xlsx')
except:
    os.system("python -m pip install xlrd==1.2.0")
    import xlrd
    workbook = xlrd.open_workbook('screenshot_database.xlsx')
    
worksheet = workbook.sheet_by_index(0)

jsoncontent = {
    "package_count": {
        "total": 0,
        "done": 0
    },
    "icons_and_screenshots": {},
}

totalCount = 0
doneCount = 0
arrivedAtTheEnd = False
i = 2
while not arrivedAtTheEnd:
    try:
        data = [worksheet.cell_value(i, 0), worksheet.cell_value(i, 1), []]
        j = 2
        while worksheet.cell_value(i, j) != "" and worksheet.cell_value(i, j) != None:
            data[2].append(worksheet.cell_value(i, j))
            j += 1
            if j >22:
                break
        assert (type(data) == list)
        assert (len(data) == 3)
        assert (type(data[0]) == str)
        assert (type(data[1]) == str)
        assert (type(data[2]) == list)
        if data[1] != "":
            doneCount += 1
        
        if not data[0] in jsoncontent["icons_and_screenshots"].keys():
            jsoncontent["icons_and_screenshots"][data[0]] = {
                "icon": data[1],
                "images": data[2]
            }
        else:
            jsoncontent["icons_and_screenshots"][data[0]] = {
                "icon": data[1] if jsoncontent["icons_and_screenshots"][data[0]]["icon"] == "" else jsoncontent["icons_and_screenshots"][data[0]]["icon"],
                "images": data[2] if jsoncontent["icons_and_screenshots"][data[0]]["images"] == [] else jsoncontent["icons_and_screenshots"][data[0]]["images"]
            }
        totalCount += 1
        i += 1
    except IndexError as e:
        arrivedAtTheEnd = True

jsoncontent["package_count"]["total"] = totalCount
jsoncontent["package_count"]["done"] = doneCount

with open("screenshot-database-v2.json", "w") as outfile:
    json.dump(jsoncontent, outfile, indent=4)
 


os.system("pause")
