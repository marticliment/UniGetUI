import os
try:
    import requests
except ImportError:
    os.system("pip install requests")
    import requests


__project_id = 1205 # wingetui
__api_url = f"https://app.tolgee.io/v2/projects/{__project_id}"
__api_key = ""


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


def export(format = "JSON", zip = True):
    isZip = "true" if zip else "false"
    url = f"{__api_url}/export?format={format}&structureDelimiter=&filterState=UNTRANSLATED&filterState=TRANSLATED&filterState=REVIEWED&zip={isZip}"
    response = requests.get(url, headers={"X-API-Key": __api_key})
    return response


def create_key(key):
    url = f"{__api_url}/keys/create"
    json: dict[str, str] = {
        "name": key
    }
    response = requests.post(url, headers={"X-API-Key": __api_key}, json=json)
    return response
