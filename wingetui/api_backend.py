from flask import Flask, request, Response, jsonify
from flask_cors import CORS, cross_origin

from PySide6.QtCore import Signal

globalsignal: Signal = None

app = Flask("WingetUI backend")
CORS(app)

@app.route('/show-package', methods=['POST', 'GET'])
def show_package():
    try:
        response = jsonify(status="success")
        result = request.args.get('pid')
        try:
            globalsignal.emit(result)
        except AttributeError:
            pass
        print("signal emitted")
        return response
    except ValueError:
        return response

        
        
def runBackendApi(signal: Signal):
    global globalsignal
    signal.emit("test")
    globalsignal = signal
    globalsignal.emit("test")

    app.run(host="localhost", port=7058)
    
if __name__ == "__main__":
    import __init__
