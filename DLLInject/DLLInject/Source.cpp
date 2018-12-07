#include <Windows.h>


extern "C" __declspec(dllexport) bool WINAPI Dllmain(HINSTANCE hInstDll, DWORD fdwReason, LPVOID lpvReserved)
{
	switch (fdwReason)
	{
	case DLL_PROCESS_ATTACH:
	{
		MessageBox(NULL, "DLL INJECTION", "SPECIAL FOR CODEBY", MB_OK);
		break;
	}

	case DLL_PROCESS_DETACH:
		break;

	case DLL_THREAD_ATTACH:
		break;

	case DLL_THREAD_DETACH:
		break;
	}
	return true;
}