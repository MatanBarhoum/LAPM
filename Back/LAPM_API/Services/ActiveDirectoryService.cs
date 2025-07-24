using System.DirectoryServices.AccountManagement;


namespace LAPM_API.Services
{
    public interface IActiveDirectoryService
    {
        bool ComputerExists(string computerName);
        bool UserExists(string userName);
        string GetUserPrincipalName(string userName);
    }
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly IConfiguration _configuration;
        private readonly PrincipalContext _principalContext;

        public ActiveDirectoryService(IConfiguration configuration)
        {
            _configuration = configuration;
            _principalContext = new PrincipalContext(ContextType.Domain);
        }

        public bool ComputerExists(string computerName)
        {
            if (string.IsNullOrWhiteSpace(computerName)) return false;

            try
            {
                var computer = ComputerPrincipal.FindByIdentity(_principalContext, computerName);
                return computer != null;
            }
            catch
            {
                return false;
            }
        }

        public bool UserExists(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return false;

            try
            {
                // Use SamAccountName for lookups as it's typically what users will enter.
                var user = UserPrincipal.FindByIdentity(_principalContext, IdentityType.SamAccountName, userName);
                return user != null;
            }
            catch
            {
                // TODO: Log this exception in a real application
                return false;
            }
        }

        public string GetUserPrincipalName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return string.Empty;

            try
            {
                var user = UserPrincipal.FindByIdentity(_principalContext, IdentityType.SamAccountName, userName);
                return user?.UserPrincipalName ?? string.Empty;
            }
            catch
            {
                // TODO: Log this exception in a real application
                return string.Empty;
            }
        }

    }
}
