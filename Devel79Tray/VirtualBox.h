class CVirtualBox
{
public:
	CVirtualBox();
	BOOL ReadConfiguration(CString configFile);
private:
	CString ExeDirectory();
};
