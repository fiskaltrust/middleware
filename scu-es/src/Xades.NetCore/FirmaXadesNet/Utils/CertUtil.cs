// --------------------------------------------------------------------------------------------------------------------
// CertUtil.cs
//
// FirmaXadesNet - Librería para la generación de firmas XADES
// Copyright (C) 2016 Dpto. de Nuevas Tecnologías de la Dirección General de Urbanismo del Ayto. de Cartagena
//
// This program is free software: you can redistribute it and/or modify
// it under the +terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/. 
//
// E-Mail: informatica@gemuc.es
// 
// --------------------------------------------------------------------------------------------------------------------
#pragma warning disable CA1416
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FirmaXadesNet.Utils
{
    public class CertUtil
    {
        #region Public methods

        public static X509Chain GetCertChain(X509Certificate2 certificate, X509Certificate2[] certificates = null)
        {
            X509Chain chain = new X509Chain();

            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreWrongUsage;

            if (certificates != null)
            {
                chain.ChainPolicy.ExtraStore.AddRange(certificates);
            }

            if (!chain.Build(certificate))
            {
			    //TODO: Localize
			    throw new Exception("Can not build certification chain");
                // throw new Exception("No se puede construir la cadena de certificación");
            }

            return chain;
        }

        /// <summary>
        /// Selecciona un certificado del almacén de certificados
        /// </summary>
        /// <returns></returns>
        public static X509Certificate2 SelectCertificate(string message = null, string title = null)
        {
            X509Certificate2 cert = null;

            try
            {
                // Open the store of personal certificates.
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

                if (string.IsNullOrEmpty(message))
                {
                    message = "Seleccione un certificado.";
                }

                if (string.IsNullOrEmpty(title))
                {
                    title = "Firmar archivo";
                }

                X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(fcollection, title, message, X509SelectionFlag.SingleSelection);

                if (scollection != null && scollection.Count == 1)
                {
                    cert = scollection[0];

                    if (cert.HasPrivateKey == false)
                    {
                        throw new Exception("El certificado no tiene asociada una clave privada.");
                    }
                }

                store.Close();
            }
            catch (Exception ex)
            {
                // Thx @rasputino
                throw new Exception("No se ha podido obtener la clave privada.", ex);
            }

            return cert;
        }
        //TODO: da Verificare Nuova Funzione da Fork
        /// <summary>
		/// Validates certificate chain, with manual validation of the rot certificate (passed as parameter).
        /// Caller must validate that the root is correct (i.e. look it up in a database).
        /// </summary>
        /// <param name="certificateToValidate">The certificate to be validated.</param>
        /// <param name="rootCertificate">The certificate chain should terminate in this root certificate.</param>
        /// <param name="revocationChecks">Specifies what kind of revocation check should be performed. None by default.</param>
        /// <returns></returns>
        public static bool VerifyCertificate(X509Certificate2 certificateToValidate, X509Certificate2 rootCertificate, X509RevocationMode revocationChecks = X509RevocationMode.NoCheck)
        {
            X509Chain chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = revocationChecks;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            chain.ChainPolicy.VerificationTime = DateTime.Now;
            chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 0);

            // This part is very important. You're adding your known root here.
            // It doesn't have to be in the computer store at all. Neither certificates do.
            chain.ChainPolicy.ExtraStore.Add(rootCertificate);

            bool isChainValid = chain.Build(certificateToValidate);

            if (!isChainValid)
            {
                string[] errors = chain.ChainStatus
                    .Select(x => String.Format("{0} ({1})", x.StatusInformation.Trim(), x.Status))
                    .ToArray();
                string certificateErrorsString = "Unknown errors.";

                if (errors != null && errors.Length > 0)
                {
                    certificateErrorsString = String.Join(", ", errors);
                }

                throw new Exception("Trust chain did not complete to the known authority anchor. Errors: " + certificateErrorsString);
            }

            // This piece makes sure it actually matches your known root
            if (!chain.ChainElements
                .Cast<X509ChainElement>()
                .Any(x => x.Certificate.Thumbprint == rootCertificate.Thumbprint))
            {
                throw new Exception("Trust chain did not complete to the known authority anchor. Thumbprints did not match.");
            }
            return true;
        }

        #endregion
    }
}
#pragma warning restore CA1416