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

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Commands.Common.Models;
using Microsoft.WindowsAzure.Commands.SqlDatabase.Model;
using Microsoft.WindowsAzure.Commands.SqlDatabase.Services.Server;
using Microsoft.WindowsAzure.Commands.SqlDatabase.Test.UnitTests.Database.Cmdlet;
using Microsoft.WindowsAzure.Commands.SqlDatabase.Test.UnitTests.MockServer;
using Microsoft.WindowsAzure.Commands.Test.Utilities.Common;
using Microsoft.WindowsAzure.Commands.Utilities.Common;

namespace Microsoft.WindowsAzure.Commands.SqlDatabase.Test.UnitTests.Server.Cmdlet
{
    [TestClass]
    public class ServerCmdletTests : TestBase
    {
        [TestCleanup]
        public void CleanupTest()
        {
            // Save the mock session results
            MockServerHelper.SaveDefaultSessionCollection();
        }

        private static void VerifyServer(SqlDatabaseServerContext server, string adminLogin, string location)
        {
            Assert.AreEqual(adminLogin, server.AdministratorLogin, "Expecting server login to match.");
            Assert.AreEqual(location, server.Location, "Expecting matching location.");
            Assert.AreEqual(10, server.ServerName.Length, "Expecting a valid server name.");
        }

        private static void VerifyServer(SqlDatabaseServerContext server, string adminLogin, string location, string version, string state)
        {
            VerifyServer(server, adminLogin, location);
            Assert.AreEqual(version, server.Version, "Server version doesn't match");
            Assert.AreEqual(state, server.State, "Server state does not match");
        }

        [TestMethod]
        public void AzureSqlDatabaseServerTests()
        {
            // This test uses the https endpoint, setup the certificates.
            MockHttpServer.SetupCertificates();

            using (System.Management.Automation.PowerShell powershell = System.Management.Automation.PowerShell.Create())
            {
                // Setup the subscription used for the test
                AzureSubscription subscription =
                    UnitTestHelper.SetupUnitTestSubscription(powershell);

                // Create a new server
                HttpSession testSession = MockServerHelper.DefaultSessionCollection.GetSession(
                    "UnitTest.AzureSqlDatabaseServerTests");
                ServerTestHelper.SetDefaultTestSessionSettings(testSession);
                testSession.RequestValidator =
                    new Action<HttpMessage, HttpMessage.Request>(
                    (expected, actual) =>
                    {
                        Assert.AreEqual(expected.RequestInfo.Method, actual.Method);
                        Assert.IsTrue(
                            actual.UserAgent.Contains(ApiConstants.UserAgentHeaderValue),
                            "Missing proper UserAgent string.");
                    });

                powershell.Runspace.SessionStateProxy.SetVariable("login", "mylogin");
                powershell.Runspace.SessionStateProxy.SetVariable("password", "Pa$$w0rd!");
                powershell.Runspace.SessionStateProxy.SetVariable("location", "East Asia");
                Collection<PSObject> newServerResult = MockServerHelper.ExecuteWithMock(
                    testSession,
                    MockHttpServer.DefaultHttpsServerPrefixUri,
                    () =>
                    {
                        return powershell.InvokeBatchScript(
                            @"New-AzureSqlDatabaseServer" +
                            @" -AdministratorLogin $login" +
                            @" -AdministratorLoginPassword $password" +
                            @" -Location $location");
                    });

                Collection<PSObject> getServerResult = MockServerHelper.ExecuteWithMock(
                    testSession,
                    MockHttpServer.DefaultHttpsServerPrefixUri,
                    () =>
                    {
                        powershell.Runspace.SessionStateProxy.SetVariable("server", newServerResult);
                        return powershell.InvokeBatchScript(
                            @"Get-AzureSqlDatabaseServer $server.ServerName");
                    });

                Collection<PSObject> setServerResult = MockServerHelper.ExecuteWithMock(
                    testSession,
                    MockHttpServer.DefaultHttpsServerPrefixUri,
                    () =>
                    {
                        powershell.Runspace.SessionStateProxy.SetVariable("server", newServerResult);
                        powershell.Runspace.SessionStateProxy.SetVariable("password", "Pa$$w0rd2");
                        powershell.InvokeBatchScript(
                            @"$server | Set-AzureSqlDatabaseServer" +
                            @" -AdminPassword $password" +
                            @" -Force");
                        return powershell.InvokeBatchScript(
                            @"$server | Get-AzureSqlDatabaseServer");
                    });

                Collection<PSObject> removeServerResult = MockServerHelper.ExecuteWithMock(
                    testSession,
                    MockHttpServer.DefaultHttpsServerPrefixUri,
                    () =>
                    {
                        powershell.Runspace.SessionStateProxy.SetVariable("server", newServerResult);
                        powershell.InvokeBatchScript(
                            @"$server | Remove-AzureSqlDatabaseServer" +
                            @" -Force");

                        return powershell.InvokeBatchScript(
                            @"Get-AzureSqlDatabaseServer");
                    });

                Assert.AreEqual(0, powershell.Streams.Error.Count, "Unexpected Errors during run!");
                Assert.AreEqual(0, powershell.Streams.Warning.Count, "Unexpected Warnings during run!");

                // Validate New-AzureSqlDatabaseServer results
                SqlDatabaseServerContext server = 
                    newServerResult.Single().BaseObject as SqlDatabaseServerContext;
                Assert.IsNotNull(server, "Expecting a SqlDatabaseServerContext object");
                VerifyServer(
                    server,
                    (string)powershell.Runspace.SessionStateProxy.GetVariable("login"),
                    (string)powershell.Runspace.SessionStateProxy.GetVariable("location"));

                // Validate Get-AzureSqlDatabaseServer results
                server = getServerResult.Single().BaseObject as SqlDatabaseServerContext;
                Assert.IsNotNull(server, "Expecting a SqlDatabaseServerContext object");
                VerifyServer(
                    server,
                    (string)powershell.Runspace.SessionStateProxy.GetVariable("login"),
                    (string)powershell.Runspace.SessionStateProxy.GetVariable("location"),
                    "1.0",
                    null);

                server = setServerResult.Single().BaseObject as SqlDatabaseServerContext;
                Assert.IsNotNull(server, "Expecting a SqlDatabaseServerContext object");
                VerifyServer(
                    server,
                    (string)powershell.Runspace.SessionStateProxy.GetVariable("login"),
                    (string)powershell.Runspace.SessionStateProxy.GetVariable("location"),
                    "1.0",
                    null);

                // Validate Remove-AzureSqlDatabaseServer results
                Assert.IsFalse(
                    removeServerResult.Any((o) => o.GetVariableValue<string>("ServerName") == server.ServerName),
                    "Server should have been removed.");

                powershell.Streams.ClearStreams();
            }
        }

        [TestMethod]
        public void AzureSqlDatabaseServerV2Tests()
        {
            // This test uses the https endpoint, setup the certificates.
            MockHttpServer.SetupCertificates();

            using (System.Management.Automation.PowerShell powershell = System.Management.Automation.PowerShell.Create())
            {
                // Setup the subscription used for the test
                AzureSubscription subscription =
                    UnitTestHelper.SetupUnitTestSubscription(powershell);

                // Create a new V2 server
                HttpSession testSession = MockServerHelper.DefaultSessionCollection.GetSession(
                    "UnitTest.AzureSqlDatabaseServerV2Tests");
                ServerTestHelper.SetDefaultTestSessionSettings(testSession);
                testSession.RequestValidator =
                    new Action<HttpMessage, HttpMessage.Request>(
                    (expected, actual) =>
                    {
                        Assert.AreEqual(expected.RequestInfo.Method, actual.Method);
                        Assert.IsTrue(
                            actual.UserAgent.Contains(ApiConstants.UserAgentHeaderValue),
                            "Missing proper UserAgent string.");
                    });

                powershell.Runspace.SessionStateProxy.SetVariable("login", "mylogin");
                powershell.Runspace.SessionStateProxy.SetVariable("password", "Pa$$w0rd!");
                powershell.Runspace.SessionStateProxy.SetVariable("location", "East Asia");
                Collection<PSObject> newServerResult = MockServerHelper.ExecuteWithMock(
                    testSession,
                    MockHttpServer.DefaultHttpsServerPrefixUri,
                    () =>
                    {
                        return powershell.InvokeBatchScript(
                            @"New-AzureSqlDatabaseServer" +
                            @" -AdministratorLogin $login" +
                            @" -AdministratorLoginPassword $password" +
                            @" -Location $location" +
                            @" -Version 2");
                    });

                Collection<PSObject> getServerResult = MockServerHelper.ExecuteWithMock(
                    testSession,
                    MockHttpServer.DefaultHttpsServerPrefixUri,
                    () =>
                    {
                        powershell.Runspace.SessionStateProxy.SetVariable("server", newServerResult);
                        return powershell.InvokeBatchScript(
                            @"Get-AzureSqlDatabaseServer $server.ServerName");
                    });

                Collection<PSObject> removeServerResult = MockServerHelper.ExecuteWithMock(
                    testSession,
                    MockHttpServer.DefaultHttpsServerPrefixUri,
                    () =>
                    {
                        powershell.Runspace.SessionStateProxy.SetVariable("server", newServerResult);
                        powershell.InvokeBatchScript(
                            @"$server | Remove-AzureSqlDatabaseServer" +
                            @" -Force");

                        return powershell.InvokeBatchScript(
                            @"Get-AzureSqlDatabaseServer");
                    });

                Assert.AreEqual(0, powershell.Streams.Error.Count, "Unexpected Errors during run!");
                Assert.AreEqual(0, powershell.Streams.Warning.Count, "Unexpected Warnings during run!");

                // Validate New-AzureSqlDatabaseServer results
                SqlDatabaseServerContext server =
                    newServerResult.Single().BaseObject as SqlDatabaseServerContext;
                Assert.IsNotNull(server, "Expecting a SqlDatabaseServerContext object");
                VerifyServer(
                    server,
                    (string)powershell.Runspace.SessionStateProxy.GetVariable("login"),
                    (string)powershell.Runspace.SessionStateProxy.GetVariable("location"));


                // Validate Get-AzureSqlDatabaseServer results
                server = getServerResult.Single().BaseObject as SqlDatabaseServerContext;
                Assert.IsNotNull(server, "Expecting a SqlDatabaseServerContext object");
                VerifyServer(
                    server,
                    (string)powershell.Runspace.SessionStateProxy.GetVariable("login"),
                    (string)powershell.Runspace.SessionStateProxy.GetVariable("location"),
                    "2.0",
                    null);

                powershell.Streams.ClearStreams();
            }
        }

        [TestMethod]
        public void GetAzureSqlDatabaseServerQuotaSqlAuthTest()
        {
            using (System.Management.Automation.PowerShell powershell =
                System.Management.Automation.PowerShell.Create())
            {
                // Create a context
                NewAzureSqlDatabaseServerContextTests.CreateServerContextSqlAuth(
                    powershell,
                    "$context");

                // Issue another create testdb1, causing a failure
                HttpSession testSession = MockServerHelper.DefaultSessionCollection.GetSession(
                    "UnitTest.GetAzureSqlDatabaseServerQuotaSqlAuthTest");

                DatabaseTestHelper.SetDefaultTestSessionSettings(testSession);

                testSession.RequestValidator =
                    new Action<HttpMessage, HttpMessage.Request>(
                    (expected, actual) =>
                    {
                        Assert.AreEqual(expected.RequestInfo.Method, actual.Method);
                        Assert.AreEqual(expected.RequestInfo.UserAgent, actual.UserAgent);
                        switch (expected.Index)
                        {
                            // Request 0-1: Create testdb1
                            case 0:
                            case 1:
                                DatabaseTestHelper.ValidateHeadersForODataRequest(
                                    expected.RequestInfo,
                                    actual);
                                break;
                            default:
                                Assert.Fail("No more requests expected.");
                                break;
                        }
                    });

                using (AsyncExceptionManager exceptionManager = new AsyncExceptionManager())
                {
                    Services.Server.ServerDataServiceSqlAuth context;
                    using (new MockHttpServer(
                        exceptionManager,
                        MockHttpServer.DefaultServerPrefixUri,
                        testSession))
                    {
                        Collection<PSObject> ctxPsObject = powershell.InvokeBatchScript("$context");

                        context =
                            (Services.Server.ServerDataServiceSqlAuth)ctxPsObject.First().BaseObject;

                        Collection<PSObject> q1, q2;
                        q1 = powershell.InvokeBatchScript(
                            @"$context | Get-AzureSqlDatabaseServerQuota");

                        q2 = powershell.InvokeBatchScript(
                            @"$context | Get-AzureSqlDatabaseServerQuota -QuotaName ""Premium_Databases""");

                        ServerQuota quota1 = q1.FirstOrDefault().BaseObject as ServerQuota;
                        ServerQuota quota2 = q2.FirstOrDefault().BaseObject as ServerQuota;

                        Assert.AreEqual(
                            "premium_databases",
                            quota1.Name,
                            "Unexpected quota name");
                        Assert.AreEqual(
                            "premium_databases",
                            quota2.Name,
                            "Unexpected quota name");
                    }
                }

                Assert.AreEqual(0, powershell.Streams.Error.Count, "There were errors while running the tests!");
                Assert.AreEqual(0, powershell.Streams.Warning.Count, "There were warnings while running the tests!");
            }
        }
    }
}
