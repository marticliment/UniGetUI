import argparse
import json
from time import sleep

import tolgee_requests
import translation_utils

__parser = argparse.ArgumentParser()
__group = __parser.add_mutually_exclusive_group(required=True)
__group.add_argument(
    "-p", "--print", help="Print unused and not translated strings", action="store_true"
)
__group.add_argument(
    "-c", "--create", help="Create not translated strings", action="store_true"
)
__group.add_argument(
    "-d", "--delete", help="Delete unused strings", action="store_true"
)
__parser.add_argument(
    "--online", help="Compare with Tolgee translations via API", action="store_true"
)
__parser.add_argument("-y", "--yes", help="All answers are YES", action="store_true")
__args = __parser.parse_args()


def __confirm(message: str, choices: list[str], defaultValue=""):

    def createChoices():
        _choices: list[str] = []
        for key in choices:
            if key == defaultValue:
                key = key.upper()
            _choices.append(key)
        return "/".join(_choices)

    try:
        return (input(f"{message} [{createChoices()}]: ") or defaultValue).lower()
    except KeyboardInterrupt:
        exit(1)


def encode_str(str: str, strip=0):
    new_str = str
    if strip > 0:
        new_str = str[:strip].strip()
    return new_str.encode("utf-8")


def create(strs: list[str]):
    count = len(strs)
    i = 1
    for key in strs:
        print(f"[{i}/{count}] Key: {encode_str(key, strip=100)}")
        i += 1
        if not __args.yes and __confirm("Create?", ["y", "n"], "y") != "y":
            continue
        print("Creating key... ", end="")
        response = tolgee_requests.create_key(key)
        if not response.ok:
            print("Failed")
            print("Error", response.status_code, response.text)
            return
        else:
            print("Ok")
        sleep(0.150)
    print("Done")


def delete(strs: list[str]):
    count = len(strs)
    i = 1
    for key in strs:
        print(f"[{i}/{count}] Key: {encode_str(key, strip=100)}")
        i += 1
        if not __args.yes and __confirm("Delete?", ["y", "n"], "y") != "y":
            continue
        print("Deleting key... ", end="")
        response = tolgee_requests.delete_key(key)
        if not response.ok:
            print("Failed")
            print("Error", response.status_code, response.text)
            return
        else:
            print("Ok")
        sleep(0.150)
    print("Done")


def __print(strs: list[str]):
    output = json.dumps(strs, ensure_ascii=False, indent="  ")
    print(output)


def __print_all():
    output = json.dumps(
        translation_utils.compare_strings(online=__args.online),
        ensure_ascii=False,
        indent="  ",
    )
    print(output)


def __delete(strs):
    key_name = "not_used"
    if key_name in strs:
        stringsFound = len(strs[key_name])
        print(f"Found not used strings: {stringsFound}")
        if stringsFound > 0:
            sleep(1)
            delete(strs[key_name])
    else:
        print(f"Key '{key_name}' missing")


def __create(strs):
    key_name = "not_translated"
    if key_name in strs:
        stringsFound = len(strs[key_name])
        print(f"Found not translated strings: {stringsFound}")
        if stringsFound > 0:
            if __args.print:
                __print(strs[key_name])
            else:
                sleep(1)
                create(strs[key_name])
    else:
        print(f"Key '{key_name}' missing")


def __init__():
    strs = translation_utils.compare_strings(online=__args.online)
    print("Online mode:", __args.online)

    if __args.print:
        return __print_all()

    if __args.create:
        __create(strs)
        return

    if __args.delete:
        __delete(strs)
        return


__init__()
