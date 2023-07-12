// XadesSignedXml.cs
//
// XAdES Starter Kit for Microsoft .NET 3.5 (and above)
// 2010 Microsoft France
//
// Originally published under the CECILL-B Free Software license agreement,
// modified by Dpto. de Nuevas Tecnolog�as de la Direcci�n General de Urbanismo del Ayto. de Cartagena
// and published under the GNU Lesser General Public License version 3.
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Schema;
#pragma warning disable 1591
#pragma warning disable 1574

namespace Microsoft.Xades
{
    /// <summary>
    /// Types of signature standards that can be contained in XadesSignedXml class instance
    /// </summary>
    public enum KnownSignatureStandard
    {
        /// <summary>
        /// XML Digital Signature (XMLDSIG)
        /// </summary>
        XmlDsig,
        /// <summary>
        /// XML Advanced Electronic Signature (XAdES) 
        /// </summary>
        Xades
    }

    /// <summary>
    /// Bitmasks to indicate which checks need to be executed on the XAdES signature
    /// </summary>
    [FlagsAttribute]
    public enum XadesCheckSignatureMasks : ulong
    {
        /// <summary>
        /// Check the signature of the underlying XMLDSIG signature
        /// </summary>
        CheckXmldsigSignature = 0x01,
        /// <summary>
        /// Validate the XML representation of the signature against the XAdES and XMLDSIG schemas
        /// </summary>
        ValidateAgainstSchema = 0x02,
        /// <summary>
        /// Check to see if first XMLDSIG certificate has same hashvalue as first XAdES SignatureCertificate
        /// </summary>
        CheckSameCertificate = 0x04,
        /// <summary>
        /// Check if there is a HashDataInfo for each reference if there is a AllDataObjectsTimeStamp
        /// </summary>
        CheckAllReferencesExistInAllDataObjectsTimeStamp = 0x08,
        /// <summary>
        /// Check if the HashDataInfo of each IndividualDataObjectsTimeStamp points to existing Reference
        /// </summary>
        CheckAllHashDataInfosInIndividualDataObjectsTimeStamp = 0x10,
        /// <summary>
        /// Perform XAdES checks on contained counter signatures 
        /// </summary>
        CheckCounterSignatures = 0x20,
        /// <summary>
        /// Counter signatures should all contain a reference to the parent signature SignatureValue element
        /// </summary>
        CheckCounterSignaturesReference = 0x40,
        /// <summary>
        /// Check if each ObjectReference in CommitmentTypeIndication points to Reference element
        /// </summary>
        CheckObjectReferencesInCommitmentTypeIndication = 0x80,
        /// <summary>
        /// Check if at least ClaimedRoles or CertifiedRoles present in SignerRole
        /// </summary>
        CheckIfClaimedRolesOrCertifiedRolesPresentInSignerRole = 0x0100,
        /// <summary>
        /// Check if HashDataInfo of SignatureTimeStamp points to SignatureValue
        /// </summary>
        CheckHashDataInfoOfSignatureTimeStampPointsToSignatureValue = 0x0200,
        /// <summary>
        /// Check if the QualifyingProperties Target attribute points to the signature element
        /// </summary>
        CheckQualifyingPropertiesTarget = 0x0400,
        /// <summary>
        /// Check that QualifyingProperties occur in one Object, check that there is only one QualifyingProperties and that signed properties occur in one QualifyingProperties element
        /// </summary>
        CheckQualifyingProperties = 0x0800,
        /// <summary>
        /// Check if all required HashDataInfos are present on SigAndRefsTimeStamp
        /// </summary>
        CheckSigAndRefsTimeStampHashDataInfos = 0x1000,
        /// <summary>
        /// Check if all required HashDataInfos are present on RefsOnlyTimeStamp
        /// </summary>
        CheckRefsOnlyTimeStampHashDataInfos = 0x2000,
        /// <summary>
        /// Check if all required HashDataInfos are present on ArchiveTimeStamp
        /// </summary>
        CheckArchiveTimeStampHashDataInfos = 0x4000,
        /// <summary>
        /// Check if a XAdES-C signature is also a XAdES-T signature
        /// </summary>
        CheckXadesCIsXadesT = 0x8000,
        /// <summary>
        /// Check if a XAdES-XL signature is also a XAdES-X signature
        /// </summary>
        CheckXadesXLIsXadesX = 0x010000,
        /// <summary>
        /// Check if CertificateValues match CertificateRefs
        /// </summary>
        CheckCertificateValuesMatchCertificateRefs = 0x020000,
        /// <summary>
        /// Check if RevocationValues match RevocationRefs
        /// </summary>
        CheckRevocationValuesMatchRevocationRefs = 0x040000,
        /// <summary>
        /// Do all known tests on XAdES signature
        /// </summary>
        AllChecks = 0xFFFFFF
    }

    /// <summary>
    /// Facade class for the XAdES signature library.  The class inherits from
    /// the System.Security.Cryptography.Xml.SignedXml class and is backwards
    /// compatible with it, so this class can host xmldsig signatures and XAdES
    /// signatures.  The property SignatureStandard will indicate the type of the
    /// signature: XMLDSIG or XAdES.
    /// </summary>
    public class XadesSignedXml : System.Security.Cryptography.Xml.SignedXml
    {
        #region Constants
        /// <summary>
        /// The XAdES XML namespace URI
        /// </summary>
        public const string XadesNamespaceUri = "http://uri.etsi.org/01903/v1.3.2#";

        /// <summary>
        /// The XAdES v1.4.1 XML namespace URI
        /// </summary>
        public const string XadesNamespace141Uri = "http://uri.etsi.org/01903/v1.4.1#";

        /// <summary>
        /// Mandated type name for the Uri reference to the SignedProperties element
        /// </summary>
        public const string SignedPropertiesType = "http://uri.etsi.org/01903#SignedProperties";


        public const string XmlDsigObjectType = "http://www.w3.org/2000/09/xmldsig#Object";

        public const string XmlDsigManifestType = "http://www.w3.org/2000/09/xmldsig#Manifest";
        #endregion

        #region Private variables
        private static readonly string[] idAttrs = new string[]
        {
            "_id",
            "_Id",
            "_ID"
        };

        private KnownSignatureStandard signatureStandard;
        private XmlDocument cachedXadesObjectDocument;
        private string signedPropertiesIdBuffer;
        private string signatureValueId;
        private bool validationErrorOccurred;
        private string validationErrorDescription;
        private string signedInfoIdBuffer;
        private XmlDocument signatureDocument;
        private XmlElement contentElement;
        private XmlElement signatureNodeDestination;
        private bool addXadesNamespace;

        #endregion

        #region Public properties

        public static string XmlDSigPrefix { get; set; }

        public static string XmlXadesPrefix { get; set; }


        /// <summary>
        /// Property indicating the type of signature (XmlDsig or XAdES)
        /// </summary>
        public KnownSignatureStandard SignatureStandard
        {
            get
            {
                return this.signatureStandard;
            }
        }

        /// <summary>
        /// Read-only property containing XAdES information
        /// </summary>
        public XadesObject XadesObject
        {
            get
            {
                XadesObject retVal = new XadesObject();

                retVal.LoadXml(this.GetXadesObjectElement(this.GetXml()), this.GetXml());

                return retVal;
            }
        }

        /// <summary>
        /// Setting this property will add an ID attribute to the SignatureValue element.
        /// This is required when constructing a XAdES-T signature.
        /// </summary>
        public string SignatureValueId
        {
            get
            {
                return this.signatureValueId;
            }
            set
            {
                this.signatureValueId = value;
            }
        }

        /// <summary>
        /// This property allows to access and modify the unsigned properties
        /// after the XAdES object has been added to the signature.
        /// Because the unsigned properties are part of a location in the
        /// signature that is not used when computing the signature, it is save
        /// to modify them even after the XMLDSIG signature has been computed.
        /// This is needed when XAdES objects that depend on the XMLDSIG
        /// signature value need to be added to the signature. The
        /// SignatureTimeStamp element is such a property, it can only be
        /// created when the XMLDSIG signature has been computed.
        /// </summary>
        public UnsignedProperties UnsignedProperties
        {
            get
            {
                XmlElement dataObjectXmlElement;
                System.Security.Cryptography.Xml.DataObject xadesDataObject;
                XmlNamespaceManager xmlNamespaceManager;
                XmlNodeList xmlNodeList;
                UnsignedProperties retVal;

                retVal = new UnsignedProperties();
                xadesDataObject = this.GetXadesDataObject();
                if (xadesDataObject != null)
                {
                    dataObjectXmlElement = xadesDataObject.GetXml();
                    xmlNamespaceManager = new XmlNamespaceManager(dataObjectXmlElement.OwnerDocument.NameTable);
                    xmlNamespaceManager.AddNamespace("xades", XadesSignedXml.XadesNamespaceUri);
                    xmlNodeList = dataObjectXmlElement.SelectNodes("xades:QualifyingProperties/xades:UnsignedProperties", xmlNamespaceManager);
                    if (xmlNodeList.Count != 0)
                    {
                        retVal = new UnsignedProperties();
                        retVal.LoadXml((XmlElement)xmlNodeList[0], (XmlElement)xmlNodeList[0]);
                    }
                }
                else
                {
                    throw new CryptographicException("XAdES object not found. Use AddXadesObject() before accessing UnsignedProperties.");
                }

                return retVal;
            }

            set
            {
                XmlElement dataObjectXmlElement = null;
                System.Security.Cryptography.Xml.DataObject xadesDataObject, newXadesDataObject;
                XmlNamespaceManager xmlNamespaceManager;
                XmlNodeList qualifyingPropertiesXmlNodeList;
                XmlNodeList unsignedPropertiesXmlNodeList;

                xadesDataObject = this.GetXadesDataObject();
                if (xadesDataObject != null)
                {
                    dataObjectXmlElement = xadesDataObject.GetXml();
                    xmlNamespaceManager = new XmlNamespaceManager(dataObjectXmlElement.OwnerDocument.NameTable);
                    xmlNamespaceManager.AddNamespace("xades", XadesSignedXml.XadesNamespaceUri);
                    qualifyingPropertiesXmlNodeList = dataObjectXmlElement.SelectNodes("xades:QualifyingProperties", xmlNamespaceManager);
                    unsignedPropertiesXmlNodeList = dataObjectXmlElement.SelectNodes("xades:QualifyingProperties/xades:UnsignedProperties", xmlNamespaceManager);
                    if (unsignedPropertiesXmlNodeList.Count != 0)
                    {
                        qualifyingPropertiesXmlNodeList[0].RemoveChild(unsignedPropertiesXmlNodeList[0]);
                    }
                    XmlElement valueXml = value.GetXml();

                    qualifyingPropertiesXmlNodeList[0].AppendChild(dataObjectXmlElement.OwnerDocument.ImportNode(valueXml, true));

                    newXadesDataObject = new DataObject();
                    newXadesDataObject.LoadXml(dataObjectXmlElement);
                    xadesDataObject.Data = newXadesDataObject.Data;
                }
                else
                {
                    throw new CryptographicException("XAdES object not found. Use AddXadesObject() before accessing UnsignedProperties.");
                }
            }
        }

        public XmlElement ContentElement
        {
            get
            {
                return contentElement;
            }

            set
            {
                contentElement = value;
            }
        }

        public XmlElement SignatureNodeDestination
        {
            get
            {
                return this.signatureNodeDestination;
            }

            set
            {
                this.signatureNodeDestination = value;
            }
        }

        public bool AddXadesNamespace
        {
            get
            {
                return this.addXadesNamespace;
            }

            set
            {
                this.addXadesNamespace = value;
            }
        }

        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor for the XadesSignedXml class
        /// </summary>
        public XadesSignedXml()
            : base()
        {
            XmlDSigPrefix = "ds";
            XmlXadesPrefix = "xades";

            this.cachedXadesObjectDocument = null;
            this.signatureStandard = KnownSignatureStandard.XmlDsig;
        }

        /// <summary>
        /// Constructor for the XadesSignedXml class
        /// </summary>
        /// <param name="signatureElement">XmlElement used to create the instance</param>
        public XadesSignedXml(XmlElement signatureElement)
            : base(signatureElement)
        {
            XmlDSigPrefix = "ds";
            XmlXadesPrefix = "xades";

            this.cachedXadesObjectDocument = null;
        }

        /// <summary>
        /// Constructor for the XadesSignedXml class
        /// </summary>
        /// <param name="signatureDocument">XmlDocument used to create the instance</param>
        public XadesSignedXml(System.Xml.XmlDocument signatureDocument)
            : base(signatureDocument)
        {
            XmlDSigPrefix = "ds";
            XmlXadesPrefix = "xades";
            this.signatureDocument = signatureDocument;

            this.cachedXadesObjectDocument = null;
        }
        #endregion

        #region Public methods

        /// <summary>
        /// Load state from an XML element
        /// </summary>
        /// <param name="xmlElement">The XML element from which to load the XadesSignedXml state</param>
        public new void LoadXml(System.Xml.XmlElement xmlElement)
        {
            this.cachedXadesObjectDocument = null;
            this.signatureValueId = null;
            base.LoadXml(xmlElement);

            // Get original prefix for namespaces
            foreach (XmlAttribute attr in xmlElement.Attributes)
            {
                if (attr.Name.StartsWith("xmlns"))
                {
                    if (attr.Value.ToUpper() == XadesSignedXml.XadesNamespaceUri.ToUpper())
                    {
                        XmlXadesPrefix = attr.Name.Split(':')[1];
                    }
                    else if (attr.Value.ToUpper() == XadesSignedXml.XmlDsigNamespaceUrl.ToUpper())
                    {
                        XmlDSigPrefix = attr.Name.Split(':')[1];
                    }
                }
            }


            XmlNode idAttribute = xmlElement.Attributes.GetNamedItem("Id");
            if (idAttribute != null)
            {
                this.Signature.Id = idAttribute.Value;
            }
            this.SetSignatureStandard(xmlElement);

            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlElement.OwnerDocument.NameTable);

            xmlNamespaceManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
            xmlNamespaceManager.AddNamespace("xades", XadesSignedXml.XadesNamespaceUri);

            XmlNodeList xmlNodeList = xmlElement.SelectNodes("ds:SignatureValue", xmlNamespaceManager);
            if (xmlNodeList.Count > 0)
            {
                if (((XmlElement)xmlNodeList[0]).HasAttribute("Id"))
                {
                    this.signatureValueId = ((XmlElement)xmlNodeList[0]).Attributes["Id"].Value;
                }
            }

            xmlNodeList = xmlElement.SelectNodes("ds:SignedInfo", xmlNamespaceManager);
            if (xmlNodeList.Count > 0)
            {
                if (((XmlElement)xmlNodeList[0]).HasAttribute("Id"))
                {
                    this.signedInfoIdBuffer = ((XmlElement)xmlNodeList[0]).Attributes["Id"].Value;
                }
                else
                {
                    this.signedInfoIdBuffer = null;
                }
            }
        }

        /// <summary>
        /// Returns the XML representation of the this object
        /// </summary>
        /// <returns>XML element containing the state of this object</returns>
        public new XmlElement GetXml()
        {
            XmlElement retVal;
            XmlNodeList xmlNodeList;
            XmlNamespaceManager xmlNamespaceManager;

            retVal = base.GetXml();

            // Add "ds" namespace prefix to all XmlDsig nodes in the signature
            SetPrefix(XmlDSigPrefix, retVal);

            xmlNamespaceManager = new XmlNamespaceManager(retVal.OwnerDocument.NameTable);
            xmlNamespaceManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);


            /*if (this.signatureDocument != null)
            {
                                
                XmlNode nodeKeyInfoRetVal = retVal.SelectSingleNode("ds:KeyInfo", xmlNamespaceManager);
                XmlNodeList nodeKeyInfoOrig = this.signatureDocument.DocumentElement.GetElementsByTagName("KeyInfo", SignedXml.XmlDsigNamespaceUrl);

                if (nodeKeyInfoOrig.Count > 0)
                {
                    nodeKeyInfoRetVal.InnerXml = nodeKeyInfoOrig[0].InnerXml;
                }

                XmlNode nodeSignatureValue = retVal.SelectSingleNode("ds:SignatureValue", xmlNamespaceManager);
                XmlNodeList nodeSignatureValueOrign = this.signatureDocument.DocumentElement.GetElementsByTagName("SignatureValue", SignedXml.XmlDsigNamespaceUrl);

                if (nodeSignatureValueOrign.Count > 0)
                {
                    nodeSignatureValue.InnerXml = nodeSignatureValueOrign[0].InnerXml;
                }

                XmlNode nodeSignedInfo = retVal.SelectSingleNode("ds:SignedInfo", xmlNamespaceManager);
                XmlNodeList nodeSignedInfoOrig = this.signatureDocument.DocumentElement.GetElementsByTagName("SignedInfo", SignedXml.XmlDsigNamespaceUrl);

                if (nodeSignedInfoOrig.Count > 0)
                {
                    nodeSignedInfo.InnerXml = nodeSignedInfoOrig[0].InnerXml;
                }
            }*/

            if (this.signatureValueId != null && this.signatureValueId != "")
            { //Id on Signature value is needed for XAdES-T. We inject it here.
                xmlNamespaceManager = new XmlNamespaceManager(retVal.OwnerDocument.NameTable);
                xmlNamespaceManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
                xmlNodeList = retVal.SelectNodes("ds:SignatureValue", xmlNamespaceManager);
                if (xmlNodeList.Count > 0)
                {
                    ((XmlElement)xmlNodeList[0]).SetAttribute("Id", this.signatureValueId);
                }
            }


            return retVal;
        }

        /// <summary>
        /// Overridden virtual method to be able to find the nested SignedProperties
        /// element inside of the XAdES object
        /// </summary>
        /// <param name="xmlDocument">Document in which to find the Id</param>
        /// <param name="idValue">Value of the Id to look for</param>
        /// <returns>XmlElement with requested Id</returns>
        public override XmlElement GetIdElement(XmlDocument xmlDocument, string idValue)
        {
            // check to see if it's a standard ID reference
            XmlElement retVal = null;

            if (xmlDocument != null)
            {
                retVal = base.GetIdElement(xmlDocument, idValue);

                if (retVal != null)
                {
                    return retVal;
                }

                // if not, search for custom ids
                foreach (string idAttr in idAttrs)
                {
                    retVal = xmlDocument.SelectSingleNode("//*[@" + idAttr + "=\"" + idValue + "\"]") as XmlElement;
                    if (retVal != null)
                    {
                        break;
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Add a XAdES object to the signature
        /// </summary>
        /// <param name="xadesObject">XAdES object to add to signature</param>
        public void AddXadesObject(XadesObject xadesObject)
        {
            Reference reference;
            DataObject dataObject;
            XmlElement bufferXmlElement;

            if (this.SignatureStandard != KnownSignatureStandard.Xades)
            {
                dataObject = new DataObject();
                dataObject.Id = xadesObject.Id;
                dataObject.Data = xadesObject.GetXml().ChildNodes;
                this.AddObject(dataObject); //Add the XAdES object                            

                reference = new Reference();
                signedPropertiesIdBuffer = xadesObject.QualifyingProperties.SignedProperties.Id;
                reference.Uri = "#" + signedPropertiesIdBuffer;
                reference.Type = SignedPropertiesType;
                this.AddReference(reference); //Add the XAdES object reference

                this.cachedXadesObjectDocument = new XmlDocument();
                bufferXmlElement = xadesObject.GetXml();

                // Add "ds" namespace prefix to all XmlDsig nodes in the XAdES object
                SetPrefix("ds", bufferXmlElement);

                this.cachedXadesObjectDocument.PreserveWhitespace = true;
                this.cachedXadesObjectDocument.LoadXml(bufferXmlElement.OuterXml); //Cache to XAdES object for later use

                this.signatureStandard = KnownSignatureStandard.Xades;
            }
            else
            {
                throw new CryptographicException("Can't add XAdES object, the signature already contains a XAdES object");
            }
        }

        /// <summary>
        /// Additional tests for XAdES signatures.  These tests focus on
        /// XMLDSIG verification and correct form of the XAdES XML structure
        /// (schema validation and completeness as defined by the XAdES standard).
        /// </summary>
        /// <remarks>
        /// Because of the fact that the XAdES library is intentionally
        /// independent of standards like TSP (RFC3161) or OCSP (RFC2560),
        /// these tests do NOT include any verification of timestamps nor OCSP
        /// responses.
        /// These checks are important and have to be done in the application
        /// built on top of the XAdES library.
        /// </remarks>
        /// <exception cref="System.Exception">Thrown when the signature is not
        /// a XAdES signature.  SignatureStandard should be equal to
        /// <see cref="KnownSignatureStandard.Xades">KnownSignatureStandard.Xades</see>.
        /// Use the CheckSignature method for non-XAdES signatures.</exception>
        /// <param name="xadesCheckSignatureMasks">Bitmask to indicate which
        /// tests need to be done.  This function will call a public virtual
        /// methods for each bit that has been set in this mask.
        /// See the <see cref="XadesCheckSignatureMasks">XadesCheckSignatureMasks</see>
        /// enum for the bitmask definitions.  The virtual test method associated
        /// with a bit in the mask has the same name as enum value name.</param>
        /// <returns>If the function returns true the check was OK.  If the
        /// check fails an exception with a explanatory message is thrown.</returns>
        public bool XadesCheckSignature(XadesCheckSignatureMasks xadesCheckSignatureMasks)
        {
            bool retVal;

            retVal = true;
            if (this.SignatureStandard != KnownSignatureStandard.Xades)
            {
                throw new Exception("SignatureStandard is not XAdES.  CheckSignature returned: " + this.CheckSignature());
            }
            else
            {
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckXmldsigSignature) != 0)
                {
                    retVal &= this.CheckXmldsigSignature();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.ValidateAgainstSchema) != 0)
                {
                    retVal &= this.ValidateAgainstSchema();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckSameCertificate) != 0)
                {
                    retVal &= this.CheckSameCertificate();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckAllReferencesExistInAllDataObjectsTimeStamp) != 0)
                {
                    retVal &= this.CheckAllReferencesExistInAllDataObjectsTimeStamp();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckAllHashDataInfosInIndividualDataObjectsTimeStamp) != 0)
                {
                    retVal &= this.CheckAllHashDataInfosInIndividualDataObjectsTimeStamp();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckCounterSignatures) != 0)
                {
                    retVal &= this.CheckCounterSignatures(xadesCheckSignatureMasks);
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckCounterSignaturesReference) != 0)
                {
                    retVal &= this.CheckCounterSignaturesReference();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckObjectReferencesInCommitmentTypeIndication) != 0)
                {
                    retVal &= this.CheckObjectReferencesInCommitmentTypeIndication();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckIfClaimedRolesOrCertifiedRolesPresentInSignerRole) != 0)
                {
                    retVal &= this.CheckIfClaimedRolesOrCertifiedRolesPresentInSignerRole();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckHashDataInfoOfSignatureTimeStampPointsToSignatureValue) != 0)
                {
                    retVal &= this.CheckHashDataInfoOfSignatureTimeStampPointsToSignatureValue();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckQualifyingPropertiesTarget) != 0)
                {
                    retVal &= this.CheckQualifyingPropertiesTarget();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckQualifyingProperties) != 0)
                {
                    retVal &= this.CheckQualifyingProperties();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckSigAndRefsTimeStampHashDataInfos) != 0)
                {
                    retVal &= this.CheckSigAndRefsTimeStampHashDataInfos();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckRefsOnlyTimeStampHashDataInfos) != 0)
                {
                    retVal &= this.CheckRefsOnlyTimeStampHashDataInfos();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckArchiveTimeStampHashDataInfos) != 0)
                {
                    retVal &= this.CheckArchiveTimeStampHashDataInfos();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckXadesCIsXadesT) != 0)
                {
                    retVal &= this.CheckXadesCIsXadesT();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckXadesXLIsXadesX) != 0)
                {
                    retVal &= this.CheckXadesXLIsXadesX();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckCertificateValuesMatchCertificateRefs) != 0)
                {
                    retVal &= this.CheckCertificateValuesMatchCertificateRefs();
                }
                if ((xadesCheckSignatureMasks & XadesCheckSignatureMasks.CheckRevocationValuesMatchRevocationRefs) != 0)
                {
                    retVal &= this.CheckRevocationValuesMatchRevocationRefs();
                }
            }

            return retVal;
        }


        public X509Certificate2 GetSigningCertificate()
        {
            XmlNode keyXml = this.KeyInfo.GetXml().GetElementsByTagName("X509Certificate", SignedXml.XmlDsigNamespaceUrl)[0];

            if (keyXml == null)
            {
                throw new Exception("No se ha podido obtener el certificado de firma");
            }

            return new X509Certificate2(Convert.FromBase64String(keyXml.InnerText));
        }

        #region XadesCheckSignature routines
        /// <summary>
        /// Check the signature of the underlying XMLDSIG signature
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckXmldsigSignature()
        {
            bool retVal = false;
            IEnumerable<XmlAttribute> namespaces = GetAllNamespaces(GetSignatureElement());

            if (this.KeyInfo == null)
            {
                KeyInfo keyInfo = new KeyInfo();
                X509Certificate xmldsigCert = GetSigningCertificate();
                keyInfo.AddClause(new KeyInfoX509Data(xmldsigCert));
                this.KeyInfo = keyInfo;
            }

            SignatureDescription description = CryptoConfig.CreateFromName(this.SignedInfo.SignatureMethod) as SignatureDescription;

            if (description == null)
            {
                if (this.SignedInfo.SignatureMethod == "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256")
                {
                    CryptoConfig.AddAlgorithm(typeof(Microsoft.Xades.RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
                }
                else if (this.SignedInfo.SignatureMethod == "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512")
                {
                    CryptoConfig.AddAlgorithm(typeof(Microsoft.Xades.RSAPKCS1SHA512SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512");
                }
            }

            foreach (Reference reference in SignedInfo.References)
            {
                foreach (System.Security.Cryptography.Xml.Transform transform in reference.TransformChain)
                {
                    if (transform.GetType() == typeof(XmlDsigXPathTransform))
                    {
                        Type transform_Type = typeof(XmlDsigXPathTransform);
                        FieldInfo nsm_FieldInfo = transform_Type.GetField("_nsm", BindingFlags.NonPublic | BindingFlags.Instance);
                        XmlNamespaceManager nsm = (XmlNamespaceManager)nsm_FieldInfo.GetValue(transform);

                        foreach (var ns in namespaces)
                        {
                            nsm.AddNamespace(ns.LocalName, ns.Value);
                        }
                    }
                }
            }

            retVal = this.CheckDigestedReferences();

            if (retVal == false)
            {
                throw new CryptographicException("CheckXmldsigSignature() failed");
            }

            var key = this.GetPublicKey();
            retVal = this.CheckSignedInfo(key);

            if (retVal == false)
            {
                throw new CryptographicException("CheckXmldsigSignature() failed");
            }

            return retVal;
        }

        /// <summary>
        /// Validate the XML representation of the signature against the XAdES and XMLDSIG schemas
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool ValidateAgainstSchema()
        {
            bool retValue = false;

            Assembly assembly = Assembly.GetExecutingAssembly();
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            XmlSchema xmlSchema;
            Stream schemaStream;

            NameTable xadesNameTable;
            XmlNamespaceManager xmlNamespaceManager;
            XmlParserContext xmlParserContext;

            this.validationErrorOccurred = false;
            this.validationErrorDescription = "";

            try
            {
                schemaStream = assembly.GetManifestResourceStream("Microsoft.Xades.xmldsig-core-schema.xsd");
                xmlSchema = XmlSchema.Read(schemaStream, new ValidationEventHandler(this.SchemaValidationHandler));
                schemaSet.Add(xmlSchema);
                schemaStream.Close();


                schemaStream = assembly.GetManifestResourceStream("Microsoft.Xades.XAdES.xsd");
                xmlSchema = XmlSchema.Read(schemaStream, new ValidationEventHandler(this.SchemaValidationHandler));
                schemaSet.Add(xmlSchema);
                schemaStream.Close();

                if (this.validationErrorOccurred)
                {
                    throw new CryptographicException("Schema read validation error: " + this.validationErrorDescription);
                }
            }
            catch (Exception exception)
            {
                throw new CryptographicException("Problem during access of validation schemas", exception);
            }

            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.ValidationEventHandler += new ValidationEventHandler(this.XmlValidationHandler);
            xmlReaderSettings.ValidationType = ValidationType.Schema;
            xmlReaderSettings.Schemas = schemaSet;
            xmlReaderSettings.ConformanceLevel = ConformanceLevel.Auto;

            xadesNameTable = new NameTable();
            xmlNamespaceManager = new XmlNamespaceManager(xadesNameTable);
            xmlNamespaceManager.AddNamespace("xsd", XadesSignedXml.XadesNamespaceUri);

            xmlParserContext = new XmlParserContext(null, xmlNamespaceManager, null, XmlSpace.None);

            XmlTextReader txtReader = new XmlTextReader(this.GetXml().OuterXml, XmlNodeType.Element, xmlParserContext);
            XmlReader reader = XmlReader.Create(txtReader, xmlReaderSettings);
            try
            {
                while (reader.Read()) ;
                if (this.validationErrorOccurred)
                {
                    throw new CryptographicException("Schema validation error: " + this.validationErrorDescription);
                }
            }
            catch (Exception exception)
            {
                throw new CryptographicException("Schema validation error", exception);
            }
            finally
            {
                reader.Close();
            }

            retValue = true;

            return retValue;
        }

        /// <summary>
        /// Check to see if first XMLDSIG certificate has same hashvalue as first XAdES SignatureCertificate
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckSameCertificate()
        {
            bool retVal = false;

            //KeyInfoX509Data keyInfoX509Data = new KeyInfoX509Data();
            //keyInfoX509Data.LoadXml(this.KeyInfo.GetXml());
            //if (keyInfoX509Data.Certificates.Count <= 0)
            //{
            //    throw new CryptographicException("Certificate not found in XMLDSIG signature while doing CheckSameCertificate()");
            //}
            //string xmldsigCertHash = Convert.ToBase64String(((X509Certificate)keyInfoX509Data.Certificates[0]).GetCertHash());

            X509Certificate xmldsigCert = GetSigningCertificate();
            string xmldsigCertHash = Convert.ToBase64String(xmldsigCert.GetCertHash());

            CertCollection xadesSigningCertificateCollection = this.XadesObject.QualifyingProperties.SignedProperties.SignedSignatureProperties.SigningCertificate.CertCollection;
            if (xadesSigningCertificateCollection.Count <= 0)
            {
                throw new CryptographicException("Certificate not found in SigningCertificate element while doing CheckSameCertificate()");
            }
            string xadesCertHash = Convert.ToBase64String(((Cert)xadesSigningCertificateCollection[0]).CertDigest.DigestValue);


            if (String.Compare(xmldsigCertHash, xadesCertHash, true, CultureInfo.InvariantCulture) != 0)
            {
                throw new CryptographicException("Certificate in XMLDSIG signature doesn't match certificate in SigningCertificate element");
            }
            retVal = true;

            return retVal;
        }

        /// <summary>
        /// Check if there is a HashDataInfo for each reference if there is a AllDataObjectsTimeStamp
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckAllReferencesExistInAllDataObjectsTimeStamp()
        {
            AllDataObjectsTimeStampCollection allDataObjectsTimeStampCollection;
            bool allHashDataInfosExist;
            TimeStamp timeStamp;
            int timeStampCounter;
            bool retVal;

            allHashDataInfosExist = true;
            retVal = false;
            allDataObjectsTimeStampCollection = this.XadesObject.QualifyingProperties.SignedProperties.SignedDataObjectProperties.AllDataObjectsTimeStampCollection;
            if (allDataObjectsTimeStampCollection.Count > 0)
            {
                for (timeStampCounter = 0; allHashDataInfosExist && (timeStampCounter < allDataObjectsTimeStampCollection.Count); timeStampCounter++)
                {
                    timeStamp = allDataObjectsTimeStampCollection[timeStampCounter];
                    allHashDataInfosExist &= this.CheckHashDataInfosForTimeStamp(timeStamp);
                }
                if (!allHashDataInfosExist)
                {
                    throw new CryptographicException("At least one HashDataInfo is missing in AllDataObjectsTimeStamp element");
                }
            }
            retVal = true;

            return retVal;
        }

        /// <summary>
        /// Check if the HashDataInfo of each IndividualDataObjectsTimeStamp points to existing Reference
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckAllHashDataInfosInIndividualDataObjectsTimeStamp()
        {
            IndividualDataObjectsTimeStampCollection individualDataObjectsTimeStampCollection;
            bool hashDataInfoExists;
            TimeStamp timeStamp;
            int timeStampCounter;
            bool retVal;

            hashDataInfoExists = true;
            retVal = false;
            individualDataObjectsTimeStampCollection = this.XadesObject.QualifyingProperties.SignedProperties.SignedDataObjectProperties.IndividualDataObjectsTimeStampCollection;
            if (individualDataObjectsTimeStampCollection.Count > 0)
            {
                for (timeStampCounter = 0; hashDataInfoExists && (timeStampCounter < individualDataObjectsTimeStampCollection.Count); timeStampCounter++)
                {
                    timeStamp = individualDataObjectsTimeStampCollection[timeStampCounter];
                    hashDataInfoExists &= this.CheckHashDataInfosExist(timeStamp);
                }
                if (hashDataInfoExists == false)
                {
                    throw new CryptographicException("At least one HashDataInfo is pointing to non-existing reference in IndividualDataObjectsTimeStamp element");
                }
            }
            retVal = true;

            return retVal;
        }

        /// <summary>
        /// Perform XAdES checks on contained counter signatures.  If couter signature is XMLDSIG, only XMLDSIG check (CheckSignature()) is done.
        /// </summary>
        /// <param name="counterSignatureMask">Check mask applied to counter signatures</param>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckCounterSignatures(XadesCheckSignatureMasks counterSignatureMask)
        {
            CounterSignatureCollection counterSignatureCollection;
            XadesSignedXml counterSignature;
            bool retVal;

            retVal = true;
            counterSignatureCollection = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties.CounterSignatureCollection;
            for (int counterSignatureCounter = 0; (retVal == true) && (counterSignatureCounter < counterSignatureCollection.Count); counterSignatureCounter++)
            {
                counterSignature = counterSignatureCollection[counterSignatureCounter];
                //TODO: check if parent signature document is present in counterSignature (maybe a deep copy is required)
                if (counterSignature.signatureStandard == KnownSignatureStandard.Xades)
                {
                    retVal &= counterSignature.XadesCheckSignature(counterSignatureMask);
                }
                else
                {
                    retVal &= counterSignature.CheckSignature();
                }
            }
            if (retVal == false)
            {
                throw new CryptographicException("XadesCheckSignature() failed on at least one counter signature");
            }
            retVal = true;

            return retVal;
        }

        /// <summary>
        /// Counter signatures should all contain a reference to the parent signature SignatureValue element
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckCounterSignaturesReference()
        {
            CounterSignatureCollection counterSignatureCollection;
            XadesSignedXml counterSignature;
            string referenceUri;
            ArrayList parentSignatureValueChain;
            bool referenceToParentSignatureFound;
            bool retVal;

            retVal = true;
            parentSignatureValueChain = new ArrayList();
            parentSignatureValueChain.Add("#" + this.signatureValueId);
            counterSignatureCollection = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties.CounterSignatureCollection;
            for (int counterSignatureCounter = 0; (retVal == true) && (counterSignatureCounter < counterSignatureCollection.Count); counterSignatureCounter++)
            {
                counterSignature = counterSignatureCollection[counterSignatureCounter];
                referenceToParentSignatureFound = false;
                for (int referenceCounter = 0; referenceToParentSignatureFound == false && (referenceCounter < counterSignature.SignedInfo.References.Count); referenceCounter++)
                {
                    referenceUri = ((Reference)counterSignature.SignedInfo.References[referenceCounter]).Uri;
                    if (parentSignatureValueChain.BinarySearch(referenceUri) >= 0)
                    {
                        referenceToParentSignatureFound = true;
                    }
                    parentSignatureValueChain.Add("#" + counterSignature.SignatureValueId);
                    parentSignatureValueChain.Sort();
                }
                retVal = referenceToParentSignatureFound;
            }
            if (retVal == false)
            {
                throw new CryptographicException("CheckCounterSignaturesReference() failed on at least one counter signature");
            }
            retVal = true;

            return retVal;
        }

        /// <summary>
        /// Check if each ObjectReference in CommitmentTypeIndication points to Reference element
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckObjectReferencesInCommitmentTypeIndication()
        {
            CommitmentTypeIndicationCollection commitmentTypeIndicationCollection;
            CommitmentTypeIndication commitmentTypeIndication;
            bool objectReferenceOK;
            bool retVal;

            retVal = true;
            commitmentTypeIndicationCollection = this.XadesObject.QualifyingProperties.SignedProperties.SignedDataObjectProperties.CommitmentTypeIndicationCollection;
            if (commitmentTypeIndicationCollection.Count > 0)
            {
                for (int commitmentTypeIndicationCounter = 0; (retVal == true) && (commitmentTypeIndicationCounter < commitmentTypeIndicationCollection.Count); commitmentTypeIndicationCounter++)
                {
                    commitmentTypeIndication = commitmentTypeIndicationCollection[commitmentTypeIndicationCounter];
                    objectReferenceOK = true;
                    foreach (ObjectReference objectReference in commitmentTypeIndication.ObjectReferenceCollection)
                    {
                        objectReferenceOK &= this.CheckObjectReference(objectReference);
                    }
                    retVal = objectReferenceOK;
                }
                if (retVal == false)
                {
                    throw new CryptographicException("At least one ObjectReference in CommitmentTypeIndication did not point to a Reference");
                }
            }

            return retVal;
        }

        /// <summary>
        /// Check if at least ClaimedRoles or CertifiedRoles present in SignerRole
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckIfClaimedRolesOrCertifiedRolesPresentInSignerRole()
        {
            SignerRole signerRole;
            bool retVal;

            retVal = false;
            signerRole = this.XadesObject.QualifyingProperties.SignedProperties.SignedSignatureProperties.SignerRole;
            if (signerRole != null)
            {
                if (signerRole.CertifiedRoles != null)
                {
                    retVal = (signerRole.CertifiedRoles.CertifiedRoleCollection.Count > 0);
                }
                if (retVal == false)
                {
                    if (signerRole.ClaimedRoles != null)
                    {
                        retVal = (signerRole.ClaimedRoles.ClaimedRoleCollection.Count > 0);
                    }
                }
                if (retVal == false)
                {
                    throw new CryptographicException("SignerRole element must contain at least one CertifiedRole or ClaimedRole element");
                }
            }
            else
            {
                retVal = true;
            }

            return retVal;
        }

        /// <summary>
        /// Check if HashDataInfo of SignatureTimeStamp points to SignatureValue
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckHashDataInfoOfSignatureTimeStampPointsToSignatureValue()
        {
            SignatureTimeStampCollection signatureTimeStampCollection;
            bool hashDataInfoPointsToSignatureValue;
            TimeStamp timeStamp;
            int timeStampCounter;
            bool retVal;

            hashDataInfoPointsToSignatureValue = true;
            retVal = false;
            signatureTimeStampCollection = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties.SignatureTimeStampCollection;
            if (signatureTimeStampCollection.Count > 0)
            {
                for (timeStampCounter = 0; hashDataInfoPointsToSignatureValue && (timeStampCounter < signatureTimeStampCollection.Count); timeStampCounter++)
                {
                    timeStamp = signatureTimeStampCollection[timeStampCounter];
                    hashDataInfoPointsToSignatureValue &= this.CheckHashDataInfoPointsToSignatureValue(timeStamp);
                }
                if (hashDataInfoPointsToSignatureValue == false)
                {
                    throw new CryptographicException("HashDataInfo of SignatureTimeStamp doesn't point to signature value element");
                }
            }
            retVal = true;

            return retVal;
        }

        /// <summary>
        /// Check if the QualifyingProperties Target attribute points to the signature element
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckQualifyingPropertiesTarget()
        {
            string qualifyingPropertiesTarget;
            bool retVal;

            retVal = true;
            qualifyingPropertiesTarget = this.XadesObject.QualifyingProperties.Target;
            if (this.Signature.Id == null)
            {
                retVal = false;
            }
            else
            {
                if (qualifyingPropertiesTarget != ("#" + this.Signature.Id))
                {
                    retVal = false;
                }
            }
            if (retVal == false)
            {
                throw new CryptographicException("Qualifying properties target doesn't point to signature element or signature element doesn't have an Id");
            }

            return retVal;
        }

        /// <summary>
        /// Check that QualifyingProperties occur in one Object, check that there is only one QualifyingProperties and that signed properties occur in one QualifyingProperties element
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckQualifyingProperties()
        {
            XmlElement signatureElement;
            XmlNamespaceManager xmlNamespaceManager;
            XmlNodeList xmlNodeList;

            signatureElement = this.GetXml();
            xmlNamespaceManager = new XmlNamespaceManager(signatureElement.OwnerDocument.NameTable);
            xmlNamespaceManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
            xmlNamespaceManager.AddNamespace("xsd", XadesSignedXml.XadesNamespaceUri);
            xmlNodeList = signatureElement.SelectNodes("ds:Object/xsd:QualifyingProperties", xmlNamespaceManager);
            if (xmlNodeList.Count > 1)
            {
                throw new CryptographicException("More than one Object contains a QualifyingProperties element");
            }

            return true;
        }

        /// <summary>
        /// Check if all required HashDataInfos are present on SigAndRefsTimeStamp
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckSigAndRefsTimeStampHashDataInfos()
        {
            SignatureTimeStampCollection signatureTimeStampCollection;
            TimeStamp timeStamp;
            bool allRequiredhashDataInfosFound;
            bool retVal;

            retVal = true;
            signatureTimeStampCollection = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties.SigAndRefsTimeStampCollection;
            if (signatureTimeStampCollection.Count > 0)
            {
                allRequiredhashDataInfosFound = true;
                for (int timeStampCounter = 0; allRequiredhashDataInfosFound && (timeStampCounter < signatureTimeStampCollection.Count); timeStampCounter++)
                {
                    timeStamp = signatureTimeStampCollection[timeStampCounter];
                    allRequiredhashDataInfosFound &= this.CheckHashDataInfosOfSigAndRefsTimeStamp(timeStamp);
                }
                if (allRequiredhashDataInfosFound == false)
                {
                    throw new CryptographicException("At least one required HashDataInfo is missing in a SigAndRefsTimeStamp element");
                }
            }

            return retVal;
        }

        /// <summary>
        /// Check if all required HashDataInfos are present on RefsOnlyTimeStamp
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckRefsOnlyTimeStampHashDataInfos()
        {
            SignatureTimeStampCollection signatureTimeStampCollection;
            TimeStamp timeStamp;
            bool allRequiredhashDataInfosFound;
            bool retVal;

            retVal = true;
            signatureTimeStampCollection = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties.RefsOnlyTimeStampCollection;
            if (signatureTimeStampCollection.Count > 0)
            {
                allRequiredhashDataInfosFound = true;
                for (int timeStampCounter = 0; allRequiredhashDataInfosFound && (timeStampCounter < signatureTimeStampCollection.Count); timeStampCounter++)
                {
                    timeStamp = signatureTimeStampCollection[timeStampCounter];
                    allRequiredhashDataInfosFound &= this.CheckHashDataInfosOfRefsOnlyTimeStamp(timeStamp);
                }
                if (allRequiredhashDataInfosFound == false)
                {
                    throw new CryptographicException("At least one required HashDataInfo is missing in a RefsOnlyTimeStamp element");
                }
            }

            return retVal;
        }

        /// <summary>
        /// Check if all required HashDataInfos are present on ArchiveTimeStamp
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckArchiveTimeStampHashDataInfos()
        {
            SignatureTimeStampCollection signatureTimeStampCollection;
            TimeStamp timeStamp;
            bool allRequiredhashDataInfosFound;
            bool retVal;

            retVal = true;
            signatureTimeStampCollection = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties.ArchiveTimeStampCollection;
            if (signatureTimeStampCollection.Count > 0)
            {
                allRequiredhashDataInfosFound = true;
                for (int timeStampCounter = 0; allRequiredhashDataInfosFound && (timeStampCounter < signatureTimeStampCollection.Count); timeStampCounter++)
                {
                    timeStamp = signatureTimeStampCollection[timeStampCounter];
                    allRequiredhashDataInfosFound &= this.CheckHashDataInfosOfArchiveTimeStamp(timeStamp);
                }
                if (allRequiredhashDataInfosFound == false)
                {
                    throw new CryptographicException("At least one required HashDataInfo is missing in a ArchiveTimeStamp element");
                }
            }

            return retVal;
        }

        /// <summary>
        /// Check if a XAdES-C signature is also a XAdES-T signature
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckXadesCIsXadesT()
        {
            UnsignedSignatureProperties unsignedSignatureProperties;
            bool retVal;

            retVal = true;
            unsignedSignatureProperties = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties;
            if (((unsignedSignatureProperties.CompleteCertificateRefs != null) && (unsignedSignatureProperties.CompleteCertificateRefs.HasChanged()))
                || ((unsignedSignatureProperties.CompleteCertificateRefs != null) && (unsignedSignatureProperties.CompleteCertificateRefs.HasChanged())))
            {
                if (unsignedSignatureProperties.SignatureTimeStampCollection.Count == 0)
                {
                    throw new CryptographicException("XAdES-C signature should also contain a SignatureTimeStamp element");
                }
            }

            return retVal;
        }

        /// <summary>
        /// Check if a XAdES-XL signature is also a XAdES-X signature
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckXadesXLIsXadesX()
        {
            UnsignedSignatureProperties unsignedSignatureProperties;
            bool retVal;

            retVal = true;
            unsignedSignatureProperties = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties;
            if (((unsignedSignatureProperties.CertificateValues != null) && (unsignedSignatureProperties.CertificateValues.HasChanged()))
                || ((unsignedSignatureProperties.RevocationValues != null) && (unsignedSignatureProperties.RevocationValues.HasChanged())))
            {
                if ((unsignedSignatureProperties.SigAndRefsTimeStampCollection.Count == 0) && (unsignedSignatureProperties.RefsOnlyTimeStampCollection.Count == 0))
                {
                    throw new CryptographicException("XAdES-XL signature should also contain a XAdES-X element");
                }
            }

            return retVal;
        }

        /// <summary>
        /// Check if CertificateValues match CertificateRefs
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckCertificateValuesMatchCertificateRefs()
        {
            SHA1Managed sha1Managed;
            UnsignedSignatureProperties unsignedSignatureProperties;
            ArrayList certDigests;
            byte[] certDigest;
            int index;
            bool retVal;

            //TODO: Similar test should be done for XML based (Other) certificates, but as the check needed is not known, there is no implementation
            retVal = true;
            unsignedSignatureProperties = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties;
            if ((unsignedSignatureProperties.CompleteCertificateRefs != null) && (unsignedSignatureProperties.CompleteCertificateRefs.CertRefs != null) &&
                (unsignedSignatureProperties.CertificateValues != null))
            {
                certDigests = new ArrayList();
                foreach (Cert cert in unsignedSignatureProperties.CompleteCertificateRefs.CertRefs.CertCollection)
                {
                    certDigests.Add(Convert.ToBase64String(cert.CertDigest.DigestValue));
                }
                certDigests.Sort();
                foreach (EncapsulatedX509Certificate encapsulatedX509Certificate in unsignedSignatureProperties.CertificateValues.EncapsulatedX509CertificateCollection)
                {
                    sha1Managed = new SHA1Managed();
                    certDigest = sha1Managed.ComputeHash(encapsulatedX509Certificate.PkiData);
                    index = certDigests.BinarySearch(Convert.ToBase64String(certDigest));
                    if (index >= 0)
                    {
                        certDigests.RemoveAt(index);
                    }
                }
                if (certDigests.Count != 0)
                {
                    throw new CryptographicException("Not all CertificateRefs correspond to CertificateValues");
                }
            }


            return retVal;
        }

        /// <summary>
        /// Check if RevocationValues match RevocationRefs
        /// </summary>
        /// <returns>If the function returns true the check was OK</returns>
        public virtual bool CheckRevocationValuesMatchRevocationRefs()
        {
            SHA1Managed sha1Managed;
            UnsignedSignatureProperties unsignedSignatureProperties;
            ArrayList crlDigests;
            byte[] crlDigest;
            int index;
            bool retVal;

            //TODO: Similar test should be done for XML based (Other) revocation information and OCSP responses, but to keep the library independent of these technologies, this test is left to appliactions using the library
            retVal = true;
            unsignedSignatureProperties = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties;
            if ((unsignedSignatureProperties.CompleteRevocationRefs != null) && (unsignedSignatureProperties.CompleteRevocationRefs.CRLRefs != null) &&
                (unsignedSignatureProperties.RevocationValues != null))
            {
                crlDigests = new ArrayList();
                foreach (CRLRef crlRef in unsignedSignatureProperties.CompleteRevocationRefs.CRLRefs.CRLRefCollection)
                {
                    crlDigests.Add(Convert.ToBase64String(crlRef.CertDigest.DigestValue));
                }
                crlDigests.Sort();
                foreach (CRLValue crlValue in unsignedSignatureProperties.RevocationValues.CRLValues.CRLValueCollection)
                {
                    sha1Managed = new SHA1Managed();
                    crlDigest = sha1Managed.ComputeHash(crlValue.PkiData);
                    index = crlDigests.BinarySearch(Convert.ToBase64String(crlDigest));
                    if (index >= 0)
                    {
                        crlDigests.RemoveAt(index);
                    }
                }
                if (crlDigests.Count != 0)
                {
                    throw new CryptographicException("Not all RevocationRefs correspond to RevocationValues");
                }
            }

            return retVal;
        }
        #endregion

        #endregion

        #region Fix to add a namespace prefix for all XmlDsig nodes

        private void SetPrefix(String prefix, XmlNode node)
        {
            if (node.NamespaceURI == SignedXml.XmlDsigNamespaceUrl)
            {
                node.Prefix = prefix;


            }

            foreach (XmlNode child in node.ChildNodes)
            {
                SetPrefix(prefix, child);
            }

            return;
        }


        private SignatureDescription GetSignatureDescription()
        {
            SignatureDescription description = CryptoConfig.CreateFromName(this.SignedInfo.SignatureMethod) as SignatureDescription;

            if (description == null)
            {
                if (this.SignedInfo.SignatureMethod == "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256")
                {
                    CryptoConfig.AddAlgorithm(typeof(Microsoft.Xades.RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
                }
                else if (this.SignedInfo.SignatureMethod == "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512")
                {
                    CryptoConfig.AddAlgorithm(typeof(Microsoft.Xades.RSAPKCS1SHA512SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512");
                }

                description = CryptoConfig.CreateFromName(this.SignedInfo.SignatureMethod) as SignatureDescription;
            }

            return description;
        }

        public new void ComputeSignature()
        {

            this.BuildDigestedReferences();

            AsymmetricAlgorithm signingKey = this.SigningKey;
            if (signingKey == null)
            {
                throw new CryptographicException("Cryptography_Xml_LoadKeyFailed");
            }
            if (this.SignedInfo.SignatureMethod == null)
            {
                if (!(signingKey is DSA))
                {
                    if (!(signingKey is RSA))
                    {
                        throw new CryptographicException("Cryptography_Xml_CreatedKeyFailed");
                    }
                    if (this.SignedInfo.SignatureMethod == null)
                    {
                        this.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                    }
                }
                else
                {
                    this.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                }
            }

            SignatureDescription description = GetSignatureDescription();
            if (description == null)
            {
                throw new CryptographicException("Cryptography_Xml_SignatureDescriptionNotCreated");
            }

            HashAlgorithm hash = description.CreateDigest();
            if (hash == null)
            {
                throw new CryptographicException("Cryptography_Xml_CreateHashAlgorithmFailed");
            }
            //this.GetC14NDigest(hash);
            byte[] hashValue = this.GetC14NDigest(hash, "ds");

            this.m_signature.SignatureValue = description.CreateFormatter(signingKey).CreateSignature(hash);
        }

        public Reference GetContentReference()
        {
            XadesObject xadesObject = null;

            if (this.cachedXadesObjectDocument != null)
            {
                xadesObject = new XadesObject();
                xadesObject.LoadXml(this.cachedXadesObjectDocument.DocumentElement, null);
            }
            else
            {
                xadesObject = this.XadesObject;
            }

            if (xadesObject.QualifyingProperties.SignedProperties.SignedDataObjectProperties.DataObjectFormatCollection.Count > 0)
            {
                string referenceId = xadesObject.QualifyingProperties.SignedProperties.SignedDataObjectProperties.DataObjectFormatCollection[0].ObjectReferenceAttribute.Substring(1);

                foreach (var reference in SignedInfo.References)
                {
                    if (((Reference)reference).Id == referenceId)
                    {
                        return (Reference)reference;
                    }
                }
            }

            return (Reference)SignedInfo.References[0];
        }

        public void FindContentElement()
        {
            Reference contentRef = GetContentReference();

            if (!string.IsNullOrEmpty(contentRef.Uri) &&
                contentRef.Uri.StartsWith("#"))
            {
                contentElement = GetIdElement(this.signatureDocument, contentRef.Uri.Substring(1));
            }
            else
            {
                contentElement = this.signatureDocument.DocumentElement;
            }
        }

        public XmlElement GetSignatureElement()
        {
            var signatureElement = GetIdElement(this.signatureDocument, this.Signature.Id);

            if (signatureElement != null)
            {
                return signatureElement;
            }

            if (signatureNodeDestination != null)
            {
                return signatureNodeDestination;
            }

            if (contentElement == null)
            {
                return null;
            }

            if (contentElement.ParentNode.NodeType != XmlNodeType.Document)
            {
                return (XmlElement)contentElement.ParentNode;
            }
            else
            {
                return contentElement;
            }
        }


        public List<XmlAttribute> GetAllNamespaces(XmlElement fromElement)
        {
            List<XmlAttribute> namespaces = new List<XmlAttribute>();

            if (fromElement != null &&
                fromElement.ParentNode.NodeType == XmlNodeType.Document)
            {
                foreach (XmlAttribute attr in fromElement.Attributes)
                {
                    if (attr.Name.StartsWith("xmlns") && !namespaces.Exists(f => f.Name == attr.Name))
                    {
                        namespaces.Add(attr);
                    }
                }

                return namespaces;
            }

            XmlNode currentNode = fromElement;

            while (currentNode != null && currentNode.NodeType != XmlNodeType.Document)
            {
                foreach (XmlAttribute attr in currentNode.Attributes)
                {
                    if (attr.Name.StartsWith("xmlns") && !namespaces.Exists(f => f.Name == attr.Name))
                    {
                        namespaces.Add(attr);
                    }
                }

                currentNode = currentNode.ParentNode;
            }

            return namespaces;
        }

        /// <summary>
        /// Copy of System.Security.Cryptography.Xml.SignedXml.BuildDigestedReferences() which will add a "ds" 
        /// namespace prefix to all nodes
        /// </summary>
        private void BuildDigestedReferences()
        {
            ArrayList references = this.SignedInfo.References;

            //this.m_refProcessed = new bool[references.Count];
            Type SignedXml_Type = typeof(SignedXml);
            FieldInfo SignedXml_m_refProcessed = SignedXml_Type.GetField("_refProcessed", BindingFlags.NonPublic | BindingFlags.Instance);
            SignedXml_m_refProcessed.SetValue(this, new bool[references.Count]);
            //

            //this.m_refLevelCache = new int[references.Count];
            FieldInfo SignedXml_m_refLevelCache = SignedXml_Type.GetField("_refLevelCache", BindingFlags.NonPublic | BindingFlags.Instance);
            SignedXml_m_refLevelCache.SetValue(this, new int[references.Count]);
            //

            //ReferenceLevelSortOrder comparer = new ReferenceLevelSortOrder();
            Assembly System_Security_Assembly = Assembly.Load("System.Security");//, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            Type ReferenceLevelSortOrder_Type = System_Security_Assembly.GetType("System.Security.Cryptography.Xml.SignedXml+ReferenceLevelSortOrder");
            ConstructorInfo ReferenceLevelSortOrder_Constructor = ReferenceLevelSortOrder_Type.GetConstructor(new Type[] { });
            Object comparer = ReferenceLevelSortOrder_Constructor.Invoke(null);
            //

            //comparer.References = references;
            PropertyInfo ReferenceLevelSortOrder_References = ReferenceLevelSortOrder_Type.GetProperty("References", BindingFlags.Public | BindingFlags.Instance);
            ReferenceLevelSortOrder_References.SetValue(comparer, references, null);
            //

            ArrayList list2 = new ArrayList();
            foreach (Reference reference in references)
            {
                list2.Add(reference);
            }

            list2.Sort((IComparer)comparer);
            Assembly System_Security_Assembly_Xml = Assembly.Load("System.Security.Cryptography.Xml");//, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            Type CanonicalXmlNodeList_Type = System_Security_Assembly_Xml.GetType("System.Security.Cryptography.Xml.CanonicalXmlNodeList");
            ConstructorInfo CanonicalXmlNodeList_Constructor = CanonicalXmlNodeList_Type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null);

            // refList is a list of elements that might be targets of references
            Object refList = CanonicalXmlNodeList_Constructor.Invoke(null);

            MethodInfo CanonicalXmlNodeList_Add = CanonicalXmlNodeList_Type.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);

            //
            FieldInfo SignedXml_m_containingDocument = SignedXml_Type.GetField("_containingDocument", BindingFlags.NonPublic | BindingFlags.Instance);
            Type Reference_Type = typeof(Reference);
            MethodInfo Reference_UpdateHashValue = Reference_Type.GetMethod("UpdateHashValue", BindingFlags.NonPublic | BindingFlags.Instance);
            //

            object m_containingDocument = SignedXml_m_containingDocument.GetValue(this);

            if (contentElement == null)
            {
                FindContentElement();
            }

            var signatureParentNodeNameSpaces = GetAllNamespaces(GetSignatureElement());

            if (addXadesNamespace)
            {
                var attr = signatureDocument.CreateAttribute("xmlns:xades");
                attr.Value = XadesSignedXml.XadesNamespaceUri;

                signatureParentNodeNameSpaces.Add(attr);
            }

            foreach (Reference reference2 in list2)
            {
                XmlDocument xmlDoc = null;
                bool addSignatureNamespaces = false;

                if (reference2.Uri.StartsWith("#KeyInfoId-"))
                {
                    XmlElement keyInfoXml = this.KeyInfo.GetXml();
                    SetPrefix(XmlDSigPrefix, keyInfoXml);

                    xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(keyInfoXml.OuterXml);

                    addSignatureNamespaces = true;
                }
                else if (reference2.Type == SignedPropertiesType)
                {
                    xmlDoc = (XmlDocument)cachedXadesObjectDocument.Clone();

                    addSignatureNamespaces = true;
                }
                else if (reference2.Type == XmlDsigObjectType)
                {
                    string dataObjectId = reference2.Uri.Substring(1);
                    XmlElement dataObjectXml = null;

                    foreach (DataObject dataObject in this.m_signature.ObjectList)
                    {
                        if (dataObjectId == dataObject.Id)
                        {
                            dataObjectXml = dataObject.GetXml();

                            SetPrefix(XmlDSigPrefix, dataObjectXml);

                            addSignatureNamespaces = true;

                            xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(dataObjectXml.OuterXml);

                            break;
                        }
                    }

                    // If no DataObject found, search on document
                    if (dataObjectXml == null)
                    {
                        dataObjectXml = GetIdElement(this.signatureDocument, dataObjectId);

                        if (dataObjectXml != null)
                        {
                            xmlDoc = new XmlDocument();
                            xmlDoc.PreserveWhitespace = true;
                            xmlDoc.LoadXml(dataObjectXml.OuterXml);
                        }
                        else
                        {
                            throw new Exception("No reference target found");
                        }
                    }
                }
                else if (reference2.Type == XmlDsigManifestType)
                {
                    string manifestId = reference2.Uri.Substring(1);

                    foreach (DataObject dataObject in this.m_signature.ObjectList)
                    {
                        XmlNode idAttribute = dataObject.Data[0].Attributes.GetNamedItem("Id");
                        if (idAttribute != null && 
                            idAttribute.Value == manifestId)
                        {
                            XmlElement dataObjectXml = dataObject.GetXml();

                            SetPrefix(XmlDSigPrefix, dataObjectXml);

                            addSignatureNamespaces = true;

                            xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(dataObjectXml.OuterXml);

                            break;
                        }
                    }
                }
                else
                {
                    xmlDoc = (XmlDocument)m_containingDocument;
                }


                if (addSignatureNamespaces)
                {
                    foreach (XmlAttribute attr in signatureParentNodeNameSpaces)
                    {
                        XmlAttribute newAttr = xmlDoc.CreateAttribute(attr.Name);
                        newAttr.Value = attr.Value;

                        xmlDoc.DocumentElement.Attributes.Append(newAttr);
                    }
                }

                if (xmlDoc != null)
                {
                    CanonicalXmlNodeList_Add.Invoke(refList, new object[] { xmlDoc.DocumentElement });
                }

                Reference_UpdateHashValue.Invoke(reference2, new object[] { xmlDoc, refList });

                if (reference2.Id != null)
                {
                    XmlElement xml = reference2.GetXml();

                    SetPrefix(XmlDSigPrefix, xml);
                }
            }
        }


        private AsymmetricAlgorithm GetPublicKey()
        {
            Type SignedXml_Type = typeof(SignedXml);

            MethodInfo SignedXml_Type_GetPublicKey = SignedXml_Type.GetMethod("GetPublicKey", BindingFlags.NonPublic | BindingFlags.Instance);

            return SignedXml_Type_GetPublicKey.Invoke(this, null) as AsymmetricAlgorithm;
        }


        private bool CheckDigestedReferences()
        {
            ArrayList references = m_signature.SignedInfo.References;

            Assembly System_Security_Assembly = Assembly.Load("System.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            Type CanonicalXmlNodeList_Type = System_Security_Assembly.GetType("System.Security.Cryptography.Xml.CanonicalXmlNodeList");
            ConstructorInfo CanonicalXmlNodeList_Constructor = CanonicalXmlNodeList_Type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null);

            MethodInfo CanonicalXmlNodeList_Add = CanonicalXmlNodeList_Type.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
            Object refList = CanonicalXmlNodeList_Constructor.Invoke(null);

            CanonicalXmlNodeList_Add.Invoke(refList, new object[] { this.signatureDocument });

            Type Reference_Type = typeof(Reference);
            MethodInfo Reference_CalculateHashValue = Reference_Type.GetMethod("CalculateHashValue", BindingFlags.NonPublic | BindingFlags.Instance);

            for (int i = 0; i < references.Count; ++i)
            {
                Reference digestedReference = (Reference)references[i];

                byte[] calculatedHash = (byte[])Reference_CalculateHashValue.Invoke(digestedReference, new object[] { this.signatureDocument, refList });

                if (calculatedHash.Length != digestedReference.DigestValue.Length)
                    return false;

                byte[] rgb1 = calculatedHash;
                byte[] rgb2 = digestedReference.DigestValue;
                for (int j = 0; j < rgb1.Length; ++j)
                {
                    if (rgb1[j] != rgb2[j]) return false;
                }
            }

            return true;
        }


        private bool CheckSignedInfo(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            SignatureDescription signatureDescription = CryptoConfig.CreateFromName(SignatureMethod) as SignatureDescription;
            if (signatureDescription == null)
                throw new CryptographicException("signature description can't be created");

            // Let's see if the key corresponds with the SignatureMethod
            Type ta = Type.GetType(signatureDescription.KeyAlgorithm);
            Type tb = key.GetType();
            if ((ta != tb) && !ta.IsSubclassOf(tb) && !tb.IsSubclassOf(ta))
                // Signature method key mismatch
                return false;

            HashAlgorithm hashAlgorithm = signatureDescription.CreateDigest();
            if (hashAlgorithm == null)
                throw new CryptographicException("signature description can't be created");

            // NECESARIO PARA EL CALCULO CORRECTO
            byte[] hashval = GetC14NDigest(hashAlgorithm, "ds");

            AsymmetricSignatureDeformatter asymmetricSignatureDeformatter = signatureDescription.CreateDeformatter(key);

            return asymmetricSignatureDeformatter.VerifySignature(hashval, m_signature.SignatureValue);
        }


        /// <summary>
        /// We won't call System.Security.Cryptography.Xml.SignedXml.GetC14NDigest(), as we want to use our own.
        /// </summary>
        private byte[] GetC14NDigest(HashAlgorithm hash)
        {
            return null;
        }

        /// <summary>
        /// Copy of System.Security.Cryptography.Xml.SignedXml.GetC14NDigest() which will add a
        /// namespace prefix to all XmlDsig nodes
        /// </summary>
        private byte[] GetC14NDigest(HashAlgorithm hash, string prefix)
        {
            //if (!this.bCacheValid || !this.SignedInfo.CacheValid)
            //{
            Type SignedXml_Type = typeof(SignedXml);
            FieldInfo SignedXml_bCacheValid = SignedXml_Type.GetField("_bCacheValid", BindingFlags.NonPublic | BindingFlags.Instance);
            bool bCacheValid = (bool)SignedXml_bCacheValid.GetValue(this);
            Type SignedInfo_Type = typeof(SignedInfo);
            PropertyInfo SignedInfo_CacheValid = SignedInfo_Type.GetProperty("CacheValid", BindingFlags.NonPublic | BindingFlags.Instance);
            bool CacheValid = (bool)SignedInfo_CacheValid.GetValue(this.SignedInfo, null);

            FieldInfo SignedXml__digestedSignedInfo = SignedXml_Type.GetField("_digestedSignedInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            if (!bCacheValid || !CacheValid)
            {
                //
                //string securityUrl = (this.m_containingDocument == null) ? null : this.m_containingDocument.BaseURI;
                FieldInfo SignedXml_m_containingDocument = SignedXml_Type.GetField("_containingDocument", BindingFlags.NonPublic | BindingFlags.Instance);
                XmlDocument m_containingDocument = (XmlDocument)SignedXml_m_containingDocument.GetValue(this);
                string securityUrl = (m_containingDocument == null) ? null : m_containingDocument.BaseURI;
                //

                //XmlResolver xmlResolver = this.m_bResolverSet ? this.m_xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), securityUrl);
                FieldInfo SignedXml_m_bResolverSet = SignedXml_Type.GetField("_bResolverSet", BindingFlags.NonPublic | BindingFlags.Instance);
                bool m_bResolverSet = (bool)SignedXml_m_bResolverSet.GetValue(this);
                FieldInfo SignedXml_m_xmlResolver = SignedXml_Type.GetField("_xmlResolver", BindingFlags.NonPublic | BindingFlags.Instance);
                XmlResolver m_xmlResolver = (XmlResolver)SignedXml_m_xmlResolver.GetValue(this);
                XmlResolver xmlResolver = m_bResolverSet ? m_xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), securityUrl);
                //

                //XmlDocument document = Utils.PreProcessElementInput(this.SignedInfo.GetXml(), xmlResolver, securityUrl);
                Assembly System_Security_Assembly = Assembly.Load("System.Security.Cryptography.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                Type Utils_Type = System_Security_Assembly.GetType("System.Security.Cryptography.Xml.Utils");
                MethodInfo Utils_PreProcessElementInput = Utils_Type.GetMethod("PreProcessElementInput", BindingFlags.NonPublic | BindingFlags.Static);

                XmlElement xml = this.SignedInfo.GetXml();
                SetPrefix(prefix, xml); // <---

                XmlDocument document = (XmlDocument)Utils_PreProcessElementInput.Invoke(null, new object[] { xml, xmlResolver, securityUrl });

                var docNamespaces = GetAllNamespaces(GetSignatureElement());

                if (addXadesNamespace)
                {
                    var attr = signatureDocument.CreateAttribute("xmlns:xades");
                    attr.Value = XadesSignedXml.XadesNamespaceUri;

                    docNamespaces.Add(attr);
                }


                foreach (XmlAttribute attr in docNamespaces)
                {
                    XmlAttribute newAttr = document.CreateAttribute(attr.Name);
                    newAttr.Value = attr.Value;

                    document.DocumentElement.Attributes.Append(newAttr);
                }

                //CanonicalXmlNodeList namespaces = (this.m_context == null) ? null : Utils.GetPropagatedAttributes(this.m_context);
                FieldInfo SignedXml_m_context = SignedXml_Type.GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo Utils_GetPropagatedAttributes = Utils_Type.GetMethod("GetPropagatedAttributes", BindingFlags.NonPublic | BindingFlags.Static);
                object m_context = SignedXml_m_context.GetValue(this);
                object namespaces = (m_context == null) ? null : Utils_GetPropagatedAttributes.Invoke(null, new object[] { m_context });


                //

                // Utils.AddNamespaces(document.DocumentElement, namespaces);
                Type CanonicalXmlNodeList_Type = System_Security_Assembly.GetType("System.Security.Cryptography.Xml.CanonicalXmlNodeList");
                MethodInfo Utils_AddNamespaces = Utils_Type.GetMethod("AddNamespaces", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(XmlElement), CanonicalXmlNodeList_Type }, null);
                Utils_AddNamespaces.Invoke(null, new object[] { document.DocumentElement, namespaces });
                //

                //Transform canonicalizationMethodObject = this.SignedInfo.CanonicalizationMethodObject;
                System.Security.Cryptography.Xml.Transform canonicalizationMethodObject = this.SignedInfo.CanonicalizationMethodObject;
                //

                canonicalizationMethodObject.Resolver = xmlResolver;

                //canonicalizationMethodObject.BaseURI = securityUrl;
                Type Transform_Type = typeof(System.Security.Cryptography.Xml.Transform);
                PropertyInfo Transform_BaseURI = Transform_Type.GetProperty("BaseURI", BindingFlags.NonPublic | BindingFlags.Instance);
                Transform_BaseURI.SetValue(canonicalizationMethodObject, securityUrl, null);
                //

                canonicalizationMethodObject.LoadInput(document);

                //this._digestedSignedInfo = canonicalizationMethodObject.GetDigestedOutput(hash);
                SignedXml__digestedSignedInfo.SetValue(this, canonicalizationMethodObject.GetDigestedOutput(hash));
                //

                //this.bCacheValid = true;
                SignedXml_bCacheValid.SetValue(this, true);
                //
            }

            //return this._digestedSignedInfo;
            byte[] _digestedSignedInfo = (byte[])SignedXml__digestedSignedInfo.GetValue(this);
            return _digestedSignedInfo;
            //
        }

        #endregion

        #region Private methods

        private XmlElement GetXadesObjectElement(XmlElement signatureElement)
        {
            XmlElement retVal = null;

            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(signatureElement.OwnerDocument.NameTable); //Create an XmlNamespaceManager to resolve namespace
            xmlNamespaceManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
            xmlNamespaceManager.AddNamespace("xades", XadesSignedXml.XadesNamespaceUri);

            XmlNodeList xmlNodeList = signatureElement.SelectNodes("ds:Object/xades:QualifyingProperties", xmlNamespaceManager);
            if (xmlNodeList.Count > 0)
            {
                retVal = (XmlElement)xmlNodeList.Item(0).ParentNode;
            }
            else
            {
                retVal = null;
            }

            return retVal;
        }

        private void SetSignatureStandard(XmlElement signatureElement)
        {
            if (this.GetXadesObjectElement(signatureElement) != null)
            {
                this.signatureStandard = KnownSignatureStandard.Xades;
            }
            else
            {
                this.signatureStandard = KnownSignatureStandard.XmlDsig;
            }
        }

        private System.Security.Cryptography.Xml.DataObject GetXadesDataObject()
        {
            System.Security.Cryptography.Xml.DataObject retVal = null;

            for (int dataObjectCounter = 0; dataObjectCounter < (this.Signature.ObjectList.Count); dataObjectCounter++)
            {
                System.Security.Cryptography.Xml.DataObject dataObject = (System.Security.Cryptography.Xml.DataObject)this.Signature.ObjectList[dataObjectCounter];
                XmlElement dataObjectXmlElement = dataObject.GetXml();
                XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(dataObjectXmlElement.OwnerDocument.NameTable);
                xmlNamespaceManager.AddNamespace("xades", XadesSignedXml.XadesNamespaceUri);
                XmlNodeList xmlNodeList = dataObjectXmlElement.SelectNodes("xades:QualifyingProperties", xmlNamespaceManager);
                if (xmlNodeList.Count != 0)
                {
                    retVal = dataObject;

                    break;
                }
            }

            return retVal;
        }

        private void SchemaValidationHandler(object sender, ValidationEventArgs validationEventArgs)
        {
            this.validationErrorOccurred = true;
            this.validationErrorDescription += "Validation error:\n";
            this.validationErrorDescription += "\tSeverity: " + validationEventArgs.Severity.ToString() + "\n";
            this.validationErrorDescription += "\tMessage: " + validationEventArgs.Message + "\n";
        }

        private void XmlValidationHandler(object sender, ValidationEventArgs validationEventArgs)
        {
            if (validationEventArgs.Severity != XmlSeverityType.Warning)
            {
                this.validationErrorOccurred = true;
                this.validationErrorDescription += "Validation error:\n";
                this.validationErrorDescription += "\tSeverity: " + validationEventArgs.Severity.ToString() + "\n";
                this.validationErrorDescription += "\tMessage: " + validationEventArgs.Message + "\n";
            }
        }

        private bool CheckHashDataInfosForTimeStamp(TimeStamp timeStamp)
        {
            bool retVal = true;

            for (int referenceCounter = 0; retVal == true && (referenceCounter < this.SignedInfo.References.Count); referenceCounter++)
            {
                string referenceId = ((Reference)this.SignedInfo.References[referenceCounter]).Id;
                string referenceUri = ((Reference)this.SignedInfo.References[referenceCounter]).Uri;
                if (referenceUri != ("#" + this.XadesObject.QualifyingProperties.SignedProperties.Id))
                {
                    bool hashDataInfoFound = false;
                    for (int hashDataInfoCounter = 0; hashDataInfoFound == false && (hashDataInfoCounter < timeStamp.HashDataInfoCollection.Count); hashDataInfoCounter++)
                    {
                        HashDataInfo hashDataInfo = timeStamp.HashDataInfoCollection[hashDataInfoCounter];
                        hashDataInfoFound = (("#" + referenceId) == hashDataInfo.UriAttribute);
                    }
                    retVal = hashDataInfoFound;
                }
            }

            return retVal;
        }

        private bool CheckHashDataInfosExist(TimeStamp timeStamp)
        {
            bool retVal = true;

            for (int hashDataInfoCounter = 0; retVal == true && (hashDataInfoCounter < timeStamp.HashDataInfoCollection.Count); hashDataInfoCounter++)
            {
                HashDataInfo hashDataInfo = timeStamp.HashDataInfoCollection[hashDataInfoCounter];
                bool referenceFound = false;
                string referenceId;

                for (int referenceCounter = 0; referenceFound == false && (referenceCounter < this.SignedInfo.References.Count); referenceCounter++)
                {
                    referenceId = ((Reference)this.SignedInfo.References[referenceCounter]).Id;
                    if (("#" + referenceId) == hashDataInfo.UriAttribute)
                    {
                        referenceFound = true;
                    }
                }
                retVal = referenceFound;
            }

            return retVal;
        }


        private bool CheckObjectReference(ObjectReference objectReference)
        {
            bool retVal = false;

            for (int referenceCounter = 0; retVal == false && (referenceCounter < this.SignedInfo.References.Count); referenceCounter++)
            {
                string referenceId = ((Reference)this.SignedInfo.References[referenceCounter]).Id;
                if (("#" + referenceId) == objectReference.ObjectReferenceUri)
                {
                    retVal = true;
                }
            }

            return retVal;
        }

        private bool CheckHashDataInfoPointsToSignatureValue(TimeStamp timeStamp)
        {
            bool retVal = true;
            foreach (HashDataInfo hashDataInfo in timeStamp.HashDataInfoCollection)
            {
                retVal &= (hashDataInfo.UriAttribute == ("#" + this.signatureValueId));
            }

            return retVal;
        }

        private bool CheckHashDataInfosOfSigAndRefsTimeStamp(TimeStamp timeStamp)
        {
            UnsignedSignatureProperties unsignedSignatureProperties;
            bool signatureValueHashDataInfoFound = false;
            bool allSignatureTimeStampHashDataInfosFound = false;
            bool completeCertificateRefsHashDataInfoFound = false;
            bool completeRevocationRefsHashDataInfoFound = false;

            ArrayList signatureTimeStampIds = new ArrayList();

            bool retVal = true;

            unsignedSignatureProperties = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties;

            foreach (TimeStamp signatureTimeStamp in unsignedSignatureProperties.SignatureTimeStampCollection)
            {
                signatureTimeStampIds.Add("#" + signatureTimeStamp.EncapsulatedTimeStamp.Id);
            }
            signatureTimeStampIds.Sort();
            foreach (HashDataInfo hashDataInfo in timeStamp.HashDataInfoCollection)
            {
                if (hashDataInfo.UriAttribute == "#" + this.signatureValueId)
                {
                    signatureValueHashDataInfoFound = true;
                }
                int signatureTimeStampIdIndex = signatureTimeStampIds.BinarySearch(hashDataInfo.UriAttribute);
                if (signatureTimeStampIdIndex >= 0)
                {
                    signatureTimeStampIds.RemoveAt(signatureTimeStampIdIndex);
                }
                if (hashDataInfo.UriAttribute == "#" + unsignedSignatureProperties.CompleteCertificateRefs.Id)
                {
                    completeCertificateRefsHashDataInfoFound = true;
                }
                if (hashDataInfo.UriAttribute == "#" + unsignedSignatureProperties.CompleteRevocationRefs.Id)
                {
                    completeRevocationRefsHashDataInfoFound = true;
                }
            }
            if (signatureTimeStampIds.Count == 0)
            {
                allSignatureTimeStampHashDataInfosFound = true;
            }
            retVal = signatureValueHashDataInfoFound && allSignatureTimeStampHashDataInfosFound && completeCertificateRefsHashDataInfoFound && completeRevocationRefsHashDataInfoFound;

            return retVal;
        }

        private bool CheckHashDataInfosOfRefsOnlyTimeStamp(TimeStamp timeStamp)
        {
            UnsignedSignatureProperties unsignedSignatureProperties;
            bool completeCertificateRefsHashDataInfoFound;
            bool completeRevocationRefsHashDataInfoFound;
            bool retVal;

            completeCertificateRefsHashDataInfoFound = false;
            completeRevocationRefsHashDataInfoFound = false;
            retVal = true;

            unsignedSignatureProperties = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties;
            foreach (HashDataInfo hashDataInfo in timeStamp.HashDataInfoCollection)
            {
                if (hashDataInfo.UriAttribute == "#" + unsignedSignatureProperties.CompleteCertificateRefs.Id)
                {
                    completeCertificateRefsHashDataInfoFound = true;
                }
                if (hashDataInfo.UriAttribute == "#" + unsignedSignatureProperties.CompleteRevocationRefs.Id)
                {
                    completeRevocationRefsHashDataInfoFound = true;
                }
            }
            retVal = completeCertificateRefsHashDataInfoFound && completeRevocationRefsHashDataInfoFound;

            return retVal;
        }

        private bool CheckHashDataInfosOfArchiveTimeStamp(TimeStamp timeStamp)
        {
            UnsignedSignatureProperties unsignedSignatureProperties;
            SignedProperties signedProperties;

            bool allReferenceHashDataInfosFound = false;
            bool signedInfoHashDataInfoFound = false;
            bool signedPropertiesHashDataInfoFound = false;
            bool signatureValueHashDataInfoFound = false;
            bool allSignatureTimeStampHashDataInfosFound = false;
            bool completeCertificateRefsHashDataInfoFound = false;
            bool completeRevocationRefsHashDataInfoFound = false;
            bool certificatesValuesHashDataInfoFound = false;
            bool revocationValuesHashDataInfoFound = false;
            bool allSigAndRefsTimeStampHashDataInfosFound = false;
            bool allRefsOnlyTimeStampHashDataInfosFound = false;
            bool allArchiveTimeStampHashDataInfosFound = false;
            bool allOlderArchiveTimeStampsFound = false;

            ArrayList referenceIds = new ArrayList();
            ArrayList signatureTimeStampIds = new ArrayList();
            ArrayList sigAndRefsTimeStampIds = new ArrayList();
            ArrayList refsOnlyTimeStampIds = new ArrayList();
            ArrayList archiveTimeStampIds = new ArrayList();

            bool retVal = true;

            unsignedSignatureProperties = this.XadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties;
            signedProperties = this.XadesObject.QualifyingProperties.SignedProperties;

            foreach (Reference reference in this.Signature.SignedInfo.References)
            {
                if (reference.Uri != "#" + signedProperties.Id)
                {
                    referenceIds.Add(reference.Uri);
                }
            }
            referenceIds.Sort();
            foreach (TimeStamp signatureTimeStamp in unsignedSignatureProperties.SignatureTimeStampCollection)
            {
                signatureTimeStampIds.Add("#" + signatureTimeStamp.EncapsulatedTimeStamp.Id);
            }
            signatureTimeStampIds.Sort();
            foreach (TimeStamp sigAndRefsTimeStamp in unsignedSignatureProperties.SigAndRefsTimeStampCollection)
            {
                sigAndRefsTimeStampIds.Add("#" + sigAndRefsTimeStamp.EncapsulatedTimeStamp.Id);
            }
            sigAndRefsTimeStampIds.Sort();
            foreach (TimeStamp refsOnlyTimeStamp in unsignedSignatureProperties.RefsOnlyTimeStampCollection)
            {
                refsOnlyTimeStampIds.Add("#" + refsOnlyTimeStamp.EncapsulatedTimeStamp.Id);
            }
            refsOnlyTimeStampIds.Sort();
            allOlderArchiveTimeStampsFound = false;
            for (int archiveTimeStampCounter = 0; !allOlderArchiveTimeStampsFound && (archiveTimeStampCounter < unsignedSignatureProperties.ArchiveTimeStampCollection.Count); archiveTimeStampCounter++)
            {
                TimeStamp archiveTimeStamp = unsignedSignatureProperties.ArchiveTimeStampCollection[archiveTimeStampCounter];
                if (archiveTimeStamp.EncapsulatedTimeStamp.Id == timeStamp.EncapsulatedTimeStamp.Id)
                {
                    allOlderArchiveTimeStampsFound = true;
                }
                else
                {
                    archiveTimeStampIds.Add("#" + archiveTimeStamp.EncapsulatedTimeStamp.Id);
                }
            }

            archiveTimeStampIds.Sort();
            foreach (HashDataInfo hashDataInfo in timeStamp.HashDataInfoCollection)
            {
                int index = referenceIds.BinarySearch(hashDataInfo.UriAttribute);
                if (index >= 0)
                {
                    referenceIds.RemoveAt(index);
                }
                if (hashDataInfo.UriAttribute == "#" + this.signedInfoIdBuffer)
                {
                    signedInfoHashDataInfoFound = true;
                }
                if (hashDataInfo.UriAttribute == "#" + signedProperties.Id)
                {
                    signedPropertiesHashDataInfoFound = true;
                }
                if (hashDataInfo.UriAttribute == "#" + this.signatureValueId)
                {
                    signatureValueHashDataInfoFound = true;
                }
                index = signatureTimeStampIds.BinarySearch(hashDataInfo.UriAttribute);
                if (index >= 0)
                {
                    signatureTimeStampIds.RemoveAt(index);
                }
                if (hashDataInfo.UriAttribute == "#" + unsignedSignatureProperties.CompleteCertificateRefs.Id)
                {
                    completeCertificateRefsHashDataInfoFound = true;
                }
                if (hashDataInfo.UriAttribute == "#" + unsignedSignatureProperties.CompleteRevocationRefs.Id)
                {
                    completeRevocationRefsHashDataInfoFound = true;
                }
                if (hashDataInfo.UriAttribute == "#" + unsignedSignatureProperties.CertificateValues.Id)
                {
                    certificatesValuesHashDataInfoFound = true;
                }
                if (hashDataInfo.UriAttribute == "#" + unsignedSignatureProperties.RevocationValues.Id)
                {
                    revocationValuesHashDataInfoFound = true;
                }
                index = sigAndRefsTimeStampIds.BinarySearch(hashDataInfo.UriAttribute);
                if (index >= 0)
                {
                    sigAndRefsTimeStampIds.RemoveAt(index);
                }
                index = refsOnlyTimeStampIds.BinarySearch(hashDataInfo.UriAttribute);
                if (index >= 0)
                {
                    refsOnlyTimeStampIds.RemoveAt(index);
                }
                index = archiveTimeStampIds.BinarySearch(hashDataInfo.UriAttribute);
                if (index >= 0)
                {
                    archiveTimeStampIds.RemoveAt(index);
                }
            }
            if (referenceIds.Count == 0)
            {
                allReferenceHashDataInfosFound = true;
            }
            if (signatureTimeStampIds.Count == 0)
            {
                allSignatureTimeStampHashDataInfosFound = true;
            }
            if (sigAndRefsTimeStampIds.Count == 0)
            {
                allSigAndRefsTimeStampHashDataInfosFound = true;
            }
            if (refsOnlyTimeStampIds.Count == 0)
            {
                allRefsOnlyTimeStampHashDataInfosFound = true;
            }
            if (archiveTimeStampIds.Count == 0)
            {
                allArchiveTimeStampHashDataInfosFound = true;
            }

            retVal = allReferenceHashDataInfosFound && signedInfoHashDataInfoFound && signedPropertiesHashDataInfoFound &&
                signatureValueHashDataInfoFound && allSignatureTimeStampHashDataInfosFound && completeCertificateRefsHashDataInfoFound &&
                completeRevocationRefsHashDataInfoFound && certificatesValuesHashDataInfoFound && revocationValuesHashDataInfoFound &&
                allSigAndRefsTimeStampHashDataInfosFound && allRefsOnlyTimeStampHashDataInfosFound && allArchiveTimeStampHashDataInfosFound;

            return retVal;
        }
        #endregion
    }
}
#pragma warning restore 1591
#pragma warning restore 1574
