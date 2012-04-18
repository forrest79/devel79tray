#ifndef TRAYCOMMANDLINEINFO_H
#define TRAYCOMMANDLINEINFO_H

class CTrayCommandLineInfo : public CCommandLineInfo
{
private:
	BOOL    runServer;
	BOOL    nextConfigFile;
	CString configFile;

public:
	CTrayCommandLineInfo();
	virtual void ParseParam(const TCHAR* pszParam, BOOL bFlag, BOOL bLast);
	BOOL    IsRunServer();
	CString GetConfigFile();
};

#endif