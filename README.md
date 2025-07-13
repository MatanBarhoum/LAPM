# LAPM Portal: Temporary Local Admin Access Management

LAPM Portal is a secure, web-based solution for managing temporary, just-in-time (JIT) local administrator access on Windows workstations within an Active Directory environment. It replaces the insecure practice of granting permanent local admin rights with an auditable, approval-based workflow.

Users can request temporary elevation on a specific machine, which is then sent to a designated group of administrators for approval. Upon approval, a lightweight agent on the workstation automatically grants the access for the specified duration and revokes it upon expiration.

![Admin Dashboard Screenshot](https://placehold.co/800x450/6366f1/ffffff?text=Admin+Dashboard+Screenshot)

---

## ✨ Features

* **Self-Service Portal:** An intuitive Angular frontend allows any authorized domain user to request temporary local admin rights.
* **Real-time Validation:** The request form validates computer and user names against Active Directory in real-time.
* **Approval Workflow:** Designated administrators are notified and can approve or reject requests through a secure admin dashboard.
* **Time-Based Access:** Privileges are granted only for the approved duration and are automatically revoked.
* **Full Management:** Admins can extend the time for active sessions or revoke them immediately from the web UI.
* **Automated Agent:** A lightweight PowerShell agent on each workstation handles the granting and revocation of privileges without manual intervention.
* **Secure & Auditable:**
    * Built on ASP.NET Core 6.0 with Windows Authentication.
    * Role-based access control using dedicated Active Directory security groups.
    * All requests and actions are logged in a central SQL Server database.
* **User Notifications:** The agent notifies logged-in users via a pop-up message when their access has been revoked, prompting them to log off.

---

## 🏗️ Architecture

The system is built with a modern, decoupled architecture:

* **Backend API:** A secure C# ASP.NET Core 6.0 Web API that handles business logic, Active Directory communication, and database interactions.
* **Frontend UI:** A responsive and modern Angular standalone application built with TypeScript and styled with Tailwind CSS.
* **Database:** Microsoft SQL Server (including Express edition) to store all request data, managed by Entity Framework Core.
* **Workstation Agent:** A resilient PowerShell script designed to be deployed via Group Policy as a scheduled task.

---

## 🚀 Setup and Deployment Guide

Follow these steps to deploy the LAPM Portal in your own environment.

### Prerequisites

1.  **Domain Controller:** An Active Directory environment.
2.  **Web Server (IIS):** A Windows Server with IIS installed.
    * [.NET 6.0 Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
    * [IIS URL Rewrite Module](https://www.iis.net/downloads/microsoft/url-rewrite)
3.  **Database Server:** Microsoft SQL Server (Express edition is sufficient).
4.  **Development Machine:** A PC with Node.js (for building Angular) and the .NET 6 SDK.

### Step 1: Active Directory Configuration

1.  Open **Active Directory Users and Computers**.
2.  Create two new **Security Groups** with **Global** scope:
    * `LAPM_Users`: Members of this group can log in and submit requests.
    * `LAPM_Admins`: Members of this group can access the admin dashboard to approve/reject/manage requests.
3.  Add your test accounts (and later, your real users and admins) to these groups.

### Step 2: Backend API Deployment

1.  **Build the Project:** On your development machine, publish the C# API project to a folder.
2.  **Configure `appsettings.json`:** Edit the `appsettings.json` file in your published folder.
    ```json
    {
      // ...
      "ConnectionStrings": {
        "DefaultConnection": "Server=YOUR_SQL_SERVER\\INSTANCE;Database=LAPM_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
      },
      "AccessControl": {
        "AdminGroup": "YOUR_DOMAIN\\LAPM_Admins",
        "UserGroup": "YOUR_DOMAIN\\LAPM_Users"
      }
    }
    ```
3.  **Create the Database:** Use the provided `SQL Server Database Creation Script` to create the `LAPM_DB` database and tables on your SQL Server instance.
4.  **Deploy to IIS:**
    * Copy the published backend files to a folder on your web server (e.g., `C:\inetpub\LAPM-API`).
    * In IIS Manager, create a new website pointing to this folder.
    * **Authentication:** Enable **Windows Authentication** and **Anonymous Authentication**.
    * **Application Pool:** Ensure the App Pool Identity has login, `db_datareader`, and `db_datawriter` permissions on the `LAPM_DB` database.
    * **Add `web.config`:** Place the provided `web.config` for preflight requests in the root of the API folder.

### Step 3: Frontend UI Deployment

1.  **Configure Environment:** In the Angular project, edit `src/environments/environment.prod.ts` and set the `apiUrl` to the URL of your backend API site.
2.  **Build the Project:** On your development machine, run `ng build`. This will create the static files in the `dist/lapm-frontend/browser` folder.
3.  **Add `web.config`:** Place the provided `web.config` for Angular routing into the build output folder (`dist/lapm-frontend/browser`).
4.  **Deploy to IIS:**
    * Copy the contents of the build output folder to a new folder on your web server (e.g., `C:\inetpub\LAPM-Frontend`).
    * In IIS Manager, create a new website pointing to this folder.
    * **Authentication:** Enable **Anonymous Authentication** and disable all other types.

### Step 4: Workstation Agent Deployment

1.  **Configure the Script:** Edit the `LAPM_Agent.ps1` script and set the `$ApiBaseUrl` variable to the URL of your backend API site.
2.  **Deploy via GPO:**
    * Place the configured script on a network share accessible to all computers (e.g., NETLOGON).
    * Create a new Group Policy Object and link it to the OU containing your workstations.
    * Create a **Scheduled Task** (`Computer Configuration` -> `Control Panel Settings` -> `Scheduled Tasks`).
    * **General:** Run as `NT AUTHORITY\System` with highest privileges.
    * **Triggers:** Run every 15 or 30 minutes.
    * **Actions:** Start a program with the following arguments:
        * Program: `powershell.exe`
        * Arguments: `-NonInteractive -ExecutionPolicy Bypass -File "\\your-domain.com\NETLOGON\LAPM_Agent.ps1"`

---

## 📄 License

This project is licensed under the **MIT License**. See the `LICENSE` file for details.

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! Feel free to check the [issues page](https://github.com/your-username/your-repo/issues).
