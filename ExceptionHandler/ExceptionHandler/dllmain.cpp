// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "APIHook.h"
#include <iostream>
#include <stdio.h>
#include <exception>
#include <Windows.h>
#include <mutex>

std::mutex uncorrupt_mutex;
struct HeapInfo
{
	size_t base;
	size_t length;
};

void SendUncorruptMessage()
{

	std::lock_guard<std::mutex> guard(uncorrupt_mutex);
	// Open the named pipe
	// Most of these parameters aren't very relevant for pipes.
	HANDLE sendpipe = CreateNamedPipe(
		L"\\\\.\\pipe\\processstub_client", // name of the pipe
		PIPE_ACCESS_OUTBOUND, // 1-way pipe -- send only
		PIPE_TYPE_BYTE, // send data as a byte stream
		1, // only allow 1 instance of this pipe
		0, // no outbound buffer
		0, // no inbound buffer
		0, // use default wait time
		nullptr // use default security attributes
	);
	// This call blocks until a client process connects to the pipe
	BOOL r = ConnectNamedPipe(sendpipe, NULL);
	if (!r) {
		std::cout << "Failed to make connection on named pipe." << GetLastError() << std::endl;
		DWORD a = GetLastError();
		// look up error code here using GetLastError()
		CloseHandle(sendpipe); // close the pipe
		return;
	}

	const char *data = "UNCORRUPT";
	size_t l = strlen(data) * sizeof(char);
	
	DWORD numBytesWritten = 0;
	char* length = static_cast<char*>(static_cast<void*>(&l));
	bool result = WriteFile(
		sendpipe, // handle to our outbound pipe
		length, // data to send
		sizeof(l), // length of data to send (bytes)
		&numBytesWritten, // will store actual amount of data sent
		nullptr // not using overlapped IO
	);
	numBytesWritten = 0;
	result = WriteFile(
		sendpipe, // handle to our outbound pipe
		data, // data to send
		strlen(data) * sizeof(char), // length of data to send (bytes)
		&numBytesWritten, // will store actual amount of data sent
		nullptr // not using overlapped IO
	);
	CloseHandle(sendpipe);
}

LONG WINAPI RedirectedSetUnhandledExceptionFilter(_EXCEPTION_POINTERS* ExceptionInfo)
{
	SendUncorruptMessage();
#if _WIN64
	// 64 bit build
	ExceptionInfo->ContextRecord->Rip--;
	return EXCEPTION_CONTINUE_EXECUTION;
#else
		ExceptionInfo->ContextRecord->Eip--;
		return EXCEPTION_CONTINUE_EXECUTION;
#endif
}


LONG WINAPI VectoredExceptionHandler(_EXCEPTION_POINTERS* ExceptionInfo)
{
	SendUncorruptMessage();
#if _WIN64
	// 64 bit build
	ExceptionInfo->ContextRecord->Rip--;
	return EXCEPTION_CONTINUE_SEARCH;
#else
	ExceptionInfo->ContextRecord->Eip--;
	return EXCEPTION_CONTINUE_SEARCH;
#endif
}

void termination_handler()
{
	SendUncorruptMessage();
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


int SendHeapQueryInformation()
{
	HANDLE hHeap;
	HeapInfo hi{};
	//HEAP_SUMMARY lpSummary;

	//
	// Get a handle to the default process heap.
	//
	//
	hHeap = GetProcessHeap();
	if (hHeap == nullptr)
	{
		std::cout << "Failed to retrieve default process heap with LastError" << GetLastError() << ".\n";
		return 1;
	}
	/*

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
	}
	CloseHandle(pipe);*/
}

int SendStackQueryInformation()
{
	BOOL bResult;
	HANDLE hHeap;
	ULONG HeapInformation;

	//
	// Get a handle to the default process heap.
	//
	hHeap = GetProcessHeap();
	if (hHeap == nullptr)
	{
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
	                               nullptr);
	if (bResult == FALSE)
	{
		std::cout << "Failed to retrieve heap features with LastError" << GetLastError() << ".\n";
		return 1;
	}
}


BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )

//int WINAPI WinMain(HINSTANCE hInst, HINSTANCE hPrevInst, LPSTR, int)
{
	SetUnhandledExceptionFilter(RedirectedSetUnhandledExceptionFilter);
	PSTR a = (char*)("kernel32.dll");
	PSTR b = (char*)("SetUnhandledExceptionFilter");
	CAPIHook apiHook(a, b, (PROC)RedirectedSetUnhandledExceptionFilter);

	AddVectoredExceptionHandler(0, VectoredExceptionHandler);
	std::set_terminate(termination_handler);

	std::cout << "EH Override Injected!" << std::endl;
	return TRUE;
}
