#pragma warning (disable : 4047 4024)

#include "GuideEFH.h"
#include "Messages.h"
#include "events.h"
#include "Data.h"
#include "Communication.h"
#include <ntddk.h>


NTSTATUS  Entry( PDRIVER_OBJECT pDriverObject, PUNICODE_STRING pRegistryPath) 
{
	UNREFERENCED_PARAMETER(pRegistryPath);
	UNREFERENCED_PARAMETER(pDriverObject);
	DebugMessage("Enter\n");

	HANDLE out;
	NTSTATUS result = PsCreateSystemThread(&out, GENERIC_ALL, NULL, NULL, NULL, DEntry, NULL);
	if (NT_SUCCESS(result)) {
		DebugMessage("COK\n");
		ZwClose(out);
	}
	else {
		DebugMessage("no puto thread?!");
	}
	return result;
}

extern void DEntry(PVOID obj) 
{
	UNREFERENCED_PARAMETER(obj);
	RtlInitUnicodeString(&dev, L"\\Device\\guideeh");
	RtlInitUnicodeString(&dos, L"\\DosDevices\\guideeh");
	if (!NT_SUCCESS(IoCreateDriver(NULL, (PDRIVER_INITIALIZE)DriverEntry))) 
	{
		DebugMessage("Error al crear el driver...\n");
	}
}

NTSTATUS DriverEntry(PDRIVER_OBJECT pDriverObject, PUNICODE_STRING pRegistryPath)
{
	try
	{
		UNREFERENCED_PARAMETER(pRegistryPath);
		pDriverObject->DriverUnload = UnloadDriver;
		DebugMessage("Welcome to the guide!");

		PsSetLoadImageNotifyRoutine(ImageLoadCallBack);

		RtlInitUnicodeString(&dev, L"\\Device\\guideeh");
		RtlInitUnicodeString(&dos, L"\\DosDevices\\guideeh");

		IoCreateDevice(pDriverObject, 0, &dev, FILE_DEVICE_UNKNOWN, FILE_DEVICE_SECURE_OPEN, FALSE, &pDeviceObject);
		IoCreateSymbolicLink(&dos, &dev);

		pDriverObject->MajorFunction[IRP_MJ_CREATE] = CreateCall;
		pDriverObject->MajorFunction[IRP_MJ_CLOSE] = CloseCall;
		pDriverObject->MajorFunction[IRP_MJ_DEVICE_CONTROL] = IoControl;

		pDeviceObject->Flags |= DO_DIRECT_IO;
		pDeviceObject->Flags &= ~DO_DEVICE_INITIALIZING;
	}
	__except(EXCEPTION_EXECUTE_HANDLER)
	{
		DebugMessage("MainError!");
	}

	return STATUS_SUCCESS;
}

NTSTATUS UnloadDriver(PDRIVER_OBJECT pDriverObject)
{
	UNREFERENCED_PARAMETER(pDriverObject);
	DebugMessage("TestDriver Unloaded!");

	PsRemoveLoadImageNotifyRoutine(ImageLoadCallBack);

	IoDeleteSymbolicLink(&dos);
	IoDeleteDevice(pDriverObject->DeviceObject);

	return STATUS_SUCCESS;
}