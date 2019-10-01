// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <iostream>
#include "APIHook.h"
#include <exception>
#include <Windows.h>


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

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{

	SetUnhandledExceptionFilter(RedirectedSetUnhandledExceptionFilter);
	PSTR a = (char*)"kernel32.dll";
	PSTR b = (char*)"SetUnhandledExceptionFilter";
	CAPIHook apiHook(a,b, (PROC)RedirectedSetUnhandledExceptionFilter);

	AddVectoredExceptionHandler(0, VectoredExceptionHandler);
	std::set_terminate(termination_handler);

	printf("EH Override Injected!\n");
	printf("\n");

	
    return TRUE;
}

