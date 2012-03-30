#include "stdafx.h"
#include "TrayCommandLineInfo.h"

// CTrayCommandLineInfo

// CTrayCommandLineInfo construction
CTrayCommandLineInfo::CTrayCommandLineInfo()
{
	runServer = FALSE;
	nextConfigFile = FALSE;
	configFile = _T("");
}

//
void CTrayCommandLineInfo::ParseParam(const TCHAR* pszParam, BOOL bFlag, BOOL bLast)
{
	TRACE(pszParam);
	if (nextConfigFile) {
		configFile = pszParam;
	} else {
		if ((wcsicmp(pszParam, _T("-runserver")) == 0) || (wcsicmp(pszParam, _T("r")) == 0)) {
			runServer = TRUE;
		} else if ((0 == wcsicmp(pszParam, _T("-config"))) || (wcsicmp(pszParam, _T("c")) == 0)) {
			nextConfigFile = TRUE;
		}
	}
}

//
BOOL CTrayCommandLineInfo::IsRunServer()
{
	return runServer;
}

//
CString CTrayCommandLineInfo::GetConfigFile()
{
	return configFile;
}