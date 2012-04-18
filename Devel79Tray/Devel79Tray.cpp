#include "stdafx.h"
#include "afxwinappex.h"
#include "TrayCommandLineInfo.h"
#include "Devel79Tray.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// CDevel79TrayApp

BEGIN_MESSAGE_MAP(CDevel79TrayApp, CWinApp)
	ON_COMMAND(ID_TRAY_SHOWCONSOLE, &CDevel79TrayApp::OnShowConsole)
	ON_COMMAND(ID_TRAY_HIDECONSOLE, &CDevel79TrayApp::OnHideConsole)
	ON_COMMAND(ID_TRAY_STARTSERVER, &CDevel79TrayApp::OnStartServer)
END_MESSAGE_MAP()


// CDevel79TrayApp construction

CDevel79TrayApp::CDevel79TrayApp()
{
	vbTray = new CVirtualBoxTray;
}

// The one and only CDevel79TrayApp object

CDevel79TrayApp trayApp;

// CDevel79TrayApp initialization

BOOL CDevel79TrayApp::InitInstance()
{
	CWinApp::InitInstance();

	CTrayCommandLineInfo cmdLineInfo;
	ParseCommandLine(cmdLineInfo);

	// Load configuration...
	if (!vbTray->ReadConfiguration(cmdLineInfo.GetConfigFile())) {
		ShowError(vbTray->GetErrorMessage());
		Close();
		return FALSE;
	}

	// Init VirtualBox...
	if (!vbTray->InitVirtualBox()) {
		ShowError(vbTray->GetErrorMessage());
		Close();
		return FALSE;
	}
 
	// Standard initialization

	// To create the main window, this code creates a new frame window
	// object and then sets it as the application's main window object
	mainFrame = new CMainFrame(vbTray);
	if (!mainFrame) {
		return FALSE;
	}
	m_pMainWnd = mainFrame;

	// create and load the frame with its resources
	//pFrame->LoadFrame(IDR_TRAY, WS_OVERLAPPEDWINDOW | FWS_ADDTOTITLE, NULL, NULL);
	if (!mainFrame->Create(NULL, _T("Devel79Tray"))) {
		return FALSE;
	}

	m_pMainWnd->ShowWindow(SW_HIDE);
	m_pMainWnd->UpdateWindow();

	// Run server is there is command line argument...
	if (cmdLineInfo.IsRunServer()) {
		StartServer();
	}

	return TRUE;
}

//
void CDevel79TrayApp::Close()
{
	vbTray->ReleaseVirtualBox();
}

// CDevel79TrayApp message handlers

//
void CDevel79TrayApp::OnShowConsole()
{
	((CMainFrame*)mainFrame)->RunServer();
}

//
void CDevel79TrayApp::OnHideConsole()
{
	
}

//
void CDevel79TrayApp::OnStartServer()
{
	StartServer();
}

// PRIVATE

//
void CDevel79TrayApp::ShowConsole()
{
	if (!vbTray->ShowConsole()) {
		ShowError(vbTray->GetErrorMessage());
	}
}

//
void CDevel79TrayApp::HideConsole()
{
	if (!vbTray->HideConsole()) {
		ShowError(vbTray->GetErrorMessage());
	}
}

//
void CDevel79TrayApp::StartServer()
{
	if (!vbTray->StartServer()) {
		ShowError(vbTray->GetErrorMessage());
	}
}

//
void CDevel79TrayApp::ShowInfo(CString info)
{
	MessageBox(NULL, info, vbTray->GetName(), MB_ICONINFORMATION | MB_OK);
}

//
void CDevel79TrayApp::ShowError(CString error)
{
	MessageBox(NULL, error, _T("[Error]") + vbTray->GetName(), MB_ICONERROR | MB_OK);
}


