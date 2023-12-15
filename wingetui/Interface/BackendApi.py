"""

wingetui/apiBacked.py

This file contains the API used to communicate with https://marticliment.com/wingetui/share

"""
if __name__ == "__main__":
    import subprocess
    import os
    import sys
    import __init__
    sys.exit(0)  # This code is here to allow vscode syntax highlighting to find the classes required
    from wingetui.PackageManagers.PackageClasses import UpgradablePackage
    from wingetui.Interface.CustomWidgets.SpecificWidgets import UpgradablePackageItem


from flask import Flask, jsonify, request, abort
from flask_cors import CORS
from PySide6.QtCore import Signal
from wingetui.Core.Globals import CurrentSessionToken
from wingetui.Core.Tools import *
from wingetui.Core.Tools import _
import wingetui.Core.Globals as Globals
from waitress import serve
from wingetui.Interface.Tools import *



globalsignal: Signal = None
availableUpdates: list['UpgradablePackageItem'] = []

app = Flask("WingetUI backend")
api_v1_cors_config = {
    "methods": ["OPTIONS", "GET", "POST"],
    "allow_headers": ["Authorization"],
    "send_wildcard": True,
}
CORS(app, resources={"*": api_v1_cors_config})


@app.route('/show-package', methods=['POST', 'GET', 'OPTIONS'])
def show_package():
    try:
        response = jsonify(status="success")
        result = request.args.get('pid')
        try:
            globalsignal.emit(result)
        except AttributeError:
            pass
        return response
    except ValueError:
        return response


@app.route('/v2/show-package', methods=['POST', 'GET', 'OPTIONS'])
def v2_show_package():
    try:
        response = jsonify(status="success")
        result = request.args.get('pid') + "#" + request.args.get('psource')
        try:
            globalsignal.emit(result)
        except AttributeError:
            pass
        return response
    except ValueError:
        return response


@app.route('/is-running', methods=['POST', 'GET', 'OPTIONS'])
def v2_is_running():
    try:
        response = jsonify(status="success")
        return response
    except ValueError:
        return response


@app.route('/widgets/v1/get_wingetui_version', methods=['POST', 'GET', 'OPTIONS'])
def widgets_attempt_connection():
    try:
        if "token" not in request.args.keys():
            abort(422, "Required parameter(s): token")
        if request.method in ('POST', 'GET') and request.args["token"] == CurrentSessionToken:
            response = str(version)
        else:
            abort(401, "Invalid session token")
        return response
    except ValueError:
        return response


@app.route('/widgets/v1/get_updates', methods=['POST', 'GET', 'OPTIONS'])
def widgets_get_updates():
    try:
        if "token" not in request.args.keys():
            abort(422, "Required parameter(s): token")
        if request.args["token"] != CurrentSessionToken:
            abort(401, "Invalid session token")
        else:
            packages = ""
            for packageItem in availableUpdates:
                packageItem: UpgradablePackageItem
                if packageItem.CurrentTag not in (packageItem.Tag.Pending, packageItem.Tag.BeingProcessed):
                    packages += f"{packageItem.Package.Name.replace('|', '-')}|{packageItem.Package.Id}|{packageItem.Package.Version}|{packageItem.Package.NewVersion}|{packageItem.Package.Source}|{packageItem.Package.PackageManager.NAME}|{packageItem.Package.getPackageIconUrl()}&&"
            response = jsonify(status="success", packages=packages[:-2])
            return response
    except ValueError:
        return response


@app.route('/widgets/v1/open_wingetui', methods=['POST', 'GET', 'OPTIONS'])
def widgets_open_wingetui():
    try:
        if "token" not in request.args.keys():
            abort(422, "Required parameter(s): token")
        if request.args["token"] != CurrentSessionToken:
            abort(401, "Invalid session token")
        else:
            try:
                Globals.mainWindow.callInMain.emit(Globals.mainWindow.showWindow)
            except AttributeError:
                print("🔴 Could not show WingetUI (called from Widgets API)")
            response = jsonify(status="success")
            return response
    except ValueError:
        return response


@app.route('/widgets/v1/view_on_wingetui', methods=['POST', 'GET', 'OPTIONS'])
def widgets_view_on_wingetui():
    try:
        if "token" not in request.args.keys():
            abort(422, "Required parameter(s): token")
        if request.args["token"] != CurrentSessionToken:
            abort(401, "Invalid session token")
        else:
            try:
                Globals.mainWindow.callInMain.emit(lambda: Globals.mainWindow.showWindow(1))
            except AttributeError:
                print("🔴 Could not show WingetUI (called from Widgets API)")
            response = jsonify(status="success")
            return response
    except ValueError:
        return response


@app.route('/widgets/v1/update_package', methods=['POST', 'GET', 'OPTIONS'])
def widgets_update_app():
    try:
        if "token" not in request.args.keys() or "id" not in request.args.keys():
            abort(422, "Required parameter(s): token, id")
        if request.args["token"] != CurrentSessionToken:
            abort(401, "Invalid session token")
        else:
            try:
                Globals.mainWindow.callInMain.emit(lambda id=request.args["id"]: Globals.updates.updatePackageForGivenId(id))
            except Exception as e:
                report(e)
                abort(500, "Internal server error: " + str(e))
            response = jsonify(status="success")
            return response
    except ValueError:
        return response


@app.route('/widgets/v1/update_all_packages', methods=['POST', 'GET', 'OPTIONS'])
def widgets_update_all_apps():
    try:
        if "token" not in request.args.keys():
            abort(422, "Required parameter(s): token")
        if request.args["token"] != CurrentSessionToken:
            abort(401, "Invalid session token")
        else:
            try:
                Globals.mainWindow.callInMain.emit(Globals.updates.updateAllPackageItems)
            except Exception as e:
                report(e)
                abort(500, "Internal server error: " + str(e))
            response = jsonify(status="success")
            return response
    except ValueError:
        return response


@app.route('/widgets/v1/update_all_packages_for_source', methods=['POST', 'GET', 'OPTIONS'])
def widgets_update_all_apps_for_source():
    try:
        if "token" not in request.args.keys() or "source" not in request.args.keys():
            abort(422, "Required parameter(s): token")
        if request.args["token"] != CurrentSessionToken:
            abort(401, "Invalid session token")
        else:
            try:
                Globals.mainWindow.callInMain.emit(lambda source=request.args["source"]: Globals.updates.updateAllPackageItemsForSource(source))
            except Exception as e:
                report(e)
                abort(500, "Internal server error: " + str(e))
            response = jsonify(status="success")
            return response
    except ValueError:
        return response


def runBackendApi(signal: Signal):
    global globalsignal
    globalsignal = signal

    print("🔵 Starting API with random session authentication token", CurrentSessionToken)
    serve(app, host="localhost", port=7058)


if __name__ == "__main__":
    import __init__
