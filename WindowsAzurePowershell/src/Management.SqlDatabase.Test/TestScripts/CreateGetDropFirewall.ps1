# ----------------------------------------------------------------------------------
#
# Copyright 2011 Microsoft Corporation
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
# ----------------------------------------------------------------------------------

[CmdletBinding()]
Param
(
    [Parameter(Mandatory=$true, Position=0)]
    [ValidateNotNullOrEmpty()]
    [string]
    $subscriptionID, 
    [Parameter(Mandatory=$true, Position=1)]
    [ValidateNotNullOrEmpty()]
    [String]
    $certThumbPrint,
    [Parameter(Mandatory=$true, Position=2)]
    [ValidateNotNullOrEmpty()]
    [String]
    $serverLocation
)
Write-Output "`$subscriptionID=$subscriptionID"
Write-Output "`$certThumbPrint=$certThumbPrint"
Write-Output "`$serverLocation=$serverLocation"

. .\CommonFunctions.ps1

Try
{
    Init-TestEnvironment -subscriptionID $subscriptionID -certThumbPrint $certThumbPrint
    $isTestPass = $False
    
    # Create Server
    $loginName="mylogin1"
    $loginPassword="Sql@zure1"
    Write-Output "Creating server"
    $server = New-AzureSqlDatabaseServer -AdministratorLogin $loginName -AdministratorLoginPassword $loginPassword -Location $serverLocation
    Assert {$server} "Server is not created"
    Write-Output "Server $($server.ServerName) created"
    
    # Create two Firewall rules
    $rule1Name="rule1"
    $rule1StartIP="1.0.0.0"
    $rule1EndIP="2.0.0.0"
    Write-Output "Creating Firewall rule $rule1Name ..."
    $rule = New-AzureSqlDatabaseFirewallRule -ServerName $server.ServerName -RuleName $rule1Name -StartIpAddress $rule1StartIP -EndIpAddress $rule1EndIP
    Write-Output "created"
    Validate-FirewallRule -rule $rule -expectedServerName $server.ServerName -expectedName $rule1Name -expectedStartIP $rule1StartIP -expectedEndIP $rule1EndIP
    
    $rule2Name="rule2"
    $rule2StartIP="2.3.4.5"
    $rule2EndIP="3.4.5.6"
    Write-Output "Creating Firewall rule $rule2Name ..."
    $rule = New-AzureSqlDatabaseFirewallRule -ServerName $server.ServerName -RuleName $rule2Name -StartIpAddress $rule2StartIP -EndIpAddress $rule2EndIP
    Write-Output "created"
    Validate-FirewallRule -rule $rule -expectedServerName $server.ServerName -expectedName $rule2Name -expectedStartIP $rule2StartIP -expectedEndIP $rule2EndIP
    
    # Get Firewall rules and validate
    Write-Output "Getting firewall rules..."
    $rules = Get-AzureSqlDatabaseFirewallRule -ServerName $server.ServerName
    Write-Output "Got firewall rules"
    Assert {$rules} "Get firewall rule didn't return any rule"
    Assert {$rules.Count -eq 2} "Get firewall rule didn't return expected number of rules 2. But returned $rule.Count"
    
    Write-Output "validating Firewall rule $rule1Name ..."
    $rule = $rules | Where-Object {$_.RuleName -eq $rule1Name}
    Validate-FirewallRule -rule $rule -expectedServerName $server.ServerName -expectedName $rule1Name -expectedStartIP $rule1StartIP -expectedEndIP $rule1EndIP

    Write-Output "validating Firewall rule $rule2Name ..."
    $rule = $rules | Where-Object {$_.RuleName -eq $rule2Name}
    Validate-FirewallRule -rule $rule -expectedServerName $server.ServerName -expectedName $rule2Name -expectedStartIP $rule2StartIP -expectedEndIP $rule2EndIP
    
    # Delete a Firewall rules
    Write-Output "Deleting firewall rule $rule1Name ..."
    Remove-AzureSqlDatabaseFirewallRule -ServerName $server.ServerName -RuleName $rule1Name
    Write-Output "Deleted firewall rule $rule1Name"
    $rules = Get-AzureSqlDatabaseFirewallRule -ServerName $server.ServerName | Where-Object {$_.RuleName -eq $rule1Name}
    Assert {$rules -eq $null} "Firewall rule $rule1Name is not dropped"
    
    $isTestPass = $True
}
Finally
{
    if($server)
    {
        # Drop server
        Write-Output "Dropping server $($server.ServerName) ..."
        Remove-AzureSqlDatabaseServer -ServerName $server.ServerName
        Write-Output "Dropped server $($server.ServerName)"
    }
    if($IsTestPass)
    {
        Write-Output "PASS"
    }
    else
    {
        Write-Output "FAILED"
    }
}

