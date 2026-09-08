using System;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// Picks the transport the configured DeviceUrl calls for: TCP for a network address, serial for a
/// USB or COM port. Everything above the transport is the same for both.
/// </summary>
public static class PosNetTransportFactory
{
    public static IPosNetTransport Create(PosNetConfiguration configuration) => configuration.ParseDeviceAddress() switch
    {
        PosNetDeviceAddress.Tcp => new TcpPosNetTransport(configuration),
        PosNetDeviceAddress.Serial => new SerialPosNetTransport(configuration),
        var other => throw new NotSupportedException($"No transport for the device address '{other}'."),
    };
}
