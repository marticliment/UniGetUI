"""

wingetui/Interface/GenericSections.py

This file contains the code for miscellanious User Interface sections, such as the about tab, the settings and the logs.

"""

if __name__ == "__main__":
    import subprocess
    import os
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "__init__.py"], shell=True, cwd=os.path.join(os.path.dirname(__file__), "..")).returncode)


import glob
import os
import subprocess
import sys
from threading import Thread
import win32mica

import globals
from Interface.CustomWidgets.SpecificWidgets import *
from data.contributors import contributorsInfo
from data.translations import languageCredits, untranslatedPercentage
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from Interface.CustomWidgets.InstallerWidgets import *
from tools import *
from tools import _


class AboutSection(SmoothScrollArea):
    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.setFrameShape(QFrame.NoFrame)
        self.widget = QWidget()
        self.setWidgetResizable(True)
        self.setStyleSheet("margin-left: 0px;")
        self.mainLayout = QVBoxLayout()
        w = QWidget()
        w.setLayout(self.mainLayout)
        w.setMaximumWidth(1300)
        hLayout = QHBoxLayout()
        hLayout.addSpacing(20)
        hLayout.addStretch()
        hLayout.addWidget(w, stretch=0)
        hLayout.addStretch()
        self.widget.setLayout(hLayout)
        self.setWidget(self.widget)
        self.announcements = AnnouncementsPane()
        self.mainLayout.addWidget(self.announcements)
        title = QLabel(_("Component Information"))
        title.setStyleSheet(
            f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        self.mainLayout.addWidget(title)

        self.mainLayout.addSpacing(15)
        try:
            table = QTableWidget()
            table.setAutoFillBackground(True)
            table.setStyleSheet("*{border: 0px solid transparent; background-color: transparent;}QHeaderView{font-size: 13pt;padding: 0px;margin: 0px;}QTableCornerButton::section,QHeaderView,QHeaderView::section,QTableWidget,QWidget,QTableWidget::item{background-color: transparent;border: 0px solid transparent}")
            table.setColumnCount(2)
            table.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
            table.setShowGrid(False)
            table.setEnabled(False)
            table.setHorizontalHeaderLabels([_("Status"), _("Version")])
            table.setColumnWidth(1, 500)
            table.setColumnWidth(0, 150)
            table.verticalHeader().setFixedWidth(100)
            table.setRowCount(len(PackageManagersList) + 1)
            table.setVerticalHeaderLabels(
                ["Gsudo"] + [manager.NAME for manager in PackageManagersList])
            currentIndex: int = 0
            table.setItem(currentIndex, 0, QTableWidgetItem(
                "  " + _("Found") if globals.componentStatus["sudoFound"] else _("Not found")))
            table.setItem(currentIndex, 1, QTableWidgetItem(
                " " + str(globals.componentStatus["sudoVersion"])))

            for manager in PackageManagersList:
                try:
                    currentIndex += 1
                    table.setItem(currentIndex, 0, QTableWidgetItem(
                        "  " + _("Found") if globals.componentStatus[f"{manager.NAME}Found"] else _("Not found")))
                    table.setItem(currentIndex, 1, QTableWidgetItem(
                        " " + str(globals.componentStatus[f"{manager.NAME}Version"])))
                    table.verticalHeaderItem(
                        currentIndex).setTextAlignment(Qt.AlignRight)
                    table.setRowHeight(currentIndex, 35)
                except Exception as e:
                    report(e)
                    currentIndex += 1
                    table.setItem(currentIndex, 0,
                                  QTableWidgetItem("  " + _("Error")))
                    table.setItem(currentIndex, 1,
                                  QTableWidgetItem(" " + str(e)))
                    table.verticalHeaderItem(
                        currentIndex).setTextAlignment(Qt.AlignRight)
                    table.setRowHeight(currentIndex, 35)

            table.horizontalHeaderItem(0).setTextAlignment(Qt.AlignLeft)
            table.setRowHeight(0, 35)
            table.horizontalHeaderItem(1).setTextAlignment(Qt.AlignLeft)
            table.verticalHeaderItem(0).setTextAlignment(Qt.AlignRight)
            table.setCornerWidget(QLabel(""))
            table.setCornerButtonEnabled(False)
            table.setFixedHeight(330)
            table.setEditTriggers(QAbstractItemView.NoEditTriggers)
            table.cornerWidget().setStyleSheet("background: transparent;")
            self.mainLayout.addWidget(table)
            title = QLabel(_("About WingetUI version {0}").format(versionName))
            title.setStyleSheet(
                f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
            self.mainLayout.addWidget(title)
            self.mainLayout.addSpacing(5)
            description = CustomLabel(
                _("The main goal of this project is to create an intuitive UI to manage the most common CLI package managers for Windows, such as Winget and Scoop.") + "\n" + _("This project has no connection with the official {0} project — it's completely unofficial.").format(
                    f"<a style=\"color: {blueColor};\" href=\"https://github.com/microsoft/winget-cli\">Winget</a>")
            )
            self.mainLayout.addWidget(description)
            self.mainLayout.addSpacing(5)
            self.mainLayout.addWidget(CustomLabel(
                f"{_('Homepage')}:   <a style=\"color: {blueColor};\" href=\"https://marticliment.com/wingetui\">https://marticliment.com/wingetui</a>"))
            self.mainLayout.addWidget(CustomLabel(
                f"{_('Repository')}:   <a style=\"color: {blueColor};\" href=\"https://github.com/marticliment/wingetui\">https://github.com/marticliment/wingetui</a>"))
            self.mainLayout.addSpacing(30)

            self.mainLayout.addWidget(CustomLabel(
                f"{_('Contributors')}:", f"font-size: 22pt;font-family: \"{globals.dispfont}\";font-weight: bold;"))
            self.mainLayout.addWidget(CustomLabel(
                _("WingetUI wouldn't have been possible with the help of our dear contributors. Check out their GitHub profile, WingetUI wouldn't be possible without them!")))
            contributorsHTMLList = "<ul>"
            for contributor in contributorsInfo:
                contributorsHTMLList += f"<li><a style=\"color:{blueColor}\" href=\"{contributor.get('link')}\">{contributor.get('name')}</a></li>"
            contributorsHTMLList += "</ul>"
            self.mainLayout.addWidget(CustomLabel(contributorsHTMLList))
            self.mainLayout.addSpacing(15)

            self.mainLayout.addWidget(CustomLabel(
                f"{_('Translators')}:", f"font-size: 22pt;font-family: \"{globals.dispfont}\";font-weight: bold;"))
            self.mainLayout.addWidget(CustomLabel(
                _("WingetUI has not been machine translated. The following users have been in charge of the translations:")))
            translatorsHTMLList = "<ul>"
            translatorList = []
            translatorData: dict[str, str] = {}
            for key, value in languageCredits.items():
                langName = languageReference[key] if (
                    key in languageReference) else key
                for translator in value:
                    link = translator.get("link")
                    name = translator.get("name")
                    translatorLine = name
                    if (link):
                        translatorLine = f"<a style=\"color:{blueColor}\" href=\"{link}\">{name}</a>"
                    translatorKey = f"{name}{langName}"  # for sort
                    translatorList.append(translatorKey)
                    translatorData[translatorKey] = f"{translatorLine} ({langName})"
            translatorList.sort(key=str.casefold)
            for translator in translatorList:
                translatorsHTMLList += f"<li>{translatorData[translator]}</li>"
            translatorsHTMLList += "</ul><br>"
            translatorsHTMLList += _("Do you want to translate WingetUI to your language? See how to contribute <a style=\"color:{0}\" href=\"{1}\"a>HERE!</a>").format(
                blueColor, "https://github.com/marticliment/WingetUI/wiki#translating-wingetui")
            self.mainLayout.addWidget(CustomLabel(translatorsHTMLList))
            self.mainLayout.addSpacing(15)

            self.mainLayout.addWidget(CustomLabel(
                f"{_('About the dev')}:", f"font-size: 22pt;font-family: \"{globals.dispfont}\";font-weight: bold;"))
            self.mainLayout.addWidget(CustomLabel(
                _("Hi, my name is Martí, and i am the <i>developer</i> of WingetUI. WingetUI has been entirely made on my free time!")))
            try:
                self.mainLayout.addWidget(CustomLabel(_("Check out my {0} and my {1}!").format(
                    f"<a style=\"color:{blueColor}\" href=\"https://github.com/marticliment\">{_('GitHub profile')}</a>", f"<a style=\"color:{blueColor}\" href=\"http://www.marticliment.com\">{_('homepage')}</a>")))
            except Exception as e:
                print(e)
                print(blueColor)
                print(_('homepage'))
                print(_('GitHub profile'))
            self.mainLayout.addWidget(CustomLabel(_("Do you find WingetUI useful? You'd like to support the developer? If so, you can {0}, it helps a lot!").format(
                f"<a style=\"color:{blueColor}\" href=\"https://ko-fi.com/martinet101\">{_('buy me a coffee')}</a>")))

            self.mainLayout.addSpacing(15)
            self.mainLayout.addWidget(CustomLabel(
                f"{_('Licenses')}:", f"font-size: 22pt;font-family: \"{globals.dispfont}\";font-weight: bold;"))

            licensesTable = QTableWidget()
            licensesTable.setAutoFillBackground(True)
            licensesTable.setStyleSheet(
                "*{border: 0px solid transparent; background-color: transparent;}QHeaderView{font-size: 13pt;padding: 0px;margin: 0px;}QTableCornerButton::section,QHeaderView,QHeaderView::section,QTableWidget,QWidget,QTableWidget::item{background-color: transparent;border: 0px solid transparent}")
            licensesTable.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
            licensesTable.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
            licensesTable.setShowGrid(False)
            licensesTable.horizontalHeader().hide()
            licensesTable.verticalHeader().hide()
            licensesTable.verticalHeader().setFixedWidth(30)
            licensesTable.setColumnCount(3)
            licensesTable.setColumnWidth(0, 120)
            licensesTable.setColumnWidth(1, 120)
            licensesTable.setColumnWidth(2, 460)
            licensesTable.setEditTriggers(QAbstractItemView.NoEditTriggers)

            licensesTable.setRowHeight(0, 35)
            licensesTable.setCornerWidget(QLabel(""))
            licensesTable.setCornerButtonEnabled(False)
            licensesTable.cornerWidget().setStyleSheet("background: transparent;")

            from data.licenses import licenses, licenseUrls

            licensesTable.setRowCount(len(list(licenses.keys())))
            licensesTable.setFixedHeight(len(list(licenses.keys())) * 32)

            i = 0
            for library in licenses.keys():
                licensesTable.setItem(i, 0, QTableWidgetItem(library))
                licensesTable.setItem(
                    i, 1, QTableWidgetItem(licenses[library]))
                licensesTable.setCellWidget(i, 2, CustomLabel(
                    f'<a style="color: {blueColor};" href="{licenseUrls[library]}">{licenseUrls[library]}</a>'))
                i += 1

            self.mainLayout.addWidget(licensesTable)

            button = QPushButton(_("About Qt6"))
            button.setFixedWidth(710)
            button.setFixedHeight(25)
            button.clicked.connect(lambda: MessageBox.aboutQt(
                self, f"WingetUI - {_('About Qt6')}"))
            self.mainLayout.addWidget(button)
            self.mainLayout.addWidget(CustomLabel())
            self.mainLayout.addStretch()
        except Exception as e:
            self.mainLayout.addWidget(
                QLabel("An error occurred while loading the about section"))
            self.mainLayout.addWidget(QLabel(str(e)))
            report(e)
        print("🟢 About tab loaded!")

    def showEvent(self, event: QShowEvent) -> None:
        Thread(target=self.announcements.loadAnnouncements,
               daemon=True, name="Settings: Announce loader").start()
        return super().showEvent(event)


class SettingsSection(SmoothScrollArea):
    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.setFrameShape(QFrame.NoFrame)
        self.widget = QWidget()
        self.setWidgetResizable(True)
        self.setStyleSheet("margin-left: 0px;")
        self.mainLayout = QVBoxLayout()
        w = QWidget()
        w.setLayout(self.mainLayout)
        w.setMaximumWidth(1300)
        hLayout = QHBoxLayout()
        hLayout.addSpacing(20)
        hLayout.addStretch()
        hLayout.addWidget(w, stretch=0)
        hLayout.addStretch()
        self.widget.setLayout(hLayout)
        self.setWidget(self.widget)
        self.announcements = AnnouncementsPane()
        self.announcements.setMinimumWidth(800)
        self.mainLayout.addWidget(self.announcements)
        title = QLabel(_("WingetUI Settings"))

        title.setStyleSheet(
            f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        self.mainLayout.addWidget(title)
        self.mainLayout.addSpacing(20)

        self.generalTitle = CollapsableSection(_("General preferences"), getMedia(
            "settings"), _("Language, theme and other miscellaneous preferences"))
        self.mainLayout.addWidget(self.generalTitle)

        self.language = SectionComboBox(
            _("WingetUI display language:") + " (Language)")
        self.generalTitle.addWidget(self.language)
        self.language.restartButton.setText(_("Restart WingetUI") + " (Restart)")
        self.language.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")

        langListWithPercentage = []
        langDictWithPercentage = {}
        invertedLangDict = {}
        for key, value in languageReference.items():
            langValue = value
            if key in untranslatedPercentage:
                perc = untranslatedPercentage[key]
                if perc == "0%":
                    continue
                if key in lang["locale"]:
                    k = len(lang.keys())
                    v = len([val for val in lang.values() if val is not None])
                    percNum = v / k
                    perc = f"{percNum:.0%}"
                    if (perc == "100%" and percNum < 1):
                        perc = "99%"
                langValue = f"{value} ({perc})"
            langListWithPercentage.append(langValue)
            langDictWithPercentage[key] = langValue
            invertedLangDict[langValue] = key
        try:
            self.language.combobox.insertItems(0, langListWithPercentage)
            self.language.combobox.setCurrentIndex(
                langListWithPercentage.index(langDictWithPercentage[langName]))
        except Exception as e:
            report(e)
            self.language.combobox.insertItems(0, langListWithPercentage)

        def changeLang():
            self.language.restartButton.setVisible(True)
            # list(languageReference.keys())[i]
            selectedLang = invertedLangDict[self.language.combobox.currentText(
            )]
            cprint(invertedLangDict[self.language.combobox.currentText()])
            self.language.toggleRestartButton(selectedLang != langName)
            setSettingsValue("PreferredLanguage", selectedLang)

        def restartWingetUIByLangChange():
            subprocess.run(
                str("start /B \"\" \"" + sys.executable) + "\"", shell=True)
            globals.app.quit()

        self.language.restartButton.clicked.connect(
            restartWingetUIByLangChange)
        self.language.combobox.currentTextChanged.connect(changeLang)

        self.wizardButton = SectionButton(
            _("Open the welcome wizard"), _("Open"))

        def ww():
            subprocess.run(
                str("start /B \"\" \"" + sys.executable) + "\" --welcome", shell=True)
            globals.app.quit()

        self.wizardButton.clicked.connect(ww)
        self.wizardButton.button.setObjectName("AccentButton")
        self.wizardButton.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.generalTitle.addWidget(self.wizardButton)

        updateCheckBox = SectionCheckBox(_("Update WingetUI automatically"))
        updateCheckBox.setChecked(not getSettings("DisableAutoUpdateWingetUI"))
        updateCheckBox.stateChanged.connect(
            lambda v: setSettings("DisableAutoUpdateWingetUI", not bool(v)))
        self.generalTitle.addWidget(updateCheckBox)

        checkForUpdates = SectionCheckBox(
            _("Check for package updates periodically"))
        checkForUpdates.setChecked(
            not getSettings("DisableAutoCheckforUpdates"))
        self.generalTitle.addWidget(checkForUpdates)
        frequencyCombo = SectionComboBox(
            _("Check for updates every:"), buttonEnabled=False)

        times = {
            _("{0} minutes").format(10): "600",
            _("{0} minutes").format(30): "1800",
            _("1 hour"): "3600",
            _("{0} hours").format(2): "7200",
            _("{0} hours").format(4): "14400",
            _("{0} hours").format(8): "28800",
            _("{0} hours").format(12): "43200",
            _("1 day"): "86400",
            _("{0} days").format(2): "172800",
            _("{0} days").format(3): "259200",
            _("1 week"): "604800",
        }
        invertedTimes = {
            "600": _("{0} minutes").format(10),
            "1800": _("{0} minutes").format(30),
            "3600": _("1 hour"),
            "7200": _("{0} hours").format(2),
            "14400": _("{0} hours").format(4),
            "28800": _("{0} hours").format(8),
            "43200": _("{0} hours").format(12),
            "86400": _("1 day"),
            "172800": _("{0} days").format(2),
            "259200": _("{0} days").format(3),
            "604800": _("1 week")
        }

        frequencyCombo.setEnabled(checkForUpdates.isChecked())
        checkForUpdates.stateChanged.connect(lambda v: (setSettings(
            "DisableAutoCheckforUpdates", not bool(v)), frequencyCombo.setEnabled(bool(v))))
        frequencyCombo.combobox.insertItems(0, list(times.keys()))
        currentValue = getSettingsValue("UpdatesCheckInterval")
        try:
            frequencyCombo.combobox.setCurrentText(invertedTimes[currentValue])
        except KeyError:
            frequencyCombo.combobox.setCurrentText(_("1 hour"))
        except Exception as e:
            report(e)

        frequencyCombo.combobox.currentTextChanged.connect(
            lambda v: setSettingsValue("UpdatesCheckInterval", times[v]))

        self.generalTitle.addWidget(frequencyCombo)
        frequencyCombo.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius:0 ;border-bottom: 0px;}")

        automaticallyInstallUpdates = SectionCheckBox(
            _("Update packages automatically"))
        automaticallyInstallUpdates.setChecked(
            getSettings("AutomaticallyUpdatePackages"))
        automaticallyInstallUpdates.stateChanged.connect(
            lambda v: setSettings("AutomaticallyUpdatePackages", bool(v)))
        self.generalTitle.addWidget(automaticallyInstallUpdates)

        self.theme = SectionComboBox(_("Application theme:"))
        self.theme.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0px;}")
        self.generalTitle.addWidget(self.theme)

        themes = {
            _("Light"): "light",
            _("Dark"): "dark",
            _("Follow system color scheme"): "auto"
        }
        invertedThemes = {
            "light": _("Light"),
            "dark": _("Dark"),
            "auto": _("Follow system color scheme")
        }

        self.theme.combobox.insertItems(0, list(themes.keys()))
        currentValue = getSettingsValue("PreferredTheme")
        try:
            self.theme.combobox.setCurrentText(invertedThemes[currentValue])
        except KeyError:
            self.theme.combobox.setCurrentText(_("Follow system color scheme"))
        except Exception as e:
            report(e)

        def applyTheme():
            mode = win32mica.MicaTheme.AUTO
            theme = getSettingsValue("PreferredTheme")
            match theme:
                case "dark":
                    mode = win32mica.MicaTheme.DARK
                case "light":
                    mode = win32mica.MicaTheme.LIGHT
            win32mica.ApplyMica(globals.mainWindow.winId(), mode)
            globals.mainWindow.ApplyStyleSheetsAndIcons()

        self.theme.combobox.currentTextChanged.connect(lambda v: (
            setSettingsValue("PreferredTheme", themes[v]), applyTheme()))

        def exportSettings():
            nonlocal self
            try:
                rawstr = ""
                for file in glob.glob(os.path.join(os.path.expanduser("~"), ".wingetui/*")):
                    if "Running" not in file and "png" not in file and "PreferredLanguage" not in file and "json" not in file:
                        sName = file.replace("\\", "/").split("/")[-1]
                        rawstr += sName + "|@|" + \
                            getSettingsValue(sName) + "|~|"
                fileName = QFileDialog.getSaveFileName(self, _("Export settings to a local file"), os.path.expanduser(
                    "~"), f"{_('WingetUI Settings File')} (*.conf);;{_('All files')} (*.*)")
                if fileName[0] != "":
                    oFile = open(fileName[0], "w", encoding="utf-8", errors="ignore")
                    oFile.write(rawstr)
                    oFile.close()
                    subprocess.run(
                        "explorer /select,\"" + fileName[0].replace('/', '\\') + "\"", shell=True)
            except Exception as e:
                report(e)

        def importSettings():
            nonlocal self
            try:
                fileName = QFileDialog.getOpenFileName(self, _("Import settings from a local file"), os.path.expanduser(
                    "~"), f"{_('WingetUI Settings File')} (*.conf);;{_('All files')} (*.*)")
                if fileName:
                    iFile = open(fileName[0], "r")
                    rawstr = iFile.read()
                    iFile.close()
                    resetSettings()
                    for element in rawstr.split("|~|"):
                        pairValue = element.split("|@|")
                        if len(pairValue) == 2:
                            setSettings(pairValue[0], True)
                            if pairValue[1] != "":
                                setSettingsValue(pairValue[0], pairValue[1])
                    os.startfile(sys.executable)
                    globals.app.quit()
            except Exception as e:
                report(e)

        def resetSettings():
            for file in glob.glob(os.path.join(os.path.expanduser("~"), ".wingetui/*")):
                if "Running" not in file:
                    try:
                        os.remove(file)
                    except Exception:
                        pass

        self.importSettings = SectionButton(
            _("Import settings from a local file"), _("Import"))
        self.importSettings.clicked.connect(lambda: importSettings())
        self.importSettings.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.generalTitle.addWidget(self.importSettings)
        self.exportSettings = SectionButton(
            _("Export settings to a local file"), _("Export"))
        self.exportSettings.clicked.connect(lambda: exportSettings())
        self.exportSettings.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.generalTitle.addWidget(self.exportSettings)
        self.resetButton = SectionButton(_("Reset WingetUI"), _("Reset"))
        self.resetButton.clicked.connect(
            lambda: (resetSettings(), os.startfile(sys.executable), globals.app.quit()))
        self.generalTitle.addWidget(self.resetButton)

        self.startup = CollapsableSection(_("Startup options"), getMedia(
            "launch"), _("WingetUI autostart behaviour, application launch settings"))
        self.mainLayout.addWidget(self.startup)
        doCloseWingetUI = SectionCheckBox(
            _("Autostart WingetUI in the notifications area"))
        doCloseWingetUI.setChecked(not getSettings("DisableAutostart"))
        doCloseWingetUI.stateChanged.connect(
            lambda v: setSettings("DisableAutostart", not bool(v)))
        self.startup.addWidget(doCloseWingetUI)
        disableUpdateIndexes = SectionCheckBox(
            _("Do not update package indexes on launch"))
        disableUpdateIndexes.setChecked(getSettings("DisableUpdateIndexes"))
        self.startup.addWidget(disableUpdateIndexes)
        enableScoopCleanup = SectionCheckBox(
            _("Enable Scoop cleanup on launch"))
        disableUpdateIndexes.stateChanged.connect(
            lambda v: setSettings("DisableUpdateIndexes", bool(v)))
        enableScoopCleanup.setChecked(getSettings("EnableScoopCleanup"))
        enableScoopCleanup.stateChanged.connect(
            lambda v: setSettings("EnableScoopCleanup", bool(v)))
        enableScoopCleanup.setStyleSheet(
            "QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")

        self.startup.addWidget(enableScoopCleanup)

        self.UITitle = CollapsableSection(_("User interface preferences"), getMedia(
            "interactive"), _("Action when double-clicking packages, hide successful installations"))
        self.mainLayout.addWidget(self.UITitle)
        changeDefaultInstallAction = SectionCheckBox(
            _("Directly install when double-clicking an item on the \"{discoveryTab}\" tab (instead of showing the package info)").format(discoveryTab=_("Discover Packages")))
        changeDefaultInstallAction.setChecked(
            getSettings("InstallOnDoubleClick"))
        changeDefaultInstallAction.stateChanged.connect(
            lambda v: setSettings("InstallOnDoubleClick", bool(v)))
        self.UITitle.addWidget(changeDefaultInstallAction)
        changeDefaultUpdateAction = SectionCheckBox(
            _("Show info about the package on the Updates tab"))
        changeDefaultUpdateAction.setChecked(
            not getSettings("DoNotUpdateOnDoubleClick"))
        changeDefaultUpdateAction.stateChanged.connect(
            lambda v: setSettings("DoNotUpdateOnDoubleClick", bool(not v)))
        self.UITitle.addWidget(changeDefaultUpdateAction)
        dontUseBuiltInGsudo = SectionCheckBox(
            _("Remove successful installs/uninstalls/updates from the installation list"))
        dontUseBuiltInGsudo.setChecked(
            not getSettings("MaintainSuccessfulInstalls"))
        dontUseBuiltInGsudo.stateChanged.connect(
            lambda v: setSettings("MaintainSuccessfulInstalls", not bool(v)))
        dontUseBuiltInGsudo.setStyleSheet(
            "QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")
        self.UITitle.addWidget(dontUseBuiltInGsudo)

        self.trayTitle = CollapsableSection(_("Notification tray options"), getMedia(
            "systemtray"), _("WingetUI tray application preferences"))
        self.mainLayout.addWidget(self.trayTitle)

        doCloseWingetUI = SectionCheckBox(
            _("Close WingetUI to the notification area"))
        doCloseWingetUI.setChecked(not getSettings("DisablesystemTray"))
        doCloseWingetUI.stateChanged.connect(
            lambda v: setSettings("DisablesystemTray", not bool(v)))
        self.trayTitle.addWidget(doCloseWingetUI)
        generalNotifications = SectionCheckBox(
            _("Enable WingetUI notifications"))

        updatesNotifications = SectionCheckBox(
            _("Show a notification when there are available updates"))
        errorNotifications = SectionCheckBox(
            _("Show a notification when an installation fails"))
        successNotifications = SectionCheckBox(
            _("Show a notification when an installation finishes successfully"))

        def updateNotificationCheckboxes(v):
            setSettings("DisableNotifications", not bool(v))
            updatesNotifications.setEnabled(not getSettings("DisableNotifications"))
            errorNotifications.setEnabled(not getSettings("DisableNotifications"))
            successNotifications.setEnabled(not getSettings("DisableNotifications"))

        updatesNotifications.setEnabled(not getSettings("DisableNotifications"))
        errorNotifications.setEnabled(not getSettings("DisableNotifications"))
        successNotifications.setEnabled(not getSettings("DisableNotifications"))

        generalNotifications.setChecked(
            not getSettings("DisableNotifications"))
        generalNotifications.stateChanged.connect(
            lambda v: updateNotificationCheckboxes(v))
        self.trayTitle.addWidget(generalNotifications)
        updatesNotifications.setChecked(
            not getSettings("DisableUpdatesNotifications"))
        updatesNotifications.stateChanged.connect(
            lambda v: setSettings("DisableUpdatesNotifications", not bool(v)))
        self.trayTitle.addWidget(updatesNotifications)
        errorNotifications.setChecked(
            not getSettings("DisableErrorNotifications"))
        errorNotifications.stateChanged.connect(
            lambda v: setSettings("DisableErrorNotifications", not bool(v)))
        self.trayTitle.addWidget(errorNotifications)
        successNotifications.setChecked(
            not getSettings("DisableSuccessNotifications"))
        successNotifications.stateChanged.connect(
            lambda v: setSettings("DisableSuccessNotifications", not bool(v)))
        self.trayTitle.addWidget(successNotifications)
        successNotifications.setStyleSheet(
            "QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")
        
        self.backupOptions = CollapsableSection(_("Backup installed packages"), getMedia(
            "disk"), _("Automatically save a list of all your installed packages to easily restore them."))
        self.mainLayout.addWidget(self.backupOptions)
        
        backupInfo = SectionHWidget(biggerMargins=True)
        backupInfo.addWidget(CustomLabel("&nbsp;●&nbsp;"+ _("The backup will include the complete list of the installed packages and their installation options. Ignored updates and skipped versions will also be saved.")+"<br>&nbsp;●&nbsp;"+_("The backup will NOT include any binary file nor any program's saved data.")+"<br>&nbsp;●&nbsp;"+_("The size of the backup is estimated to be less than 1MB.")+"<br>&nbsp;●&nbsp;"+_("The backup will be performed after login.")))
        self.backupOptions.addWidget(backupInfo)
        backupInfo.setFixedHeight(100)
        
        enableBackups = SectionCheckBox(_("Automatically save a list of your installed packages on your computer."))
        enableBackups.setChecked(getSettings("EnablePackageBackup"))
        enableBackups.stateChanged.connect(lambda v: setSettings("EnablePackageBackup", v))
        self.backupOptions.addWidget(enableBackups)
        
        backupTimestamping = SectionCheckBox(_("Add a timestamp to the backup files"))
        backupTimestamping.setChecked(getSettings("EnableBackupTimestamping"))
        backupTimestamping.stateChanged.connect(lambda v: setSettings("EnableBackupTimestamping", v))
        self.backupOptions.addWidget(backupTimestamping)
        
        backupFileName = SectionCheckBoxTextBox(_("Set custom backup file name"))
        backupFileName.setChecked(getSettings("ChangeBackupFileName"))
        backupFileName.stateChanged.connect(lambda v: setSettings("ChangeBackupFileName", v))
        backupFileName.setText(getSettingsValue("ChangeBackupFileName"))
        
        def getSafeFileName(s: str) -> str:
            for illegalchar in "#%&{}\\/<>*?$!'\":;@`|~":
                s = s.replace(illegalchar, "")
            return s
        
        backupFileName.valueChanged.connect(lambda v: setSettingsValue("ChangeBackupFileName", getSafeFileName(v)))
        self.backupOptions.addWidget(backupFileName)
        
        backupLocation = SectionCheckBoxDirPicker(_("Change backup output directory"))
        backupLocation.setDefaultText(globals.DEFAULT_PACKAGE_BACKUP_DIR)
        backupLocation.setChecked(getSettings("ChangeBackupOutputDirectory"))
        backupLocation.stateChanged.connect(lambda v: setSettings("ChangeBackupOutputDirectory", v))
        backupLocation.setText(getSettingsValue("ChangeBackupOutputDirectory"))
        backupLocation.valueChanged.connect(lambda v: setSettingsValue("ChangeBackupOutputDirectory", v))
        self.backupOptions.addWidget(backupLocation)
        
        openBackupDirectory = SectionButton(_("Open backup location"), _("Open"))
        
        def showBackupDir():
            dir = getSettingsValue("ChangeBackupOutputDirectory")
            if not dir:
                dir = globals.DEFAULT_PACKAGE_BACKUP_DIR
            if not os.path.exists(dir):
                os.makedirs(dir)
            os.startfile(dir)
        
        openBackupDirectory.clicked.connect(showBackupDir)
        self.backupOptions.addWidget(openBackupDirectory)

        self.advancedOptions = CollapsableSection(_("Administrator privileges preferences"), getMedia(
            "runasadmin"), _("Ask once or always for administrator rights, elevate installations by default"))
        self.mainLayout.addWidget(self.advancedOptions)
        doCacheAdminPrivileges = SectionCheckBox(
            _("Ask only once for administrator privileges (not recommended)"))
        doCacheAdminPrivileges.setChecked(getSettings("DoCacheAdminRights"))
        doCacheAdminPrivileges.setStyleSheet(
            "QWidget#stChkBg{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0px;}")

        def resetAdminRightsCache():
            resetsudo = subprocess.Popen([GSUDO_EXECUTABLE, "-k"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                         stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            resetsudo.wait()
            globals.adminRightsGranted = False

        doCacheAdminPrivileges.stateChanged.connect(lambda v: (
            setSettings("DoCacheAdminRights", bool(v)), resetAdminRightsCache()))
        self.advancedOptions.addWidget(doCacheAdminPrivileges)

        # Due to lambda's nature, the following code can NOT be placed in a for loop

        WINGET = Winget.NAME
        alwaysRunAsAdmin = SectionCheckBox(
            _("Always elevate {pm} installations by default").format(pm=WINGET))
        alwaysRunAsAdmin.setChecked(getSettings("AlwaysElevate" + WINGET))
        alwaysRunAsAdmin.stateChanged.connect(
            lambda v: setSettings("AlwaysElevate" + WINGET, bool(v)))
        self.advancedOptions.addWidget(alwaysRunAsAdmin)

        SCOOP = Scoop.NAME
        alwaysRunAsAdmin = SectionCheckBox(
            _("Always elevate {pm} installations by default").format(pm=SCOOP))
        alwaysRunAsAdmin.setChecked(getSettings("AlwaysElevate" + SCOOP))
        alwaysRunAsAdmin.stateChanged.connect(
            lambda v: setSettings("AlwaysElevate" + SCOOP, bool(v)))
        self.advancedOptions.addWidget(alwaysRunAsAdmin)

        CHOCO = Choco.NAME
        alwaysRunAsAdmin = SectionCheckBox(
            _("Always elevate {pm} installations by default").format(pm=CHOCO))
        alwaysRunAsAdmin.setChecked(getSettings("AlwaysElevate" + CHOCO))
        alwaysRunAsAdmin.stateChanged.connect(
            lambda v: setSettings("AlwaysElevate" + CHOCO, bool(v)))
        self.advancedOptions.addWidget(alwaysRunAsAdmin)

        PIP = Pip.NAME
        alwaysRunAsAdmin = SectionCheckBox(
            _("Always elevate {pm} installations by default").format(pm=PIP))
        alwaysRunAsAdmin.setChecked(getSettings("AlwaysElevate" + PIP))
        alwaysRunAsAdmin.stateChanged.connect(
            lambda v: setSettings("AlwaysElevate" + PIP, bool(v)))
        self.advancedOptions.addWidget(alwaysRunAsAdmin)

        NPM = Npm.NAME
        alwaysRunAsAdmin = SectionCheckBox(
            _("Always elevate {pm} installations by default").format(pm=NPM))
        alwaysRunAsAdmin.setChecked(getSettings("AlwaysElevate" + NPM))
        alwaysRunAsAdmin.stateChanged.connect(
            lambda v: setSettings("AlwaysElevate" + NPM, bool(v)))
        self.advancedOptions.addWidget(alwaysRunAsAdmin)

        dontUseBuiltInGsudo = SectionCheckBox(
            _("Use installed GSudo instead of the bundled one (requires app restart)"))
        dontUseBuiltInGsudo.setChecked(getSettings("UseUserGSudo"))
        dontUseBuiltInGsudo.stateChanged.connect(lambda v: (setSettings(
            "UseUserGSudo", bool(v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        dontUseBuiltInGsudo.setStyleSheet(
            "QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")
        self.advancedOptions.addWidget(dontUseBuiltInGsudo)

        self.advancedOptions = CollapsableSection(_("Experimental settings and developer options"), getMedia(
            "testing"), _("Beta features and other options that shouldn't be touched"))
        self.mainLayout.addWidget(self.advancedOptions)
        disableShareApi = SectionCheckBox(
            _("Disable new share API (port 7058)"))
        disableShareApi.setChecked(getSettings("DisableApi"))
        disableShareApi.stateChanged.connect(lambda v: (setSettings("DisableApi", bool(
            v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.advancedOptions.addWidget(disableShareApi)
        parallelInstalls = SectionCheckBox(
            _("Allow parallel installs (NOT RECOMMENDED)"))
        parallelInstalls.setChecked(getSettings("AllowParallelInstalls"))
        parallelInstalls.stateChanged.connect(
            lambda v: setSettings("AllowParallelInstalls", bool(v)))
        self.advancedOptions.addWidget(parallelInstalls)

        enableSystemWinget = SectionCheckBox(
            _("Use system Winget (Needs a restart)"))
        enableSystemWinget.setChecked(getSettings("UseSystemWinget"))
        enableSystemWinget.stateChanged.connect(lambda v: (setSettings(
            "UseSystemWinget", bool(v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.advancedOptions.addWidget(enableSystemWinget)
        enableSystemWinget = SectionCheckBox(
            _("Force ARM compiled winget version (ONLY FOR ARM64 SYSTEMS)"))
        enableSystemWinget.setChecked(getSettings("EnableArmWinget"))
        enableSystemWinget.stateChanged.connect(lambda v: (setSettings(
            "EnableArmWinget", bool(v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.advancedOptions.addWidget(enableSystemWinget)
        disableLangUpdates = SectionCheckBox(
            _("Do not download new app translations from GitHub automatically"))
        disableLangUpdates.setChecked(getSettings("DisableLangAutoUpdater"))
        disableLangUpdates.stateChanged.connect(
            lambda v: setSettings("DisableLangAutoUpdater", bool(v)))
        self.advancedOptions.addWidget(disableLangUpdates)

        useCustomIconProvider = SectionCheckBoxTextBox(_("Use a custom icon and screenshot database URL"), None,
                                                       f"<a style='color:rgb({getColors()[2 if isDark() else 4]})' href=\"https://www.marticliment.com/wingetui/help/icons-and-screenshots#custom-source\">{_('More details')}</a>")
        useCustomIconProvider.setPlaceholderText(
            _("Paste a valid URL to the database"))
        useCustomIconProvider.setText(getSettingsValue("IconDataBaseURL"))
        useCustomIconProvider.setChecked(getSettings("IconDataBaseURL"))
        useCustomIconProvider.stateChanged.connect(
            lambda e: setSettings("IconDataBaseURL", e))
        useCustomIconProvider.valueChanged.connect(
            lambda v: setSettingsValue("IconDataBaseURL", v))
        self.advancedOptions.addWidget(useCustomIconProvider)

        resetyWingetUICache = SectionButton(
            _("Reset WingetUI icon and screenshot cache"), _("Reset"))
        resetyWingetUICache.clicked.connect(lambda: (shutil.rmtree(ICON_DIR), self.inform(_("Cache was reset successfully!"))))
        resetyWingetUICache.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")
        self.advancedOptions.addWidget(resetyWingetUICache)

        def resetWingetUIStore():
            for file in glob.glob(os.path.join(os.path.expanduser("~"), ".wingetui/*")):
                if "Running" not in file:
                    try:
                        os.remove(file)
                    except Exception:
                        pass
            restartWingetUIByLangChange()

        resetWingetUI = SectionButton(
            _("Reset WingetUI and its preferences"), _("Reset"))
        resetWingetUI.clicked.connect(lambda: resetWingetUIStore())
        self.advancedOptions.addWidget(resetWingetUI)

        title = QLabel(_("Package manager preferences"))
        self.mainLayout.addSpacing(40)
        title.setStyleSheet(
            f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        self.mainLayout.addWidget(title)
        self.mainLayout.addSpacing(20)

        # Winget Preferences

        self.wingetPreferences = CollapsableSection(_("{pm} preferences").format(pm="Winget"), getMedia(
            "winget"), _("{pm} package manager specific preferences").format(pm="Winget"))
        self.mainLayout.addWidget(self.wingetPreferences)

        path = SectionButton(Winget.EXECUTABLE, _("Copy"), h=50)
        path.clicked.connect(
            lambda: globals.app.clipboard().setText(Winget.EXECUTABLE))
        self.wingetPreferences.addWidget(path)
        path.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")

        disableWinget = SectionCheckBox(_("Enable {pm}").format(pm="Winget"))
        disableWinget.setChecked(not getSettings(f"Disable{Winget.NAME}"))
        disableWinget.stateChanged.connect(lambda v: (setSettings(f"Disable{Winget.NAME}", not bool(v)), parallelInstalls.setEnabled(
            v), button.setEnabled(v), enableSystemWinget.setEnabled(v), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.wingetPreferences.addWidget(disableWinget)

        bucketManager = WingetBucketManager()
        bucketManager.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.wingetPreferences.addWidget(bucketManager)

        button = SectionButton(
            _("Reset Winget sources (might help if no packages are listed)"), _("Reset"))
        button.clicked.connect(lambda: (os.startfile(
            os.path.join(realpath, "resources/reset_winget_sources.cmd"))))
        self.wingetPreferences.addWidget(button)

        parallelInstalls.setEnabled(disableWinget.isChecked())
        button.setEnabled(disableWinget.isChecked())
        enableSystemWinget.setEnabled(disableWinget.isChecked())

        # Scoop Preferences

        self.scoopPreferences = CollapsableSection(_("{pm} preferences").format(pm="Scoop"), getMedia(
            "scoop"), _("{pm} package manager specific preferences").format(pm="Scoop"))
        self.mainLayout.addWidget(self.scoopPreferences)

        pathvar = Scoop.EXECUTABLE.replace("scoop", str(shutil.which("scoop"))) if shutil.which("scoop") is not None else Scoop.EXECUTABLE
        path = SectionButton(pathvar, _("Copy"), h=50)
        path.clicked.connect(
            lambda path=pathvar: globals.app.clipboard().setText(path))
        self.scoopPreferences.addWidget(path)
        path.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")

        disableScoop = SectionCheckBox(_("Enable {pm}").format(pm="Scoop"))
        disableScoop.setChecked(not getSettings(f"Disable{Scoop.NAME}"))
        disableScoop.stateChanged.connect(lambda v: (setSettings(f"Disable{Scoop.NAME}", not bool(v)), bucketManager.setEnabled(
            v), uninstallScoop.setEnabled(v), enableScoopCleanup.setEnabled(v), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.scoopPreferences.addWidget(disableScoop)

        bucketManager = ScoopBucketManager()
        bucketManager.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.scoopPreferences.addWidget(bucketManager)

        resetScoop = SectionButton(
            _("Reset Scoop's global app cache"), _("Reset"))
        resetScoop.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        resetScoop.clicked.connect(lambda: Thread(target=lambda: subprocess.Popen(
            [GSUDO_EXECUTABLE, os.path.join(realpath, "resources", "scoop_cleanup.cmd")]), daemon=True).start())
        self.scoopPreferences.addWidget(resetScoop)

        installScoop = SectionButton(_("Install Scoop"), _("Install"))
        installScoop.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        installScoop.clicked.connect(lambda: (setSettings("DisableScoop", False), disableScoop.setChecked(
            False), os.startfile(os.path.join(realpath, "resources/install_scoop.cmd"))))
        self.scoopPreferences.addWidget(installScoop)

        uninstallScoop = SectionButton(
            _("Uninstall Scoop (and its packages)"), _("Uninstall"))
        uninstallScoop.clicked.connect(lambda: (setSettings("DisableScoop", True), disableScoop.setChecked(
            True), os.startfile(os.path.join(realpath, "resources/uninstall_scoop.cmd"))))
        self.scoopPreferences.addWidget(uninstallScoop)

        bucketManager.setEnabled(disableScoop.isChecked())
        uninstallScoop.setEnabled(disableScoop.isChecked())
        enableScoopCleanup.setEnabled(disableScoop.isChecked())

        # Chocolatey preferences

        self.chocoPreferences = CollapsableSection(_("{pm} preferences").format(pm="Chocolatey"), getMedia(
            "choco"), _("{pm} package manager specific preferences").format(pm="Chocolatey"))
        self.mainLayout.addWidget(self.chocoPreferences)

        pathvar = str(shutil.which(Choco.EXECUTABLE)) if shutil.which(Choco.EXECUTABLE) else Choco.EXECUTABLE
        path = SectionButton(pathvar, _("Copy"), h=50)
        path.clicked.connect(
            lambda path=pathvar: globals.app.clipboard().setText(path))
        self.chocoPreferences.addWidget(path)
        path.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")

        disableChocolatey = SectionCheckBox(
            _("Enable {pm}").format(pm="Chocolatey"))
        disableChocolatey.setChecked(not getSettings(f"Disable{Choco.NAME}"))
        disableChocolatey.stateChanged.connect(lambda v: (setSettings(
            f"Disable{Choco.NAME}", not bool(v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.chocoPreferences.addWidget(disableChocolatey)

        enableSystemChocolatey = SectionCheckBox(
            _("Use system Chocolatey (Needs a restart)"))
        enableSystemChocolatey.setChecked(getSettings("UseSystemChocolatey"))
        enableSystemChocolatey.stateChanged.connect(
            lambda v: setSettings("UseSystemChocolatey", bool(v)))
        enableSystemChocolatey.setStyleSheet(
            "QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")
        self.chocoPreferences.addWidget(enableSystemChocolatey)

        # Pip preferences

        self.pipPreferences = CollapsableSection(_("{pm} preferences").format(pm="Pip"), getMedia(
            "python"), _("{pm} package manager specific preferences").format(pm="Pip"))
        self.mainLayout.addWidget(self.pipPreferences)

        pathvar = str(shutil.which(Pip.EXECUTABLE.split(' ')[0]) + " -m pip") if shutil.which(Pip.EXECUTABLE.split(' ')[0]) else Pip.EXECUTABLE
        path = SectionButton(pathvar, _("Copy"), h=50)
        path.clicked.connect(
            lambda path=pathvar: globals.app.clipboard().setText(path))
        self.pipPreferences.addWidget(path)
        path.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")

        disablePip = SectionCheckBox(_("Enable {pm}").format(pm="Pip"))
        disablePip.setChecked(not getSettings(f"Disable{Pip.NAME}"))
        disablePip.stateChanged.connect(lambda v: (setSettings(f"Disable{Pip.NAME}", not bool(
            v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.pipPreferences.addWidget(disablePip)
        disablePip.setStyleSheet(
            "QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")

        # Npm preferences

        self.npmPreferences = CollapsableSection(_("{pm} preferences").format(pm="Npm"), getMedia(
            "node"), _("{pm} package manager specific preferences").format(pm="Npm"))
        self.mainLayout.addWidget(self.npmPreferences)

        pathvar = str(shutil.which(Npm.EXECUTABLE)) if shutil.which(Npm.EXECUTABLE) else Npm.EXECUTABLE
        path = SectionButton(pathvar, _("Copy"), h=50)
        path.clicked.connect(
            lambda path=pathvar: globals.app.clipboard().setText(path))
        path.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")
        self.npmPreferences.addWidget(path)

        disableNpm = SectionCheckBox(_("Enable {pm}").format(pm=Npm.NAME))
        disableNpm.setChecked(not getSettings(f"Disable{Npm.NAME}"))
        disableNpm.stateChanged.connect(lambda v: (setSettings(f"Disable{Npm.NAME}", not bool(
            v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.npmPreferences.addWidget(disableNpm)
        disableNpm.setStyleSheet(
            "QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")

        # Dotnet preferences

        self.dotnetPreferences = CollapsableSection(_("{pm} preferences").format(pm=".NET Tool"), getMedia(
            "dotnet"), _("{pm} package manager specific preferences").format(pm=".NET Tool"))
        self.mainLayout.addWidget(self.dotnetPreferences)

        pathvar = (str(shutil.which(Dotnet.EXECUTABLE)) if shutil.which(Dotnet.EXECUTABLE) else Dotnet.EXECUTABLE) + " tool"
        path = SectionButton(pathvar, _("Copy"), h=50)
        path.clicked.connect(
            lambda path=pathvar: globals.app.clipboard().setText(path))
        path.setStyleSheet(
            "QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")
        self.dotnetPreferences.addWidget(path)

        disableDotnet = SectionCheckBox(_("Enable {pm}").format(pm=Dotnet.NAME))
        disableDotnet.setChecked(not getSettings(f"Disable{Dotnet.NAME}"))
        disableDotnet.stateChanged.connect(lambda v: (setSettings(f"Disable{Dotnet.NAME}", not bool(
            v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.dotnetPreferences.addWidget(disableDotnet)
        disableDotnet.setStyleSheet(
            "QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")

        self.mainLayout.addStretch()

        print("🟢 Settings tab loaded!")

    def showEvent(self, event: QShowEvent) -> None:
        Thread(target=self.announcements.loadAnnouncements,
               daemon=True, name="Settings: Announce loader").start()
        return super().showEvent(event)

    def inform(self, text: str) -> None:
        self.notif = InWindowNotification(self, text)
        self.notif.show()


class BaseLogSection(QWidget):
    def __init__(self):
        super().__init__()

        class QPlainTextEditWithFluentMenu(QPlainTextEdit):
            def __init__(selftext):
                super().__init__()

            def contextMenuEvent(selftext, e: QContextMenuEvent) -> None:
                menu = selftext.createStandardContextMenu()
                menu.addSeparator()

                a = QAction()
                a.setText(_("Reload"))
                a.triggered.connect(self.loadData)
                menu.addAction(a)

                a4 = QAction()
                a4.setText(_("Show missing translation strings"))
                a4.triggered.connect(lambda: selftext.setPlainText(
                    '\n'.join(MissingTranslationList)))  # buffer.getvalue()))
                menu.addAction(a4)

                a2 = QAction()
                a2.setText(_("Export log as a file"))
                a2.triggered.connect(lambda: saveLog())
                menu.addAction(a2)

                a3 = QAction()
                a3.setText(_("Copy to clipboard"))
                a3.triggered.connect(lambda: copyLog())
                menu.addAction(a3)

                ApplyMenuBlur(menu.winId().__int__(), menu)
                menu.exec(e.globalPos())

        self.setObjectName("background")

        self.setLayout(QVBoxLayout())
        self.setContentsMargins(0, 0, 0, 0)

        self.textEdit = QPlainTextEditWithFluentMenu()
        self.textEdit.setReadOnly(True)

        self.textEdit.setPlainText(stdout_buffer.getvalue())

        reloadButton = QPushButton(_("Reload log"))
        reloadButton.setFixedWidth(200)
        reloadButton.clicked.connect(self.loadData)

        def saveLog():
            try:
                print("🔵 Saving log...")
                self.loadData()
                f = QFileDialog.getSaveFileName(
                    None, _("Export"), os.path.expanduser("~"), f"{_('Text file')} (*.txt)")
                if f[0]:
                    fpath = f[0]
                    if ".txt" not in fpath.lower():
                        fpath += ".txt"
                    with open(fpath, "wb") as fobj:
                        fobj.write(self.textEdit.toPlainText().encode(
                            "utf-8", errors="ignore"))
                        fobj.close()
                    os.startfile(fpath)
                    print("🟢 log saved successfully")
                else:
                    print("🟡 log save cancelled!")
            except Exception as e:
                report(e)

        exportButtom = QPushButton(_("Export to a file"))
        exportButtom.setFixedWidth(200)
        exportButtom.clicked.connect(saveLog)

        def copyLog():
            try:
                print("🔵 Copying log to the clipboard...")
                self.loadData()
                globals.app.clipboard().setText(self.textEdit.toPlainText())
                print("🟢 Log copied to the clipboard successfully!")
            except Exception as e:
                report(e)
                self.textEdit.setPlainText(stdout_buffer.getvalue())

        copyButton = QPushButton(_("Copy to clipboard"))
        copyButton.setFixedWidth(200)
        copyButton.clicked.connect(lambda: copyLog())

        hl = QHBoxLayout()
        hl.setSpacing(5)
        hl.setContentsMargins(10, 10, 10, 0)
        hl.addWidget(exportButtom)
        hl.addWidget(copyButton)
        hl.addStretch()
        hl.addWidget(reloadButton)

        self.layout().setSpacing(0)
        self.layout().setContentsMargins(5, 5, 5, 5)
        self.layout().addLayout(hl, stretch=0)
        self.layout().addWidget(self.textEdit, stretch=1)

        self.setAutoFillBackground(True)

        self.ApplyIcons()
        self.registeredThemeEvent = False

    def ApplyIcons(self):
        if isDark():
            self.textEdit.setStyleSheet(
                "QPlainTextEdit{margin: 10px;border-radius: 6px;border: 1px solid #161616;}")
        else:
            self.textEdit.setStyleSheet(
                "QPlainTextEdit{margin: 10px;border-radius: 6px;border: 1px solid #dddddd;}")

    def showEvent(self, event: QShowEvent) -> None:
        if not self.registeredThemeEvent:
            self.registeredThemeEvent = False
            self.window().OnThemeChange.connect(self.ApplyIcons)
        self.loadData()
        return super().showEvent(event)


class BaseBrowserSection(QWidget):
    HOME_URL = "https://www.marticliment.com/wingetui/help"
    loaded = False
    def __init__(self):

        super().__init__()

    def loadWebView(self):
        self.loaded = True
        from ExternalLibraries.PyWebView2 import WebView2
        self.setObjectName("background")

        hLayout = QHBoxLayout()

        openBtn = QPushButton(_("Open in browser"))
        openBtn.clicked.connect(lambda: os.startfile(self.webview.getUrl()))
        openBtn.setFixedHeight(30)

        hLayout.addWidget(openBtn)
        hLayout.addStretch()

        
        loadBtn = QPushButton(_("Reload"))
        loadBtn.clicked.connect(lambda: self.loadWebContents())
        loadBtn.setFixedHeight(30)
        hLayout.addWidget(loadBtn)

        self.setLayout(QVBoxLayout())
        self.setContentsMargins(0, 0, 0, 0)

        self.webview = WebView2()

        self.layout().setSpacing(5)
        self.layout().setContentsMargins(5, 5, 5, 5)
        self.layout().addLayout(hLayout)
        self.layout().addWidget(self.webview, stretch=1)

        self.setAutoFillBackground(True)

        self.ApplyIcons()
        self.registeredThemeEvent = False

    def ApplyIcons(self):
        if isDark():
            self.webview.setStyleSheet("margin: 10px;border-radius: 6px;border: 1px solid #161616;")
        else:
            self.webview.setStyleSheet("margin: 10px;border-radius: 6px;border: 1px solid #dddddd;")

    def showEvent(self, event: QShowEvent) -> None:
        if not self.loaded:
            self.loadWebView()
        if not self.registeredThemeEvent:
            self.registeredThemeEvent = False
            self.window().OnThemeChange.connect(self.ApplyIcons)
        self.loadWebContents()
        return super().showEvent(event)

    def loadWebContents(self):
        if not self.loaded:
            self.loadWebView()
        self.webview.setLocation(self.HOME_URL)

    def changeHomeUrl(self, url: str):
        if not self.loaded:
            self.loadWebView()
        self.HOME_URL = url
        self.loadWebContents()


class OperationHistorySection(BaseLogSection):

    def loadData(self):
        print("🔵 Loading operation log...")
        self.textEdit.setPlainText(getSettingsValue("OperationHistory"))


class LogSection(BaseLogSection):

    def loadData(self):
        print("🔵 Loading WingetUI log...")
        self.textEdit.setPlainText(stdout_buffer.getvalue())
        self.textEdit.verticalScrollBar().setValue(
            self.textEdit.verticalScrollBar().maximum())


class PackageManagerLogSection(BaseLogSection):

    def loadData(self):
        print("🔵 Reloading Package Manager logs...")
        self.textEdit.setPlainText(
            globals.PackageManagerOutput.replace("\n\n\n", ""))
        self.textEdit.verticalScrollBar().setValue(
            self.textEdit.verticalScrollBar().maximum())
