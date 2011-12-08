﻿//using System;
//using System.Text;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Uhuru.CloudFoundry.DEA.Plugins;
//using Uhuru.CloudFoundry.Server.DEA.PluginBase;
//using System.Threading;
//using Uhuru.Utilities;
//using System.Net;
//using System.IO;

//namespace Uhuru.CloudFoundry.Test.Integration
//{
//    [TestClass]
//    public class IISPluginTest
//    {
//        private TestContext testContextInstance;

//        /// <summary>
//        ///Gets or sets the test context which provides
//        ///information about and functionality for the current test run.
//        ///</summary>
//        public TestContext TestContext
//        {
//            get
//            {
//                return testContextInstance;
//            }
//            set
//            {
//                testContextInstance = value;
//            }
//        }

//        #region Additional test attributes
//        // 
//        //You can use the following additional attributes as you write your tests:
//        //
//        //Use ClassInitialize to run code before running the first test in the class
//        //[ClassInitialize()]
//        //public static void MyClassInitialize(TestContext testContext)
//        //{
//        //}
//        //
//        //Use ClassCleanup to run code after all tests in a class have run
//        //[ClassCleanup()]
//        //public static void MyClassCleanup()
//        //{
//        //}
//        //
//        //Use TestInitialize to run code before running each test
//        //[TestInitialize()]
//        //public void MyTestInitialize()
//        //{
//        //}
//        //
//        //Use TestCleanup to run code after each test has run
//        //[TestCleanup()]
//        //public void MyTestCleanup()
//        //{
//        //}
//        //
//        #endregion


//        /// <summary>
//        ///A test for ConfigureApplication
//        ///</summary>
//        [TestMethod()]
//        [TestCategory("Integration")]
//        public void TC001_ConfigureApplicationTest()
//        {
//            //Arrange
//            IISPlugin target = new IISPlugin();
//            ApplicationVariable[] appVariables = new ApplicationVariable[] {
//              new ApplicationVariable() { Name = "VCAP_PLUGIN_STAGING_INFO", Value=@"{""assembly"":""Uhuru.CloudFoundry.DEA.Plugins.dll"",""class_name"":""Uhuru.CloudFoundry.DEA.Plugins.IISPlugin"",""logs"":{""app_error"":""logs/stderr.log"",""dea_error"":""logs/err.log"",""startup"":""logs/startup.log"",""app"":""logs/stdout.log""},""auto_wire_templates"":{""mssql-2008"":""Data Source={host},{port};Initial Catalog={name};User Id={user};Password={password};MultipleActiveResultSets=true"",""mysql-5.1"":""server={host};port={port};Database={name};Uid={user};Pwd={password};""}}" },
//              new ApplicationVariable() { Name = "VCAP_APPLICATION", Value=@"{""instance_id"":""646c477f54386d8afb279ec2f990a823"",""instance_index"":0,""name"":""sinatra_env_test_app"",""uris"":[""sinatra_env_test_app.uhurucloud.net""],""users"":[""dev@cloudfoundry.org""],""version"":""c394f661a907710b8a8bb70b84ff0c83354dbbed-1"",""start"":""2011-12-07 14:40:12 +0200"",""runtime"":""iis"",""state_timestamp"":1323261612,""port"":51202,""limits"":{""fds"":256,""mem"":67108864,""disk"":2147483648},""host"":""192.168.1.117""}" },
//              new ApplicationVariable() { Name = "VCAP_SERVICES", Value=@"{""mssql-2008"":[{""name"":""mssql-b24a2"",""label"":""mssql-2008"",""plan"":""free"",""tags"":[""mssql"",""2008"",""relational""],""credentials"":{""name"":""D4Tac4c307851cfe495bb829235cd384f094"",""username"":""US3RTfqu78UpPM5X"",""user"":""US3RTfqu78UpPM5X"",""password"":""P4SSdCGxh2gYjw54"",""hostname"":""192.168.1.3"",""port"":1433,""bind_opts"":{}}}]}" },
//              new ApplicationVariable() { Name = "VCAP_APP_HOST", Value=@"192.168.1.118" },
//              new ApplicationVariable() { Name = "VCAP_APP_PORT", Value=@"65498" },
//              new ApplicationVariable() { Name = "VCAP_WINDOWS_USER_PASSWORD", Value=@"password" },
//              new ApplicationVariable() { Name = "VCAP_WINDOWS_USER", Value=@"cfuser" },
//              new ApplicationVariable() { Name = "HOME", Value=@"c:\droplets\mydroplet" }
//            };

//            Exception exception = null;

//            //Act
//            try
//            {
//                target.ConfigureApplication(appVariables);
//            }
//            catch (Exception ex)
//            {
//                exception = ex;
//            }

//            //Assert
//            Assert.IsNull(exception, "Exception thrown");
//        }




//        /// <summary>
//        ///A test for Start WebApp
//        ///</summary>
//        [TestMethod()]
//        [TestCategory("Integration")]
//        public void TC002_StartWebAppTest()
//        {
//            //Arrange
//            IISPlugin target = new IISPlugin();
//            ApplicationInfo appInfo = new ApplicationInfo();
//            appInfo.InstanceId = Guid.NewGuid().ToString();
//            appInfo.LocalIp = TestUtil.GetLocalIp();
//            appInfo.Name = "MyTestApp";
//            appInfo.Path = Path.GetFullPath(@"..\..\..\TestApps\CloudTestApp");
//            appInfo.Port = Uhuru.Utilities.NetworkInterface.GrabEphemeralPort();
//            appInfo.WindowsPassword = "cfuser";
//            appInfo.WindowsPassword = "Password1234!";

//            Runtime runtime = new Runtime();
//            runtime.Name = "iis";

//            ApplicationVariable[] variables = null;
//            ApplicationService[] services = null;

//            string logFilePath = appInfo.Path + @"\cloudtestapp.log";

//            //Act
//            target.ConfigureApplication(appInfo, runtime, variables, services, logFilePath);
//            target.StartApplication();
//            Thread.Sleep(5000);

//            //Assert
//            WebClient client = new WebClient();
//            string html = client.DownloadString("http://localhost:" + appInfo.Port.ToString());
//            Assert.IsTrue(html.Contains("Welcome to ASP.NET!"));

//            target.StopApplication();

//            try
//            {
//                html = client.DownloadString("http://localhost:" + appInfo.Port.ToString());
//            }
//            catch
//            {
//                return;
//            }
//            Assert.Fail();
//        }


//        /// <summary>
//        ///A test for Start WebSite
//        ///</summary>
//        [TestMethod()]
//        [TestCategory("Integration")]
//        public void TC003_StartWebSiteTest()
//        {
//            //Arrange
//            IISPlugin target = new IISPlugin();
//            ApplicationInfo appInfo = new ApplicationInfo();
//            appInfo.InstanceId = Guid.NewGuid().ToString();
//            appInfo.LocalIp = TestUtil.GetLocalIp();
//            appInfo.Name = "MyTestApp";
//            appInfo.Path = Path.GetFullPath(@"..\..\..\TestApps\CloudTestWebSite");
//            appInfo.Port = Uhuru.Utilities.NetworkInterface.GrabEphemeralPort();
//            appInfo.WindowsPassword = "cfuser";
//            appInfo.WindowsPassword = "Password1234!";

//            Runtime runtime = new Runtime();
//            runtime.Name = "iis";

//            ApplicationVariable[] variables = null;
//            ApplicationService[] services = null;

//            string logFilePath = appInfo.Path + @"\cloudtestapp.log";

//            //Act
//            target.ConfigureApplication(appInfo, runtime, variables, services, logFilePath);
//            target.StartApplication();
//            Thread.Sleep(5000);

//            //Assert
//            WebClient client = new WebClient();
//            string html = client.DownloadString("http://localhost:" + appInfo.Port.ToString());
//            Assert.IsTrue(html.Contains("Welcome to ASP.NET!"));

//            target.StopApplication();

//            try
//            {
//                html = client.DownloadString("http://localhost:" + appInfo.Port.ToString());
//            }
//            catch
//            {
//                return;
//            }
//            Assert.Fail();
//        }

//        [TestMethod()]
//        [TestCategory("Integration")]
//        public void TC003_MultipleWebApps()
//        {
//            List<ApplicationInfo> appInfos = new List<ApplicationInfo>();
//            List<IISPlugin> plugins = new List<IISPlugin>();
//            List<Thread> threadsStart = new List<Thread>();
//            List<Thread> threadsStop = new List<Thread>();

//            for (int i = 0; i < 20; i++)
//            {
//                ApplicationInfo appInfo = new ApplicationInfo();
//                appInfo.InstanceId = Guid.NewGuid().ToString();
//                appInfo.LocalIp = TestUtil.GetLocalIp();
//                appInfo.Name = "MyTestApp";



//                appInfo.Path = TestUtil.CopyFolderToTemp(Path.GetFullPath(@"..\..\..\TestApps\CloudTestApp"));
//                appInfo.Port = Uhuru.Utilities.NetworkInterface.GrabEphemeralPort();
//                appInfo.WindowsPassword = "cfuser";
//                appInfo.WindowsPassword = "Password1234!";
//                appInfos.Add(appInfo);
//                plugins.Add(new IISPlugin());
//            }


//            for (int i = 0; i < 20; i++)
//            {
//                threadsStart.Add(new Thread(new ParameterizedThreadStart(delegate(object data)
//                {
//                    try
//                    {
//                        IISPlugin target = plugins[(int)data];
//                        Runtime runtime = new Runtime();
//                        runtime.Name = "iis";
//                        ApplicationVariable[] variables = null;
//                        ApplicationService[] services = null;
//                        string logFilePath = appInfos[(int)data].Path + @"\cloudtestapp.log";

//                        target.ConfigureApplication(appInfos[(int)data], runtime, variables, services, logFilePath);
//                        target.StartApplication();

//                        Thread.Sleep(5000);
//                    }
//                    catch (Exception ex)
//                    {
//                        Logger.Fatal(ex.ToString());
//                    }
//                })));
//            }

//            for (int i = 0; i < threadsStart.Count; i++)
//            {
//                Thread thread = threadsStart[i];
//                thread.Start(i);
//            }

//            foreach (Thread thread in threadsStart)
//            {
//                thread.Join();
//            }


//            foreach (ApplicationInfo appInfo in appInfos)
//            {
//                WebClient client = new WebClient();
//                string html = client.DownloadString("http://localhost:" + appInfo.Port.ToString());
//                Assert.IsTrue(html.Contains("My ASP.NET Application"));
//            }


//            for (int i = 0; i < 20; i++)
//            {
//                threadsStop.Add(new Thread(new ParameterizedThreadStart(delegate(object data)
//                {
//                    try
//                    {
//                        IISPlugin target = plugins[(int)data];
//                        target.StopApplication();
//                        Thread.Sleep(5000);
//                    }
//                    catch (Exception ex)
//                    {
//                        Logger.Fatal(ex.ToString());
//                    }
//                })));
//            }


//            for (int i = 0; i < threadsStop.Count; i++)
//            {
//                Thread thread = threadsStop[i];
//                thread.Start(i);
//            }

//            foreach (Thread thread in threadsStop)
//            {
//                thread.Join();
//            }

//            foreach (ApplicationInfo appInfo in appInfos)
//            {
//                try
//                {
//                    WebClient client = new WebClient();
//                    string html = client.DownloadString("http://localhost:" + appInfo.Port.ToString());
//                    Assert.Fail();
//                }
//                catch
//                {
//                }
//            }
//        }

//    }
//}
