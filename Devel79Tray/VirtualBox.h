class CVirtualBox
{
public:
	CVirtualBox();
	
	BOOL ReadConfiguration(CString configFile);

	CString getName();
	CString getMachine();
	CString getIp();
	short   getChecktime();

private:
	CString name;
	CString machine;
	CString ip;
	short   checktime;
	CString ExeDirectory();
};
