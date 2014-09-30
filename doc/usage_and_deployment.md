Usage and Deployment of the Windows Extensions for HP ALS
=========================================================


##Deployment

###Changes to the HP ALS installation

- [*optional*] Add the IIS8 buildpack to your ALS installation

      wget https://github.com/UhuruSoftware/uhuru-buildpack-iis8/archive/master.zip -O dotNet.zip
      stackato create-buildpack dotNet dotNet.zip


###Adding a Windows DEA to the cluster

On a Windows Server 2012 R2 enable IIS Web Server Role with all Role Services except WebDAV support.

1. The following features need to be available on the Windows server:
	
	- Windows Process Activation Service
		- Process Model
		- .NET Environment
		- Configuration APIs
	- .NET Framework Features
		- .NET Framework 3.5.1
		- .NET Framework 4.0
		- WCF Activation
			- HTTP Activation
			- Non-HTTP Activation      

2. Install Git following standard installation.

3. Install DEA using the DEAInstaller.msi; this will install the necessary files and create a windows service  
called WinDEA.
	

	- The installer can be used from the command line by using msiexec 
		
		<code>Sample call: msiexec /i c:\DEAInstaller.msi /qn messageBus="nats://192.168.1.109:4222/" baseDir="C:\droplets"  maxMemory="4000" /lvx! C:\\DEAInstaller.log ALLUSERS=2</code>

	- The following parameters can be used with the installer; it is **required** to set the MessageBus and Domain according to your configuration
	
			- MessageBus  		   
			- Domain	 
			- Index 	 		   -> default value: 0
			- BaseDir 			   -> default value: C:\droplets
			- LocalRoute 		   -> default value: 8.8.8.8
			- FilerPort   		   -> default value: 12345
			- StatusPort  		   -> default value: 0			 		   
			- MultiTenant 		   -> default value: true
			- MaxMemoryMB 		   -> default value: 4096
			- Secure			   -> default value: true
			- EnforceULimit 	   -> default value: true
			- HeartBeatIntervalMS  -> default value: 10000
			- AdvertiseIntervalMS  -> default value: 5000
			- UseDiskQuota		   -> default value: true
			- UploadThrottleBitsPS -> default value: 0
			- MaxConcurrentStarts  -> default value: 3
			- DirectoryServerPort  -> default value: 34567
			- StramingTimeoutMS	   -> default value: 60000
			- StagingEnabled	   -> default value: true
			- BuildpacksDirectory  -> default value: buildpacks
			- Git				   -> default value: C:\Program Files (x86)\Git\bin\git.exe
			- StagingTimeoutMS	   -> default value: 1200000
			- Stacks			   -> default value: iis8

  Default IIS8 buildpack will be **automatically** installed in the buildpacks folder of the target directory.

####The NATS URL

####Troubleshooting

#####Log files

##Usage

###IIS8 Buildpack

###Sample Applications

####Vanilla sample app with a MySQL Membership Provider

- Create the app yourself
 - Get the MySQL connector 
- Download the app


####Umbraco

###Debugging your app

####Resource constraints

####Streaming log files

####Configuration files

- root web.config file
- applicationHost config file

