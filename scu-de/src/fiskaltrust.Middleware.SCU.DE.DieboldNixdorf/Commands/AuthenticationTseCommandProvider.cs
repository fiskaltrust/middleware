using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Exceptions;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands
{
    public class AuthenticationTseCommandProvider
    {
        private readonly TseCommunicationCommandHelper _tseCommunicationHelper;
        private readonly ILogger<AuthenticationTseCommandProvider> _logger;

        public AuthenticationTseCommandProvider(ILogger<AuthenticationTseCommandProvider> logger, TseCommunicationCommandHelper tseCommunicationHelper)
        {
            _tseCommunicationHelper = tseCommunicationHelper;
            _logger = logger;
        }

        public void ExecuteAuthorized(string userId, string pin, Action action)
        {
            LoginUser(userId, pin);
            action();
            LogoutUser(userId);
        }

        public T ExecuteAuthorized<T>(string userId, string pin, Func<T> action)
        {
            LoginUser(userId, pin);
            var result = action();
            LogoutUser(userId);
            return result;
        }

        public void LoginUser(string userId, string pin)
        {
            _tseCommunicationHelper.SetManagementClientId();
            var parameters = new List<string> {
                RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber),
                userId,
                pin
            };
            try
            {
                var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.LoginUser, parameters);
                if (resultParameters != null && resultParameters.Count > 2)
                {
                    throw new DieboldNixdorfException("User is not authorized");
                }

                var loginResult = ResponseHelper.GetResultForAsciiDigit(resultParameters[0]);
                if (loginResult > 1)
                {
                    throw new DieboldNixdorfException("User is not authorized");
                }
            }
            catch (DieboldNixdorfException exception) when (exception.Message == "E_CERTIFICATE_EXPIRED")
            {
                _logger.LogError(exception, "The certificate is expired. We still are able to login, but we should replace the certificate.");
            }
        }

        public void LogoutUser(string userId)
        {
            _tseCommunicationHelper.SetManagementClientId();
            var parameters = new List<string> {
                RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber),
                userId
            };
            try
            {
                var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.LogoutUser, parameters);
                var logoutResult = ResponseHelper.GetResultForAsciiDigit(resultParameters[0]);
                if (logoutResult > 1)
                {
                    _logger.LogWarning("Failed to Logout user. This can be treaded as warning. (LogoutResult: {0})", logoutResult);
                }
            }
            catch (DieboldNixdorfException exception) when (exception.Message == "E_CERTIFICATE_EXPIRED")
            {
                _logger.LogError(exception, "The certificate is expired. We still are able to login, but we should replace the certificate.");
            }
        }
    }
}