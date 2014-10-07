Usage and Deployment of the Windows Extensions for HP ALS
=========================================================


##Deployment

###Changes to the HP ALS installation

- [*optional*] Add the IIS8 buildpack to your ALS installation

      wget https://github.com/UhuruSoftware/uhuru-buildpack-iis8/archive/master.zip -O dotNet.zip
      stackato create-buildpack dotNet dotNet.zip


###Adding a Windows DEA to the cluster

####The NATS URL

####Troubleshooting

#####Log files

##Usage

###IIS8 Buildpack

###Sample Applications

####Vanilla sample app with a MySQL Membership Provider

- Create the app yourself
	- From Visual Studio 2013 create an empty ASP.NET application
	<img src="create_app.png"/>
	- Install the MySQL connector nuget packages: MySql.ConnectorNET.Entity, MySql.ConnectorNET.Data, MySql.ConnectorNET.Web
	<img src="install_nuget_packages.png"/>
	- In web.config, add autogenerateschema="true" in membership provider MySQLMembershipProvider section
	<img src="add_autogenerate_section.png"/>
	- In web.config add connectionStrings section
	<img src="add_connectionString_section.png"/>
- Download the app


####Umbraco

###Debugging your app

####Resource constraints

####Streaming log files

####Configuration files

- root web.config file
- applicationHost config file

# Windows DEA creation

## Using the evaluation VM available from cloudbase

You can download an evaluation VM offered by cloudbase from **http://www.cloudbase.it/ws2012r2/**

## Prerequisites for manual customization

>Install a linux distro with X server and the following packages:
	
	apt-get install -y qemu-kvm qemu-common virt-manager virt-viewer

	
	
>Create and run the following script :


 
     rm -rf win2012r2.qcow2
    
     [ ! -e virtio-win-0.1-81.iso ] && wget http://alt.fedoraproject.org/pub/alt/virtio-win/latest/images/virtio-win-0.1-81.iso
    
     qemu-img create -f raw win2012r2.raw 10G
     
     virsh destroy win2012r2
     virsh undefine win2012r2
     
     virt-install --connect qemu:///system \
       --name win2012r2 --ram 2048 --vcpus 2 \
       --network network=default,model=virtio \
       --disk path=/home/uhuru/win2012r2/win2012r2.raw,device=disk,bus=virtio \
       --cdrom /home/uhuru/win2012r2/en_windows_server_2012_r2_x64_dvd_2707946.iso \
       --disk path=/home/uhuru/win2012r2/virtio-win-0.1-81.iso,device=cdrom \
       --vnc --os-type windows --os-variant win7 \
       --force
    

## Windows installation

>Enable the VirtIO drivers.
	
The disk is not detected by default by the Windows installer. When requested to choose an installation target, click Load driver and browse the file system to select the E:\WIN8\AMD64 folder. The Windows installer displays a list of drivers to install. Select the VirtIO SCSI and network drivers, and continue the installation.

>Open a terminal and install all drivers :

	pnputil -i -a E:\WIN8\AMD64\*.INF

>Download and install CloudBase-Init (in the same terminal)

     powershell
     
     Set-ExecutionPolicy Unrestricted
     
     Invoke-WebRequest -UseBasicParsing http://www.cloudbase.it/downloads/CloudbaseInitSetup_Beta_x64.msi -OutFile cloudbaseinit.msi
     
     .\cloudbaseinit.msi
    
In the configuration options window, change the following settings:

Username: *Administrator*

Network adapter to configure: *Red Hat VirtIO Ethernet Adapter*

Serial port for logging: *COM1*

When the installation is done, in the Complete the Cloudbase-Init Setup Wizard window, select the Run Sysprep and Shutdown check boxes and click Finish.

Wait for the machine shutdown.

> Upload the image using glance

	glance image-create --name WS2012 --disk-format qcow2 --container-format bare --is-public true --file ws2012.qcow2