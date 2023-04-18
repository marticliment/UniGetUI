import os
try:
    import requests
except ImportError:
    os.system("pip install requests")
    import requests


__project_id = 1205 # wingetui
__api_url = f"https://app.tolgee.io/v2/projects/{__project_id}"
__api_key = ""
__headers: dict[str, str] = {}

try:
    with open("APIKEY.txt", "r") as f:
        __api_key = f.read().strip()
        if not __api_key:
            raise ValueError("APIKEY.txt is empty")
        # print("API key found in APIKEY.txt")
except FileNotFoundError:
    __api_key = os.environ.get("TOLGEE_KEY", "")
    if not __api_key:
        __api_key = input("Write api key and press enter: ")
__headers["X-API-Key"] = __api_key


def export(format = "JSON", zip = True, langs: list[str] = []):
    params = {
        "format": format,
        "languages": langs,
        "structureDelimiter": "",
        "filterState": [
            "REVIEWED",
            "TRANSLATED",
            "UNTRANSLATED",
        ],
        "zip": zip,
    }
    response = requests.get(f"{__api_url}/export", headers=__headers, params=params)
    return response


def create_key(key):
    url = f"{__api_url}/keys/create"
    json: dict[str, str] = {
        "name": key
    }
    response = requests.post(url, headers=__headers, json=json)
    return response
