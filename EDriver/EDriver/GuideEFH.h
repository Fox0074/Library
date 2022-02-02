#pragma once
#include <ntifs.h>

extern 
NTKERNELAPI NTSTATUS IoCreateDriver(
	IN PUNICODE_STRING DriverName,
	OPTIONAL IN PDRIVER_INITIALIZE InitializationFunction
);

extern void DEntry(PVOID obj);

NTSTATUS Entry(PDRIVER_OBJECT, PUNICODE_STRING);

NTSTATUS DriverEntry(PDRIVER_OBJECT pDriverObject, PUNICODE_STRING pRegistryPath);

NTSTATUS UnloadDriver(PDRIVER_OBJECT pDriverObject);