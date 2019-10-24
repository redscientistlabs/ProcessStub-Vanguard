// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "APIHook.h"
#include <iostream>
#include <stdio.h>
#include <exception>
#include <Windows.h>


struct HeapInfo
{
	size_t base;
	size_t length;
};

HANDLE pipe = INVALID_HANDLE_VALUE;

LONG WINAPI RedirectedSetUnhandledExceptionFilter(_EXCEPTION_POINTERS* ExceptionInfo)
{
	#if _WIN64 
		// 64 bit build
	return EXCEPTION_EXECUTE_HANDLER;
	#else
		ExceptionInfo->ContextRecord->Eip++;
		return EXCEPTION_CONTINUE_EXECUTION;
	#endif
}


LONG WINAPI VectoredExceptionHandler(_EXCEPTION_POINTERS* ExceptionInfo)
{ 
#if _WIN64 
	// 64 bit build
	return EXCEPTION_CONTINUE_SEARCH;
#else
	ExceptionInfo->ContextRecord->Eip++;
	return EXCEPTION_CONTINUE_EXECUTION;
#endif
  
}

void termination_handler(){

	std::cout << "termination_handler\n";
}

bool GetHeapInfo(HANDLE heap, HeapInfo* hi)
{
	HeapLock(heap);

	PROCESS_HEAP_ENTRY entry;
	memset(&entry, '\0', sizeof entry);

	while (HeapWalk(heap, &entry))
	{
		if (hi->base == 0)
			hi->base = (size_t)&(entry.lpData);
		if (entry.wFlags == PROCESS_HEAP_ENTRY_BUSY)
		{
			hi->length += entry.cbData;
		}
	}

	HeapUnlock(heap);

	if (hi->length == 0)
		return false;
	return true;
}


int SendHeapQueryInformation() {
	HANDLE hHeap;
	HeapInfo hi{};
	HEAP_SUMMARY lpSummary;

	//
	// Get a handle to the default process heap.
	//
	hHeap = GetProcessHeap();
	if (hHeap == NULL) {
		std::cout << "Failed to retrieve default process heap with LastError" << GetLastError() << ".\n";
		return 1;
	}

	if(GetHeapInfo(hHeap, &hi))
	{
		char* data = reinterpret_cast<char*>(&hi);
		DWORD numBytesWritten = 0;
		bool result = WriteFile(
			pipe, // handle to our outbound pipe
			data, // data to send
			sizeof(HeapInfo), // length of data to send (bytes)
			&numBytesWritten, // will store actual amount of data sent
			NULL // not using overlapped IO
		);
			std::cout << "a" << std::endl;
	}

	CloseHandle(pipe);

}

int SendStackQueryInformation() {
	BOOL bResult;
	HANDLE hHeap;
	ULONG HeapInformation;

	//
	// Get a handle to the default process heap.
	//
	hHeap = GetProcessHeap();
	if (hHeap == NULL) {
		std::cout << "Failed to retrieve default process heap with LastError" << GetLastError() << ".\n";
		return 1;
	}

	//
	// Query heap features that are enabled.
	//
	bResult = HeapQueryInformation(hHeap,
		HeapCompatibilityInformation,
		&HeapInformation,
		sizeof(HeapInformation),
		NULL);
	if (bResult == FALSE) {
		std::cout << "Failed to retrieve heap features with LastError" << GetLastError() << ".\n";
		return 1;
	}

	
}
/*
BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
*/
int WINAPI WinMain(HINSTANCE hInst, HINSTANCE hPrevInst, LPSTR, int)
{

	
	SetUnhandledExceptionFilter(RedirectedSetUnhandledExceptionFilter);
	PSTR a = (char*)"kernel32.dll";
	PSTR b = (char*)"SetUnhandledExceptionFilter";
	CAPIHook apiHook(a,b, (PROC)RedirectedSetUnhandledExceptionFilter);

	AddVectoredExceptionHandler(0, VectoredExceptionHandler);
	std::set_terminate(termination_handler);

	std::cout << "EH Override Injected!" << std::endl;



	while(true){

    while (pipe == INVALID_HANDLE_VALUE) {
        std::cout << "Failed to connect to pipe." << std::endl;
		Sleep(1000);

		// Open the named pipe
		// Most of these parameters aren't very relevant for pipes.
		pipe = CreateFile(
			L"\\\\.\\pipe\\processstub",
			PIPE_ACCESS_DUPLEX,
			FILE_SHARE_READ | FILE_SHARE_WRITE,
			NULL,
			OPEN_EXISTING,
			FILE_ATTRIBUTE_NORMAL,
			NULL
		);
    }
	
    // The read operation will block until there is data to read
	char buffer[128];
	DWORD numBytesRead = 0;
	BOOL result = ReadFile(
		pipe,
		buffer, // the data from the pipe will be put here
		127 * sizeof(char), // number of bytes allocated
		&numBytesRead, // this will store number of bytes actually read
		NULL // not using overlapped IO
	);

	if (result) {
		std::string cmd(buffer, numBytesRead - 2); //We'll always have /r/n at the end of the string when coming from C#, so remove that

		if (cmd == "HEAPQUERY")
		{
			SendHeapQueryInformation();
		}
		if (cmd == "STACKQUERY")
		{
			SendStackQueryInformation();
		}
	}
	else {
		std::wcout << "Failed to read data from the pipe." << std::endl;
	}
	
}
	
    return TRUE;
}

