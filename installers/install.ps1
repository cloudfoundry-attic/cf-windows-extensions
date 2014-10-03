#ps1_sysnative
<#
.SYNOPSIS
    ALS Windows DEA installation script
.DESCRIPTION
    This script installs Windows DEA and all its dependencies
.PARAMETER messageBus
    ALS Nats endpoint.
.PARAMETER domain
    Domain used in the ALS deployment.
.PARAMETER index
    Windows DEA index in the deployment. Default 0.
    Note that each Windows DEA must have a unique index.
.PARAMETER dropletDir
    Target droplet directory. This is where all the droplets will be deployed. Default is C:\Droplets
.PARAMETER localRoute
    Used to determine the network interface.
    The application takes the interface used when trying to reach the provided IP. Default is 8.8.8.8
.PARAMETER statusPort
    Port for publishing an http endpoint used for monitoring.
    If 0, the Windows DEA takes the first available port. Default is 0.
.PARAMETER multiTenant
    Determine if muliple application can be deployed on the current DEA. Default true.
.PARAMETER maxMemoryMB
    Maximum megabytes that all the droplets in the DEA can use.
.PARAMETER heartBeatIntervalMS
    Time interval in milliseconds in which the Windows DEA sends its vitals to the Cloud Controller.
    Default is 10000.
.PARAMETER advertiseIntervalMS
    Time interval in milliseconds in which the Windows DEA verifies the deployed droplets.
    Default is 5000.    
.PARAMETER uploadThrottleBitsPS
    Used for limiting the network upload rate for each deployed app.
    If 0, limitation is disabled. Default is 0.
.PARAMETER maxConcurrentStarts  
    Determine the maximum amount of droplets that can start simultaneously. Default is 3.
.PARAMETER directoryServerPort  
    Port used for directory server. Default is 34567.
.PARAMETER streamingTimeoutMS
    Http timeout in milliseconds used for file streaming. Default is 60000.
.PARAMETER stagingEnabled
    Determine if the current DEA accepts staging requests. Default is true.
.PARAMETER stagingTimeoutMS
    Time in milliseconds after witch the Windows DEA marks the staging as failed. Default is 1200000.
.PARAMETER stack
    Name of the stack that is going to be announced to the Cloud Controller. Default windows2012.
.PARAMETER installDir
    Target install directory. This is where all the Windows DEA binaries will be installed. Default is C:\WinDEA
    If git is not installed on the system, this is where it is going to be installed
#>

[CmdletBinding()]
param (

    $messageBus = '',
    $domain = '',
    $index = 0,
    $dropletDir = 'C:\Droplets',
    $localRoute =  '8.8.8.8',
    $statusPort = 0,
    $multiTenant = 'true',
    $maxMemoryMB = 4096,
    $heartBeatIntervalMS = 10000,
    $advertiseIntervalMS = 5000,
    $uploadThrottleBitsPS = 0,
    $maxConcurrentStarts = 3,
    $directoryServerPort = 34567,
    $streamingTimeoutMS = 60000,
    $stagingEnabled = 'true',
    $stagingTimeoutMS = 1200000,
    $stack = "windows2012",
    $installDir = 'C:\WinDEA'
)

$neccessaryFeatures = "Web-Server","Web-WebServer","Web-Common-Http","Web-Default-Doc","Web-Dir-Browsing","Web-Http-Errors","Web-Static-Content","Web-Http-Redirect","Web-Health","Web-Http-Logging","Web-Custom-Logging","Web-Log-Libraries","Web-ODBC-Logging","Web-Request-Monitor","Web-Http-Tracing","Web-Performance","Web-Stat-Compression","Web-Dyn-Compression","Web-Security","Web-Filtering","Web-Basic-Auth","Web-CertProvider","Web-Client-Auth","Web-Digest-Auth","Web-Cert-Auth","Web-IP-Security","Web-Url-Auth","Web-Windows-Auth","Web-App-Dev","Web-Net-Ext","Web-Net-Ext45","Web-AppInit","Web-ASP","Web-Asp-Net","Web-Asp-Net45","Web-CGI","Web-ISAPI-Ext","Web-ISAPI-Filter","Web-Includes","Web-WebSockets","Web-Mgmt-Tools","Web-Mgmt-Console","Web-Mgmt-Compat","Web-Metabase","Web-Lgcy-Mgmt-Console","Web-Lgcy-Scripting","Web-WMI","Web-Scripting-Tools","Web-Mgmt-Service","WAS","WAS-Process-Model","WAS-NET-Environment","WAS-Config-APIs","NET-Framework-Features","NET-Framework-Core","NET-Framework-45-Features","NET-Framework-45-Core","NET-Framework-45-ASPNET","NET-WCF-Services45","NET-WCF-HTTP-Activation45","Web-WHC"
$gitDownloadURL = "https://github.com/msysgit/msysgit/releases/download/Git-1.9.4-preview20140815/Git-1.9.4-preview20140815.exe"
$deaDownloadURL = "http://rpm.uhurucloud.net/wininstaller/inst/Installer.msi"
$location = $pwd.Path
$tempDir = [System.Guid]::NewGuid().ToString()
$gitLocation = "C:\Program Files (x86)\Git\bin\git.exe"


function VerifyParameters{
    if ($messageBus -eq "")
    {
      throw [System.ArgumentException] "messageBus parameter is mandatory."
      exit 1
    }
    if ($domain -eq "")
    {
     throw [System.ArgumentException] "domain parameter is mandatory."
     exit 1
    }
    
    
}

function CheckFeatureDependency(){
    $i = 1 
    Foreach ($featureName in $neccessaryFeatures)
    {
        Write-Host "Checking Windows Feature" $i "/" $neccessaryFeatures.Count $featureName
    $winFeature = Get-WindowsFeature $featureName | where Installed
    if ($winFeature.InstallState -ne "Installed")
    {
        Write-Host "Not installed, trying to install"
        $featureStatus = Install-WindowsFeature $featureName
        if ($featureStatus.Success)
        {
            Write-Host "Succesfully installed" $featureName -ForegroundColor Green
        }
        Else
        {
            Write-Error "Failed to install" $featureName -ForegroundColor Red "please contact support"
            exit -1
        }
    }
    Else 
    {
        Write-Host "Installed, skipping" -ForegroundColor Green
    }
         $i++
    }
}

function CheckGit()
{
   Write-Host "Checking git"

   #check if default git location exists
   $defaultGitPath = "C:\Program Files (x86)\Git\bin\git.exe"
   If (Test-Path $defaultGitPath){
     Write-Host "git installed on system" -ForegroundColor Green
        $gitLocation = $defaultGitPath
        return
    }
    #test install folder
    $gitDir = Join-Path -Path $installDir -ChildPath "Git"
    $gitLocation = Join-Path -Path $gitDir -ChildPath "bin\git.exe"
    If (Test-Path $gitLocation){
     Write-Host "git installed on system" -ForegroundColor Green
        return
    }

    Write-Host "git not installed, trying to install"
    Write-Host "Downloading git"

    Invoke-WebRequest $gitDownloadURL -OutFile "Git-Install.exe"
    
    Write-Host "Installing git"
    $gitInstallFile = Join-Path -Path $env:temp -ChildPath "$tempDir\Git-Install.exe"
    $gitInstallArgs = "/c `"$gitInstallFile`" /DIR=$gitDir /silent"
    
    
    [System.Diagnostics.Process]::Start("cmd", [System.String]::Join(" ",$gitInstallArgs)).WaitForExit()

    
    Write-Host "Done!" -ForegroundColor Green
}

function InstallDEA{
    Write-Host "Downloading Windows DEA"
    Invoke-WebRequest $deaDownloadURL -OutFile "DEAInstaller.msi"
    $deaInstallFile = Join-Path -Path $env:temp -ChildPath "$tempDir\DEAInstaller.msi"
    $deaArgs =  "/c", "msiexec", "/i", "`"$deaInstallFile`"", "/qn",  "INSTALLDIR=`"$installDir`""
    $deaArgs += "MessageBus=$messageBus", "Domain=$domain", "Index=$index"
    $deaArgs += "LocalRoute=$localRoute", "StatusPort=$statusPort", "MultiTenant=$multiTenant"
    $deaArgs += "MaxMemoryMB=$maxMemoryMB", "HeartBeatIntervalMS=$heartBeatIntervalMS", "AdvertiseIntervalMS=$advertiseIntervalMS"
    $deaArgs += "UploadThrottleBitsPS=$uploadThrottleBitsPS", "MaxConcurrentStarts=$maxConcurrentStarts", "DirectoryServerPort=$directoryServerPort"
    $deaArgs += "StreamingTimeoutMS=$streamingTimeoutMS", "StagingEnabled=$stagingEnabled", "Git=`"$gitLocation`""
    Write-Host "Installing Windows DEA"
    [System.Diagnostics.Process]::Start("cmd", [System.String]::Join(" ", $deaArgs)).WaitForExit()
    Write-Host "Done!" -ForegroundColor Green
}


function Install{
    Write-Host "Using message bus" $messageBus
    Write-Host "Using domain" $domain
    Write-Host "Checking dependecies"


    Set-Location $env:temp | Out-Null 
    New-Item -Type Directory -Name $tempDir | Out-Null
    Set-Location $tempDir | Out-Null
    
    VerifyParameters
    #Install windows features
    CheckFeatureDependency
    #check if git is installed, if not, install it
    CheckGit

    #download and install winDEA
    InstallDEA

}

function Cleanup{
    Write-Host "Cleaning up"
    #clean temporary folder used
    Remove-Item *.* -Force
    Set-Location ..
    Remove-Item $tempDir
    Set-Location $location
}

Install
Cleanup