#include "stdafx.h"
#include "Shlwapi.h"
#include "VirtualBoxTray.h"

#define DEFAULT_CONFIGFILE   "devel79.conf"

#define DEFAULT_NAME         "Devel79 Server"
#define DEFAULT_MACHINE      "devel79"
#define DEFAULT_IP           "192.168.56.1"
#define DEFAULT_CHECKSECONDS 15

// CVirtualBoxTray

// CVirtualBoxTray construction
CVirtualBoxTray::CVirtualBoxTray()
{
	name = DEFAULT_NAME;
	machine = DEFAULT_MACHINE;
	ip = DEFAULT_IP;
	checktime = DEFAULT_CHECKSECONDS;

	virtualBox = NULL;
	vbMachine = NULL;
}

//
BOOL CVirtualBoxTray::InitVirtualBox()
{
    HRESULT status;

    // Initialize the COM subsystem
    CoInitialize(NULL);

    // Instantiate the VirtualBox root object
    status = CoCreateInstance(CLSID_VirtualBox,     // the VirtualBox base object
                              NULL,                 // no aggregation
                              CLSCTX_LOCAL_SERVER,  // the object lives in a server process on this machine
                              IID_IVirtualBox,      // IID of the interface
                              (void**)&virtualBox);

    if (!SUCCEEDED(status))
    {
        errorMessage = _T("Error while connecting to VirtualBox.");
        return FALSE;
    }

	// Search machine
    machineName = SysAllocString(machine);

    status = virtualBox->FindMachine(machineName, &vbMachine);

    if (FAILED(status)) {
        errorMessage = _T("Machine '") + machine + _T("' not found.");
		return FALSE;
    }

	return TRUE;
}

//
void CVirtualBoxTray::ReleaseVirtualBox()
{
	// Release VirtualBox machine

    if (vbMachine) {
		vbMachine->Release();
		vbMachine = NULL;
	}
	SysFreeString(machineName);

	// Release the VirtualBox object

	if (virtualBox != NULL) {
		virtualBox->Release();
	}

	CoUninitialize();
}

//
BOOL CVirtualBoxTray::ReadConfiguration(CString configFile)
{
	if (configFile.Compare(_T("")) == 0) {
		configFile = DEFAULT_CONFIGFILE;
	}

	CString filename = ExeDirectory() + _T("\\") + configFile;

	CStdioFile file; 
	if (!file.Open(filename, CFile::modeRead)) {
		errorMessage = _T("Error while opening configuration file: '") + filename + _T("'.");
		return FALSE;
	}

	CString line;
	while(file.ReadString(line)) {
		line = line.Trim();

		if ((line.Compare(_T("")) != 0) && (line.GetAt(0) != '#')) { // Ignore blank lines and comment lines ('#')
			CString configName, configValue;

			int tokenPos = 0;
			CString token = line.Tokenize(_T("="), tokenPos);
			if (!token.IsEmpty()) { // Read name...
				configName = token.Trim();

				token = line.Tokenize(_T("="), tokenPos);
				if (!token.IsEmpty()) { // ...and value
					configValue = token.Trim();

					if (name.CollateNoCase(_T("name")) == 0) {
						name = configValue;
					} else if (name.CollateNoCase(_T("machine")) == 0) {
						machine = configValue;
					} else if (name.CollateNoCase(_T("ip")) == 0) {
						ip = configValue;
					} else if (name.CollateNoCase(_T("checktime")) == 0) {
						checktime = _ttoi(configValue);
					}
				}
			} else {
				continue;
			}
		}
	}

	file.Close();

	return TRUE;
}

//
CString CVirtualBoxTray::GetErrorMessage()
{
	return errorMessage;
}

//
CString CVirtualBoxTray::ExeDirectory()
{
	// Get the full path of current exe file.
	TCHAR path[MAX_PATH] = { 0 };
	::GetModuleFileName(NULL, path, MAX_PATH);

	// Strip the exe filename from path and get folder name.
	PathRemoveFileSpec(path);

	return path;
}
