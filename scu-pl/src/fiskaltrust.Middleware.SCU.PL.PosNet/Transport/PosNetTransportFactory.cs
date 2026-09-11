using System;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// Picks the transport the configured DeviceUrl calls for: TCP for a network address, serial for a
/// USB or COM port. Everything above the transport is the same for both.
/// </summary>
public static class PosNetTransportFactory
{
    /// <summary>
    /// The DeviceUrl is parsed once, here, and the recognized address is handed to the transport —
    /// so neither transport re-derives from the string what selecting it already established.
    /// </summary>
    public static IPosNetTransport Create(PosNetConfiguration configuration) => configuration.ParseDeviceAddress() switch
    {
        PosNetDeviceAddress.Tcp tcp => new TcpPosNetTransport(tcp, configuration),
        PosNetDeviceAddress.Serial serial => new SerialPosNetTransport(serial, configuration),
        var other => throw new NotSupportedException($"No transport for the device address '{other}'."),
    };
}
