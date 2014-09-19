# Prerequisites
	Create and run the following script:


> 
> rm -rf win2012r2.qcow2
>
> [ ! -e virtio-win-0.1-81.iso ] && wget http://alt.fedoraproject.org/pub/alt/virtio-win/latest/images/virtio-win-0.1-81.iso
>
> qemu-img create -f raw win2012r2.raw 10G
> 
> virsh destroy win2012r2
> virsh undefine win2012r2
> 
> virt-install --connect qemu:///system \
>   --name win2012r2 --ram 2048 --vcpus 2 \
>   --network network=default,model=virtio \
>   --disk path=/home/uhuru/win2012r2/win2012r2.raw,device=disk,bus=virtio \
>   --cdrom /home/uhuru/win2012r2/en_windows_server_2012_r2_x64_dvd_2707946.iso \
>   --disk path=/home/uhuru/win2012r2/virtio-win-0.1-81.iso,device=cdrom \
>   --vnc --os-type windows --os-variant win7 \
>   --force


# Windows installation

	Enable the VirtIO drivers.

The disk is not detected by default by the Windows installer. When requested to choose an installation target, click Load driver and browse the file system to select the E:\WIN8\AMD64 folder. The Windows installer displays a list of drivers to install. Select the VirtIO SCSI and network drivers, and continue the installation.

	Open a terminal and install all drivers :

> pnputil -i -a E:\WIN8\AMD64\*.INF

	Download and install CloudBase-Init (in the same terminal)

> powershell
> 
> Set-ExecutionPolicy Unrestricted
> 
> Invoke-WebRequest -UseBasicParsing http://www.cloudbase.it/downloads/CloudbaseInitSetup_Beta_x64.msi -OutFile cloudbaseinit.msi
> 
> .\cloudbaseinit.msi

In the configuration options window, change the following settings:

Username: Administrator

Network adapter to configure: Red Hat VirtIO Ethernet Adapter

Serial port for logging: COM1

When the installation is done, in the Complete the Cloudbase-Init Setup Wizard window, select the Run Sysprep and Shutdown check boxes and click Finish.

Wait for the machine shutdown.
