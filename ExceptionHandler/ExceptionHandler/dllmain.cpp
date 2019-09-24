// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <iostream>
#include "APIHook.h"


LONG WINAPI RedirectedSetUnhandledExceptionFilter(struct _EXCEPTION_POINTERS* pInfo)
{
	printf("CAUGHT EXCEPTION\n");
	return 0;
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

	printf("EH Override Injected!\n");
	printf("\n");

	
    return TRUE;
}

