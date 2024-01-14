"""

wingetui/lang/lang_tools.py

This file contains a list of the available languages and other related information

"""

import os
from wingetui.Core.Data.Contributors import contributors
from wingetui.Core.Data.Translations import languageCredits, untranslatedPercentage

languageReference = {
    "default": "System language",
    "ar"    : "Arabic - عربي‎",
    "bg"    : "Bulgarian - български",
    "bn"    : "Bangla - বাংলা",
    "ca"    : "Catalan - Català",
    "cs"    : "Czech - Čeština",
    "da"    : "Danish - Dansk",
    "de"    : "German - Deutsch",
    "el"    : "Greek - Ελληνικά",
    "en"    : "English - English",
    "es"    : "Spanish - Castellano",
    "fa"    : "Persian - فارسی‎",
    "fr"    : "French - Français",
    "hi"    : "Hindi - हिंदी",
    "hr"    : "Croatian - Hrvatski",
    "he"    : "Hebrew - עִבְרִית‎",
    "hu"    : "Hungarian - Magyar",
    "it"    : "Italian - Italiano",
    "id"    : "Indonesian - Bahasa Indonesia",
    "ja"    : "Japanese - 日本語",
    "ko"    : "Korean - 한국어",
    "mk"    : "Macedonian - Македонски",
    "nb"    : "Norwegian (bokmål)",
    "nl"    : "Dutch - Nederlands",
    "pl"    : "Polish - Polski",
    "pt_BR" : "Portuguese (Brazil)",
    "pt_PT" : "Portuguese (Portugal)",
    "ro"    : "Romanian - Română",
    "ru"    : "Russian - Русский",
    "sr"    : "Serbian - Srpski",
    "si"    : "Sinhala - සිංහල",
    "sl"    : "Slovene - Slovenščina",
    "sv"      : "Swedish - Svenska",
    "tg"    : "Tagalog - Tagalog",
    "th"    : "Thai - ภาษาไทย",
    "tr"    : "Turkish - Türkçe",
    "ua"    : "Ukranian - Yкраї́нська",
    "vi"    : "Vietnamese - Tiếng Việt",
    "zh_CN" : "Simplified Chinese (China)",
    "zh_TW" : "Traditional Chinese (Taiwan)",
}


languageRemap = {
    "pt-BR":   "pt_BR",
    "pt-PT":   "pt_PT",
    "uk":      "ua",
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
    "tg": "ph"
}


def getMarkdownSupportLangs():

    readmeLangs = [
        "| Language | Translated | Translator(s) |",
        "| :-- | :-- | --- |",
    ]

    dir = os.path.dirname(__file__)
    for lang, langName in languageReference.items():
        if (not os.path.exists(f"{dir}/lang_{lang}.json")): continue
        perc = untranslatedPercentage[lang] if (lang in untranslatedPercentage) else "100%"
        if (perc == "0%"): continue
        langName = languageReference[lang] if (lang in languageReference) else lang
        flag = languageFlagsRemap[lang] if (lang in languageFlagsRemap) else lang
        credits = makeURLFromTranslatorList(languageCredits[lang] if (lang in languageCredits) else "")
        readmeLangs.append(f"| <img src='https://flagcdn.com/{flag}.svg' width=20> &nbsp; {langName} | {perc} | {credits} |")
    readmeLangs.append("")

    return "\n".join(readmeLangs)


def getTranslatorsFromCredits(translators: str) -> list:
    if translators == None:
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
    if translators == None:
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
