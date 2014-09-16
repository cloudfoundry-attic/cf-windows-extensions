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

1. Software Services and Components
 - Windows DEA (Droplet Execution Engine), providing:
    - Management of Windows Warden containers
    - Staging of Windows applications
    - Execution of Windows droplets (using the IIS hostable web core)
 - Windows Build Pack
 - Automated (un-)installer; provisioning of Windows application-host nodes in a HP Helion ALS cluster
 - Standard Visual Studio hosted ASP.net test application using membership, using MySQL for persistence

  **Planned Completion Date**:01/01/2000

2. Automated Test Suites
 - Automated test suite validating the (un-) installer functionality testing provisioning and de-provisioning of Windows application-host nodes in a HP Helion ALS cluster
 - Automated test suite validating the complete set of application lifecycle cfcommands for Windows applications

  **Planned Completion Date**:01/01/2000

3. Documentation
 - Wiki style documentation covering:
    - Installation and uninstalling Windows application-host nodes
    - Troubleshooting Windows application-host nodes installation
    - The complete lifecycle of a Windows application using the cf-commands
    - Troubleshooting all the steps/stages in the Windows applications lifecycle
    - Demo script installing, deploying and managing the lifecycle of the two reference applications

  **Planned Completion Date**:01/01/2000

##Questions/Open Issues

1. Is HP ALS completely compatible with Stackato?
We downloaded the micro-cloud. We could use this for tweaking the messaging for the Windows DEA.   
*Yes, it's compatible with the latest*

2. The SOW mentions `warden` containers. We are not planning to use the windows-warden interface for this project, since it's not needed. They would be `prison` containers.
*Just boilerplate wording*

3. The SOW mentions cf-commands. Can we assume that we don't need to add compatibility for stackato specific commands? 
*Make a list of diffs, and decide after; careful about licensing*

4. Automatic deployment on an ALS cluster. Do we need to provide automation for spawning Windows VMs, or just automation for the installation process? 
*Just use Nova commands*
*For private and managed, in the horizon panel we need to allow user to say how many Windows nodes they want*
*We probably won't code the horizon extension, just have an automated solution (a shell script that (de)spawns a windea)*

5. Does HP support cloud-init for Windows? 
*we will have to test*
*no 2012 images right now*

5. Can HP provide us with an OpenStack environment for testing?
*We might have access to some environments. Could be public access. ALS will not be the same there. You start with Stackato 3.4*
*In private deployments you go to horizon and use a wizard*
*Once we have something to deploy, we'll figure something out for the private installation*

6. We have previously obtained permission from Stackato to look at NATS messages via nats-sub, since they have a restrictive license. Should we obtain permission again?
*Gert will talk to Jeff Hobbs*

7. Which accounts should we add to github and trello?
- github *gert.drapers@live.com*
- trello *gert.drapers@live.com*

8. Are Thursdays good for the status report?
*Good, 10:00 PM Thursday*
*Friday or Wednesdays 8:00 AM*

  *Wednesday 6PM*

9. Is this document good as a template for a status report? 
*Yep*

10. Where should we host build artifacts? Keep the in the Romania LAN for now?
*github*
*ftp*

##Work Items (to be moved to Trello)

1. CI for unit testing *not needed for HP*
 - Deploy a Windows Jenkins Server in the Romania LAN
 - Create a project that can test/build each commit
 - Expose the Jenkins server to the internet
 - Create account(s) for HP
 - Setup a site for hosting build artifacts (WinDEA installer)
2. Documentation
 - Start a markdown document (vcap-dotnet-hp/doc/windea.md) with sections all items in the deliverable 
3. NATS messaging compatibility *Stackato should be closer to v2 than v1 used to be*
 - Research for incompatibilities between vanilla CFv2 and Stackato
 - Implement changes in the Windows DEA
 - Document application lifecycle
4. Integrate the latest prison code with the v2 DEA
 - Create a submodule for the prison library in the vcap-dotnet-hp repository
 - Replace the older prison code
5. Logging compatibility (Stackato does not use the loggregator, but logyard)
 - Research how Stackato handles logging
 - Document application lifecycle
6. Deployment Automation
 - Setup an OpenStack environment
 - Research how ALS cluster creation/destruction happens *Not needed, it's a script + a Horizon extension*
 - *Implementation*
 - Document installation procedures
7. CI for system tests
 - *not sure how this will work yet*
 *we'll do Linux based tests, probably go (because of the go cf client)*
