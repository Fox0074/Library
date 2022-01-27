#pragma warning (disable : 4047)

#include "Events.h"
#include "Messages.h"
#include "Data.h"

PLOAD_IMAGE_NOTIFY_ROUTINE ImageLoadCallBack(PUNICODE_STRING fullImageName, HANDLE processId, PIMAGE_INFO imageInfo)
{
	//DebugMessage("ImageLoaded: %ls \n", fullImageName->Buffer);
	
	if (wcsstr(fullImageName->Buffer, L"TestDll.dll"))
	{
		DebugMessage("AssemblyCSharp Dll found!\n");
		GameDllAdress = imageInfo->ImageBase;

		DebugMessage("ProcessID: %d \n", processId);
	}

	return STATUS_SUCCESS;
}