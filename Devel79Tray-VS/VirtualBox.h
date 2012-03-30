class CVirtualBox
{
public:
	CVirtualBox();
	BOOL ReadConfiguration(CString configFile);

private:
	CString name;
	CString machine;
	CString ip;
	short   checkSeconds;
	CString ExeDirectory();
};
