<#
.SYNOPSIS
    Packaging and installation script for the LogYard components of the Windows DEA.
.DESCRIPTION
    This script packages all the LogYard binaries into an self-extracting file.
    Upon self-extraction this script is run to unpack and install the LogYard services.

.PARAMETER action
    This is the parameter that specifies what the script should do: package the binaries and create the installer, or install the services.

.PARAMETER binDir
    When the action is 'package', this parameter specifies where the logyard binaries are located. Not used otherwise.

.NOTES
    Author: Vlad Iovanov
    Date:   October 11, 2014
#>
param (
    [Parameter(Mandatory=$true)]
    [ValidateSet('package','install')]
    [string] $action,
    [string] $binDir
)

if (($pshome -like "*syswow64*") -and ((Get-WmiObject Win32_OperatingSystem).OSArchitecture -like "64*")) {
    Write-Warning "Restarting script under 64 bit powershell"
    
    $powershellLocation = join-path ($pshome -replace "syswow64", "sysnative") "powershell.exe"
    $scriptPath = $SCRIPT:MyInvocation.MyCommand.Path
    
    # relaunch this script under 64 bit shell
    $process = Start-Process -Wait -PassThru -NoNewWindow $powershellLocation "-nologo -noexit -file ${scriptPath} -action $action"
    
    # This will exit the original powershell process. This will only be done in case of an x86 process on a x64 OS.
    exit $process.ExitCode
}

function DoAction-Package($binDir)
{
    Write-Output "Packaging files from the ${binDir} dir ..."
    [Reflection.Assembly]::LoadWithPartialName( "System.IO.Compression.FileSystem" ) | out-null

    $destFile = Join-Path $(Get-Location) "binaries.zip"
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    $includeBaseDir = $false
    Remove-Item -Force -Path $destFile -ErrorAction SilentlyContinue

    Write-Output 'Creating zip ...'

    [System.IO.Compression.ZipFile]::CreateFromDirectory($binDir, $destFile, $compressionLevel, $includeBaseDir)

    Write-Output 'Creating the self extracting exe ...'

    $installerProcess = Start-Process -Wait -PassThru -NoNewWindow 'iexpress' "/N /Q logyard-installer.sed"

    if ($installerProcess.ExitCode -ne 0)
    {
        Write-Output $installerProcess.StandardOutput.ReadToEnd()
        Write-Error "There was an error building the installer."
        Write-Error $installerProcess.StandardError.ReadToEnd()
        exit 1
    }
    
    Write-Output 'Removing artifacts ...'
    Remove-Item -Force -Path $destfile -ErrorAction SilentlyContinue
    
    Write-Output 'Done.'
}

function DoAction-Install()
{
    Write-Output 'Installing Logyard services for the Windows DEA ...'
    
    if ($env:LOGYARD_REDIS -eq $null)
    {
        Write-Error 'Could not find environment variable LOGYARD_REDIS. Please set it to the redis server used by Logyard and run the setup again.'
        exit 1
    }
    else
    {
        $redisURI = $env:LOGYARD_REDIS
    }
    
    if ($env:LOGYARD_DESTFOLDER -eq $null)
    {
        $destFolder = 'c:\logyard'
    }
    else
    {
        $destFolder = $env:LOGYARD_DESTFOLDER
    }

    if ($env:LOGYARD_LOGFOLDER -eq $null)
    {
        $logFolder = (Join-Path $destFolder 'log')
    }
    else
    {
        $logFolder = $env:LOGYARD_LOGFOLDER
    }

    Write-Output "Using redis URL ${redisURI}"
    Write-Output "Using logyard installation folder ${destFolder}"
    Write-Output "Using logyard log folder ${logFolder}"

    foreach ($dir in @($destFolder, $logFolder))
    {
        Write-Output "Cleaning up directory ${dir}"
        Remove-Item -Force -Recurse -Path $dir -ErrorVariable errors -ErrorAction SilentlyContinue

        if ($errs.Count -eq 0)
        {
            Write-Output "Successfully cleaned the directory ${dir}"
        }
        else
        {
            Write-Error "There was an error cleaning up the directory '${dir}'.`r`nPlease make sure the folder and any of its child items are not in use, then run the installer again."
            exit 1;
        }

        Write-Output "Setting up directory ${dir}"
        New-Item -path $dir -type directory -Force -ErrorAction SilentlyContinue
    }

    [Reflection.Assembly]::LoadWithPartialName( "System.IO.Compression.FileSystem" ) | out-null
    $srcFile = ".\binaries.zip"

    Write-Output 'Unpacking files ...'
    try
    {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($srcFile, $destFolder)
    }
    catch
    {
        Write-Error "There was an error writing to the installation directory '${destFolder}'.`r`nPlease make sure the folder and any of its child items are not in use, then run the installer again."
        exit 1;
    }

    InstallLogyard $destfolder $logFolder $redisURI
}

function InstallLogyard($logyardDir, $logyardLogDir, $redisURI)
{
    Write-Output "Installing logyard services"

    $logyardBinary = (Join-Path $logyardDir 'logyard.exe')
    $apptailBinary = (Join-Path $logyardDir 'apptail.exe')
    $systailBinary = (Join-Path $logyardDir 'systail.exe')
    $uidFile = (Join-Path $logyardDir 'logyard.uid')

    foreach ($serviceName in @("Logyard", "AppTail", "SysTail"))
    {
        $service = Get-WmiObject -Class Win32_Service -Filter "Name='${serviceName}'"
        if ($service -ne $null)
        {
            Write-Output "Stopping service ${serviceName}"
            Stop-Service -DisplayName $serviceName
            Write-Output "Removing service ${serviceName}"
            $service.delete()            
        }
    }
    
    New-Service -Name 'Logyard' -BinaryPathName "${logyardBinary} -logDir ${logyardLogDir} -redisUri ${redisURI}" -DisplayName 'Logyard' -StartupType Automatic
    Start-Service -DisplayName 'Logyard'
    New-Service -Name 'AppTail' -BinaryPathName "${apptailBinary} -logDir ${logyardLogDir} -uidFile ${uidFile} -redisUri ${redisURI}" -DisplayName 'AppTail' -StartupType Automatic
    Start-Service -DisplayName 'AppTail'
    New-Service -Name 'SysTail' -BinaryPathName "${logyardDir}\systail.exe -logDir ${logyardLogDir} -redisUri ${redisURI}" -DisplayName 'SysTail' -StartupType Automatic
    Start-Service -DisplayName 'SysTail'
}

if ($action -eq 'package')
{
    if ([string]::IsNullOrWhiteSpace($binDir))
    {
        Write-Error 'The binDir parameter is mandatory when packaging.'
        exit 1
    }
    
    $binDir = Resolve-Path $binDir
    
    if ((Test-Path $binDir) -eq $false)
    {
        Write-Error "Could not find directory ${binDir}."
        exit 1        
    }
    
    Write-Output "Using binary dir ${binDir}"
    
    DoAction-Package $binDir
}
elseif ($action -eq 'install')
{
    DoAction-Install
}
