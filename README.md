# Lapster
Welcome! Lapster is an application designed to streamline the process of searching for LAPS (Local Administrator Password Solution) information across various platforms. This application is particulary useful for IT administrators and security professionals who need to manage and secure a wide range of devices.

![image0](https://github.com/altrhombus/lapster/assets/5846422/f9c3bbd2-915b-4eae-89d0-ce10d4670bd7)

Lapster helps you search across all LAPS sources:
 - **Legacy LAPS** clients (Windows Server 2016 and older) that store their password information in Active Directory in the `ms-Mcs-AdmPwd` AD attribute
 - **Windows LAPS** clients (Windows Server 2019 and newer, Windows 10, Windows 11) that store their password information in Active Directory in the `msLAPS-*` attributes
 - **Windows LAPS** clients (Windows 10, Windows 11) that store their password information in Microsoft Intune

## How to Use
### Environment Preparations
For Lapster to connect to your Microsoft Graph instance, you will need to register an app for it first:
1. Sign in to the [Microsoft Entra admin center](https://entra.microsoft.com/) as at least an [Application Developer](https://learn.microsoft.com/en-us/entra/identity/role-based-access-control/permissions-reference#application-developer).
2. If you have access to multiple tenants, use the Settings icon in the top menu to switch to the tenant in which you want to register the application from the `Directories + subscriptions` menu.
3. Browse to `Identity` > `Applications` > `App registrations` (or search for `App registrations`).
4. Select `New registration`.
5. Enter a Name for your application, for example `Lapster`. Users of your app might see this name, and you can change it later.
6. In the Supported account types section, select `Accounts in this organizational directory only (O365 only - Single tenant)`.
7. Select `Register`.
8. Under `Manage`, select `Authentication` > `Add a platform`.
9. Select `Mobile and desktop applications`.
10. In the Redirect URIs section, select `https://login.microsoftonline.com/common/oauth2/nativeclient`.
11. Select `Configure`.
12. In the Properties page, take note of the `Application ID` and `Tenant ID`.

### Building the application
At the moment, the application ID and tenant ID are built into the app. Before you build Lapster, modify `App.xaml.cs` by pasting your `Application ID` in the `private static string ClientId` field and your `Tenant ID` in the `private static string Tenant` field.

### Using the application
To use Lapster, just search for a computer in the search box. Lapster will search AD and Intune and display and found passwords and their expiration dates. For Windows LAPS (Active Directory) passwords, the username and last refresh time will be displayed. If encryption and password history is enabled, all previous passwords stored in AD will be listed as well.

When searching for a computer, you may enter just the computer name to search the local domain. Enter the computer's FQDN to search another domain. Press `CTRL + Enter` to skip searching Microsoft Graph if you know a computer has it's credentials stored on-premises.
