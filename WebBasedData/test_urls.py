import requests
import time
import json
import os

def write_invalid_url(file_path, url):
    with open(file_path, "a") as f:
        f.write(url + "\n")

def get_icon_url(package_data):
    return package_data.get("icon", "")

def get_status_code_for_url(url):
    try:
        response = requests.get(url, timeout=60)
        return response.status_code
    except requests.exceptions.ConnectionError:
        time.sleep(0.1)  # If a ConnectionError occurs, wait and try again
        return get_status_code_for_url(url)

def package_failed(status_code):
    return status_code == 404

def check_icons_and_screenshots(data, filepath, start_after_package):
    for package, contents in data["icons_and_screenshots"].items():
        if package <= start_after_package:
            continue
        
        icon_url = get_icon_url(contents)
        if icon_url:
            status_code = get_status_code_for_url(icon_url)
            
            if package_failed(status_code):
                print(f"Package failed: {package} {icon_url}")
                write_invalid_url(filepath, icon_url)
            elif not is_valid_response(status_code):
                print(f"{status_code} failed for: {icon_url}")

def is_valid_response(status_code):
    return status_code == 200

if __name__ == "__main__":
    invalid_urls_filepath = "invalid_urls.txt"
    json_filepath = "screenshot-database-v2.json"
    start_after_package = "nosqlworkbench"

    try:
        with open(json_filepath, 'r') as json_file:
            data = json.load(json_file)
        check_icons_and_screenshots(data, invalid_urls_filepath, start_after_package)
    except Exception as e:
        print(e)
    finally:
        os.system("pause")
