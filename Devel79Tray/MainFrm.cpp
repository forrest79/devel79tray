// MainFrm.cpp : implementation of the CMainFrame class
//

#include "stdafx.h"
#include "Devel79Tray.h"

#include "MainFrm.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

#define	WM_ICON_NOTIFY WM_APP + 10

// CMainFrame

IMPLEMENT_DYNAMIC(CMainFrame, CFrameWnd)

BEGIN_MESSAGE_MAP(CMainFrame, CFrameWnd)
	ON_WM_CREATE()
	ON_WM_CLOSE()
END_MESSAGE_MAP()

// CMainFrame construction/destruction

CMainFrame::CMainFrame()
{
	// TODO: add member initialization code here
}

CMainFrame::~CMainFrame()
{
}

int CMainFrame::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
	if (CFrameWnd::OnCreate(lpCreateStruct) == -1) {
		return -1;
	}

	HICON hIcon = ::LoadIcon(AfxGetResourceHandle(), MAKEINTRESOURCE(IDI_MAIN));  // Icon to use

	if (!trayIcon.Create(
		NULL,                            // Let icon deal with its own messages
		WM_ICON_NOTIFY,                  // Icon notify message to use
		_T("This is a Tray Icon - Right click on me!"),  // tooltip
		hIcon,
		IDR_TRAY,                   // ID of tray icon
		FALSE,
		_T("Here's a cool new Win2K balloon!"), // balloon tip
		_T("Look at me!"),               // balloon title
		NIIF_WARNING,                    // balloon icon
		10))                             // balloon timeout
    {
		return -1;
    }

    CSystemTray::MinimiseToTray(this);

	trayIcon.SetMenuDefaultItem(0, TRUE);

	return 0;
}

//
afx_msg void CMainFrame::OnClose()
{
	trayApp.Close();
	DestroyWindow();
}

BOOL CMainFrame::PreCreateWindow(CREATESTRUCT& cs)
{
	if(!CFrameWnd::PreCreateWindow(cs)) {
		return FALSE;
	}

	cs.style = WS_OVERLAPPED | WS_CAPTION | FWS_ADDTOTITLE;

	cs.dwExStyle &= ~WS_EX_CLIENTEDGE;
	cs.lpszClass = AfxRegisterWndClass(0);
	return TRUE;
}

// CMainFrame diagnostics

#ifdef _DEBUG
void CMainFrame::AssertValid() const
{
	CFrameWnd::AssertValid();
}

void CMainFrame::Dump(CDumpContext& dc) const
{
	CFrameWnd::Dump(dc);
}
#endif //_DEBUG


// CMainFrame message handlers

BOOL CMainFrame::OnCmdMsg(UINT nID, int nCode, void* pExtra, AFX_CMDHANDLERINFO* pHandlerInfo)
{
	// otherwise, do default handling
	return CFrameWnd::OnCmdMsg(nID, nCode, pExtra, pHandlerInfo);
}
