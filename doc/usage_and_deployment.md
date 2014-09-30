Usage and Deployment of the Windows Extensions for HP ALS
=========================================================


##Deployment

###Deploying stackato micro cloud

Full Documentation is available online on the stackato website: https://docs.stackato.com/admin/server/hpcs.html

- Create Security Group and add HTTP, HTTPS and SSH ingress rules
- Add Ingress Rule: All TCP ports; set the current security group as Remote
- Launch a "ActiveState Stackato v3.4.1" instance with 4GB of RAM (standard.medium)
- Associate floating IP to the newly created instance
- Login using ssh and rename the node: `kato node rename {floating_ip}.xip.io`

At this point, the web console should be available at {floating_ip}.xip.io

####Setup node in cluster mode

    kato node setup core
    kato role add dea

- add additional roles using `kato role add` (eg mysql, view available roles with `kato info`)

###Changes to the HP ALS installation

- on the stackato instance, edit /home/stackato/stackato/code/cloud_controller_ng/config/stacks.yml and add windows stack
```
default: "lucid64"
stacks: 
 - name: lucid64
   description: "Ubuntu 10.04 on x86-64"
 - name: windows2012r2
   description: "Windows 2012 R2"
```
- run `kato restart`

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

