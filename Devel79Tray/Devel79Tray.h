// Devel79Tray.h : main header file for the Devel79Tray application
//
#pragma once

#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h" // main symbols
#include "VirtualBoxTray.h"


// CDevel79TrayApp:
// See Devel79Tray.cpp for the implementation of this class
//

class CDevel79TrayApp : public CWinApp
{
public:
	CDevel79TrayApp();
	void Close();


// Overrides
public:
	virtual BOOL InitInstance();

// Implementation

public:
	CVirtualBoxTray* vbTray;

	DECLARE_MESSAGE_MAP()

	afx_msg void OnStartServer();

private:
	void StartServer();

	void ShowInfo(CString info);
	void ShowError(CString error);
};

extern CDevel79TrayApp trayApp;
