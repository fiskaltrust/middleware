using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models
{
#nullable disable

    [Serializable]
    [XmlType("SignatureType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("Signature", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class SignatureType
    {
        [Required]
        [XmlElement("SignedInfo")]
        public SignedInfoType SignedInfo { get; set; }

        [Required]
        [XmlElement("SignatureValue")]
        public SignatureValueType SignatureValue { get; set; }

        [XmlElement("KeyInfo")]
        public KeyInfoType KeyInfo { get; set; }

        [XmlElement("Object")]
        public List<ObjectType> Object { get; set; }

        [XmlAttribute("Id")]
        public string Id { get; set; }
    }

    [Serializable]
    [XmlType("SignedInfoType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("SignedInfo", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class SignedInfoType
    {
        [Required]
        [XmlElement("CanonicalizationMethod")]
        public CanonicalizationMethodType CanonicalizationMethod { get; set; }

        [Required]
        [XmlElement("SignatureMethod")]
        public SignatureMethodType SignatureMethod { get; set; }

        [Required]
        [XmlElement("Reference")]
        public List<ReferenceType> Reference { get; set; }

        [XmlAttribute("Id")]
        public string Id { get; set; }
    }

    [Serializable]
    [XmlType("CanonicalizationMethodType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("CanonicalizationMethod", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class CanonicalizationMethodType
    {
        [XmlAnyElement]
        public List<System.Xml.XmlElement> Any { get; set; }

        [Required]
        [XmlAttribute("Algorithm")]
        public string Algorithm { get; set; }

        [XmlText]
        public string[] Text { get; set; }
    }

    [Serializable]
    [XmlType("SignatureMethodType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("SignatureMethod", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class SignatureMethodType
    {
        [XmlElement("HMACOutputLength")]
        public string HMACOutputLength { get; set; }

        [XmlAnyElement]
        public List<System.Xml.XmlElement> Any { get; set; }

        [Required]
        [XmlAttribute("Algorithm")]
        public string Algorithm { get; set; }

        [XmlText]
        public string[] Text { get; set; }
    }

    [Serializable]
    [XmlType("ReferenceType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("Reference", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class ReferenceType
    {
        [XmlArray("Transforms")]
        [XmlArrayItem("Transform", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public List<TransformType> Transforms { get; set; }

        [Required]
        [XmlElement("DigestMethod")]
        public DigestMethodType DigestMethod { get; set; }

        [Required]
        [XmlElement("DigestValue", DataType = "base64Binary")]
        public byte[] DigestValue { get; set; }

        [XmlAttribute("Id")]
        public string Id { get; set; }

        [XmlAttribute("URI")]
        public string URI { get; set; }

        [XmlAttribute("Type")]
        public string Type { get; set; }
    }

    [Serializable]
    [XmlType("TransformsType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("Transforms", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class TransformsType
    {
        [Required]
        [XmlElement("Transform")]
        public List<TransformType> Transform { get; set; }
    }


    [Serializable]
    [XmlType("TransformType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("Transform", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class TransformType
    {
        [XmlAnyElement]
        public List<System.Xml.XmlElement> Any { get; set; }

        [XmlElement("XPath")]
        public List<string> XPath { get; set; }

        [Required]
        [XmlAttribute("Algorithm")]
        public string Algorithm { get; set; }

        [XmlText]
        public string[] Text { get; set; }
    }

    [Serializable]
    [XmlType("DigestMethodType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("DigestMethod", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class DigestMethodType
    {
        [XmlAnyElement]
        public List<System.Xml.XmlElement> Any { get; set; }

        [Required]
        [XmlAttribute("Algorithm")]
        public string Algorithm { get; set; }

        [XmlText]
        public string[] Text { get; set; }
    }

    [Serializable]
    [XmlType("SignatureValueType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("SignatureValue", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class SignatureValueType
    {
        [XmlText(DataType = "base64Binary")]
        public byte[] Value { get; set; }

        [XmlAttribute("Id")]
        public string Id { get; set; }
    }

    [Serializable]
    [XmlType("KeyInfoType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("KeyInfo", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class KeyInfoType
    {
        [XmlElement("KeyName")]
        public List<string> KeyName { get; set; }

        [XmlElement("KeyValue")]
        public List<KeyValueType> KeyValue { get; set; }

        [XmlElement("RetrievalMethod")]
        public List<RetrievalMethodType> RetrievalMethod { get; set; }

        [XmlElement("X509Data")]
        public List<X509DataType> X509Data { get; set; }

        [XmlElement("PGPData")]
        public List<PGPDataType> PGPData { get; set; }

        [XmlElement("SPKIData")]
        public List<SPKIDataType> SPKIData { get; set; }

        [XmlElement("MgmtData")]
        public List<string> MgmtData { get; set; }


        [XmlAnyElement]
        public List<System.Xml.XmlElement> Any { get; set; }

        [XmlAttribute("Id")]
        public string Id { get; set; }

        [XmlText]
        public string[] Text { get; set; }
    }


    [Serializable]
    [XmlType("KeyValueType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("KeyValue", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class KeyValueType
    {
        [XmlElement("DSAKeyValue")]
        public DSAKeyValueType DSAKeyValue { get; set; }

        [XmlElement("RSAKeyValue")]
        public RSAKeyValueType RSAKeyValue { get; set; }

        [XmlAnyElement]
        public System.Xml.XmlElement Any { get; set; }

        [XmlText]
        public string[] Text { get; set; }
    }

    [Serializable]
    [XmlType("DSAKeyValueType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("DSAKeyValue", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class DSAKeyValueType
    {
        [XmlElement("P", DataType = "base64Binary")]
        public byte[] P { get; set; }

        [XmlElement("Q", DataType = "base64Binary")]
        public byte[] Q { get; set; }

        [XmlElement("G", DataType = "base64Binary")]
        public byte[] G { get; set; }

        [Required]
        [XmlElement("Y", DataType = "base64Binary")]
        public byte[] Y { get; set; }

        [XmlElement("J", DataType = "base64Binary")]
        public byte[] J { get; set; }

        [XmlElement("Seed", DataType = "base64Binary")]
        public byte[] Seed { get; set; }

        [XmlElement("PgenCounter", DataType = "base64Binary")]
        public byte[] PgenCounter { get; set; }
    }

    [Serializable]
    [XmlType("RSAKeyValueType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("RSAKeyValue", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class RSAKeyValueType
    {
        [Required]
        [XmlElement("Modulus", DataType = "base64Binary")]
        public byte[] Modulus { get; set; }

        [Required]
        [XmlElement("Exponent", DataType = "base64Binary")]
        public byte[] Exponent { get; set; }
    }

    [Serializable]
    [XmlType("RetrievalMethodType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("RetrievalMethod", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class RetrievalMethodType
    {
        [XmlArray("Transforms")]
        [XmlArrayItem("Transform", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public List<TransformType> Transforms { get; set; }

        [XmlAttribute("URI")]
        public string URI { get; set; }

        [XmlAttribute("Type")]
        public string Type { get; set; }
    }

    [Serializable]
    [XmlType("X509DataType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("X509Data", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class X509DataType
    {
        [XmlElement("X509IssuerSerial")]
        public List<X509IssuerSerialType> X509IssuerSerial { get; set; }

        [XmlElement("X509SKI", DataType = "base64Binary")]
        public List<byte[]> X509SKI { get; set; }

        [XmlElement("X509SubjectName")]
        public List<string> X509SubjectName { get; set; }

        [XmlElement("X509Certificate", DataType = "base64Binary")]
        public List<byte[]> X509Certificate { get; set; }

        [XmlElement("X509CRL", DataType = "base64Binary")]
        public List<byte[]> X509CRL { get; set; }

        [XmlAnyElement]
        public List<System.Xml.XmlElement> Any { get; set; }
    }

    [Serializable]
    [XmlType("X509IssuerSerialType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class X509IssuerSerialType
    {
        [Required]
        [XmlElement("X509IssuerName")]
        public string X509IssuerName { get; set; }

        [Required]
        [XmlElement("X509SerialNumber")]
        public string X509SerialNumber { get; set; }
    }

    [Serializable]
    [XmlType("PGPDataType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("PGPData", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class PGPDataType
    {
        [XmlElement("PGPKeyID", DataType = "base64Binary")]
        public byte[] PGPKeyID { get; set; }

        [XmlElement("PGPKeyPacket", DataType = "base64Binary")]
        public byte[] PGPKeyPacket { get; set; }

        [XmlAnyElement]
        public List<System.Xml.XmlElement> Any { get; set; }
    }

    [Serializable]
    [XmlType("SPKIDataType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("SPKIData", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class SPKIDataType
    {
        [Required]
        [XmlElement("SPKISexp", DataType = "base64Binary")]
        public List<byte[]> SPKISexp { get; set; }

        [XmlAnyElement]
        public List<System.Xml.XmlElement> Any { get; set; }
    }

    [Serializable]
    [XmlType("ObjectType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("Object", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class ObjectType
    {
        [XmlAnyElement]
        public List<System.Xml.XmlElement> Any { get; set; }

        [XmlAttribute("Id")]
        public string Id { get; set; }

        [XmlAttribute("MimeType")]
        public string MimeType { get; set; }

        [XmlAttribute("Encoding")]
        public string Encoding { get; set; }

        [XmlText]
        public string[] Text { get; set; }
    }

    [Serializable]
    [XmlType("ManifestType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("Manifest", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class ManifestType
    {
        [Required]
        [XmlElement("Reference")]
        public List<ReferenceType> Reference { get; set; }

        [XmlAttribute("Id")]
        public string Id { get; set; }
    }

    [Serializable]
    [XmlType("SignaturePropertiesType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("SignatureProperties", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class SignaturePropertiesType
    {
        [Required]
        [XmlElement("SignatureProperty")]
        public List<SignaturePropertyType> SignatureProperty { get; set; }

        [XmlAttribute("Id")]
        public string Id { get; set; }
    }

    [Serializable]
    [XmlType("SignaturePropertyType", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot("SignatureProperty", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class SignaturePropertyType
    {
        [XmlAnyElement]
        public List<System.Xml.XmlElement> Any { get; set; }

        [Required]
        [XmlAttribute("Target")]
        public string Target { get; set; }

        [XmlAttribute("Id")]
        public string Id { get; set; }

        [XmlText]
        public string[] Text { get; set; }
    }
}