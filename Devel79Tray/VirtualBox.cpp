#include "stdafx.h"
#include "Shlwapi.h"
#include "VirtualBox.h"

#define DEFAULT_CONFIGFILE "devel79.conf"

// CVirtualBox

// CVirtualBox construction

CVirtualBox::CVirtualBox()
{

	// TODO: add construction code here,
	// Place all significant initialization in InitInstance
}

BOOL CVirtualBox::ReadConfiguration(CString configFile)
{
	if (configFile.Compare(_T("")) == 0) {
		configFile = DEFAULT_CONFIGFILE;
	}

	CString file = ExeDirectory() + _T("\\") + configFile;
	TRACE(file);

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
