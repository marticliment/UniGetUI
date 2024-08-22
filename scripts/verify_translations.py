import glob
import os
import re
import sys


def eprint(*args, **kwargs):
    print(*args, file=sys.stderr, **kwargs)


try:
    sys.stdout.reconfigure(encoding="utf-8")
    Correct = True

    os.chdir(os.path.dirname(__file__))
    os.chdir("../src/UniGetUI.Core.LanguageEngine/Assets/Languages")
    for FILE in glob.glob("./lang_*.json"):
        with open(FILE, "r", encoding="utf-8") as f:
            # print(f"Begin analyzing file {FILE}")

            for LINE in f.readlines():
                LINE = LINE.replace('\\"', "'")

                if len(LINE) <= 2 or LINE.removesuffix("\n").removesuffix(",").endswith(
                    "null"
                ):
                    continue

                results = re.match(r'^ +"([^"]+)" ?: ?"([^"]+)"', LINE)
                BASE, COMPARE = results[1], results[2]

                for find in re.findall(r"{[a-zA-Z0-9]+}", BASE):
                    if BASE.count(find) > COMPARE.count(find):
                        Correct = False
                        print(
                            f'Faulting line on file {FILE}, missing key is {find} on translation "{BASE}", with translation {COMPARE}'
                        )

    sys.exit(0 if Correct else 1)

except Exception as e:
    print(e)
    input("Press <ENTER> to close...")
