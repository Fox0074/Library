#pragma warning (disable : 4022 42)

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
			PULONGLONG outPut = (PULONGLONG)irp->AssociatedIrp.SystemBuffer;

			PEPROCESS process = NULL;
			NTSTATUS status2 = PsLookupProcessByProcessId((HANDLE) *outPut, &process);

			if (!(!NT_SUCCESS(status2) || process == NULL))
			{
				*outPut = (UINT64)PsGetProcessSectionBaseAddress(process);

				//*outPut = GameDllAdress;

				DebugMessage("Clent adress requested!\n");

				status = STATUS_SUCCESS;
				byteIO = sizeof(*outPut);
			}
		}break;

		case IO_READ_REQUEST:
		{
			PKERNEL_READ_REQUEST readInput = (PKERNEL_READ_REQUEST)irp->AssociatedIrp.SystemBuffer;
			PEPROCESS process;

			if (NT_SUCCESS(PsLookupProcessByProcessId(readInput->ProcessId, &process)))
			{
				try
				{
					KernelReadVirtualMemory(process, readInput->Address, readInput->pBuff, readInput->Size);
					status = STATUS_SUCCESS;
					byteIO = sizeof(KERNEL_READ_REQUEST);
				}
				__except (EXCEPTION_EXECUTE_HANDLER)
				{

				}
			}

		}break;


		case IO_WRITE_REQUEST:
		{
			PKERNEL_WRITE_REQUEST writeInput = (PKERNEL_WRITE_REQUEST)irp->AssociatedIrp.SystemBuffer;
			PEPROCESS process;

			if (NT_SUCCESS(PsLookupProcessByProcessId(writeInput->ProcessId, &process)))
			{
				KernelWriteVirtualMemory(process, writeInput->pBuff, writeInput->Address, writeInput->Size);
				DebugMessage("Write Virtual Memory!\n");
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
		DebugMessage("IoControlException!\n");
		return status;
	}
}


NTSTATUS CloseCall(PDEVICE_OBJECT deviceObject, PIRP irp)
{
	UNREFERENCED_PARAMETER(deviceObject);
	try
	{
		irp->IoStatus.Status = STATUS_SUCCESS;
		irp->IoStatus.Information = 0;

		IoCompleteRequest(irp, IO_NO_INCREMENT);

		return STATUS_SUCCESS;
	}
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		return STATUS_UNSUCCESSFUL;
	}
}

NTSTATUS CreateCall(PDEVICE_OBJECT deviceObject, PIRP irp)
{
	try
	{
	UNREFERENCED_PARAMETER(deviceObject);
	irp->IoStatus.Status = STATUS_SUCCESS;
	irp->IoStatus.Information = 0;

	IoCompleteRequest(irp, IO_NO_INCREMENT);

	return STATUS_SUCCESS;
	}
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		return STATUS_UNSUCCESSFUL;
	}
}