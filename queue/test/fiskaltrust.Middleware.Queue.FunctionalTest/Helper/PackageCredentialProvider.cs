using NuGet;
using System;
using System.Net;

namespace fiskaltrust.Middleware.Queue.FunctionalTest.Helper
{
    public class PackageCredentialProvider : ICredentialProvider
    {
        private readonly NetworkCredential _networkCredential = null;

        public PackageCredentialProvider(Guid cashBoxId, string accessToken)
        {
            _networkCredential = new NetworkCredential(cashBoxId.ToString(), accessToken);
        }

        public ICredentials GetCredentials(Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying) => _networkCredential;
    }
}
