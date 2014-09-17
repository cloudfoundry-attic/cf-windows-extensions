Usage and Deployment of the Windows Extensions for HP ALS
=========================================================


##Deployment

###Changes to the HP ALS installation

- [*optional*] Add the IIS8 buildpack to your ALS installation

      wget https://github.com/UhuruSoftware/uhuru-buildpack-iis8/archive/master.zip -O dotNet.zip
      stackato create-buildpack dotNet dotNet.zip


###Adding a Windows DEA to the cluster

####Making the win2012 stack available

The cloud foundry stacks can be configured in the cloud controller's stacks.yml configuration file. The stack used for the Windows DEA is `win2012`. This is how the stacks.yml config file should look like:

    default: "lucid64"
    stacks:
     - name: "lucid64"           
       description: "Linux stack"
     - name: "win2012"           
       description: "Windows"

####The NATS URL

For development purposes, the NATS url can be found in the cloud_controller.yml configuration file.
Can we assume that in an automated scenario, the NATS url is well known?


####Troubleshooting

#####Log files

##Usage

###IIS8 Buildpack

###Sample Applications

####Vanilla sample app with a MySQL Membership Provide

####Umbraco

###Debugging your app

####Resource constraints

####Streaming log files

####Configuration files

- root web.config file
- applicationHost config file

