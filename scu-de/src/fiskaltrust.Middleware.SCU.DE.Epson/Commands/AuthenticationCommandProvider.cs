using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.Epson.ResultModels;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Commands
{
    public class AuthenticationCommandProvider
    {
        private readonly OperationalCommandProvider _operationalCommandProvider;

        public AuthenticationCommandProvider(OperationalCommandProvider operationalCommandProvider)
        {
            _operationalCommandProvider = operationalCommandProvider;
        }

        public async Task PerformAuthorizationAsync(string clientId, string pin, string sharedSecret)
        {
            var challenge = await GetChallengeAsync(clientId);
            var hashBase64 = EpsonUtilities.GenerateHash(challenge.Challenge, sharedSecret);
            await AuthenticateUserForTimeAdmin(clientId, pin, hashBase64);
        }

        public async Task PerformAuthorizationForAdminAsync(string userId, string pin, string sharedSecret)
        {
            var challenge = await GetChallengeAsync(userId);
            var hashBase64 = EpsonUtilities.GenerateHash(challenge.Challenge, sharedSecret);
            await AuthenticateUserForAdminAsync(userId, pin, hashBase64);
        }

        public async Task<AuthenticateUserForAdminResult> AuthenticateUserForAdminAsync(string userId, string pin, string hash) => await _operationalCommandProvider.ExecuteRequestAsync<AuthenticateUserForAdminResult>(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.UserAuthentication.AuthenticateUserForAdmin,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"userId", userId },
                {"pin", pin },
                {"hash", hash }
            }
        });

        public async Task<AuthenticateUserForTimeAdminResult> AuthenticateUserForTimeAdmin(string clientId, string pin, string hash) => await _operationalCommandProvider.ExecuteRequestAsync<AuthenticateUserForTimeAdminResult>(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.UserAuthentication.AuthenticateUserForTimeAdmin,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"clientId", clientId },
                {"pin", pin },
                {"hash", hash }
            }
        });

        public async Task LogOutForAdmin() => await _operationalCommandProvider.ExecuteRequestAsync(new EpsonTSEJsonCommand(Constants.Functions.UserAuthentication.LogOutForAdmin));

        public async Task LogOutForTimeAdmin(string clientId) => await _operationalCommandProvider.ExecuteRequestAsync(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.UserAuthentication.LogOutForTimeAdmin,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"clientId", clientId }
            }
        });

        // TODO 
        // UnblockUserForAdmin
        // UnblockUserForTimeAdmin

        public async Task<OutputGetChallengeResult> GetChallengeAsync(string userId) => await _operationalCommandProvider.ExecuteRequestAsync<OutputGetChallengeResult>(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.UserAuthentication.GetChallenge,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"userId", userId }
            }
        });

        // TODO 
        // AuthenticateHost
        // DeauthenticateHost
        // GetAuthenticatedUserList
        // ChangePuk
        // ChangePinForAdmin
        // ChangePinForTimeAdmin
    }
}
