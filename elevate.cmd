:: //***************************************************************************
:: // ***** Script Header *****
:: // =======================================================
:: // Elevation PowerToys for Windows Vista v1.1 (04/29/2008)
:: // =======================================================
:: //
:: // File:      Elevate.cmd
:: //
:: // Additional files required:  Elevate.vbs
:: //
:: // Purpose:   To provide a command line method of launching applications that
:: //            prompt for elevation (Run as Administrator) on Windows Vista.
:: //
:: // Usage:     elevate.cmd application <application arguments>
:: //
:: // Version:   1.0.0
:: // Date :     01/02/2007
:: //
:: // History:
:: // 1.0.0   01/02/2007  Created initial version.
:: //
:: // ***** End Header *****
:: //***************************************************************************

@setlocal
@echo off

:: Pass raw command line agruments and first argument to Elevate.vbs
:: through environment variables.
set ELEVATE_CMDLINE=%*
set ELEVATE_APP=%1

wscript //nologo "%~dpn0.vbs" %*