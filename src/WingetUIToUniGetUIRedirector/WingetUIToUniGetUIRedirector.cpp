#include <iostream>
#include <string>
#include <cstdlib>
#include <Windows.h>

using namespace std;

LPWSTR ConvertStringToLPWSTR(const string& input) {
    // Calculate the size of the wide string
    int bufferSize = MultiByteToWideChar(CP_UTF8, 0, input.c_str(), -1, nullptr, 0);
    if (bufferSize == 0) {
        return nullptr; // Conversion failed
    }

    // Allocate buffer for the wide string
    LPWSTR wideString = new wchar_t[bufferSize];

    // Perform the conversion
    int result = MultiByteToWideChar(CP_UTF8, 0, input.c_str(), -1, wideString, bufferSize);
    if (result == 0) {
        delete[] wideString; // Clean up if conversion failed
        return nullptr;
    }

    return wideString;
}

int main(int argc, char* argv[]) {
    try
    {
        string currentExecutablePath(argv[0]);
        size_t lastSlashPos = currentExecutablePath.find_last_of("\\/");
        string directory;
        if (lastSlashPos != string::npos) {
            directory = currentExecutablePath.substr(0, lastSlashPos + 1);
        } else {
            directory = "";
        }

        cout << "The working directory has been set to: " << directory << endl;
        
        string targetExecutable = directory + "UniGetUI.exe"; // Adjusted for the actual executable name

        FILE* file_stream;
        errno_t err = fopen_s(&file_stream, targetExecutable.c_str(), "rb");

        if (err == 0)
        {
            cout << "The target executable exists and is located here: " << targetExecutable << endl; 

            string cmdLine = "UniGetUI.exe"; // Replace with your executable name
            for (int i = 1; i < argc; ++i) {
                cmdLine += " ";
                cmdLine += argv[i];
            }

            LPWSTR formatted_command = ConvertStringToLPWSTR(cmdLine);
            
            cout << "Calling command: " << cmdLine << endl;

            STARTUPINFO si;
            PROCESS_INFORMATION pi;

            ZeroMemory( &si, sizeof(si) );
            si.cb = sizeof(si);
            ZeroMemory( &pi, sizeof(pi) );

            if (!CreateProcess(NULL,
                formatted_command,
                NULL,           
                NULL,           
                FALSE,          
                0,              
                NULL,
                NULL, 
                &si,
                &pi)
                ) 
            {
                int err = GetLastError();
                std::cout << "CreateProcess failed (" << err << ")." << endl;
                return err;
            }

            std::cout << "Started process, not waiting for it to finish." << endl;

            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
            return 0;
        }
        else if(err == ENOENT)
        {
            cout << "the target executable " << targetExecutable << " does not exist: " << err << endl;
        }
        else
        {
            cout << "the target executable " << targetExecutable << " could not be opened due to error " << err << endl;
        }
        
        return err;
    }
    catch  (exception& e)
    {
        cout << "An exception was thrown: " << e.what() << endl;
    }
}