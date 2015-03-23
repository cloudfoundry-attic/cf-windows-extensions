using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CloudFoundry.Configuration;
using System.IO;
using System.Configuration;

namespace CloudFoundry.Test.Integration
{
    [TestClass]
    [DeploymentItem("cfTest.config")]
    public class ConfigurationTest
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void TC001_TestConfig()
        {
            CloudFoundrySection cfSection = (CloudFoundrySection)ConfigurationManager.GetSection("cloudfoundry");

            if (!File.Exists("cfTest.config"))
                Assert.Fail();
            Assert.AreEqual("c:\\droplets", cfSection.DEA.BaseDir);
        }
    }
}
