import argparse
import tolgee_requests
import translation_utils
import json
from time import sleep


__parser = argparse.ArgumentParser()
__parser.add_argument("--export-all", help="Export not used and not translated strings", action="store_true")
__parser.add_argument("--print-only", help="Print not translated strings", action="store_true")
__args = __parser.parse_args()


def encode_str(str: str, strip = 0):
    new_str = str
    if strip > 0:
        new_str = str[:strip].strip()
    return new_str.encode("utf-8")


def upload(strs: list[str]):
    count = len(strs)
    i = 1
    for key in strs:
        print("[{num}/{count}] Uploading key... ".format(num=i, count=count))
        print(encode_str(key, strip=100), "... ", end="")
        response = tolgee_requests.create_key(key)
        if (not response.ok):
            print("Failed")
            print("Error", response.status_code, response.text)
            return
        else:
            print("Ok")
        sleep(0.150)
        i += 1
    print("Done")


def print_only(strs: list[str]):
    for key in strs:
        print(encode_str(key))


def __export():
    f = open("../lang_compare.json", "w", encoding="utf-8")
    output = translation_utils.compare_strings()
    json.dump(output, f, ensure_ascii=False, indent="  ")
    f.close()


def __init__():
    strs = translation_utils.compare_strings()
    key_name = "not_translated"
    if key_name in strs:
        stringsFound = len(strs[key_name])
        print("Found not translated strings: {count}".format(count=stringsFound))
        if stringsFound > 0:
            if __args.print_only:
                print_only(strs[key_name])
            else:
                sleep(1)
                upload(strs[key_name])
    else:
        print(f"Key '{key_name}' missing")


if (__args.export_all):
    __export()
    exit()

__init__()
