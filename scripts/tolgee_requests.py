import json
import os

try:
    import requests
except ImportError:
    os.system("pip install requests")
    import requests

__project_id = 1  # UniGetUI
__api_url = f"https://tolgee.marticliment.com/v2/projects/{__project_id}"
__api_key = ""
__headers: dict[str, str] = {}
__all_keys: dict = None

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


def check_api_key():
    url = f"{__api_url}/activity"
    response = requests.get(url, headers=__headers)
    if not response.ok:
        print("Issue with API key!")
        print("Error", response.status_code, response.json().get("error"))
        exit(1)


def export(format="JSON", zip=True, langs: list[str] = []):
    url = f"{__api_url}/export"
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
    response = requests.get(url, headers=__headers, params=params)
    return response


def create_key(key):
    url = f"{__api_url}/keys/create"
    json: dict[str, str] = {"name": key}
    response = requests.post(url, headers=__headers, json=json)
    return response


def get_keys():
    global __all_keys
    if __all_keys:
        return __all_keys
    url = f"{__api_url}/keys"
    params = {"size": 1000}
    response = requests.get(url, headers=__headers, params=params)
    if not response.ok:
        return False
    data = json.loads(response.text)
    retValue = {}
    for value in data["_embedded"]["keys"]:
        retValue[value["name"]] = value
    __all_keys = retValue
    return retValue


def delete_key(key):
    all_keys = get_keys()
    key_data = all_keys.get(key)
    if not key_data:
        return False
    id = key_data.get("id")
    url = f"{__api_url}/keys"
    json: dict[str, str] = {
        "ids": [id],
    }
    response = requests.delete(url, headers=__headers, json=json)
    return response


check_api_key()
