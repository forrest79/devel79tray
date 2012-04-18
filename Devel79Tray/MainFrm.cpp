#include "stdafx.h"
#include "MainFrm.h"

#define WM_TRAYNOTIFY WM_USER + 100

// CMainFrame

IMPLEMENT_DYNAMIC(CMainFrame, CFrameWnd)

BEGIN_MESSAGE_MAP(CMainFrame, CFrameWnd)
	ON_WM_CREATE()
	ON_WM_CLOSE()
	ON_MESSAGE(WM_TRAYNOTIFY, OnTrayNotification)
END_MESSAGE_MAP()

//
CMainFrame::CMainFrame(CVirtualBoxTray* vbTray)
{
	this->vbTray = vbTray;
	iconMain = CTrayNotifyIcon::LoadIcon(IDI_MAIN);
	iconRun  = CTrayNotifyIcon::LoadIcon(IDI_RUN);
	iconStop = CTrayNotifyIcon::LoadIcon(IDI_STOP);
}

//
void CMainFrame::RunServer()
{
	trayIcon.SetIcon(iconStop);
}

//
int CMainFrame::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
	if (CFrameWnd::OnCreate(lpCreateStruct) == -1) {
		return -1;
	}

	if (!trayIcon.Create(this, IDR_TRAY, vbTray->GetName(), iconMain, WM_TRAYNOTIFY, IDR_TRAY, TRUE)) {
		return -1;
    }

	return 0;
}

//
void CMainFrame::OnClose()
{
	trayApp.Close();
	DestroyWindow();
}

//
LRESULT CMainFrame::OnTrayNotification(WPARAM wParam, LPARAM lParam)
{
  trayIcon.OnTrayNotification(wParam, lParam);

  return 0L;
}
