#ifndef MAINFRM_H
#define MAINFRM_H

#pragma once

#include "Devel79Tray.h"
#include "VirtualBoxTray.h"
#include "NTray.h"

class CMainFrame : public CFrameWnd
{
public:
	CMainFrame(CVirtualBoxTray* vbTray);
	void RunServer();
	void StopServer();

protected:
	CTrayNotifyIcon trayIcon;
	HICON iconMain;
	HICON iconRun;
	HICON iconStop;

	CVirtualBoxTray* vbTray;

	DECLARE_DYNAMIC(CMainFrame)

	DECLARE_MESSAGE_MAP()

	afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
	afx_msg void OnClose();
	afx_msg LRESULT OnTrayNotification(WPARAM wParam, LPARAM lParam);
};

#endif