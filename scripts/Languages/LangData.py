"""

UniGetUI/lang/lang_tools.py

This file contains a list of the available languages and other related information

"""

import os
import json

if os.path.exists("../src/UniGetUI.Core.Data/Assets/Data/Contributors.list"):
    f = open("../src/UniGetUI.Core.Data/Assets/Data/Contributors.list", "r", encoding="utf-8")
    contributors = f.readlines()
else:
    print("No contributors file!")
    contributors = []

if os.path.exists("../src/UniGetUI.Core.LanguageEngine/Assets/Data/Translators.json"):
    f = open("../src/UniGetUI.Core.LanguageEngine/Assets/Data/Translators.json", "r", encoding="utf-8")
    languageCredits = json.load(f)
else:
    print("No translators file!")
    languageCredits = {}

if os.path.exists("../src/UniGetUI.Core.LanguageEngine/Assets/Data/TranslatedPercentages.json"):
    f = open("../src/UniGetUI.Core.LanguageEngine/Assets/Data/TranslatedPercentages.json", "r", encoding="utf-8")
    untranslatedPercentage = json.load(f)
else:
    print("No translated percent file!")
    untranslatedPercentage = {}

if os.path.exists("../src/UniGetUI.Core.LanguageEngine/Assets/Data/LanguagesReference.json"):
    f = open("../src/UniGetUI.Core.LanguageEngine/Assets/Data/LanguagesReference.json", "r", encoding="utf-8")
    languageReference = json.load(f)
else:
    print("No translated percent file!")
    languageReference = {}

languageRemap = {
    "pt-BR": "pt_BR",
    "pt-PT": "pt_PT",
    "nn-NO": "nn",
    "uk": "ua",
    "zh-Hans": "zh_CN",
    "zh-Hant": "zh_TW",
}


# ISO 3166-1
languageFlagsRemap = {
    "ar": "sa",
    "bs": "ba",
    "ca": "ad",
    "cs": "cz",
    "da": "dk",
    "el": "gr",
    "en": "gb",
    "et": "ee",
    "fa": "ir",
    "he": "il",
    "hi": "in",
    "ja": "jp",
    "ko": "kr",
    "nb": "no",
    "nn": "no",
    "pt_BR": "br",
    "pt_PT": "pt",
    "si": "lk",
    "sr": "rs",
    "sv": "se",
    "sl": "si",
    "vi": "vn",
    "zh_CN": "cn",
    "zh_TW": "tw",
    "zh": "cn",
    "bn": "bd",
    "tg": "ph",
    "sq": "al",
    "kn": "in",
    "sa": "in"
}


def getMarkdownSupportLangs():

    readmeLangs = [
        "| Language | Translated | Translator(s) |",
        "| :-- | :-- | --- |",
    ]

    dir = os.path.dirname(__file__)
    for lang, langName in languageReference.items():
        if (not os.path.exists(f"{dir}/../../src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_{lang}.json")):
            continue

        perc = untranslatedPercentage[lang] if (lang in untranslatedPercentage) else "100%"
        if (perc == "0%"):
            continue

        langName = languageReference[lang] if (lang in languageReference) else lang
        flag = languageFlagsRemap[lang] if (lang in languageFlagsRemap) else lang
        credits = makeURLFromTranslatorList(languageCredits[lang] if (lang in languageCredits) else "")
        readmeLangs.append(f"| <img src='https://flagcdn.com/{flag}.svg' width=20> &nbsp; {langName} | {perc} | {credits} |")
    readmeLangs.append("")

    return "\n".join(readmeLangs)


def getTranslatorsFromCredits(translators: str) -> list:
    if translators is None:
        return []
    credits: list = []
    translatorList = []
    translatorData = {}
    for translator in translators.split(","):
        translatorStriped = translator.strip()
        if (translatorStriped != ""):
            translatorPrefixed = (translatorStriped[0] == "@")
            if (translatorPrefixed):
                translatorStriped = translatorStriped[1:]
            link = ""
            if (translatorPrefixed or translatorStriped in contributors):
                link = f"https://github.com/{translatorStriped}"
            translatorList.append(translatorStriped)
            translatorData[translatorStriped] = {
                "name": translatorStriped,
                "link": link,
            }
    translatorList.sort(key=str.casefold)
    for translator in translatorList:
        credits.append(translatorData[translator])
    return credits


def makeURLFromTranslatorList(translators: list) -> str:
    if translators is None:
        return ""
    credits: list[str] = []
    for translator in translators:
        link = translator.get("link")
        name = translator.get("name")
        if (link):
            credits.append(f"[{name}]({link})")
        else:
            credits.append(name)
    return ", ".join(credits)
