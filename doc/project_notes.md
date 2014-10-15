Windows DEA on HP ALS
=====================

##Links

All work is assumed to be private.

###Trello Project
https://trello.com/b/lojVVJdY/hp-stackato-net

###Github Project
https://github.com/UhuruSoftware/vcap-dotnet-hp

###CI
We will host a Jenkins server inside the Romania lab, and provide HP with credentials.

##Deliverables

1. Demo on November 26
 - GO tools for managing Windows extensions to ALS
 - PoC MSSQL service running on the hpcloud demo account
 - NerdDinner working with SQL Server  


##Questions/Open Issues

1. **Should we use a custom domain and a proper SSL Certificate for the demo?**
2. 

##Demo Resources and Useful Material

### Environment for building your own Windows OpenStack image

Use an Ubuntu 14 Desktop VM, with VMware Workstation 10 (it supports virtual VT-X)

    apt-get install -y qemu-kvm qemu-common virt-manager virt-viewer git-core
    git clone https://github.com/cloudbase/windows-openstack-imaging-tools.git
    cd windows-openstack-imaging-tools
    vim Autounattend.xml 

Used key `JMXNR-FRPQH-VXRMX-KFX93-974RY` for ProductKey `element`.
Add the following to `Specialize.ps1`

    $neccessaryFeatures = "Web-Server","Web-WebServer","Web-Common-Http","Web-Default-Doc","Web-Dir-Browsing","Web-Http-Errors","Web-Static-Content","Web-Http-Redirect","Web-Health","Web-Http-Logging","Web-Custom-Logging","Web-Log-Libraries","Web-ODBC-Logging","Web-Request-Monitor","Web-Http-Tracing","Web-Performance","Web-Stat-Compression","Web-Dyn-Compression","Web-Security","Web-Filtering","Web-Basic-Auth","Web-CertProvider","Web-Client-Auth","Web-Digest-Auth","Web-Cert-Auth","Web-IP-Security","Web-Url-Auth","Web-Windows-Auth","Web-App-Dev","Web-Net-Ext","Web-Net-Ext45","Web-AppInit","Web-ASP","Web-Asp-Net","Web-Asp-Net45","Web-CGI","Web-ISAPI-Ext","Web-ISAPI-Filter","Web-Includes","Web-WebSockets","Web-Mgmt-Tools","Web-Mgmt-Console","Web-Mgmt-Compat","Web-Metabase","Web-Lgcy-Mgmt-Console","Web-Lgcy-Scripting","Web-WMI","Web-Scripting-Tools","Web-Mgmt-Service","WAS","WAS-Process-Model","WAS-NET-Environment","WAS-Config-APIs","NET-Framework-Features","NET-Framework-Core","NET-Framework-45-Features","NET-Framework-45-Core","NET-Framework-45-ASPNET","NET-WCF-Services45","NET-WCF-HTTP-Activation45","Web-WHC"

    Install-WindowsFeature $neccessaryFeatures


##Spawning a new Windows DEA on hpcloud.com

It takes the windows image ~12 min to spawn.
Installing dependencies and the services takes ~2 min using resources from the local LAN.


##Configs for Demo site

NATS: `nats://192.168.0.130:4222`
Domain: `15.125.74.2.xip.io`
Redis: `redis://192.168.0.130:7474/0`

##Installers download links from hpcloud LAN 

`http://15.125.102.70/installers/install.ps1`
`http://15.125.102.70/installers/ZeroMQ-3.2.4~miru1.0-x64.exe`
`http://15.125.102.70/installers/Git-1.9.4-preview20140929.exe`
`http://15.125.102.70/installers/logyard-installer-1.2.38.exe`
`http://15.125.102.70/installers/deainstaller-1.2.38.msi`

##install.ps1 settings

These are settings from the param section of the install script, taht can be copy pasted for easy testing.

    $messageBus = 'nats://192.168.0.130:4222',
    $domain = '15.125.74.2.xip.io',
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
    $logyardRedisURL = 'redis://192.168.0.130:7474/0',
    $defaultGitPath = "c:\Program Files (x86)\git\bin\git.exe",
    $deaDownloadURL = "http://15.125.102.70/installers/deainstaller-1.2.47.msi",
    $logyardInstallerURL = "http://15.125.102.70/installers/logyard-installer-1.2.27.exe",
    $zmqDownloadURL = "http://15.125.102.70/installers/ZeroMQ-3.2.4~miru1.0-x64.exe",
    $gitDownloadURL = "http://15.125.102.70/installers/Git-1.9.4-preview20140929.exe"



##Better Windows 2012

Use `OS_REGION_NAME=region-b.geo-1` for testing the specialized image.


##MS SQL on ALS

These is a log of what we've done on the mssql gateway box so far, to try an make it work with Stackato.

Get a new Ubuntu 10.04
Do the following (everything as root, to keep things simple):

    apt-get update
    apt-get install git-core build-essential libcurl4-openssl-dev libpq-dev libexpat1-dev

    curl -sSL https://get.rvm.io | bash
    source /etc/profile.d/rvm.sh
    rvm install 1.9.3
    gem install bundler

    git clone git@github.com:UhuruSoftware/uhuru-services-release.git --recursive
    cd ./uhuru-services-release/src/mssql_service
    bundle install --without test
