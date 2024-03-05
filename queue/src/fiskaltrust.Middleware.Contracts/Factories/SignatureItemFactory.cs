using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Contracts.Factories
{
    public abstract class SignatureItemFactory
    {
        public abstract long CountryBaseState { get; }
        public SignatureItemFactory() { 
        }

        public SignaturItem CreateInitialOperationSignature(string data)
        {
            return new SignaturItem()
            {
                ftSignatureType = CountryBaseState & 0x3,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Initial-operation receipt",
                Data = data
            };
        }

        public SignaturItem CreateOutOfOperationSignature(string data)
        {
            return new SignaturItem()
            {
                ftSignatureType = CountryBaseState & 0x4,
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                Caption = $"Out-of-operation receipt",
                Data = data
            };
        }
    }
}
