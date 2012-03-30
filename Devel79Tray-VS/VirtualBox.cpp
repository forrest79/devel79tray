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
	checkSeconds = DEFAULT_CHECKSECONDS;
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

		if ((line.Compare(_T("")) != 0) && (line.GetAt(0) != '#')) {
			TRACE(line);
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
