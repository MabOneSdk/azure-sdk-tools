﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------


namespace Microsoft.WindowsAzure.Commands.ScenarioTest.MediaServicesTests
{

    using VisualStudio.TestTools.UnitTesting;
    using Common;

    [TestClass]
    public class MediaServicesTests : WindowsAzurePowerShellTest
    {
        public MediaServicesTests()
            : base("MediaServices\\MediaServices.ps1")
        {

        }

        [TestInitialize]
        public override void TestSetup()
        {
            base.TestSetup();
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            powershell.AddScript("MediaServicesTest-Cleanup");
            powershell.Invoke();
            base.TestCleanup();
        }

        [TestMethod]
        public void TestNewAzureMediaServicesKey()
        {
            RunPowerShellTest("Test-NewAzureMediaServicesKey");
        }

        [TestMethod]
        public void TestRemoveAzureMediaServicesAccount()
        {
            RunPowerShellTest("Test-RemoveAzureMediaServicesAccount");
        }

        [TestMethod]
        public void TestGetAzureMediaServicesAccount()
        {
            RunPowerShellTest("Test-GetAzureMediaServicesAccount");
        }
        [TestMethod]
        public void TestGetAzureMediaServicesAccountByName()
        {
            RunPowerShellTest("Test-GetAzureMediaServicesAccountByName");
        }

        public void TestNewAzureMediaServiceAccountPassingStorageKey()
        {
            RunPowerShellTest("NewAzureMediaServicesAccountWithStorageKey");
        }
    }
}
