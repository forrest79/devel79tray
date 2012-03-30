#include "stdafx.h"
#include "Shlwapi.h"
#include "VirtualBox.h"

#define DEFAULT_CONFIGFILE "devel79.conf"

#define DEFAULT_NAME         "Devel79 Server"
#define DEFAULT_MACHINE      "devel79"
#define DEFAULT_IP           "192.168.56.1"
#define DEFAULT_CHECKSECONDS 15

// CVirtualBox

// CVirtualBox construction

CVirtualBox::CVirtualBox()
{
	name = DEFAULT_NAME;
	machine = DEFAULT_MACHINE;
	ip = DEFAULT_IP;
	checktime = DEFAULT_CHECKSECONDS;
}

//
BOOL CVirtualBox::ReadConfiguration(CString configFile)
{
	if (configFile.Compare(_T("")) == 0) {
		configFile = DEFAULT_CONFIGFILE;
	}

	CString filename = ExeDirectory() + _T("\\") + configFile;

	CStdioFile file; 
	if (!file.Open(filename, CFile::modeRead)) {
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

CString CVirtualBox::ExeDirectory()
{
	// Get the full path of current exe file.
	TCHAR path[MAX_PATH] = { 0 };
	::GetModuleFileName(NULL, path, MAX_PATH);

	// Strip the exe filename from path and get folder name.
	PathRemoveFileSpec(path);

	return path;
}
