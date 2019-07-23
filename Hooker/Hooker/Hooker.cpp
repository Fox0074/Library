﻿#include <Windows.h>

HANDLE WINAPI _My_CreateFileW(LPCWSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode, LPSECURITY_ATTRIBUTES lpSecurity, DWORD dwCreationDisp, DWORD dwFlags, HANDLE hTemplate)
{
	OutputDebugStringA(__FUNCTION__);
	return (HANDLE)-1;
}

#pragma pack(push, 1)
struct jump_near
{
	BYTE opcode; // 0xe9
	DWORD relativeAddress;
};
#pragma pack(pop)

size_t _CalculateDispacement(void* lpFirst, void* lpSecond)
{
	return reinterpret_cast<char*>(lpSecond) - (reinterpret_cast<char*>(lpFirst) + 5);
}

int main()
{
	HMODULE hKernel32 = GetModuleHandle(L"kernel32.dll");
	jump_near* lpFunc = reinterpret_cast<jump_near*>(GetProcAddress(hKernel32, "CreateFileW"));

	DWORD dwProtect = PAGE_READWRITE;

	VirtualProtect(lpFunc, sizeof(jump_near), dwProtect, &dwProtect);
	lpFunc->opcode = 0xe9;
	lpFunc->relativeAddress = _CalculateDispacement(lpFunc, &_My_CreateFileW);
	VirtualProtect(lpFunc, sizeof(jump_near), dwProtect, &dwProtect);

	HANDLE hFile = CreateFile(L"test.txt", GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	CloseHandle(hFile);
	return  0;
}