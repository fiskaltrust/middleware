using System;
using System.Runtime.Serialization;

[Serializable]
internal class CustomRTServerCommunicationException : Exception
{
    public int CustomRTServerErrorCode { get; }

    public CustomRTServerCommunicationException(string message, int errorCode) : base(message)
    {
        CustomRTServerErrorCode = errorCode;
    }

    protected CustomRTServerCommunicationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}