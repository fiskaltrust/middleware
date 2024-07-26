using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Interface.Tagging.ErrorHandling
{
    public class JournalTypeVersionException : Exception
    {
        public JournalTypeVersionException(string message) : base(message) { }
    }
    public class JournalTypeCountryException : Exception
    {
        public JournalTypeCountryException(string message) : base(message) { }
    }
    public class ReceiptCaseVersionException : Exception
    {
        public ReceiptCaseVersionException(string message) : base(message) { }
    }
    public class ReceiptCaseCountryException : Exception
    {
        public ReceiptCaseCountryException(string message) : base(message) { }
    }
    public class ChargeItemVersionException : Exception
    {
        public ChargeItemVersionException(string message) : base(message) { }
    }
    public class ChargeItemCountryException : Exception
    {
        public ChargeItemCountryException(string message) : base(message) { }
    }
    public class PayItemVersionException : Exception
    {
        public PayItemVersionException(string message) : base(message) { }
    }
    public class PayItemCountryException : Exception
    {
        public PayItemCountryException(string message) : base(message) { }
    }
    public class SignatureTypeVersionException : Exception
    {
        public SignatureTypeVersionException(string message) : base(message) { }
    }
    public class SignatureTypeCountryException : Exception
    {
        public SignatureTypeCountryException(string message) : base(message) { }
    }
    public class SignatureFormatVersionException : Exception
    {
        public SignatureFormatVersionException(string message) : base(message) { }
    }
    public class SignatureFormatCountryException : Exception
    {
        public SignatureFormatCountryException(string message) : base(message) { }
    }
    public class StateVersionException : Exception
    {
        public StateVersionException(string message) : base(message) { }
    }
    public class StateCountryException : Exception
    {
        public StateCountryException(string message) : base(message) { }
    }
}
