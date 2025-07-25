<#
.SYNOPSIS
    Workstation Agent for the Temporary Local Admin Privileges (LAPM) System.
    This script synchronizes the local 'Administrators' group and notifies logged-in users
    if their access has been granted or revoked.

.DESCRIPTION
    This script is intended to be run as a scheduled task on each target workstation.
    It performs the following actions:
    1. Fetches the required admin state from the central API.
    2. Gets the current members of the local 'Administrators' group.
    3. Calculates which users to add and which to remove.
    4. Adds and removes users from the group as needed.
    5. For each user that is added, it checks if they are logged in and notifies them to log off/on to activate their new rights.
    6. For each user that is removed, it checks if they are logged in and notifies them that their rights have been revoked.

.VERSION
    1.4
#>

# --- CONFIGURATION ---
# IMPORTANT: Modify these variables to match your environment.

# The base URL of your deployed ASP.NET Core API.
$ApiBaseUrl = "https://control.lab.local"

# The name of the local group to manage.
$LocalAdminGroupName = "Administrators"

# (Optional) Path for a log file. If left empty, no log file will be created.
$LogFilePath = "C:\ProgramData\LAPM\agent.log"

# --- SCRIPT LOGIC (Do not modify below this line) ---

# Function for writing log messages
function Write-Log {
    param ([string]$Message)
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogMessage = "[$Timestamp] $Message"
    Write-Output $LogMessage
    if (-not [string]::IsNullOrWhiteSpace($LogFilePath)) {
        try {
            if (-not (Test-Path (Split-Path $LogFilePath -Parent))) {
                New-Item -ItemType Directory -Path (Split-Path $LogFilePath -Parent) -Force | Out-Null
            }
            Add-Content -Path $LogFilePath -Value $LogMessage
        }
        catch {
            Write-Warning "Could not write to log file: $($_.Exception.Message)"
        }
    }
}

# Function to send a message to a logged-in user's session
function Send-User-Notification {
    param (
        [string]$UserName,
        [string]$Message
    )
    Write-Log "Checking if user '$UserName' is currently logged in to send notification."
    try {
        $loggedInSession = Get-WmiObject -ClassName Win32_LogonSession -Filter "LogonType = 2" -ErrorAction Stop | 
                           Get-WmiObject -ClassName Win32_LoggedOnUser -ErrorAction Stop |
                           Where-Object { ($_.Antecedent -match "Name=`"$UserName`"") }
        
        if ($loggedInSession) {
            $sessionId = ($loggedInSession.Antecedent -split "LogonId=`"")[1].split("`"")[0]
            Write-Log "User '$UserName' is logged in to session $sessionId. Sending notification."
            msg.exe $sessionId $Message
        } else {
            Write-Log "User '$UserName' is not currently logged in."
        }
    }
    catch {
        Write-Log "Could not check for logged-in user or send notification. Error: $($_.Exception.Message)"
    }
}


Write-Log "--- LAPM Agent script started ---"

# Add custom type to ignore SSL certificate errors for older PowerShell
try {
    Add-Type -TypeDefinition @"
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint srvPoint, X509Certificate certificate,
            WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@ -ErrorAction Stop
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
}
catch {
    Write-Warning "Could not create custom certificate policy. This may fail on HTTPS if the cert is not trusted."
}

# Initialize as an empty array to prevent null issues
$RequiredAdmins = @()

try {
    $ComputerName = $env:COMPUTERNAME
    $ApiEndpoint = "$ApiBaseUrl/api/agent/state/$ComputerName"
    Write-Log "Querying API endpoint: $ApiEndpoint"
    $ApiResponse = Invoke-RestMethod -Uri $ApiEndpoint -Method Get -ErrorAction Stop
    $RequiredAdmins = @($ApiResponse.activeAdmins | ForEach-Object { $_.ToLower() })
    Write-Log "API returned $($RequiredAdmins.Count) required admin(s): $($RequiredAdmins -join ', ')"
}
catch {
    if ($_.Exception.Response.StatusCode -eq 'NotFound') {
        Write-Log "API returned 404 Not Found. Assuming no active admins are required."
    }
    else {
        Write-Log "FATAL: Could not get required state from API. $($_.Exception.Message). Script will exit to be safe."
        exit 1
    }
}

try {
    Write-Log "Getting current members of the '$LocalAdminGroupName' group."
    $LocalAdminGroup = [ADSI]"WinNT://./$LocalAdminGroupName,group"
    $CurrentMembers = @($LocalAdminGroup.psbase.Invoke("Members") | ForEach-Object { $_.GetType().InvokeMember("Name", "GetProperty", $null, $_, $null).ToLower() })
    
    $UsersToAdd = Compare-Object -ReferenceObject $CurrentMembers -DifferenceObject $RequiredAdmins -PassThru | Where-Object { $_.SideIndicator -eq "=>" }
    
    $domainUsersInGroup = @(Get-LocalGroupMember -Name $LocalAdminGroupName | Where-Object { $_.ObjectClass -eq "User" -and $_.PrincipalSource -eq "ActiveDirectory" } | ForEach-Object { $_.Name.Split('\')[-1].ToLower() })
    $UsersToRemove = Compare-Object -ReferenceObject $RequiredAdmins -DifferenceObject $domainUsersInGroup -PassThru | Where-Object { $_.SideIndicator -eq "=>" }

    # Perform ADD operations
    if ($UsersToAdd.Count -gt 0) {
        Write-Log "Users to ADD: $($UsersToAdd -join ', ')"
        foreach ($user in $UsersToAdd) {
            try {
                Write-Log "Adding '$user' to '$LocalAdminGroupName' group."
                $LocalAdminGroup.psbase.Invoke("Add", "WinNT://$($env:USERDOMAIN)/$user,user")
                
                # --- NEW: Notify user that they have been granted access ---
                $grantMessage = "You have been granted temporary administrator privileges. Please save your work and log off and back on for this change to take effect."
                Send-User-Notification -UserName $user -Message $grantMessage
            }
            catch {
                Write-Log "ERROR: Failed to add user '$user'. $($_.Exception.Message)"
            }
        }
    }
    else {
        Write-Log "No users to add."
    }

    # Perform REMOVE operations
    if ($UsersToRemove.Count -gt 0) {
        Write-Log "Users to REMOVE: $($UsersToRemove -join ', ')"
        foreach ($user in $UsersToRemove) {
            try {
                Write-Log "Removing '$user' from '$LocalAdminGroupName' group."
                $LocalAdminGroup.psbase.Invoke("Remove", "WinNT://$($env:USERDOMAIN)/$user,user")
                
                # --- Notify user that their access has been revoked ---
                $revokeMessage = "Your temporary administrator privileges have been revoked. Please save your work and log off and back on for this change to take full effect."
                Send-User-Notification -UserName $user -Message $revokeMessage
            }
            catch {
                Write-Log "ERROR: Failed to remove user '$user'. $($_.Exception.Message)"
            }
        }
    }
    else {
        Write-Log "No users to remove."
    }
}
catch {
    Write-Log "FATAL: An error occurred during group membership synchronization. $($_.Exception.Message)"
    exit 1
}

Write-Log "--- LAPM Agent script finished successfully ---"
exit 0
