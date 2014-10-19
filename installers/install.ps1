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
.PARAMETER deaDownloadURL
    URL of the DEA msi.
.PARAMETER buildpacksDir 
	Target buildpacks directory. Default is C:\WinDEA\buildpacks 
	If modified buildpacks need to be added manually to the Windows DEA
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
    $stack = "win2012",
    $installDir = 'C:\WinDEA',
    $logyardInstallDir = 'C:\logyard',
    $buildpacksDirectory = 'c:\windea\buildpacks',
    $logyardRedisURL = '',
    $defaultGitPath = "c:\Program Files (x86)\git\bin\git.exe",
    $deaDownloadURL = "http://rpm.uhurucloud.net/wininstaller/inst/deainstaller-1.2.30.msi",
    $logyardInstallerURL = "http://rpm.uhurucloud.net/wininstaller/inst/logyard-installer.exe",
    $zmqDownloadURL = "http://miru.hk/archive/ZeroMQ-3.2.4~miru1.0-x64.exe",
    $gitDownloadURL = "https://github.com/msysgit/msysgit/releases/download/Git-1.9.4-preview20140815/Git-1.9.4-preview20140815.exe",
    $vs2012BuildTools = "http://15.125.102.70/installers/vs_isoshell.exe",
    $vs2013BuildTools = "http://15.125.102.70/installers/VS2013Build.exe"
)

$neccessaryFeatures = "Web-Server","Web-WebServer","Web-Common-Http","Web-Default-Doc","Web-Dir-Browsing","Web-Http-Errors","Web-Static-Content","Web-Http-Redirect","Web-Health","Web-Http-Logging","Web-Custom-Logging","Web-Log-Libraries","Web-ODBC-Logging","Web-Request-Monitor","Web-Http-Tracing","Web-Performance","Web-Stat-Compression","Web-Dyn-Compression","Web-Security","Web-Filtering","Web-Basic-Auth","Web-CertProvider","Web-Client-Auth","Web-Digest-Auth","Web-Cert-Auth","Web-IP-Security","Web-Url-Auth","Web-Windows-Auth","Web-App-Dev","Web-Net-Ext","Web-Net-Ext45","Web-AppInit","Web-ASP","Web-Asp-Net","Web-Asp-Net45","Web-CGI","Web-ISAPI-Ext","Web-ISAPI-Filter","Web-Includes","Web-WebSockets","Web-Mgmt-Tools","Web-Mgmt-Console","Web-Mgmt-Compat","Web-Metabase","Web-Lgcy-Mgmt-Console","Web-Lgcy-Scripting","Web-WMI","Web-Scripting-Tools","Web-Mgmt-Service","WAS","WAS-Process-Model","WAS-NET-Environment","WAS-Config-APIs","NET-Framework-Features","NET-Framework-Core","NET-Framework-45-Features","NET-Framework-45-Core","NET-Framework-45-ASPNET","NET-WCF-Services45","NET-WCF-HTTP-Activation45","Web-WHC"

$location = $pwd.Path
$tempDir = [System.Guid]::NewGuid().ToString()


function CheckParam($paramName, [REF]$paramValue, $mandatory, $templateValue)
{
    if (([string]::IsNullOrWhiteSpace($templateValue) -eq $false) -and ($templateValue -notlike "*{.${paramName}}*"))
    {
        $paramValue.Value = $templateValue
    }

    if ([string]::IsNullOrWhiteSpace($paramValue.Value) -and $mandatory)
    {
        Write-Error "The ${paramName} parameter is mandatory."
        exit 1
    }
    $paramActualValue = $paramValue.Value
    Write-Host "Using <${paramName}> = '${paramActualValue}'"
}

function VerifyParameters
{
    # Mandatory parameters
    CheckParam 'messageBus'             ([ref]$messageBus)              $true  '{{if .messageBus}}{{.messageBus}}{{else}}{{end}}'
    CheckParam 'domain'                 ([ref]$domain)                  $true  '{{if .domain}}{{.domain}}{{else}}{{end}}'
    CheckParam 'logyardRedisURL'        ([ref]$logyardRedisURL)         $true  '{{if .logyardRedisURL}}{{.logyardRedisURL}}{{else}}{{end}}'

    # Optional parameters
    CheckParam 'index'                  ([ref]$index)                   $false '{{if .index}}{{.index}}{{else}}{{end}}'
    CheckParam 'dropletDir'             ([ref]$dropletDir)              $false '{{if .dropletDir}}{{.dropletDir}}{{else}}{{end}}'
    CheckParam 'localRoute'             ([ref]$localRoute)              $false '{{if .localRoute}}{{.localRoute}}{{else}}{{end}}'
    CheckParam 'statusPort'             ([ref]$statusPort)              $false '{{if .statusPort}}{{.statusPort}}{{else}}{{end}}'
    CheckParam 'multiTenant'            ([ref]$multiTenant)             $false '{{if .multiTenant}}{{.multiTenant}}{{else}}{{end}}'
    CheckParam 'maxMemoryMB'            ([ref]$maxMemoryMB)             $false '{{if .maxMemoryMB}}{{.maxMemoryMB}}{{else}}{{end}}'
    CheckParam 'heartBeatIntervalMS'    ([ref]$heartBeatIntervalMS)     $false '{{if .heartBeatIntervalMS}}{{.heartBeatIntervalMS}}{{else}}{{end}}'
    CheckParam 'advertiseIntervalMS'    ([ref]$advertiseIntervalMS)     $false '{{if .advertiseIntervalMS}}{{.advertiseIntervalMS}}{{else}}{{end}}'
    CheckParam 'uploadThrottleBitsPS'   ([ref]$uploadThrottleBitsPS)    $false '{{if .uploadThrottleBitsPS}}{{.uploadThrottleBitsPS}}{{else}}{{end}}'
    CheckParam 'maxConcurrentStarts'    ([ref]$maxConcurrentStarts)     $false '{{if .maxConcurrentStarts}}{{.maxConcurrentStarts}}{{else}}{{end}}'
    CheckParam 'directoryServerPort'    ([ref]$directoryServerPort)     $false '{{if .directoryServerPort}}{{.directoryServerPort}}{{else}}{{end}}'
    CheckParam 'streamingTimeoutMS'     ([ref]$streamingTimeoutMS)      $false '{{if .streamingTimeoutMS}}{{.streamingTimeoutMS}}{{else}}{{end}}'
    CheckParam 'stagingEnabled'         ([ref]$stagingEnabled)          $false '{{if .stagingEnabled}}{{.stagingEnabled}}{{else}}{{end}}'
    CheckParam 'stagingTimeoutMS'       ([ref]$stagingTimeoutMS)        $false '{{if .stagingTimeoutMS}}{{.stagingTimeoutMS}}{{else}}{{end}}'
    CheckParam 'stack'                  ([ref]$stack)                   $false '{{if .stack}}{{.stack}}{{else}}{{end}}'
    CheckParam 'installDir'             ([ref]$installDir)              $false '{{if .installDir}}{{.installDir}}{{else}}{{end}}'
    CheckParam 'logyardInstallDir'      ([ref]$logyardInstallDir)       $false '{{if .logyardInstallDir}}{{.logyardInstallDir}}{{else}}{{end}}'
    CheckParam 'buildpacksDirectory'    ([ref]$buildpacksDirectory)     $false '{{if .buildpacksDirectory}}{{.buildpacksDirectory}}{{else}}{{end}}'
    CheckParam 'defaultGitPath'         ([ref]$defaultGitPath)          $false '{{if .defaultGitPath}}{{.defaultGitPath}}{{else}}{{end}}'
    CheckParam 'deaDownloadURL'         ([ref]$deaDownloadURL)          $false '{{if .deaDownloadURL}}{{.deaDownloadURL}}{{else}}{{end}}'
    CheckParam 'logyardInstallerURL'    ([ref]$logyardInstallerURL)     $false '{{if .logyardInstallerURL}}{{.logyardInstallerURL}}{{else}}{{end}}'
    CheckParam 'zmqDownloadURL'         ([ref]$zmqDownloadURL)          $false '{{if .zmqDownloadURL}}{{.zmqDownloadURL}}{{else}}{{end}}'
    CheckParam 'gitDownloadURL'         ([ref]$gitDownloadURL)          $false '{{if .gitDownloadURL}}{{.gitDownloadURL}}{{else}}{{end}}'
    CheckParam 'vs2012BuildTools'       ([ref]$vs2012BuildTools)        $false '{{if .vs2012BuildTools}}{{.vs2012BuildTools}}{{else}}{{end}}'
    CheckParam 'vs2013BuildTools'       ([ref]$vs2013BuildTools)        $false '{{if .vs2013BuildTools}}{{.vs2013BuildTools}}{{else}}{{end}}'
}

function CheckFeatureDependency(){
    $featureStatus = Install-WindowsFeature $neccessaryFeatures
}

function CheckGit()
{
   Write-Host "Checking git"

   #check if default git location exists
   If (Test-Path $defaultGitPath){
     Write-Host "git installed on system" -ForegroundColor Green
        return $defaultGitPath
    }
    
    Write-Host "git not installed, trying to install ..."
    Write-Host "Downloading git from ${gitDownloadURL} ..."

    Invoke-WebRequest $gitDownloadURL -OutFile "Git-Install.exe"
    
    Write-Host "Installing git"
    $gitInstallFile = Join-Path -Path $env:temp -ChildPath "$tempDir\Git-Install.exe"
    $gitInstallArgs = "/silent"
    
    [System.Diagnostics.Process]::Start($gitInstallFile, $gitInstallArgs).WaitForExit()

    Write-Host "Done!" -ForegroundColor Green

    return $defaultGitPath
}

function CheckZMQ()
{
    Write-Host 'Checking if zmq is available ...'

    # check if zmq libraries are available
    $zmqExists = (Start-Process -FilePath 'cmd.exe' -ArgumentList '/c where libzmq-v120-mt-3_2_4.dll' -Wait -Passthru -NoNewWindow).ExitCode

    if ($zmqExists -ne 0)
    {
        Write-Host 'ZeroMQ libraries not found, downloading and installing ...'
        Write-Host "Downloading ZeroMQ installer from ${zmqDownloadURL} ..."
        Invoke-WebRequest $zmqDownloadURL -OutFile 'ZMQ-Install.exe'
        
        Write-Host 'Installing ZeroMQ ...'
        $zmqInstallFile = (Join-Path $env:temp (Join-Path $tempDir 'ZMQ-Install.exe'))
        $zmqInstallArgs = '/S /D=c:\zmq'
        
        [System.Diagnostics.Process]::Start($zmqInstallFile, $zmqInstallArgs).WaitForExit()
        
        Write-Host 'Updating PATH environment variable ...'
        [Environment]::SetEnvironmentVariable('Path', "${env:Path};c:\zmq\bin", [System.EnvironmentVariableTarget]::Machine )
    }

    Write-Host 'ZeroMQ check complete.' -ForegroundColor Green
}

function InstallLogyard()
{
    Write-Host 'Installing Logyard ...'

    Write-Host "Downloading Logyard installer from ${logyardInstallerURL} ..."
    Invoke-WebRequest $logyardInstallerURL -OutFile 'Logyard-Install.exe'
    
    Write-Host 'Installing Logyard ...'
    $logyardInstallFile = (Join-Path $env:temp (Join-Path $tempDir 'Logyard-Install.exe'))
    $logyardInstallArgs = '/Q'
    
    $env:LOGYARD_REDIS = $logyardRedisURL

    Start-Process -FilePath $logyardInstallFile -ArgumentList $logyardInstallArgs -Wait -Passthru -NoNewWindow
    
    Get-Content 'c:\logyard-setup.log'
    
    Write-Host 'Logyard installation complete.' -ForegroundColor Green
}


function InstallVSBuildTools()
{
    Write-Host 'Installing Build Tools ...'

    #VS2012
    Write-Host "Downloading VS 2012 isolated shell from ${vs2012BuildTools} ..."
    Invoke-WebRequest $vs2012BuildTools -OutFile 'vs_isoshell.exe' #DO NOT CHANGE OUT FILE! THE INSTALLER NEEDS TO HAVE THIS FILENAME!
    Write-Host 'Installing VS 2012 isolated shell ...'
    $vs2012InstallFile = (Join-Path $env:temp (Join-Path $tempDir 'vs_isoshell.exe'))
    $vs2012InstallArgs = '/Q'
    Start-Process -FilePath $vs2012InstallFile -ArgumentList $vs2012InstallArgs -Wait -Passthru -NoNewWindow
    Write-Host 'VS 2012 isolated shell installation complete.' -ForegroundColor Green
    
    #VS2013
    Write-Host "Downloading VS 2013 isolated shell from ${vs2013BuildTools} ..."
    Invoke-WebRequest $vs2013BuildTools -OutFile 'vs2013Build.exe'
    Write-Host 'Installing VS 2013 isolated shell ...'
    $vs2013InstallFile = (Join-Path $env:temp (Join-Path $tempDir 'vs2013Build.exe'))
    $vs2013InstallArgs = '/Q'
    Start-Process -FilePath $vs2013InstallFile -ArgumentList $vs2013InstallArgs -Wait -Passthru -NoNewWindow
    Write-Host 'VS 2013 isolated shell installation complete.' -ForegroundColor Green    
}


function InstallDEA($gitLocation){
    Write-Host "Downloading Windows DEA"
    Invoke-WebRequest $deaDownloadURL -OutFile "DEAInstaller.msi"
    $deaInstallFile = Join-Path -Path $env:temp -ChildPath "$tempDir\DEAInstaller.msi"
    $deaArgs =  "/c", "msiexec", "/i", "`"$deaInstallFile`"", "/qn",  "INSTALLDIR=`"$installDir`""
    $deaArgs += "MessageBus=$messageBus", "Domain=$domain", "Index=$index", "Stacks=$stack"
    $deaArgs += "LocalRoute=$localRoute", "StatusPort=$statusPort", "MultiTenant=$multiTenant"
    $deaArgs += "MaxMemoryMB=$maxMemoryMB", "HeartBeatIntervalMS=$heartBeatIntervalMS", "AdvertiseIntervalMS=$advertiseIntervalMS"
    $deaArgs += "UploadThrottleBitsPS=$uploadThrottleBitsPS", "MaxConcurrentStarts=$maxConcurrentStarts", "DirectoryServerPort=$directoryServerPort"
    $deaArgs += "StreamingTimeoutMS=$streamingTimeoutMS", "StagingEnabled=$stagingEnabled", "Git=`"${gitLocation}`""
    $deaArgs += "BuildpacksDirectory=`"${buildpacksDirectory}`""
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
    $gitLocation = CheckGit
    #Install ZeroMQ
    CheckZMQ
    #Install Visual Studio Build Tools
    InstallVSBuildTools
    #download and install winDEA
    InstallDEA $gitLocation
    InstallLogyard
}

function Cleanup{
    Write-Host "Cleaning up"
    #clean temporary folder used
    Remove-Item *.* -Force
    Set-Location ..
    Remove-Item $tempDir
    Set-Location $location
}
$progressPreference = 'silentlyContinue' 
Install
Cleanup
