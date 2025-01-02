import json
import os
from urllib.request import urlopen

import xlrd

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
    os.system("python -m pip install xlrd==1.0.0")
    import xlrd
    workbook = xlrd.open_workbook('screenshot_database.xlsx')

worksheet = workbook.sheet_by_index(0)

jsoncontent = {
    "package_count": {
        "total": 0,
        "done": 0,
        "packages_with_icon": 0,
        "packages_with_screenshot": 0,
        "total_screenshots": 0,
    },
    "icons_and_screenshots": {},
}

with open("invalid_urls.txt", "r") as f:
    forbidden_string = f.read().split("\n")

totalCount = 0
doneCount = 0
packagesWithIcon = 0
packagesWithScreenshot = 0
screenshotCount = 0
arrivedAtTheEnd = False
i = 1
while not arrivedAtTheEnd:
    try:
        data = [worksheet.cell_value(i, 0), worksheet.cell_value(i, 1), []]
        if len(worksheet.row_values(i)) >= 3:
            packagesWithScreenshot += 1
            j = 2
            while j < len(worksheet.row_values(i)):
                if worksheet.cell_value(i, j) is None or worksheet.cell_value(i, j) == "":
                    if j == 2:
                        packagesWithScreenshot -= 1
                    break
                data[2].append(worksheet.cell_value(i, j))
                screenshotCount += 1
                j += 1
                if j > 23:
                    break
        assert (type(data) == list)
        assert (len(data) == 3)
        try:
            assert (type(data[0]) == str)
        except AssertionError as e:
            if data[0] == 115.0:
                data[0] = "115"
            else:
                raise e
        assert (type(data[1]) == str)
        assert (type(data[2]) == list)
        if data[1] != "":
            if(data[1] in forbidden_string):
                data[1] = ""
            else:
                doneCount += 1
                packagesWithIcon += 1

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
jsoncontent["package_count"]["packages_with_icon"] = packagesWithIcon
jsoncontent["package_count"]["packages_with_screenshot"] = packagesWithScreenshot
jsoncontent["package_count"]["total_screenshots"] = screenshotCount

with open("screenshot-database-v2.json", "w") as outfile:
    json.dump(jsoncontent, outfile, indent=4)



os.system("pause")
