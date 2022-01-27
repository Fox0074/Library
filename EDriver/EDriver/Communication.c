#pragma warning (disable : 4022)

#include "Communication.h"
#include "Messages.h"
#include "Data.h"
#include "Memory.h"

NTSTATUS IoControl(PDEVICE_OBJECT deviceObject, PIRP irp)
{
	UNREFERENCED_PARAMETER(deviceObject);
	NTSTATUS status = STATUS_UNSUCCESSFUL;

	try
	{
		
		ULONG byteIO = 0;

		PIO_STACK_LOCATION stack = IoGetCurrentIrpStackLocation(irp);

		ULONG controlCode = stack->Parameters.DeviceIoControl.IoControlCode;

		switch (controlCode)
		{
		case IO_GET_CLIENTADDRESS:
		{
			PULONG outPut = (PULONG)irp->AssociatedIrp.SystemBuffer;
			*outPut = GameDllAdress;

			DebugMessage("Clent adress requested!\n");

			status = STATUS_SUCCESS;
			byteIO = sizeof(*outPut);
		}break;

		case IO_READ_REQUEST:
		{
			PKERNEL_READ_REQUEST readInput = (PKERNEL_READ_REQUEST)irp->AssociatedIrp.SystemBuffer;

			PEPROCESS process;

			if (NT_SUCCESS(PsLookupProcessByProcessId(readInput->ProcessId, &process)))
			{
				KernelReadVirtualMemory(process, readInput->Address, readInput->pBuff, readInput->Size);
				status = STATUS_SUCCESS;


				byteIO = sizeof(KERNEL_READ_REQUEST);
			}


		}break;


		case IO_WRITE_REQUEST:
		{
			PKERNEL_WRITE_REQUEST writeInput = (PKERNEL_WRITE_REQUEST)irp->AssociatedIrp.SystemBuffer;
			PEPROCESS process;

			if (NT_SUCCESS(PsLookupProcessByProcessId(writeInput->ProcessId, &process)))
			{
				KernelWriteVirtualMemory(process, writeInput->pBuff, writeInput->Address, writeInput->Size);
				status = STATUS_SUCCESS;
				byteIO = sizeof(KERNEL_READ_REQUEST);
			}
		}break;

		default:
		{
			byteIO = 0;
		}break;
		}

		irp->IoStatus.Status = status;
		irp->IoStatus.Information = byteIO;
		IoCompleteRequest(irp, IO_NO_INCREMENT);

		return status;
	}
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		DebugMessage("Clent adress requested!\n");
		return status;
	}
}


NTSTATUS CloseCall(PDEVICE_OBJECT deviceObject, PIRP irp)
{
	UNREFERENCED_PARAMETER(deviceObject);
	irp->IoStatus.Status = STATUS_SUCCESS;
	irp->IoStatus.Information = 0;

	IoCompleteRequest(irp, IO_NO_INCREMENT);
	DebugMessage("Connection Terminated!\n");


	return STATUS_SUCCESS;
}

NTSTATUS CreateCall(PDEVICE_OBJECT deviceObject, PIRP irp)
{
	UNREFERENCED_PARAMETER(deviceObject);
	irp->IoStatus.Status = STATUS_SUCCESS;
	irp->IoStatus.Information = 0;

	IoCompleteRequest(irp, IO_NO_INCREMENT);
	DebugMessage("CreateCall was called!\n");

	return STATUS_SUCCESS;
}