// dllmain.cpp : Defines the entry point for the DLL application.

#pragma once

#include "targetver.h"

//#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files:
#include <windows.h>



#ifdef _MANAGED
#pragma managed(push, off)
#endif

BOOL APIENTRY DllMain(
	HMODULE		/*hModule*/,
	DWORD		ul_reason_for_call,
	LPVOID		/*lpReserved*/)
{
	switch (ul_reason_for_call)
	{
		case DLL_PROCESS_ATTACH:
		case DLL_THREAD_ATTACH:
		case DLL_THREAD_DETACH:
		case DLL_PROCESS_DETACH:
			break;
	}
	return TRUE;
}

#ifdef _MANAGED
#pragma managed(pop)
#endif