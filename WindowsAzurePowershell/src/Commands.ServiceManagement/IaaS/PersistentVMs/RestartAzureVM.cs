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


namespace Microsoft.WindowsAzure.Commands.ServiceManagement.IaaS
{
    using System.Management.Automation;
    using AutoMapper;
    using Commands.Utilities.Common;
    using Management.Compute;
    using Management.Compute.Models;
    using Model;

    [Cmdlet(VerbsLifecycle.Restart, "AzureVM", DefaultParameterSetName = "ByName"), OutputType(typeof(ManagementOperationContext))]
    public class RestartAzureVMCommand : IaaSDeploymentManagementCmdletBase
    {
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The name of the Virtual Machine to restart.", ParameterSetName = "ByName")]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get;
            set;
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The Virtual Machine to restart.", ParameterSetName = "Input")]
        [ValidateNotNullOrEmpty]
        [Alias("InputObject")]
        public PersistentVMNewSM VM
        {
            get;
            set;
        }

        internal override void ExecuteCommand()
        {
            Mapper.Initialize(m => m.AddProfile<ServiceManagementPofile>());
            base.ExecuteCommand();
            if (CurrentDeploymentNewSM == null)
            {
                return;
            }

            string roleName = (this.ParameterSetName == "ByName") ? this.Name : this.VM.RoleName;
            ExecuteClientActionNewSM(
                null,
                CommandRuntime.ToString(),
                () => this.ComputeClient.VirtualMachines.Restart(this.ServiceName, CurrentDeploymentNewSM.Name, roleName),
                (s, response) => ContextFactory<ComputeOperationStatusResponse, ManagementOperationContext>(response, s));
        }
    }
}
