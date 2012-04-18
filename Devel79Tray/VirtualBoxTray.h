#ifndef VIRTUALBOXTRAY_H
#define VIRTUALBOXTRAY_H

#include "..\VirtualBoxSDK\VirtualBox.h"

class CVirtualBoxTray
{
public:
	// Constructor
	CVirtualBoxTray();
	
	// Methods
	BOOL InitVirtualBox();
	void ReleaseVirtualBox();
	BOOL ReadConfiguration(CString configFile);

	BOOL ShowConsole();
	BOOL HideConsole();

	BOOL StartServer();

	CString GetName();
	CString GetMachine();
	CString GetIp();
	short   GetChecktime();

	CString GetErrorMessage();

private:

	// Properties
	CString name;
	CString machine;
	CString ip;
	short   checktime;

	IVirtualBox *virtualBox;
    IMachine    *vbMachine;
	BSTR        machineName;

	CString errorMessage;

	// Methods
	CString ExeDirectory();

};

#endif
