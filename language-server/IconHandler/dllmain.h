// dllmain.h : Declaration of module class.

class CIconHandlerModule : public ATL::CAtlDllModuleT< CIconHandlerModule >
{
public :
	DECLARE_LIBID(LIBID_IconHandlerLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_ICONHANDLER, "{21a84523-c7ae-4fb7-9ec5-f24790490915}")
};

extern class CIconHandlerModule _AtlModule;
