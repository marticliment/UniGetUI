import requests, time

try:
    import os, sys, json
    
    urls = []

    #with open("invalid_urls.txt", "w") as f:
    #    f.write("")

    with open("screenshot-database-v2.json") as f:
        data = json.load(f)
        for package in data["icons_and_screenshots"]:
            try:
                if package <= "nosqlworkbench":
                    continue
                if data["icons_and_screenshots"][package]["icon"] != "":
                    print("Package:", package, data["icons_and_screenshots"][package]["icon"])
                    response = requests.get(data["icons_and_screenshots"][package]["icon"])
                    if response.status_code == 404:
                        print("Package failed:", package, data["icons_and_screenshots"][package]["icon"])
                        with open("invalid_urls.txt", "a") as f:
                            f.write(data["icons_and_screenshots"][package]["icon"] + "\n")
                    elif response.status_code != 200 and response.status_code != 403:
                        print(response.status_code, "failed for:", data["icons_and_screenshots"][package]["icon"])

            except requests.exceptions.ConnectionError:
                time.sleep(0.1)
                try:
                    if data["icons_and_screenshots"][package]["icon"] != "":
                        response = requests.get(data["icons_and_screenshots"][package]["icon"])
                        if response.status_code == 403 or response.status_code == 404:
                            print("Package failed:", package, data["icons_and_screenshots"][package]["icon"])
                        elif response.status_code != 200:
                            response = requests.get(data["icons_and_screenshots"][package]["icon"])
                            if response.status_code != 200:
                                print("Failed to resolve DNS for:", data["icons_and_screenshots"][package]["icon"])
                except requests.exceptions.ConnectionError as e:
                    print(type(e))



except Exception as e:
    print(e)
os.system("pause")
