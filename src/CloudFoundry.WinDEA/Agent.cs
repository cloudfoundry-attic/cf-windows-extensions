﻿namespace CloudFoundry.WinDEA
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;    
    using CloudFoundry.WinDEA.DirectoryServer;
    using CloudFoundry.WinDEA.Messages;
    using CloudFoundry.Configuration;
    using CloudFoundry.NatsClient;
    using CloudFoundry.Utilities;
    using CloudFoundry.Utilities.Json;
    using System.Collections.Specialized;
    using Microsoft.Win32;
    using System.Text;
    using System.Web;
    using System.Security.Cryptography;    

    /// <summary>
    /// Callback with a Boolean parameter.
    /// </summary>
    /// <param name="state">if set to <c>true</c> [state].</param>
    public delegate void BoolStateBlockCallback(bool state);

    /// <summary>
    /// The Agent class is the DEA engine. It handles all the messages it receives on the message bus and send appropriate messages when it is requested to do so,
    /// or some external event happened.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Trying to keep similarity to Ruby version.")]
    public sealed class Agent : VCAPComponent, IDeaClient
    {
        /// <summary>
        /// The DEA version.
        /// </summary>
        private const decimal Version = 0.99m;

        /// <summary>
        /// Home variable.
        /// </summary>
        private const string HomeVariable = "HOME";

        /// <summary>
        /// Application variable.
        /// </summary>
        private const string VcapApplicationVariable = "VCAP_APPLICATION";

        /// <summary>
        /// Services variable.
        /// </summary>
        private const string VcapServicesVariable = "VCAP_SERVICES";

        /// <summary>
        /// Vcap Application Host Variable.
        /// </summary>
        private const string VcapAppHostVariable = "VCAP_APP_HOST";

        /// <summary>
        /// Vcap Application Port.
        /// </summary>
        private const string VcapAppPortVariable = "VCAP_APP_PORT";

        /// <summary>
        /// Vcap Windows User.
        /// </summary>
        private const string VcapWindowsUserVariable = "VCAP_WINDOWS_USER";

        /// <summary>
        /// Vcap Windows User Password.
        /// </summary>
        private const string VcapWindowsUserPasswordVariable = "VCAP_WINDOWS_USER_PASSWORD";

        /// <summary>
        /// Vcap Application Pid.
        /// </summary>
        private const string VcapAppPidVariable = "VCAP_APP_PID";

        /// <summary>
        /// The the droplets the DEA manages.
        /// </summary>
        private DropletCollection droplets = new DropletCollection();

        /// <summary>
        /// The application stager.
        /// </summary>
        private ApplicationBits fileResources = new ApplicationBits();

        /// <summary>
        /// The DEA's HTTP droplet file viewer. Helps receive the logs.
        /// </summary>
        private CloudFoundry.WinDEA.DirectoryServer.DirectoryServer fileViewer = new CloudFoundry.WinDEA.DirectoryServer.DirectoryServer();

        /// <summary>
        /// The monitoring resource.
        /// </summary>
        private Monitoring monitoring = new Monitoring();

        /// <summary>
        /// Set to true when more applications are allowed to be hosted on the DEA.
        /// </summary>
        private bool multiTenant;

        /// <summary>
        /// If secure mode is enabled.
        /// </summary>
        private bool secure;

        /// <summary>
        /// If the enforcement of usage limit is enabled.
        /// </summary>
        private bool enforceUlimit;

        /// <summary>
        /// If the enforcement of usage limit is enabled.
        /// </summary>
        private bool useDiskQuota;

        /// <summary>
        /// The network outbound throttle limit to be enforced for the running apps. This rule is enforced per droplet.
        /// Units are in Bits Per Second.
        /// </summary>
        private long uploadThrottleBitsps;

        /// <summary>
        /// The DEA reactor. Is is the middleware to the message bus. 
        /// </summary>
        private DeaReactor deaReactor;

        /// <summary>
        /// The hello message send to the message bus.
        /// </summary>
        private HelloMessage helloMessage = new HelloMessage();

        /// <summary>
        /// Flag set to true when the system is shutting down. It used to avoiding processing some routines when the DEA is preparing to shut down.
        /// </summary>
        private volatile bool shuttingDown = false;

        /// <summary>
        /// The delay
        /// </summary>
        private int evacuationDelayMs = 30 * 1000;

        /// <summary>
        /// The minimum register interval in seconds for routers. This is the minimum between all intervals announced on router.start.
        /// </summary>
        private int minRouterRegisterInterval = int.MaxValue;

        /// <summary>
        /// The timer for router register.
        /// </summary>
        private System.Timers.Timer routerRegisterTimer;

        /// <summary>
        /// Directory Server V2 Port.
        /// </summary>
        private int directoryServerPort;

        private bool enableStaging;
        private StagingRegistry stagingTaskRegistry;
        private string directoryServerHmacKey;
        private string gitPath;
        private string buildpacksDir;
        private int stagingTimeoutMs;
        private string logyardUidPath;
        public string ExternalHost { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Agent"/> class. Loads the configuration and initializes the members.
        /// </summary>
        public Agent()
        {
            CloudFoundrySection cfSection = (CloudFoundrySection)ConfigurationManager.GetSection("cloudfoundry");

            foreach (StackElement deaConf in cfSection.DEA.Stacks)
            {
                this.fileResources.Stacks.Add(deaConf.Name);
            }

            string baseDir = cfSection.DEA.BaseDir;
            this.fileResources.DropletDir = new DirectoryInfo(baseDir).FullName;

            this.fileResources.DisableDirCleanup = cfSection.DEA.DisableDirCleanup;
            this.multiTenant = cfSection.DEA.Multitenant;
            this.secure = cfSection.DEA.Secure;
            this.enforceUlimit = cfSection.DEA.EnforceUsageLimit;

            this.monitoring.MaxMemoryMbytes = cfSection.DEA.MaxMemoryMB;

            this.directoryServerPort = cfSection.DEA.DirectoryServer.V2Port;

            this.useDiskQuota = cfSection.DEA.UseDiskQuota;

            this.uploadThrottleBitsps = cfSection.DEA.UploadThrottleBitsps;

            this.enableStaging = cfSection.DEA.Staging.Enabled;

            this.logyardUidPath = cfSection.DEA.LogyardUidPath;

            // Replace the ephemeral monitoring port with the configured one
            if (cfSection.DEA.StatusPort > 0)
            {
                this.Port = cfSection.DEA.StatusPort;
            }

            this.ComponentType = "DEA";
            if (cfSection.DEA.Index >= 0)
            {
                this.Index = cfSection.DEA.Index;
            }

            if (this.Index != null)
            {
                this.UUID = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", this.Index, this.UUID);
            }

            this.monitoring.MonitorIntervalMilliseconds = cfSection.DEA.HeartbeatIntervalMs;
            this.monitoring.AdvertiseIntervalMilliseconds = cfSection.DEA.AdvertiseIntervalMs;

            this.monitoring.MaxClients = this.multiTenant ? Monitoring.DefaultMaxClients : 1;

            this.fileResources.StagedDir = Path.Combine(this.fileResources.DropletDir, "staged");
            this.fileResources.AppsDir = Path.Combine(this.fileResources.DropletDir, "apps");
            this.fileResources.DBDir = Path.Combine(this.fileResources.DropletDir, "db");
            this.fileResources.StagingDir = Path.Combine(this.fileResources.DropletDir, "staging");

            this.droplets.AppStateFile = Path.Combine(this.fileResources.DropletDir, "applications.json");

            this.deaReactor.UUID = this.UUID;

            this.helloMessage.Id = this.UUID;
            this.helloMessage.Host = this.Host;
            this.helloMessage.FileViewerPort = this.directoryServerPort;
            this.helloMessage.Version = Version;

            this.stagingTaskRegistry = new StagingRegistry();
            this.stagingTaskRegistry.StagingStateFile = Path.Combine(this.fileResources.DBDir, "staging.json");

            this.directoryServerHmacKey = Credentials.GenerateSecureGuid().ToString("N");
            this.ExternalHost = string.Format("{0}.{1}", Guid.NewGuid().ToString("N"), cfSection.DEA.Domain);

            this.gitPath = cfSection.DEA.Staging.GitExecutable;
            this.buildpacksDir = cfSection.DEA.Staging.BuildpacksDirectory;
            this.stagingTimeoutMs = cfSection.DEA.Staging.StagingTimeoutMs;
        }

        /// <summary>
        /// Looks up the path in the DEA.
        /// </summary>
        /// <param name="path">The path to lookup.</param>
        /// <returns>
        /// A PathLookupResponse containing the response from the DEA.
        /// </returns>
        public PathLookupResponse LookupPath(Uri path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            PathLookupResponse response = new PathLookupResponse();
            response.Error = string.Empty;

            NameValueCollection queryStrings = System.Web.HttpUtility.ParseQueryString(path.Query);
            string actualPath = HttpUtility.UrlDecode(queryStrings["path"]);

            switch (path.Segments[1].Replace("/", string.Empty))
            {
                case "instance_paths":
                    {
                        this.droplets.ForEach(delegate(DropletInstance droplet)
                        {
                            if (droplet.Properties.InstanceId == path.Segments[2].Replace("/", string.Empty))
                            {
                                if (DEAUtilities.VerifyHmacedUri(path.ToString(), this.directoryServerHmacKey, new string[] { "path", "timestamp" }))
                                {
                                    string physicalPath = Path.GetFullPath((new Uri(Path.Combine(droplet.Properties.Directory, ".\\" + actualPath))).LocalPath); ;
                                    if (physicalPath.StartsWith(droplet.Properties.Directory + Path.DirectorySeparatorChar))
                                    {
                                        response.Path = Path.GetFullPath(physicalPath);
                                    }
                                    else
                                    {
                                        response.Error = "Cannot access path";
                                    }
                                }
                                else
                                {
                                    response.Error = "Invalid HMAC";
                                }
                                if (!DEAUtilities.CheckUrlAge(path.ToString()))
                                {
                                    response.Error = "URL expired";
                                }
                            }
                        });
                        break;
                    }
                case "staging_tasks":
                    {
                        this.stagingTaskRegistry.ForEach(delegate(StagingInstance instance)
                        {
                            if (instance.Properties.TaskId == path.Segments[2].Replace("/", string.Empty))
                            {
                                if (DEAUtilities.VerifyHmacedUri(path.ToString(), this.directoryServerHmacKey, new string[] { "path", "timestamp" }))
                                {
                                    response.Path = instance.Properties.TaskLog;
                                }
                                else
                                {
                                    response.Error = "Invalid HMAC";
                                }
                                if (!DEAUtilities.CheckUrlAge(path.ToString()))
                                {
                                    response.Error = "URL expired";
                                }
                            }
                        });
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            if (response.Path == null && string.IsNullOrEmpty(response.Error))
            {
                response.Error = "Staging task not found";
            }

            return response;
        }

        /// <summary>
        /// Runs the DEA.
        /// It prepares the NATS subscriptions, stats the NATS client, and the required timers.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "More clear"),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "It is needed to capture all exceptions.")]
        public override void Run()
        {
            Logger.Info(Strings.StartingVcapDea, Version);

            Logger.Info(Strings.UsingNetwork, this.Host);
            Logger.Info(Strings.MaxMemorySetTo, this.monitoring.MaxMemoryMbytes);
            Logger.Info(Strings.UtilizingCpuCores, DEAUtilities.NumberOfCores());

            if (this.multiTenant)
            {
                Logger.Info(Strings.Allowingmultitenancy);
            }
            else
            {
                Logger.Info(Strings.RestrictingToSingleTenant);
            }

            Logger.Info(Strings.UsingDirectory, this.fileResources.DropletDir);

            this.fileResources.CreateDirectories();
            this.droplets.AppStateFile = Path.Combine(this.fileResources.DBDir, "applications.json");

            // Clean everything in the staged directory
            this.fileResources.CleanCacheDirectory();


            CloudFoundry.WindowsPrison.Prison.Init();

            this.fileViewer.Start(this.Host, DirectoryConfiguration.ReadConfig(), this);

            this.VCAPReactor.OnNatsError += new EventHandler<ReactorErrorEventArgs>(this.NatsErrorHandler);

            this.deaReactor.OnDeaStatus += new SubscribeCallback(this.DeaStatusHandler);

            this.deaReactor.OnDeaFindDroplet += new SubscribeCallback(this.DeaFindDropletHandler);
            this.deaReactor.OnDeaUpdate += new SubscribeCallback(this.DeaUpdateHandler);
            this.deaReactor.OnDeaLocate += new SubscribeCallback(this.DeaLocateHandler);

            this.deaReactor.OnStagingLocate += new SubscribeCallback(this.StagingLocateHandler);
            this.deaReactor.OnStagingStart += new SubscribeCallback(this.StagingStartHandler);
            this.deaReactor.OnStagingStop += new SubscribeCallback(this.StagingStopHandler);

            this.deaReactor.OnDeaStop += new SubscribeCallback(this.DeaStopHandler);
            this.deaReactor.OnDeaStart += new SubscribeCallback(this.DeaStartHandler);

            this.deaReactor.OnRouterStart += new SubscribeCallback(this.RouterStartHandler);
            this.deaReactor.OnHealthManagerStart += new SubscribeCallback(this.HealthmanagerStartHandler);

            base.Run();  // Start the nats client

            this.RegisterRoutes();

            // Seed routerRegisterTimer with a 5 sec timer, to be compatible with the old router nats protocol.
            this.routerRegisterTimer = TimerHelper.RecurringLongCall(
                5 * 1000,
                delegate
                {
                    this.RegisterRoutes();
                });


            if (this.enableStaging)
            {
                this.deaReactor.SubscribeToStaging();
            }

            this.RecoverExistingDroplets();
            this.CleanupStagingInstances();

            this.DeleteUntrackedInstanceDirs();

            TimerHelper.RecurringLongCall(
                Monitoring.HeartbeatIntervalMilliseconds,
                delegate
                {
                    this.SendHeartbeat();
                });

            TimerHelper.RecurringLongCall(
                this.monitoring.AdvertiseIntervalMilliseconds,
                delegate
                {
                    this.SendAdvertise();
                });

            TimerHelper.RecurringLongCall(
                this.monitoring.MonitorIntervalMilliseconds,
                delegate
                {
                    this.MonitorApps();
                });

            TimerHelper.RecurringLongCall(
                Monitoring.CrashesReaperIntervalMilliseconds,
                delegate
                {
                    this.TheReaper();
                });

            TimerHelper.RecurringLongCall(
                Monitoring.VarzUpdateIntervalMilliseconds,
                delegate
                {
                    this.SnapshotVarz();
                });

            this.deaReactor.SendRouterGreetings(new SubscribeCallback(this.RouterStartHandler));

            this.deaReactor.SendDeaStart(this.helloMessage.SerializeToJson());
            this.SendAdvertise();

            if (enableStaging)
            {
                TimerHelper.RecurringLongCall(
                    this.monitoring.AdvertiseIntervalMilliseconds,
                    delegate
                    {
                        this.SendStagingAdvertise();
                    });

                this.SendStagingAdvertise();
            }

        }

        /// <summary>
        /// Loads the saved droplet instances the last dea process has saved using the ShanpShotAppState method. 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Object is properly disposed on failure."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is logged, and error must not bubble up.")]
        public void RecoverExistingDroplets()
        {
            if (!File.Exists(this.droplets.AppStateFile))
            {
                this.droplets.RecoveredDroplets = true;
                return;
            }

            object[] instances = JsonConvertibleObject.DeserializeFromJsonArray(File.ReadAllText(this.droplets.AppStateFile));

            foreach (object obj in instances)
            {
                DropletInstance instance = null;

                try
                {
                    instance = new DropletInstance();
                    instance.Properties.FromJsonIntermediateObject(obj);
                    instance.Properties.Orphaned = true;
                    instance.Properties.ResourcesTracked = false;
                    this.monitoring.AddInstanceResources(instance);
                    instance.Properties.StopProcessed = false;

                    Logger.Info("Recovering Instance: {0}", instance.Properties.ContainerId);

                    CloudFoundry.WindowsPrison.PrisonManager.LoadPrisonAndAttach(Guid.Parse(instance.Properties.ContainerId));

                    if (instance.Properties.State == DropletInstanceState.Starting)
                    {
                        this.DetectAppReady(instance);
                    }

                    this.droplets.AddDropletInstance(instance);
                    instance = null;
                }
                catch (Exception ex)
                {
                    Logger.Warning(Strings.ErrorRecoveringDropletWarningMessage, instance.Properties.InstanceId, ex.ToString());
                }
                finally
                {
                    if (instance != null)
                    {
                        instance.Dispose();
                    }
                }
            }

            this.droplets.RecoveredDroplets = true;

            if (this.monitoring.Clients > 0)
            {
                Logger.Info(Strings.DeaRecoveredApplications, this.monitoring.Clients);
            }

            this.MonitorApps();
            this.droplets.ForEach(delegate(DropletInstance instance)
            {
                this.RegisterInstanceWithRouter(instance);
            });
            this.SendHeartbeat();
            this.droplets.ScheduleSnapshotAppState();
        }

        /// <summary>
        /// First evacuates the Instances and after a delay it's calling the shutdown.
        /// </summary>
        public void EvacuateAppsThenQuit()
        {
            this.shuttingDown = true;

            Logger.Info(Strings.Evacuatingapplications);

            this.droplets.ForEach(delegate(DropletInstance instance)
            {
                try
                {
                    instance.Lock.EnterWriteLock();
                    if (instance.Properties.State != DropletInstanceState.Crashed)
                    {
                        Logger.Debug(Strings.EvacuatingApp, instance.Properties.InstanceId);

                        instance.Properties.ExitReason = DropletExitReason.DeaEvacuation;
                        this.deaReactor.SendDropletExited(instance.GenerateDropletExitedMessage().SerializeToJson());
                        instance.Properties.Evacuated = true;
                    }
                }
                finally
                {
                    instance.Lock.ExitWriteLock();
                }
            });

            Logger.Info(Strings.SchedulingShutdownIn, this.evacuationDelayMs);

            this.droplets.ScheduleSnapshotAppState();

            TimerHelper.DelayedCall(
                this.evacuationDelayMs,
                delegate
                {
                    this.Shutdown();
                });
        }

        /// <summary>
        /// Shuts down the DEA. First it stops all the instances and then the Nats client.
        /// </summary>
        public void Shutdown()
        {
            this.UnregisterDirectoryServer(this.Host, this.directoryServerPort, this.ExternalHost);

            this.shuttingDown = true;
            Logger.Info(Strings.ShuttingDownMessage);

            this.droplets.ForEach(
                true,
                delegate(DropletInstance instance)
                {
                    try
                    {
                        instance.Lock.EnterWriteLock();
                        if (instance.Properties.State != DropletInstanceState.Crashed)
                        {
                            instance.Properties.ExitReason = DropletExitReason.DeaShutdown;
                        }

                        this.StopDroplet(instance);
                    }
                    finally
                    {
                        instance.Lock.ExitWriteLock();
                    }
                });

            // Allows messages to get out.
            Thread.Sleep(500);

            this.fileViewer.Stop();

            this.deaReactor.NatsClient.Close();
            this.TheReaper();

            // Execute twice because of TryEnterWriteLock(10)
            this.TheReaper();
            this.TheReaper();

            this.droplets.ScheduleSnapshotAppState();
            Logger.Info(Strings.ByeMessage);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (this.droplets != null)
                    {
                        this.droplets.Dispose();
                    }

                    if (this.fileViewer != null)
                    {
                        this.fileViewer.Dispose();
                    }

                    if (this.monitoring != null)
                    {
                        this.monitoring.Dispose();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Constructs the needed reactor. In this case a DeaReactor is needed.
        /// </summary>
        protected override void ConstructReactor()
        {
            if (this.deaReactor == null)
            {
                this.deaReactor = new DeaReactor();
                this.VCAPReactor = this.deaReactor;
            }
        }

        /// <summary>
        /// Creates the services application variable used when configuring the plugin.
        /// </summary>
        /// <param name="services">The services received from the Cloud Controller.</param>
        /// <returns>The services application variable</returns>
        private static string CreateServicesApplicationVariable(Dictionary<string, object>[] services = null)
        {
            List<string> whitelist = new List<string>() { "name", "label", "plan", "tags", "options", "credentials" };
            Dictionary<string, List<Dictionary<string, object>>> svcs_hash = new Dictionary<string, List<Dictionary<string, object>>>();

            foreach (Dictionary<string, object> service in services)
            {
                string label = service["label"].ToString();
                if (!svcs_hash.ContainsKey(label))
                {
                    svcs_hash[label] = new List<Dictionary<string, object>>();
                }

                Dictionary<string, object> svc_hash = new Dictionary<string, object>();

                foreach (string key in whitelist)
                {
                    if (service[key] != null)
                    {
                        svc_hash[key] = service[key];
                    }
                }

                svcs_hash[label].Add(svc_hash);
            }

            return JsonConvertibleObject.SerializeToJson(svcs_hash);
        }

        /// <summary>
        /// Detects the if an app is ready and run the callback.
        /// </summary>
        /// <param name="instance">The instance to be checked.</param>
        /// <param name="callBack">The call back.</param>
        private static void DetectAppReady(DropletInstance instance, BoolStateBlockCallback callBack)
        {
            DetectPortReady(instance, callBack);
        }

        /// <summary>
        /// Detects if an application has the port ready and then invoke the call back.
        /// </summary>
        /// <param name="instance">The instance to be checked.</param>
        /// <param name="callBack">The call back.</param>
        private static void DetectPortReady(DropletInstance instance, BoolStateBlockCallback callBack)
        {
            int attempts = 0;
            bool keep_going = true;
            while (attempts <= 1000 && instance.Properties.State == DropletInstanceState.Starting && keep_going == true)
            {
                if (instance.IsPortReady(150))
                {
                    keep_going = false;
                    callBack(true);
                }
                else
                {
                    Thread.Sleep(100);
                    attempts++;
                }
            }

            if (keep_going)
            {
                callBack(false);
            }
        }

        /// <summary>
        /// If there are lingering instance directories in the application directory, delete them. 
        /// </summary>
        private void DeleteUntrackedInstanceDirs()
        {
            HashSet<string> trackedInstanceDirs = new HashSet<string>();

            this.droplets.ForEach(delegate(DropletInstance instance)
            {
                trackedInstanceDirs.Add(instance.Properties.Directory);
            });

            List<string> allInstanceDirs = Directory.GetDirectories(this.fileResources.AppsDir, "*", SearchOption.TopDirectoryOnly).ToList();

            List<string> to_remove = (from dir in allInstanceDirs
                                      where !trackedInstanceDirs.Contains(dir)
                                      select dir).ToList();

            foreach (string dir in to_remove)
            {
                Logger.Warning(Strings.RemovingInstanceDoesn, dir);
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    Logger.Warning(Strings.CloudNotRemoveInstance, dir, e.ToString());
                }
                catch (IOException e)
                {
                    Logger.Warning(Strings.CloudNotRemoveInstance, dir, e.ToString());
                }
            }
        }

        /// <summary>
        /// NATS the error handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="CloudFoundry.NatsClient.ReactorErrorEventArgs"/> instance containing the error data.</param>
        private void NatsErrorHandler(object sender, ReactorErrorEventArgs args)
        {
            // Only snapshot app state if we had a chance to recover saved state. This prevents a connect error
            // that occurs before we can recover state from blowing existing data away.
            if (this.droplets.RecoveredDroplets)
            {
                this.droplets.SnapshotAppState();
            }

            string errorThrown = args.Message == null ? string.Empty : args.Message;
            Logger.Fatal(Strings.ExitingNatsError, errorThrown);
            Environment.FailFast(string.Format(CultureInfo.InvariantCulture, Strings.NatsError, errorThrown), args.Exception);
        }

        /// <summary>
        /// Sends the heartbeat of every droplet instnace the DEA is aware of.
        /// </summary>
        private void SendHeartbeat()
        {
            string response = this.droplets.GenerateHeartbeatMessage(this.UUID).SerializeToJson();
            this.deaReactor.SendDeaHeartbeat(response);
        }

        /// <summary>
        /// Sends the heartbeat of every droplet instnace the DEA is aware of.
        /// </summary>
        private void SendAdvertise()
        {
            if (this.shuttingDown || this.monitoring.Clients >= this.monitoring.MaxClients || this.monitoring.MemoryReservedMbytes >= this.monitoring.MaxMemoryMbytes)
            {
                return;
            }

            DeaAdvertiseMessage response = new DeaAdvertiseMessage();

            response.Id = this.UUID;
            response.AvailableMemory = this.monitoring.MaxMemoryMbytes - this.monitoring.MemoryReservedMbytes;

            response.PhysicalMemory = (long)new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024 * 1024);

            string rootPath = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.Name.ToUpperInvariant() == rootPath.ToUpperInvariant());
            long driveSize = drive != null ? drive.TotalSize : 0;

            response.AvailableDisk = driveSize / (1024 * 1024);
            response.Ip = Host;

            response.PlacementProperties = new DeaAdvertiseMessagePlacementProperties()
            {
                AvailabilityZone = "default",
                Zone = "default",
                Zones = new string[] { "default" }
            };


            response.Stacks = this.fileResources.Stacks.ToList();


            response.AppIdCount = new Dictionary<string, int>();

            this.droplets.ForEach(delegate(DropletInstance instance)
            {
                if (instance.Properties.State == DropletInstanceState.Running)
                {
                    if (!response.AppIdCount.ContainsKey(instance.Properties.DropletId))
                    {
                        response.AppIdCount[instance.Properties.DropletId] = 0;
                    }

                    response.AppIdCount[instance.Properties.DropletId] += 1;
                }
            });

            this.deaReactor.SendDeaAdvertise(response.SerializeToJson());
        }

        private void SendStagingAdvertise()
        {
            if (this.shuttingDown || this.monitoring.Clients >= this.monitoring.MaxClients || this.monitoring.MemoryReservedMbytes >= this.monitoring.MaxMemoryMbytes)
            {
                return;
            }

            StagingAdvertiseMessage response = new StagingAdvertiseMessage();

            response.Id = this.UUID;
            response.AvailableMemory = this.monitoring.MaxMemoryMbytes - this.monitoring.MemoryReservedMbytes;
            response.Stacks = this.fileResources.Stacks.ToList();

            this.deaReactor.SendStagingAdvertise(response.SerializeToJson());
        }

        /// <summary>
        /// Snapshots the varz with basic resource information.
        /// </summary>
        private new void SnapshotVarz()
        {
            try
            {
                VarzLock.EnterWriteLock();

                base.SnapshotVarz();

                Varz["apps_max_memory"] = this.monitoring.MaxMemoryMbytes;
                Varz["apps_reserved_memory"] = this.monitoring.MemoryReservedMbytes;
                Varz["apps_used_memory"] = this.monitoring.MemoryUsageKbytes / 1024;
                Varz["num_apps"] = this.monitoring.Clients;
                if (this.shuttingDown)
                {
                    Varz["state"] = "SHUTTING_DOWN";
                }
            }
            finally
            {
                VarzLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// The hander for dea.status message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="reply">The reply.</param>
        /// <param name="subject">The subject.</param>
        private void DeaStatusHandler(string message, string reply, string subject)
        {
            Logger.Debug(Strings.DEAreceivedstatusmessage);
            DeaStatusMessageResponse response = new DeaStatusMessageResponse();

            response.Id = UUID;
            response.Host = Host;
            response.FileViewerPort = this.directoryServerPort;
            response.Version = Version;
            response.MaxMemoryMbytes = this.monitoring.MaxMemoryMbytes;
            response.MemoryReservedMbytes = this.monitoring.MemoryReservedMbytes;
            response.MemoryUsageKbytes = this.monitoring.MemoryUsageKbytes;
            response.NumberOfClients = this.monitoring.Clients;
            if (this.shuttingDown)
            {
                response.State = "SHUTTING_DOWN";
            }

            this.deaReactor.SendReply(reply, response.SerializeToJson());
        }

        /// <summary>
        /// The handler for dea.find.droplet message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="reply">The reply.</param>
        /// <param name="subject">The subject.</param>
        private void DeaFindDropletHandler(string message, string reply, string subject)
        {
            if (this.shuttingDown)
            {
                return;
            }

            DeaFindDropletMessageRequest pmessage = new DeaFindDropletMessageRequest();
            pmessage.FromJsonIntermediateObject(JsonConvertibleObject.DeserializeFromJson(message));

            Logger.Debug(Strings.DeaReceivedFindDroplet, message);

            this.droplets.ForEach(delegate(DropletInstance instance)
            {
                try
                {
                    instance.Lock.EnterReadLock();

                    bool droplet_match = instance.Properties.DropletId == pmessage.DropletId;
                    bool version_match = pmessage.Version == null || pmessage.Version == instance.Properties.Version;
                    bool instace_match = pmessage.InstanceIds == null || pmessage.InstanceIds.Contains(instance.Properties.InstanceId);
                    bool index_match = pmessage.Indexes == null || pmessage.Indexes.Contains(instance.Properties.InstanceIndex);
                    bool state_match = pmessage.States == null || pmessage.States.Contains(instance.Properties.State);

                    DeaFindDropletMessageResponse response = new DeaFindDropletMessageResponse();

                    if (droplet_match && version_match && instace_match && index_match && state_match)
                    {
                        response.DeaId = UUID;
                        response.Version = instance.Properties.Version;
                        response.DropletId = instance.Properties.DropletId;
                        response.InstanceId = instance.Properties.InstanceId;
                        response.Index = instance.Properties.InstanceIndex;
                        response.HostIp = this.Host;
                        response.State = instance.Properties.State;
                        response.StateTimestamp = instance.Properties.StateTimestamp;
                        response.FileUri = string.Format(CultureInfo.InvariantCulture, Strings.HttpDroplets, Host, this.directoryServerPort);

                        if (pmessage.Path != null)
                        {
                            string uri = string.Format(
                                CultureInfo.InvariantCulture,
                                "http://{0}/instance_paths/{1}?path={2}&timestamp={3}",
                                this.ExternalHost,
                                Uri.EscapeUriString(response.InstanceId),
                                Uri.EscapeUriString(pmessage.Path),
                                RubyCompatibility.DateTimeToEpochSeconds(DateTime.Now));

                            response.FileUriV2 = DEAUtilities.GetHmacedUri(uri, this.directoryServerHmacKey, new string[] { "path", "timestamp" }).ToString();
                        }
                        else
                        {
                            string uri = string.Format(
                                CultureInfo.InvariantCulture,
                                "http://{0}/instance_paths/{1}?path&timestamp={2}",
                                this.ExternalHost,
                                Uri.EscapeUriString(response.InstanceId),
                                RubyCompatibility.DateTimeToEpochSeconds(DateTime.Now));

                            response.FileUriV2 = DEAUtilities.GetHmacedUri(uri, this.directoryServerHmacKey, new string[] { "path", "timestamp" }).ToString();
                        }

                        Logger.Debug(Strings.DebugFileUriV2Path, response.FileUri);

                        response.FileAuth = new string[] { "una", "doua" };
                        response.Staged = instance.Properties.Staged;
                        response.DebugIP = instance.Properties.DebugIP;
                        response.DebugPort = instance.Properties.DebugPort;

                        if (pmessage.IncludeStates && instance.Properties.State == DropletInstanceState.Running)
                        {
                            response.Stats = instance.GenerateDropletStatusMessage();
                            response.Stats.Host = Host;
                            response.Stats.Cores = DEAUtilities.NumberOfCores();
                        }

                        this.deaReactor.SendReply(reply, response.SerializeToJson());
                    }
                }
                finally
                {
                    instance.Lock.ExitReadLock();
                }
            });
        }

        /// <summary>
        /// The handler for dea.update handler.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="replay">The replay.</param>
        /// <param name="subject">The subject.</param>
        private void DeaUpdateHandler(string message, string replay, string subject)
        {
            if (this.shuttingDown)
            {
                return;
            }

            DeaUpdateMessageRequest pmessage = new DeaUpdateMessageRequest();
            pmessage.FromJsonIntermediateObject(JsonConvertibleObject.DeserializeFromJson(message));

            Logger.Debug(Strings.DeaReceivedUpdateMessage, message);

            this.droplets.ForEach(delegate(DropletInstance instance)
            {
                if (instance.Properties.DropletId == pmessage.DropletId)
                {
                    try
                    {
                        instance.Lock.EnterWriteLock();

                        Logger.Debug(Strings.MappingnewURIs);
                        Logger.Debug(Strings.NewCurrent, JsonConvertibleObject.SerializeToJson(pmessage.Uris), JsonConvertibleObject.SerializeToJson(instance.Properties.Uris));

                        List<string> toUnregister = new List<string>(instance.Properties.Uris.Except(pmessage.Uris));
                        List<string> toRegister = new List<string>(pmessage.Uris.Except(instance.Properties.Uris));

                        instance.Properties.Uris = toUnregister.ToArray();
                        this.UnregisterInstanceFromRouter(instance);

                        instance.Properties.Uris = toRegister.ToArray();
                        this.RegisterInstanceWithRouter(instance);

                        instance.Properties.Uris = pmessage.Uris.ToArray();
                    }
                    finally
                    {
                        instance.Lock.ExitWriteLock();
                    }
                }
            });
        }

        /// <summary>
        /// The handler for dea.locate message.
        /// The DEA should respond on dea.advertise when receiving this message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="replay">The replay.</param>
        /// <param name="subject">The subject.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "CloudFoundry.Utilities.Logger.Debug(System.String)", Justification = "Readable.")]
        private void DeaLocateHandler(string message, string replay, string subject)
        {
            Logger.Debug("Dea received locate message");
            this.SendAdvertise();
        }

        /// <summary>
        /// The handler for the dea.stop message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="replay">The replay.</param>
        /// <param name="subject">The subject.</param>
        private void DeaStopHandler(string message, string replay, string subject)
        {
            if (this.shuttingDown)
            {
                return;
            }

            DeaStopMessageRequest pmessage = new DeaStopMessageRequest();
            pmessage.FromJsonIntermediateObject(JsonConvertibleObject.DeserializeFromJson(message));

            Logger.Debug(Strings.DeaReceivedStopMessage, message);



            this.droplets.ForEach(
                true,
                delegate(DropletInstance instance)
                {
                    try
                    {
                        instance.Lock.EnterWriteLock();

                        bool droplet_match = instance.Properties.DropletId == pmessage.DropletId;
                        bool version_match = pmessage.Version == null || pmessage.Version == instance.Properties.Version;
                        bool instace_match = pmessage.InstanceIds == null || pmessage.InstanceIds.Contains(instance.Properties.InstanceId);
                        bool index_match = pmessage.Indexes == null || pmessage.Indexes.Contains(instance.Properties.InstanceIndex);
                        bool state_match = pmessage.States == null || pmessage.States.Contains(instance.Properties.State);

                        if (droplet_match && version_match && instace_match && index_match && state_match)
                        {
                            if (instance.Properties.State == DropletInstanceState.Starting || instance.Properties.State == DropletInstanceState.Running)
                            {
                                instance.Properties.ExitReason = DropletExitReason.Stopped;
                            }

                            if (instance.Properties.State == DropletInstanceState.Crashed)
                            {
                                instance.Properties.State = DropletInstanceState.Deleted;
                                instance.Properties.StopProcessed = false;
                            }

                            this.StopDroplet(instance);
                        }
                    }
                    finally
                    {
                        instance.Lock.ExitWriteLock();
                    }
                });
        }

        /// <summary>
        /// Stops the a droplet instance.
        /// </summary>
        /// <param name="instance">The instance to be stopped.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is logged, and error must not bubble up.")]
        private void StopDroplet(DropletInstance instance)
        {
            try
            {
                instance.Lock.EnterWriteLock();

                if (instance.Properties.StopProcessed)
                {
                    return;
                }

                // Unplug us from the system immediately, both the routers and health managers.
                if (!instance.Properties.NotifiedExited)
                {
                    this.UnregisterInstanceFromRouter(instance);

                    if (instance.Properties.ExitReason == null)
                    {
                        instance.Properties.ExitReason = DropletExitReason.Crashed;
                        instance.Properties.State = DropletInstanceState.Crashed;
                        instance.Properties.StateTimestamp = DateTime.Now;
                        if (!instance.IsProcessIdRunning)
                        {
                            instance.Properties.ProcessId = 0;
                        }
                    }

                    this.deaReactor.SendDropletExited(instance.GenerateDropletExitedMessage().SerializeToJson());

                    instance.Properties.NotifiedExited = true;
                }

                Logger.Info(Strings.StoppingInstance, instance.Properties.LoggingId);

                // if system thinks this process is running, make sure to execute stop script
                if (instance.Properties.State == DropletInstanceState.Starting || instance.Properties.State == DropletInstanceState.Running)
                {
                    instance.Properties.State = DropletInstanceState.Stopped;
                    instance.Properties.StateTimestamp = DateTime.Now;
                }

                // this.monitoring.RemoveInstanceResources(instance);
                instance.Properties.StopProcessed = true;
            }
            catch (Exception ex)
            {
                Logger.Error(Strings.ErrorRecoveringDropletWarningMessage, instance.Properties.DropletId, instance.Properties.InstanceId, ex.ToString());
            }
            finally
            {
                instance.Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Handler for the dea.{guid}.start message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="reply">The reply.</param>
        /// <param name="subject">The subject.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Readable message."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "No specific type known.")]
        private void DeaStartHandler(string message, string reply, string subject)
        {
            DeaStartMessageRequest pmessage;
            DropletInstance instance;

            try
            {
                this.droplets.Lock.EnterWriteLock();

                if (this.shuttingDown)
                {
                    return;
                }

                Logger.Debug(Strings.DeaReceivedStartMessage, message);

                pmessage = new DeaStartMessageRequest();

                // Environment variable ca be of any type. It comes directly from the user with no validation, sanitization or preprocessing.
                try
                {
                    pmessage.FromJsonIntermediateObject(JsonConvertibleObject.DeserializeFromJson(message));
                }
                catch (Exception e)
                {
                    Logger.Error("Ignoring dea.start request. Unable to parse dea.start message. Exception: {0}", e.ToString());
                    return;
                }

                long memoryMbytes = pmessage.Limits != null && pmessage.Limits.MemoryMbytes != null ? pmessage.Limits.MemoryMbytes.Value : Monitoring.DefaultAppMemoryMbytes;
                long diskMbytes = pmessage.Limits != null && pmessage.Limits.DiskMbytes != null ? pmessage.Limits.DiskMbytes.Value : Monitoring.DefaultAppDiskMbytes;
                long fds = pmessage.Limits != null && pmessage.Limits.FileDescriptors != null ? pmessage.Limits.FileDescriptors.Value : Monitoring.DefaultAppFds;

                if (this.monitoring.MemoryReservedMbytes + memoryMbytes > this.monitoring.MaxMemoryMbytes || this.monitoring.Clients >= this.monitoring.MaxClients)
                {
                    Logger.Info(Strings.Donothaveroomforthisclient);
                    return;
                }

                if (string.IsNullOrEmpty(pmessage.SHA1) || string.IsNullOrEmpty(pmessage.ExecutableFile) || string.IsNullOrEmpty(pmessage.ExecutableUri))
                {
                    Logger.Warning(Strings.StartRequestMissingProper, message);
                    return;
                }

                // TODO: Enable this when cc sends the stack name in the message
                //// if (!this.stager.StackSupported(pmessage.Stack))
                //// {
                ////     Logger.Warning(Strings.CloudNotStartRuntimeNot, message);
                ////     return;
                //// }

                instance = this.droplets.CreateDropletInstance(pmessage);
                instance.Properties.MemoryQuotaBytes = memoryMbytes * 1024 * 1024;
                instance.Properties.DiskQuotaBytes = diskMbytes * 1024 * 1024;
                instance.Properties.FDSQuota = fds;
                instance.Properties.Staged = instance.Properties.Name + "-" + instance.Properties.InstanceIndex + "-" + instance.Properties.InstanceId;
                instance.Properties.Directory = Path.Combine(this.fileResources.AppsDir, instance.Properties.Staged);

                if (!string.IsNullOrEmpty(instance.Properties.DebugMode))
                {
                    instance.Properties.DebugPort = NetworkInterface.GrabEphemeralPort();
                    instance.Properties.DebugIP = Host;
                }

                instance.Properties.Port = NetworkInterface.GrabEphemeralPort();
                instance.Properties.EnvironmentVariables = this.SetupInstanceEnv(instance, pmessage.Environment, pmessage.Services);

                if (this.enforceUlimit)
                {
                    // instance.JobObject.JobMemoryLimit = instance.Properties.MemoryQuotaBytes;
                }

                this.monitoring.AddInstanceResources(instance);
            }
            finally
            {
                this.droplets.Lock.ExitWriteLock();
            }

            // TODO: the pre-starting stage should be able to gracefuly stop when the shutdown flag is set
            ThreadPool.QueueUserWorkItem(delegate(object data)
            {
                this.StartDropletInstance(instance, pmessage.SHA1, pmessage.ExecutableFile, pmessage.ExecutableUri);
            });
        }

        /// <summary>
        /// Handler for the staging.{guid}.start message.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Readable message."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "No specific type known.")]
        private void StagingStartHandler(string message, string reply, string subject)
        {
            StagingStartMessageRequest pmessage;
            StagingInstance instance;

            try
            {
                this.stagingTaskRegistry.Lock.EnterWriteLock();
                if (this.shuttingDown)
                {
                    return;
                }
                Logger.Debug("DEA Received staging message: {0}", message);
                pmessage = new StagingStartMessageRequest();
                try
                {
                    pmessage.FromJsonIntermediateObject(JsonConvertibleObject.DeserializeFromJson(message));
                }
                catch (Exception e)
                {
                    Logger.Error("Ignoring staging.start request. Unable to parse message. Exception: {0}", e.ToString());
                    return;
                }
                long memoryMbytes = pmessage.Properties != null && pmessage.Properties.Resources != null && pmessage.Properties.Resources.MemoryMbytes != null ? pmessage.Properties.Resources.MemoryMbytes.Value : Monitoring.DefaultAppMemoryMbytes;
                long diskMbytes = pmessage.Properties != null && pmessage.Properties.Resources != null && pmessage.Properties.Resources.DiskMbytes != null ? pmessage.Properties.Resources.DiskMbytes.Value : Monitoring.DefaultAppDiskMbytes;
                long fds = pmessage.Properties != null && pmessage.Properties.Resources != null && pmessage.Properties.Resources.FileDescriptors != null ? pmessage.Properties.Resources.FileDescriptors.Value : Monitoring.DefaultAppFds;
                if (this.monitoring.MemoryReservedMbytes + memoryMbytes > this.monitoring.MaxMemoryMbytes || this.monitoring.Clients >= this.monitoring.MaxClients)
                {
                    Logger.Info(Strings.Donothaveroomforthisclient);
                    return;
                }
                instance = this.stagingTaskRegistry.CreateStagingInstance(pmessage);
                instance.Properties.MemoryQuotaBytes = memoryMbytes * 1024 * 1024;
                instance.Properties.DiskQuotaBytes = diskMbytes * 1024 * 1024;
                instance.Properties.FDSQuota = fds;
                instance.Properties.Directory = Path.Combine(this.fileResources.StagingDir, pmessage.TaskID);
                instance.Properties.TaskId = pmessage.TaskID;
                instance.Properties.Reply = reply;
                instance.Properties.InitializedTime = DateTime.Now;
                instance.StopRequested = false;
                this.monitoring.AddInstanceResources(instance);
            }
            finally
            {
                this.stagingTaskRegistry.Lock.ExitWriteLock();
            }

            ThreadPool.QueueUserWorkItem(delegate(object data)
            {
                this.StartStagingInstance(instance, pmessage);
            });
        }

        private void StartStagingInstance(StagingInstance instance, StagingStartMessageRequest pmessage)
        {
            StagingWorkspace workspace = new StagingWorkspace(instance.Properties.Directory);
            try
            {
                try
                {
                    instance.Lock.EnterWriteLock();

                    instance.Properties.UseDiskQuota = this.useDiskQuota;
                    instance.Properties.UploadThrottleBitsps = this.uploadThrottleBitsps;

                    UriBuilder streamingLog = new UriBuilder();
                    streamingLog.Host = this.ExternalHost;
                    streamingLog.Scheme = "http";
                    streamingLog.Path = string.Format("/staging_tasks/{0}/file_path", pmessage.TaskID);
                    streamingLog.Query = string.Format("path={0}&timestamp={1}", workspace.StagingLogSuffix, RubyCompatibility.DateTimeToEpochSeconds(DateTime.Now));

                    instance.Properties.StreamingLogUrl = DEAUtilities.GetHmacedUri(streamingLog.Uri.ToString(), this.directoryServerHmacKey, new string[] { "path", "timestamp" }).ToString();
                    instance.Workspace = workspace;
                    instance.Properties.TaskLog = workspace.StagingLogPath;
                }
                finally
                {
                    instance.Lock.ExitWriteLock();
                }

                instance.AfterSetup += new StagingInstance.StagingTaskEventHandler(this.AfterStagingSetup);

                Logger.Info("Started staging task {0}", instance.Properties.TaskId);
                try
                {
                    instance.SetupStagingEnvironment();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error setting up staging environment: ", ex.ToString());
                    throw ex;
                }

                instance.UnpackDroplet();
                instance.PrepareStagingDirs();

                if (File.Exists(this.logyardUidPath))
                {
                    LogyardInstanceRequest logyardMsg = new LogyardInstanceRequest();
                    logyardMsg.AppGUID = pmessage.AppID;
                    logyardMsg.AppName = pmessage.StartMessage.Name;
                    logyardMsg.AppSpace = "";
                    logyardMsg.DockerId = instance.Properties.InstanceId;
                    logyardMsg.Index = -1;
                    logyardMsg.LogFiles = new Dictionary<string, string>() { { "staging", Path.Combine("staging", instance.Workspace.StagingLogSuffix) } };
                    logyardMsg.Type = "staging";
                    logyardMsg.RootPath = instance.Workspace.BaseDir;

                    string logyardId = File.ReadAllText(this.logyardUidPath).Trim();
                    this.deaReactor.SendLogyardNotification(logyardId, logyardMsg.SerializeToJson());
                }
                instance.CreatePrison();

                if (pmessage.Properties.Environment != null) {
                    Dictionary<string, string> stagingEnvVars = this.ParseEnvironmnetVariables(pmessage.Properties.Environment);
                    instance.Container.User.SetUserEnvironmentVariables(stagingEnvVars);
                }

                instance.GetBuildpack(pmessage, this.gitPath, this.buildpacksDir);
                this.stagingTaskRegistry.ScheduleSnapshotStagingState();

                try
                {
                    Logger.Info("Staging task {0}: Running compilation script", pmessage.TaskID);

                    this.stagingTaskRegistry.ScheduleSnapshotStagingState();
                    instance.CompileProcess = instance.Buildpack.StartCompile(instance.Container);

                    instance.Lock.EnterWriteLock();
                    instance.Properties.Start = DateTime.Now;
                }
                finally
                {
                    if (instance.Lock.IsWriteLockHeld)
                    {
                        instance.Lock.ExitWriteLock();
                    }
                }
            }
            catch (Exception ex)
            {
                instance.StagingException = ex;
                instance.Properties.Stopped = true;
                Logger.Error(ex.ToString());
            }
        }

        private void AfterStagingSetup(StagingInstance instance)
        {
            StagingStartMessageResponse response = new StagingStartMessageResponse();
            response.TaskId = instance.Properties.TaskId;
            response.TaskStreamingLogURL = instance.Properties.StreamingLogUrl;
            if (instance.StagingException != null)
            {
                response.Error = instance.StagingException.ToString();
            }
            this.deaReactor.SendReply(instance.Properties.Reply, response.SerializeToJson());
            Logger.Debug("Staging task {0}: sent reply {1}", instance.Properties.TaskId, response.SerializeToJson());
        }

        private void StopStaging(StagingInstance instance, string reply_to)
        {
            try
            {
                if (instance.Properties.Stopped)
                {
                    return;
                }
                instance.Lock.EnterWriteLock();
                instance.Properties.Stopped = true;
                StagingStartMessageResponse response = new StagingStartMessageResponse();
                response.TaskId = instance.Properties.TaskId;
                this.deaReactor.SendReply(reply_to, response.SerializeToJson());
            }
            catch (Exception ex)
            {
                Logger.Error("Could not stop staging task {0}: {1}", instance.Properties.TaskId, ex.ToString());
            }
            finally
            {
                instance.Lock.ExitWriteLock();
            }
        }

        private void StartStagedDropletInstance(StagingInstance stagingInstance, string dropletSha)
        {
            DropletInstance instance;

            try
            {
                this.droplets.Lock.EnterWriteLock();

                if (this.shuttingDown)
                {
                    return;
                }

                string tgzFile = Path.Combine(this.fileResources.StagedDir, dropletSha + ".tgz");
                Logger.Info("Copying droplet to {0}", tgzFile);
                File.Copy(stagingInstance.Workspace.StagedDropletPath, tgzFile);

                long memoryMbytes = stagingInstance.StartMessage.Limits != null && stagingInstance.StartMessage.Limits.MemoryMbytes != null ? stagingInstance.StartMessage.Limits.MemoryMbytes.Value : Monitoring.DefaultAppMemoryMbytes;
                long diskMbytes = stagingInstance.StartMessage.Limits != null && stagingInstance.StartMessage.Limits.DiskMbytes != null ? stagingInstance.StartMessage.Limits.DiskMbytes.Value : Monitoring.DefaultAppDiskMbytes;
                long fds = stagingInstance.StartMessage.Limits != null && stagingInstance.StartMessage.Limits.FileDescriptors != null ? stagingInstance.StartMessage.Limits.FileDescriptors.Value : Monitoring.DefaultAppFds;

                if (this.monitoring.MemoryReservedMbytes + memoryMbytes > this.monitoring.MaxMemoryMbytes || this.monitoring.Clients >= this.monitoring.MaxClients)
                {
                    Logger.Info(Strings.Donothaveroomforthisclient);
                    return;
                }

                instance = this.droplets.CreateDropletInstance(stagingInstance.StartMessage);
                instance.Properties.MemoryQuotaBytes = memoryMbytes * 1024 * 1024;
                instance.Properties.DiskQuotaBytes = diskMbytes * 1024 * 1024;
                instance.Properties.FDSQuota = fds;

                instance.Properties.Staged = instance.Properties.Name + "-" + instance.Properties.InstanceIndex + "-" + instance.Properties.InstanceId;
                instance.Properties.Directory = Path.Combine(this.fileResources.AppsDir, instance.Properties.Staged);

                if (!string.IsNullOrEmpty(instance.Properties.DebugMode))
                {
                    instance.Properties.DebugPort = NetworkInterface.GrabEphemeralPort();
                    instance.Properties.DebugIP = Host;
                }

                instance.Properties.Port = NetworkInterface.GrabEphemeralPort();
                instance.Properties.EnvironmentVariables = this.SetupInstanceEnv(instance, stagingInstance.StartMessage.Environment, stagingInstance.StartMessage.Services);

                this.monitoring.AddInstanceResources(instance);
            }
            finally
            {
                this.droplets.Lock.ExitWriteLock();
            }

            ThreadPool.QueueUserWorkItem(delegate(object data)
            {
                this.StartDropletInstance(instance, dropletSha, stagingInstance.StartMessage.ExecutableFile, stagingInstance.StartMessage.ExecutableUri);
            });
        }

        private void AfterStagingFinished(StagingInstance instance)
        {
            StagingStartMessageResponse response = new StagingStartMessageResponse();
            try
            {
                if (instance.StagingException == null)
                {
                    try
                    {
                        Logger.Info("Staging task {0}: Saving buildpackInfo", instance.Properties.TaskId);
                        StagingInfo.SaveBuildpackInfo(Path.Combine(instance.Workspace.StagedDir, StagingWorkspace.StagingInfo), instance.Buildpack.Name, instance.GetStartCommand());
                        this.stagingTaskRegistry.ScheduleSnapshotStagingState();

                        Logger.Debug("Staging task {0}: Packing droplet {1}", instance.Properties.TaskId, instance.Workspace.StagedDropletPath);
                        Directory.CreateDirectory(instance.Workspace.StagedDropletDir);

                        DEAUtilities.CreateArchive(instance.Workspace.StagedDir, instance.Workspace.StagedDropletPath, false);

                        if (File.Exists(instance.Workspace.StagedDropletPath))
                        {
                            using (Stream stream = File.OpenRead(instance.Workspace.StagedDropletPath))
                            {
                                using (SHA1 sha = SHA1.Create())
                                {
                                    response.DropletSHA = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                                }
                            }
                        }

                        this.StartStagedDropletInstance(instance, response.DropletSHA);

                        Uri uri = new Uri(instance.Properties.UploadURI);
                        Logger.Debug("Staging task {0}: Uploading droplet {1} to {2}", instance.Properties.TaskId, instance.Workspace.StagedDropletPath, instance.Properties.UploadURI);
                        DEAUtilities.HttpUploadFile(instance.Properties.UploadURI, new FileInfo(instance.Workspace.StagedDropletPath), "upload[droplet]", "application/octet-stream", uri.UserInfo);
                    }
                    catch (Exception ex)
                    {
                        instance.StagingException = ex;
                    }
                    try
                    {
                        Directory.CreateDirectory(instance.Workspace.Cache);

                        DEAUtilities.CreateArchive(instance.Workspace.Cache, instance.Workspace.StagedBuildpackCachePath, false);
                        Uri uri = new Uri(instance.Properties.BuildpackCacheUploadURI);
                        Logger.Debug("Staging task {0}: Uploading buildpack cache {1} to {2}", instance.Properties.TaskId, instance.Workspace.StagedBuildpackCachePath, instance.Properties.BuildpackCacheUploadURI);
                        DEAUtilities.HttpUploadFile(instance.Properties.BuildpackCacheUploadURI, new FileInfo(instance.Workspace.StagedBuildpackCachePath), "upload[droplet]", "application/octet-stream", uri.UserInfo);
                    }
                    catch
                    {
                        Logger.Debug("Staging task {0}: Cannot pack buildpack cache", instance.Properties.TaskId);
                    }
                }

                if (instance.StagingException != null)
                {
                    response.Error = instance.StagingException.ToString();
                }

                response.TaskId = instance.Properties.TaskId;

                // try to read log. don't throw exception if it fails
                try
                {
                    response.TaskLog = File.ReadAllText(instance.Properties.TaskLog);
                }
                catch { }

                if (instance.Properties.DetectedBuildpack != null)
                {
                    response.DetectedBuildpack = instance.Properties.DetectedBuildpack;
                }

                this.deaReactor.SendReply(instance.Properties.Reply, response.SerializeToJson());
                Logger.Debug("Staging task {0}: sent reply {1}", instance.Properties.TaskId, response.SerializeToJson());
            }
            finally
            {
            }
        }


        /// <summary>
        /// Handler for the staging.locate message.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Readable message."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "No specific type known.")]
        private void StagingLocateHandler(string message, string reply, string subject)
        {
            Logger.Info("DEA received staging locate message : {0}", message);
            this.SendStagingAdvertise();
        }

        /// <summary>
        /// The handler for the staging.stop message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="reply">The reply.</param>
        /// <param name="subject">The subject.</param>
        private void StagingStopHandler(string message, string reply, string subject)
        {
            Logger.Info("DEA received staging stop message : {0}", message);
            StagingStopMessageRequest request = new StagingStopMessageRequest();
            request.FromJsonIntermediateObject(JsonConvertibleObject.DeserializeFromJson(message));
            this.stagingTaskRegistry.ForEach(
                true,
                delegate(StagingInstance instance)
                {
                    try
                    {
                        instance.Lock.EnterWriteLock();
                        if (instance.Properties.AppId == request.AppID && (DateTime.Now - instance.Properties.InitializedTime).TotalSeconds > 3)
                        {
                            instance.StopRequested = true;
                            instance.Properties.Stopped = true;
                        }
                    }
                    finally
                    {
                        instance.Lock.ExitWriteLock();
                    }
                });
        }

        /// <summary>
        /// Starts the droplet instance after the basic initialization is done.
        /// </summary>
        /// <param name="instance">The instance to be started.</param>
        /// <param name="sha1">The sha1 of the droplet file.</param>
        /// <param name="executableFile">The path to the droplet file.</param>
        /// <param name="executableUri">The URI to the droplet file.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "CloudFoundry.Utilities.Logger.Info(System.String,System.Object[])", Justification = "More clear"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Major rewrites have to be done."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is logged, and error must not bubble up.")]
        private void StartDropletInstance(DropletInstance instance, string sha1, string executableFile, string executableUri)
        {
            try
            {
                try
                {
                    instance.Lock.EnterWriteLock();

                    var containerRules = new CloudFoundry.WindowsPrison.PrisonConfiguration();

                    containerRules.PrisonHomeRootPath = instance.Properties.Directory;

                    containerRules.Rules |= CloudFoundry.WindowsPrison.RuleTypes.WindowStation;
                    containerRules.Rules |= CloudFoundry.WindowsPrison.RuleTypes.IISGroup;


                    containerRules.TotalPrivateMemoryLimitBytes = instance.Properties.MemoryQuotaBytes;
                    containerRules.PriorityClass = ProcessPriorityClass.BelowNormal;
                    containerRules.ActiveProcessesLimit = 10;

                    if (this.uploadThrottleBitsps > 0)
                    {
                        containerRules.Rules |= CloudFoundry.WindowsPrison.RuleTypes.Network;
                        containerRules.NetworkOutboundRateLimitBitsPerSecond = this.uploadThrottleBitsps;
                        containerRules.AppPortOutboundRateLimitBitsPerSecond = this.uploadThrottleBitsps;
                    }

                    containerRules.Rules |= CloudFoundry.WindowsPrison.RuleTypes.Httpsys;
                    containerRules.UrlPortAccess = instance.Properties.Port;

                    if (this.useDiskQuota)
                    {
                        containerRules.Rules |= CloudFoundry.WindowsPrison.RuleTypes.Disk;
                        containerRules.DiskQuotaBytes = instance.Properties.DiskQuotaBytes;
                    }

                    //var prisonInfo = new ProcessPrisonCreateInfo();

                    //prisonInfo.Id = instance.Properties.InstanceId;
                    //prisonInfo.TotalPrivateMemoryLimitBytes = instance.Properties.MemoryQuotaBytes;

                    //if (this.useDiskQuota)
                    //{
                    //    prisonInfo.DiskQuotaBytes = instance.Properties.DiskQuotaBytes;
                    //    prisonInfo.DiskQuotaPath = instance.Properties.Directory;
                    //}

                    //if (this.uploadThrottleBitsps > 0)
                    //{
                    //    prisonInfo.NetworkOutboundRateLimitBitsPerSecond = this.uploadThrottleBitsps;
                    //}

                    //prisonInfo.UrlPortAccess = instance.Properties.Port;

                    instance.Prison.Tag = "dea";
                    instance.Properties.ContainerId = instance.Prison.Id.ToString();

                    Logger.Info("Creating Process Prisson: {0}", instance.Properties.ContainerId);

                    instance.Prison.Lockdown(containerRules);

                    //instance.Prison.Create(prisonInfo);

                    Logger.Info("Opening firewall port {0} for instance {1}", instance.Properties.Port, instance.Properties.LoggingId);

                    FirewallTools.OpenPort(instance.Properties.Port, instance.Properties.InstanceId);

                    instance.Properties.WindowsUserName = instance.Prison.User.UserName;
                    instance.Properties.WindowsPassword = instance.Prison.User.Password;

                    //instance.Properties.WindowsPassword = instance.Prison.WindowsPassword;
                    //instance.Properties.WindowsUserName = instance.Prison.WindowsUsername;
                }
                finally
                {
                    instance.Lock.ExitWriteLock();
                }

                string tgzFile = Path.Combine(this.fileResources.StagedDir, sha1 + ".tgz");
                this.fileResources.PrepareAppDirectory(executableFile, executableUri, sha1, tgzFile, instance);
                Logger.Debug(Strings.Downloadcompleate);

                string starting = string.Format(CultureInfo.InvariantCulture, Strings.StartingUpInstanceOnPort, instance.Properties.LoggingId, instance.Properties.Port);

                Logger.Info(starting);

                Logger.Debug(Strings.Clients, this.monitoring.Clients);
                Logger.Debug(Strings.ReservedMemoryUsageMb, this.monitoring.MemoryReservedMbytes, this.monitoring.MaxMemoryMbytes);

                try
                {
                    instance.Lock.EnterWriteLock();

                    instance.Properties.EnvironmentVariables.Add(VcapWindowsUserVariable, instance.Properties.WindowsUserName);
                    instance.Properties.EnvironmentVariables.Add(VcapWindowsUserPasswordVariable, instance.Properties.WindowsPassword);

                    instance.Prison.User.SetUserEnvironmentVariables(instance.Properties.EnvironmentVariables);
                }
                finally
                {
                    instance.Lock.ExitWriteLock();
                }

                DateTime start = DateTime.Now;

                string startSciprtPath = this.CreateStartScript(instance);

                instance.Prison.Execute(null, startSciprtPath, Path.Combine(instance.Properties.Directory, "app"), false, null, null, null, null);

                Logger.Debug(Strings.TookXTimeToLoadConfigureAndStartDebugMessage, (DateTime.Now - start).TotalSeconds);

                try
                {
                    instance.Lock.EnterWriteLock();

                    if (!instance.Properties.StopProcessed)
                    {
                        this.droplets.ScheduleSnapshotAppState();
                    }
                }
                finally
                {
                    instance.Lock.ExitWriteLock();
                }

                if (File.Exists(this.logyardUidPath))
                {
                    LogyardInstanceRequest logyardMsg = new LogyardInstanceRequest();
                    logyardMsg.AppGUID = instance.Properties.DropletId;
                    logyardMsg.AppName = instance.Properties.Name;
                    logyardMsg.AppSpace = "";
                    logyardMsg.DockerId = instance.Properties.InstanceId;
                    logyardMsg.Index = -1;
                    Dictionary<string, string> logfiles = new Dictionary<string, string>();
                    logfiles["stdout"] = @"logs\stdout.log";
                    logfiles["stderr"] = @"logs\stderr.log";
                    logyardMsg.LogFiles = logfiles;
                    logyardMsg.Type = "app";
                    logyardMsg.RootPath = instance.Properties.Directory;

                    string logyardId = File.ReadAllText(this.logyardUidPath).Trim();
                    this.deaReactor.SendLogyardNotification(logyardId, logyardMsg.SerializeToJson());
                }
                this.DetectAppReady(instance);
            }
            catch (Exception ex)
            {
                Logger.Warning(Strings.FailedStagingAppDir, instance.Properties.Directory, instance.Properties.LoggingId, ex.ToString());
                try
                {
                    instance.Lock.EnterWriteLock();

                    instance.Properties.State = DropletInstanceState.Crashed;
                    instance.Properties.ExitReason = DropletExitReason.Crashed;
                    instance.Properties.StateTimestamp = DateTime.Now;

                    this.StopDroplet(instance);
                }
                finally
                {
                    instance.Lock.ExitWriteLock();
                }
            }
        }

        private string CreateStartScript(DropletInstance instance)
        {
            string startCommand = StagingInfo.getStartCommand(Path.Combine(instance.Properties.Directory, "staging_info.yml"));

            var startScriptTemplate =
            @"
                set > {0}\logs\env.log
                cd {0}\app
                {1} > {0}\logs\stdout.log 2> {0}\logs\stderr.log
            ";

            string startScript = String.Format(startScriptTemplate, instance.Properties.Directory, startCommand);

            string scriptPath = Path.Combine(instance.Properties.Directory, "start.cmd");
            File.WriteAllText(scriptPath, startScript, Encoding.ASCII);

            return scriptPath;
        }

        /// <summary>
        /// Detects if an droplet instance is ready, so that it can be set to a Running state and registerd with the router.
        /// </summary>
        /// <param name="instance">The instance do be detected.</param>
        private void DetectAppReady(DropletInstance instance)
        {
            ThreadPool.QueueUserWorkItem(
                delegate
                {
                    DetectAppReady(
                        instance,
                        delegate(bool detected)
                        {
                            try
                            {
                                instance.Lock.EnterWriteLock();
                                if (detected)
                                {
                                    if (instance.Properties.State == DropletInstanceState.Starting)
                                    {
                                        Logger.Info(Strings.InstanceIsReadyForConnections, instance.Properties.LoggingId);
                                        instance.Properties.State = DropletInstanceState.Running;
                                        instance.Properties.StateTimestamp = DateTime.Now;

                                        this.SendHeartbeat();
                                        this.RegisterInstanceWithRouter(instance);
                                        this.droplets.ScheduleSnapshotAppState();
                                    }
                                }
                                else
                                {
                                    Logger.Warning(Strings.GivingUpOnConnectingApp);
                                    this.StopDroplet(instance);
                                }
                            }
                            finally
                            {
                                instance.Lock.ExitWriteLock();
                            }
                        });
                });
        }

        /// <summary>
        /// Setups the instance environment variables to be passed when configuring the plugin of an instance.
        /// </summary>
        /// <param name="instance">The instance for which to generate the variables.</param>
        /// <param name="appVars">The user application variables.</param>
        /// <param name="services">The services to be bound to the instance.</param>
        /// <returns>The application variables.</returns>
        private Dictionary<string, string> SetupInstanceEnv(DropletInstance instance, string[] appVars, Dictionary<string, object>[] services)
        {
            Dictionary<string, string> env = new Dictionary<string, string>();

            env.Add(HomeVariable, Path.Combine(instance.Properties.Directory, "app"));
            env.Add(VcapApplicationVariable, this.CreateInstanceVariable(instance));
            env.Add(VcapServicesVariable, CreateServicesApplicationVariable(services));
            env.Add(VcapAppHostVariable, Host);
            env.Add(VcapAppPortVariable, instance.Properties.Port.ToString(CultureInfo.InvariantCulture));
            env.Add("PORT", instance.Properties.Port.ToString(CultureInfo.InvariantCulture));

            env.Add("HOMEPATH", Path.Combine(instance.Properties.Directory));

            // User's environment settings
            if (appVars != null)
            {
                var parsedAppVards = ParseEnvironmnetVariables(appVars);
                foreach (var appEnv in parsedAppVards)
                {
                    env.Add(appEnv.Key, appEnv.Value);
                }
            }

            return env;
        }

        /// <summary>
        /// Parse environment variable from string with equal separator to key value type.
        /// </summary>
        /// <param name="appVars">The user application variables.</param>
        /// <returns>Parsed environment variables.</returns>
        private Dictionary<string, string> ParseEnvironmnetVariables(string[] appVars)
        {
            Dictionary<string, string> env = new Dictionary<string, string>();

            if (appVars != null)
            {
                foreach (string appEnv in appVars)
                {
                    string[] envVar = appEnv.Split(new char[] { '=' }, 2);
                    env.Add(envVar[0], envVar[1]);
                }
            }

            return env;
        }

        /// <summary>
        /// Creates the application variable for an instance. Is is used for the plugin configuration.
        /// </summary>
        /// <param name="instance">The instance for which the application variable is to be generated.</param>
        /// <returns>The application variable.</returns>
        private string CreateInstanceVariable(DropletInstance instance)
        {
            List<string> whitelist = new List<string>() { "instance_id", "instance_index", "name", "uris", "users", "version", "start", "runtime", "state_timestamp", "port" };
            Dictionary<string, object> result = new Dictionary<string, object>();

            Dictionary<string, object> jsonInstance = instance.Properties.ToJsonIntermediateObject();

            foreach (string key in whitelist)
            {
                if (jsonInstance.ContainsKey(key))
                {
                    // result[key] = JsonConvertibleObject.ObjectToValue<object>(jInstance[key]);
                    result[key] = jsonInstance[key];
                }
            }

            result["host"] = Host;
            result["limits"] = new Dictionary<string, object>() 
            {
                { "fds", instance.Properties.FDSQuota },
                { "mem", instance.Properties.MemoryQuotaBytes },
                { "disk", instance.Properties.DiskQuotaBytes }
            };

            return JsonConvertibleObject.SerializeToJson(result);
        }

        /// <summary>
        /// Handler for router.start Nats messages.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="reply">The reply token.</param>
        /// <param name="subject">The message subject.</param>
        private void RouterStartHandler(string message, string reply, string subject)
        {
            if (this.shuttingDown)
            {
                return;
            }

            Logger.Debug(Strings.DeaReceivedRouterStart, message);

            this.RegisterRoutes();

            if (!string.IsNullOrEmpty(message))
            {
                var pmessage = new RouterStartMessageRequest();
                pmessage.FromJsonIntermediateObject(JsonConvertibleObject.DeserializeFromJson(message));

                // Recreate the timer if the MinimumRegisterIntervalInSeconds is lower the a previews recorded one.
                if (this.minRouterRegisterInterval > pmessage.MinimumRegisterIntervalInSeconds)
                {
                    this.minRouterRegisterInterval = pmessage.MinimumRegisterIntervalInSeconds;

                    if (this.routerRegisterTimer != null)
                    {
                        this.routerRegisterTimer.Dispose();
                    }

                    this.routerRegisterTimer = TimerHelper.RecurringLongCall(
                    this.minRouterRegisterInterval * 1000,
                    delegate
                    {
                        this.RegisterRoutes();
                    });
                }

            }
        }

        /// <summary>
        /// Register the routes for all instances.
        /// </summary>
        private void RegisterRoutes()
        {
            this.RegisterDirectoryServer(this.Host, this.directoryServerPort, this.ExternalHost);
            this.droplets.ForEach(delegate(DropletInstance instance)
            {
                if (instance.Properties.State == DropletInstanceState.Running)
                {
                    this.RegisterInstanceWithRouter(instance);
                }
            });
        }

        /// <summary>
        /// Registers the instance with the Vcap router. Called when the application is running and ready.
        /// </summary>
        /// <param name="instance">The instance to be registered.</param>
        private void RegisterInstanceWithRouter(DropletInstance instance)
        {
            RouterMessage response = new RouterMessage();
            try
            {
                instance.Lock.EnterReadLock();

                if (instance.Properties.Uris == null || instance.Properties.Uris.Length == 0)
                {
                    return;
                }

                response.DeaId = UUID;
                response.DropletId = instance.Properties.DropletId;
                response.Host = Host;
                response.Port = instance.Properties.Port;
                response.Uris = new List<string>(instance.Properties.Uris).ToArray();

                response.Tags = new RouterMessage.TagsObject();
                response.Tags.Component = "dea-" + this.Index.ToString();

                response.PrivateInstanceId = instance.Properties.PrivateInstanceId;
            }
            finally
            {
                instance.Lock.ExitReadLock();
            }

            this.deaReactor.SendRouterRegister(response.SerializeToJson());
        }

        /// <summary>
        /// Unregisters the instance from the Vcap router. Called when the applicatoin is not in a running state any more.
        /// </summary>
        /// <param name="instance">The instance.</param>
        private void UnregisterInstanceFromRouter(DropletInstance instance)
        {
            RouterMessage response = new RouterMessage();
            try
            {
                instance.Lock.EnterReadLock();

                if (instance.Properties.Uris == null || instance.Properties.Uris.Length == 0)
                {
                    return;
                }

                response.DeaId = UUID;
                response.Host = Host;
                response.Port = instance.Properties.Port;
                response.Uris = instance.Properties.Uris;

                response.Tags = new RouterMessage.TagsObject();
                response.Tags.Component = "dea-" + this.Index.ToString();
            }
            finally
            {
                instance.Lock.ExitReadLock();
            }

            this.deaReactor.SendRouterUnregister(response.SerializeToJson());
        }

        private void RegisterDirectoryServer(string host, int port, string uri)
        {
            DirectoryServerRequest request = new DirectoryServerRequest();
            request.Port = port;
            request.Host = host;
            request.Uris = new string[] { uri };
            request.Tags = new Dictionary<string, string>();

            this.deaReactor.SendRouterRegister(request.SerializeToJson());
        }

        private void UnregisterDirectoryServer(string host, int port, string uri)
        {
            DirectoryServerRequest request = new DirectoryServerRequest();
            request.Port = port;
            request.Host = host;
            request.Uris = new string[] { uri };
            request.Tags = new Dictionary<string, string>();

            this.deaReactor.SendRouterUnregister(request.SerializeToJson());
        }


        /// <summary>
        /// The handler for healthmanager.start Nats message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="replay">The replay token.</param>
        /// <param name="subject">The message subject.</param>
        private void HealthmanagerStartHandler(string message, string replay, string subject)
        {
            if (this.shuttingDown)
            {
                return;
            }

            Logger.Debug(Strings.DeaReceivedHealthmanagerStart, message);

            this.SendHeartbeat();
        }

        /// <summary>
        /// Checks the system resource usage (cpu, memeory, disk size) and associate the respective counters to each instance.
        /// Stop an instance if it's usage is above its quota.
        /// Update the varz with resouce usage.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Trying to keep similarity to Ruby version."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is logged, and error must not bubble up.")]
        private void MonitorApps()
        {
            // AgentMonitoring.MemoryUsageKbytes = 0;
            long memoryUsageKbytes = 0;
            List<object> runningApps = new List<object>();

            DateTime monitorStart = DateTime.Now;

            Dictionary<string, Dictionary<string, Dictionary<string, long>>> metrics = new Dictionary<string, Dictionary<string, Dictionary<string, long>>>() 
            {
                { "framework", new Dictionary<string, Dictionary<string, long>>() }, 
                { "runtime", new Dictionary<string, Dictionary<string, long>>() }
            };

            this.droplets.ForEach(
                true,
                delegate(DropletInstance instance)
                {
                    if (instance.Properties.State != DropletInstanceState.Running)
                    {
                        return;
                    }

                    bool isPortReady = instance.IsPortReady(1500);

                    if (!instance.Lock.TryEnterWriteLock(10))
                    {
                        return;
                    }

                    try
                    {
                        if (isPortReady)
                        {
                            DateTime currentWorldTicks = DateTime.Now;

                            if (instance.Prison == null)
                            {
                                Logger.Warning("Instance {0} has an empty prison", instance.Properties.InstanceId);
                                return;
                            }

                            if (instance.Prison.JobObject == null)
                            {
                                Logger.Warning("Instance {0} has an empty job object", instance.Properties.InstanceId);
                                return;
                            }

                            long usedTicks = instance.Prison.JobObject.TotalProcessorTime.Ticks;

                            long lastUsedTicks = instance.Usage.Count >= 1 ? instance.Usage[instance.Usage.Count - 1].TotalProcessTicks : 0;
                            long sampleUsedTicks = usedTicks - lastUsedTicks;

                            DateTime lastWorldTicks = instance.Usage.Count >= 1 ? instance.Usage[instance.Usage.Count - 1].Time : currentWorldTicks;
                            long sampleActiveTicks = (currentWorldTicks - lastWorldTicks).Ticks;

                            long activeTicks = (currentWorldTicks - instance.Properties.Start).Ticks;

                            // this is the case when the cpu utilization is reported as the total life of the app
                            //float cpu = activeTicks > 0 ? ((float)usedTicks / activeTicks) * 100 : 0;

                            // this is the case when the cpu utilization is reported between the last sample timestamp and now
                            float cpu = sampleActiveTicks > 0 ? ((float)sampleUsedTicks / sampleActiveTicks) * 100 : 0;

                            // trim it to one decimal precision
                            cpu = float.Parse(cpu.ToString("F1", CultureInfo.CurrentCulture), CultureInfo.CurrentCulture);

                            // PrivateMemory is Virtual Private Memory usage and is the enforced Job Object memory usage.
                            long memBytes = instance.Prison.JobObject.PrivateMemory;

                            // Return -1 is disk quota is not enforced.
                            // long diskBytes = instance.Prison.DiskUsageBytes;
                            long diskBytes = -1; // TODO: fix this

                            instance.AddUsage(memBytes, cpu, diskBytes, usedTicks);

                            memoryUsageKbytes += memBytes / 1024;

                            // Track running apps for varz tracking
                            runningApps.Add(instance.Properties.ToJsonIntermediateObject());
                        }
                        else
                        {
                            instance.Properties.ProcessId = 0;
                            if (instance.Properties.State == DropletInstanceState.Running)
                            {
                                Logger.Warning(Strings.AppNotDetectedReady, instance.Properties.Name, instance.Properties.InstanceId);
                                this.StopDroplet(instance);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error occured in  MonitorApps method. Exception: {0}", ex.ToString());
                    }
                    finally
                    {
                        instance.Lock.ExitWriteLock();
                    }
                });

            this.monitoring.MemoryUsageKbytes = memoryUsageKbytes;

            // export running app information to varz
            try
            {
                VarzLock.EnterWriteLock();
                Varz["running_apps"] = runningApps;
            }
            finally
            {
                VarzLock.ExitWriteLock();
            }

            TimeSpan ttlog = DateTime.Now - monitorStart;
            if (ttlog.TotalMilliseconds > 1000)
            {
                Logger.Warning(Strings.TookXSecondsToProcessPsAndDu, ttlog.TotalSeconds);
            }
        }

        /// <summary>
        /// Does all the cleaning that is needed for an instance if stopped gracefully or has crashed
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Manageable."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "CloudFoundry.Utilities.Logger.Warning(System.String,System.Object[])", Justification = "More clear"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is logged, and error must not bubble up.")]
        private void TheReaper()
        {
            this.droplets.ForEach(
                true,
                delegate(DropletInstance instance)
                {
                    bool removeDroplet = false;

                    bool isCrashed = instance.Properties.State == DropletInstanceState.Crashed;
                    bool isFlapping = instance.Properties.Flapping == true;
                    bool isOldCrash = instance.Properties.State == DropletInstanceState.Crashed && (DateTime.Now - instance.Properties.StateTimestamp).TotalMilliseconds > Monitoring.CrashesReaperTimeoutMilliseconds;
                    bool isStopped = instance.Properties.State == DropletInstanceState.Stopped;
                    bool isDeleted = instance.Properties.State == DropletInstanceState.Deleted;

                    // Stop the instance gracefully before cleaning up.
                    if (isStopped)
                    {
                        if (instance.Prison.IsLocked && instance.Prison.JobObject.ActiveProcesses > 0)
                        {
                            try
                            {
                                instance.Prison.JobObject.TerminateProcesses(-1);
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning("Unable to stop application {0}. Exception: {1}", instance.Properties.Name, ex.ToString());
                            }
                        }
                    }

                    // Remove the instance system resources, except the instance directory
                    if (isCrashed || isOldCrash || isStopped || isDeleted)
                    {
                        this.monitoring.RemoveInstanceResources(instance);

                        if (instance.Prison.IsLocked)
                        {
                            try
                            {
                                Logger.Info("Destroying prison for instance {0} and closing firewall port", instance.Properties.Name);

                                FirewallTools.ClosePort(instance.Properties.Port);
                                instance.Prison.Destroy();
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning("Unable to cleanup application {0}. Exception: {1}", instance.Properties.Name, ex.ToString());
                            }
                        }
                    }

                    // Remove the instance directory, including the app's logs
                    if ((isCrashed && isFlapping) || isOldCrash || isStopped || isDeleted)
                    {
                        if (this.fileResources.DisableDirCleanup)
                        {
                            instance.Properties.Directory = null;
                        }

                        if (instance.Properties.Directory != null && !instance.Prison.IsLocked)
                        {
                            try
                            {
                                try
                                {
                                    Logger.Info("Cleaning up directory for instance {0}", instance.Properties.Name);

                                    Directory.Delete(instance.Properties.Directory, true);

                                    instance.Properties.Directory = null;
                                }
                                catch (IOException)
                                {
                                }

                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                Logger.Warning("Unable to delete application directory {0}. Exception: {1}", instance.Properties.Directory, ex.ToString());
                            }
                        }

                        if (!instance.Prison.IsLocked && instance.Properties.Directory == null)
                        {
                            removeDroplet = true;
                        }
                    }

                    // If the remove droplet flag was set, delete the instance form the DEA. The removal is made here to avoid deadlocks.
                    if (removeDroplet)
                    {
                        this.monitoring.RemoveInstanceResources(instance);
                        this.droplets.RemoveDropletInstance(instance);
                    }
                });

            this.stagingTaskRegistry.ForEach(
                true,
                delegate(StagingInstance instance)
                {
                    if (instance.CompileProcess != null)
                    {
                        if (instance.CompileProcess.HasExited)
                        {
                            if (instance.CompileProcess.ExitCode != 0)
                            {
                                instance.StagingException = new Exception("Compilation failed");
                            }
                            Logger.Info("Destroying prison for staging instance {0}", instance.Properties.TaskId);
                            instance.Properties.Stopped = true;
                            try
                            {
                                instance.Container.JobObject.TerminateProcesses(-1);
                            }
                            catch { }
                            instance.Properties.CleanupInstance = true;
                            instance.CompileProcess = null;
                        }
                        else
                        {
                            if (instance.Properties.Stopped)
                            {
                                instance.CompileProcess.Kill();
                                Logger.Info("Destroying prison for staging instance {0}", instance.Properties.TaskId);
                                try
                                {
                                    instance.Container.JobObject.TerminateProcesses(-1);
                                }
                                catch { }
                                instance.Properties.CleanupInstance = true;
                                instance.CompileProcess = null;
                            }

                            if (DateTime.Now.Subtract(instance.Properties.Start) > TimeSpan.FromMilliseconds(this.stagingTimeoutMs))
                            {
                                instance.CompileProcess.Kill();
                                Logger.Info("Destroying prison for staging instance {0}", instance.Properties.TaskId);
                                try
                                {
                                    instance.Container.JobObject.TerminateProcesses(-1);
                                }
                                catch { }
                                instance.StagingException = new Exception("Compilation timed out");
                                instance.Properties.CleanupInstance = true;
                                instance.CompileProcess = null;
                            }
                        }
                    }

                    if (instance.Properties.Stopped && !instance.Properties.StagingDone)
                    {
                        if (!instance.StopRequested)
                        {
                            this.AfterStagingFinished(instance);
                        }
                        else
                        {
                            Logger.Info("Destroying prison for staging instance {0}", instance.Properties.TaskId);
                            try
                            {
                                instance.Container.JobObject.TerminateProcesses(-1);
                            }
                            catch { }
                            instance.Properties.CleanupInstance = true;
                            instance.CompileProcess = null;
                        }
                        instance.Properties.StagingDone = true;
                    }

                    if (instance.Properties.CleanupInstance)
                    {

                        try
                        {
                            if (instance.Container.IsLocked)
                            {
                                instance.Container.Destroy();
                            }

                            Logger.Debug("Cleaning up directory {0}", instance.Workspace.BaseDir);

                            if (instance.Cleanup())
                            {
                                Logger.Debug("Done cleaning up directory {0}", instance.Workspace.BaseDir);

                                this.monitoring.RemoveInstanceResources(instance);
                                this.stagingTaskRegistry.RemoveStagingInstance(instance);
                            }

                        }
                        catch
                        {
                            Logger.Info("Failed destroying prison for directory {0}", instance.Workspace.BaseDir);
                        }

                    }
                });
        }

        private void CleanupStagingInstances()
        {
            if (!File.Exists(this.stagingTaskRegistry.StagingStateFile))
            {
                return;
            }

            object[] instances = JsonConvertibleObject.DeserializeFromJsonArray(File.ReadAllText(this.stagingTaskRegistry.StagingStateFile));

            foreach (object obj in instances)
            {
                StagingInstance instance = null;
                try
                {
                    instance = new StagingInstance();
                    instance.Properties.FromJsonIntermediateObject(obj);

                    Logger.Info("Recovering Process Prison: {0}", instance.Properties.InstanceId);

                    instance.Container = CloudFoundry.WindowsPrison.PrisonManager.LoadPrisonAndAttach(Guid.Parse(instance.Properties.InstanceId));

                    foreach (Process p in instance.Container.JobObject.GetJobProcesses())
                    {
                        if (!p.HasExited)
                        {
                            p.Kill();
                        }
                    }
                    if (instance.Container.IsLocked)
                    {
                        instance.Container.Destroy();
                    }
                    instance.Workspace = new StagingWorkspace(instance.Properties.Directory);
                    instance.Cleanup();
                }
                catch (Exception ex)
                {
                    Logger.Warning("Error deleting staging environment for task {0}: {1}", instance.Properties.TaskId, ex.ToString());
                }
                finally
                {
                    this.stagingTaskRegistry.RemoveStagingInstance(instance);
                    if (instance != null)
                    {
                        instance.Dispose();
                    }
                }
            }
        }
    }
}