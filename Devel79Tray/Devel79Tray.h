#pragma once

#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h"
#include "VirtualBoxTray.h"

class CDevel79TrayApp : public CWinApp
{
public:
	CVirtualBoxTray* vbTray;

	CDevel79TrayApp();
	void Close();
	virtual BOOL InitInstance();

	DECLARE_MESSAGE_MAP()

	afx_msg void OnStartServer();

private:
	void StartServer();

	void ShowInfo(CString info);
	void ShowError(CString error);
};

extern CDevel79TrayApp trayApp;
