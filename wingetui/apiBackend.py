"""

wingetui/apiBacked.py

This file contains the API used to communicate with https://marticliment.com/wingetui/share

"""
if __name__ == "__main__":
    import subprocess
    import os
    import sys
    import __init__
    sys.exit(0)
    from wingetui.PackageManagers.PackageClasses import UpgradablePackage
    from wingetui.Interface.CustomWidgets.SpecificWidgets import UpgradablePackageItem


from flask import Flask, jsonify, request, abort
from flask_cors import CORS
from PySide6.QtCore import Signal
from globals import CurrentSessionToken

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


@app.route('/widgets/attempt_connection', methods=['POST', 'GET', 'OPTIONS'])
def widgets_attempt_connection():
    try:
        if request.method in ('POST', 'GET') and request.args["token"] == CurrentSessionToken:
            response = jsonify(status="success")
        else:
            abort(401, "Invalid session token")
        return response
    except ValueError:
        return response

@app.route('/widgets/get_updates', methods=['POST', 'GET', 'OPTIONS'])
def get_updates():
    try:
        if request.args["token"] != CurrentSessionToken:
            abort(401, "Invalid session token")
        else:
            packages = ""
            for packageItem in availableUpdates:
                packageItem: UpgradablePackageItem
                packages += f"{packageItem.Package.Name.replace('|', '-')}|{packageItem.Package.Id}|{packageItem.Package.Version}|{packageItem.Package.NewVersion}|{packageItem.Package.Source}|{packageItem.Package.PackageManager.NAME}||"
            response = jsonify(status="success", packages=packages[:-2])
            return response
    except ValueError:
        return response


def runBackendApi(signal: Signal):
    global globalsignal
    globalsignal = signal

    print("🔵 Starting API with random session authentication token", CurrentSessionToken)
    
    app.run(host="localhost", port=7058)


if __name__ == "__main__":
    import __init__
