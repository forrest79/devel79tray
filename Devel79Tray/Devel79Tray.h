
// Devel79Tray.h : main header file for the Devel79Tray application
//
#pragma once

#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h"       // main symbols


// CDevel79TrayApp:
// See Devel79Tray.cpp for the implementation of this class
//

class CDevel79TrayApp : public CWinApp
{
public:
	CDevel79TrayApp();


// Overrides
public:
	virtual BOOL InitInstance();

// Implementation

public:
	afx_msg void OnAppAbout();
	DECLARE_MESSAGE_MAP()
};

extern CDevel79TrayApp trayApp;
