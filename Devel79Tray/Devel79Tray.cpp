#include "stdafx.h"
#include "afxwinappex.h"
#include "TrayCommandLineInfo.h"
#include "Devel79Tray.h"
#include "MainFrm.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// CDevel79TrayApp

BEGIN_MESSAGE_MAP(CDevel79TrayApp, CWinApp)
	//ON_COMMAND(ID_APP_ABOUT, &CDevel79TrayApp::OnAppAbout)
END_MESSAGE_MAP()


// CDevel79TrayApp construction

CDevel79TrayApp::CDevel79TrayApp()
{
	virtualBox = new CVirtualBox;
}

// The one and only CDevel79TrayApp object

CDevel79TrayApp trayApp;


// CDevel79TrayApp initialization

BOOL CDevel79TrayApp::InitInstance()
{
	CWinApp::InitInstance();

	// Standard initialization

	// To create the main window, this code creates a new frame window
	// object and then sets it as the application's main window object
	CMainFrame* pFrame = new CMainFrame;
	if (!pFrame) {
		return FALSE;
	}
	m_pMainWnd = pFrame;
	// create and load the frame with its resources
	pFrame->LoadFrame(IDR_TRAY, WS_OVERLAPPEDWINDOW | FWS_ADDTOTITLE, NULL, NULL);

	CTrayCommandLineInfo cmdLineInfo;
	ParseCommandLine(cmdLineInfo);
  
	virtualBox->ReadConfiguration(cmdLineInfo.GetConfigFile());

	if (cmdLineInfo.IsRunServer()) {
		TRACE("RUN SERVER");
		// RunServer
	}

	return TRUE;
}

// App command to run the dialog
//void CDevel79TrayApp::OnAppAbout()
//{
//	CAboutDlg aboutDlg;
//	aboutDlg.DoModal();
//}

// CDevel79TrayApp message handlers



