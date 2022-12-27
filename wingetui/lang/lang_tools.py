from os.path import exists
from pathlib import Path


languageReference = {
    "default": "System language",
    "ar"    : "Arabic - عربي‎",
    "bn"    : "Bangla - বাংলা",
    "ca"    : "Catalan - Català",
    "cs"    : "Czech - Čeština",
    "de"    : "German - Deutsch",
    "en"    : "English - English",
    "fr"    : "French - Français",
    "hi"    : "Hindi - हिंदी",
    "hr"    : "Croatian - Hrvatski",
    "hu"    : "Hungarian - Magyar",
    "it"    : "Italian - Italiano",
    "ja"    : "Japanese - 日本語",
    "pl"    : "Polish - Polski",
    "pt_BR" : "Portuguese (Brazil)",
    "pt_PT" : "Portuguese (Portugal)",
    "ru"    : "Russian - Русский",
    "sr"    : "Serbian - Srpski",
    "es"    : "Spanish - Castellano",
    "tr"    : "Turkish - Türkçe",
    "ua"    : "Ukranian - Yкраї́нська",
    "zh_CN" : "Simplified Chinese (China)",
    "zh_TW" : "Traditional Chinese (Taiwan)",
}


languageRemap = {
    "pt-PT":      "pt_PT",
    "pt-BR":      "pt_BR",
    "uk":         "ua",
    "zh-Hant": "zh_TW",
    "zh-Hans": "zh_CN",
}


# ISO 3166-1
languageFlagsRemap = {
    "ar": "sa",
    "bs": "ba",
    "ca": "ad",
    "cs": "cz",
    "da": "dk",
    "en": "gb",
    "el": "gr",
    "et": "ee",
    "fa": "ir",
    "he": "il",
    "ja": "jp",
    "hi": "in",
    "ko": "kr",
    "nb": "no",
    "nn": "no",
    "pt_BR": "br",
    "pt_PT": "pt",
    "si": "lk",
    "zh": "cn",
    "zh_CN": "cn",
    "zh_TW": "tw",
    "vi": "vn",
    "sr": "rs",
    "sv": "se",
}


def getMarkdownSupportLangs():
    from translated_percentage import untranslatedPercentage, languageCredits

    readmeLangs = [
        "| Language | Translated | Translator(s) |",
        "| :-- | :-- | --- |",
    ]

    dir = str(Path(__file__).parent)
    for lang, langName in languageReference.items():
        if (not exists(f"{dir}/lang_{lang}.json")): continue
        perc = untranslatedPercentage[lang] if (lang in untranslatedPercentage) else "100%"
        if (perc == "0%"): continue
        langName = languageReference[lang] if (lang in languageReference) else lang
        flag = languageFlagsRemap[lang] if (lang in languageFlagsRemap) else lang
        credits = makeURLFromTranslatorList(languageCredits[lang] if (lang in languageCredits) else "")
        readmeLangs.append(f"| <img src='https://flagcdn.com/{flag}.svg' width=20> &nbsp; {langName} | {perc} | {credits} |")
    readmeLangs.append("")

    return "\n".join(readmeLangs)


def fixTranslatorList(names: str) -> str:
    if names == None:
        return ""
    credits: list[str] = []
    for name in names.split(","):
        nameStriped = name.strip()
        if (nameStriped != ""):
            credits.append(nameStriped)
    credits.sort(key=str.casefold)
    return ", ".join(credits)


def makeURLFromTranslatorList(names: str) -> str:
    if names == None:
        return ""
    credits: list[str] = []
    for name in names.split(","):
        nameStriped = name.strip()
        if (nameStriped != ""):
            if (nameStriped[0] == "@"):
                credits.append(f"[{nameStriped[1:]}](https://github.com/{nameStriped[1:]})")
            else:
                credits.append(nameStriped)
    return ", ".join(credits)
