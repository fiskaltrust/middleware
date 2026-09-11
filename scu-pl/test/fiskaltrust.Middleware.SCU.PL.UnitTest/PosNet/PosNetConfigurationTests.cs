using System.IO.Ports;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

public class PosNetConfigurationTests
{
    [Theory]
    [InlineData("tcp://192.168.1.50:6666", "192.168.1.50", 6666)]
    [InlineData("192.168.1.50:6666", "192.168.1.50", 6666)]
    [InlineData("tcp://printer.local:6666", "printer.local", 6666)]
    [InlineData("10.tcp.eu.ngrok.io:26321", "10.tcp.eu.ngrok.io", 26321)]
    public void ParseDeviceAddress_ReadsANetworkAddress(string deviceUrl, string host, int port)
    {
        PosNetDeviceAddress.Parse(deviceUrl).Should().Be(new PosNetDeviceAddress.Tcp(host, port));
    }

    [Theory]
    [InlineData("serial://COM9", "COM9")]
    [InlineData("usb://COM9", "COM9")]
    [InlineData("COM9", "COM9")]
    [InlineData("com12", "com12")]
    [InlineData(" Serial://COM3 ", "COM3")]
    [InlineData("/dev/ttyACM0", "/dev/ttyACM0")]
    [InlineData("serial:///dev/ttyACM0", "/dev/ttyACM0")]
    public void ParseDeviceAddress_ReadsASerialPort(string deviceUrl, string portName)
    {
        PosNetDeviceAddress.Parse(deviceUrl).Should().Be(new PosNetDeviceAddress.Serial(portName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("serial://")]
    [InlineData("printer")]
    [InlineData("tcp://host")]
    [InlineData("host:notaport")]
    public void ParseDeviceAddress_RejectsWhatIsNeitherAddress(string deviceUrl)
    {
        var act = () => PosNetDeviceAddress.Parse(deviceUrl);

        act.Should().Throw<PLValidationException>().WithMessage("*DeviceUrl*");
    }

    [Fact]
    public void ParseDeviceEndpoint_OnASerialAddress_FailsWithAClearError()
    {
        var configuration = new PosNetConfiguration { DeviceUrl = "serial://COM9" };

        var act = () => configuration.ParseDeviceEndpoint();

        act.Should().Throw<PLValidationException>().WithMessage("*serial port*");
    }

    [Fact]
    public void FromConfiguration_ReadsSerialSettings_AlsoWhenTheyArriveAsStrings()
    {
        // Portal parameters are strings; the launcher and the tests pass numbers.
        var configuration = PosNetConfiguration.FromConfiguration(new Dictionary<string, object>
        {
            ["DeviceUrl"] = "usb://COM9",
            ["SerialBaudRate"] = "9600",
            ["SerialStopBits"] = 2,
            ["SerialParity"] = "even",
            ["SerialHandshake"] = "XON/XOFF",
            ["ReceiveTimeoutMs"] = "30000",
        });

        configuration.ReceiveTimeoutMs.Should().Be(30_000);
        SerialSettingsOf(configuration).Should().Be(
            new PosNetSerialSettings("COM9", 9600, Parity.Even, StopBits.Two, Handshake.XOnXOff));
    }

    [Fact]
    public void SerialSettings_DefaultToThePrintersOwnDefaults_WithoutAHandshake()
    {
        var settings = SerialSettingsOf(new PosNetConfiguration { DeviceUrl = "COM9" });

        settings.Should().Be(new PosNetSerialSettings("COM9", 115_200, Parity.None, StopBits.One, Handshake.None));
        PosNetSerialSettings.DataBits.Should().Be(8);
    }

    [Theory]
    [InlineData("RTS/CTS", Handshake.RequestToSend)]
    [InlineData("RequestToSend", Handshake.RequestToSend)]
    [InlineData("XOnXOff", Handshake.XOnXOff)]
    [InlineData("xon/xoff", Handshake.XOnXOff)]
    [InlineData("Brak", Handshake.None)]
    [InlineData("none", Handshake.None)]
    public void SerialSettings_AcceptThePrinterMenusSpellingOfTheHandshake(string handshake, Handshake expected)
    {
        var settings = SerialSettingsOf(new PosNetConfiguration { DeviceUrl = "COM9", SerialHandshake = handshake });

        settings.Handshake.Should().Be(expected);
    }

    [Theory]
    [InlineData("SerialBaudRate", "0")]
    [InlineData("SerialStopBits", "3")]
    [InlineData("SerialParity", "Mark")]
    [InlineData("SerialHandshake", "DTR/DSR")]
    public void FromConfiguration_WithAnUnsupportedSerialSetting_FailsNamingTheSetting(string key, string value)
    {
        var act = () => PosNetConfiguration.FromConfiguration(new Dictionary<string, object>
        {
            ["DeviceUrl"] = "serial://COM9",
            [key] = value,
        });

        act.Should().Throw<PLValidationException>().WithMessage($"*{key}*");
    }

    [Fact]
    public void FromConfiguration_WithATcpAddress_IgnoresTheSerialSettings()
    {
        var act = () => PosNetConfiguration.FromConfiguration(new Dictionary<string, object>
        {
            ["DeviceUrl"] = "tcp://192.168.1.50:6666",
            ["SerialParity"] = "Mark",
        });

        act.Should().NotThrow();
    }

    /// <summary>
    /// A cleared parameter is the device default, as it is for the handshake — refusing it would keep
    /// the SCU from starting over a setting the operator never set.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void SerialSettings_ReadABlankParity_AsTheDeviceDefault(string parity)
    {
        var settings = SerialSettingsOf(new PosNetConfiguration { DeviceUrl = "COM9", SerialParity = parity });

        settings.Parity.Should().Be(Parity.None);
    }

    /// <summary>
    /// A numeric handshake would pass Enum.TryParse as an undefined value and only be refused by the
    /// SerialPort constructor at the first receipt — reported as an unreachable printer.
    /// </summary>
    [Theory]
    [InlineData("7")]
    [InlineData("-1")]
    [InlineData("RequestToSendXOnXOff")]
    public void SerialSettings_RejectAHandshakeThePrinterDoesNotOffer_WhenConfigured(string handshake)
    {
        var act = () => SerialSettingsOf(new PosNetConfiguration { DeviceUrl = "COM9", SerialHandshake = handshake });

        act.Should().Throw<PLValidationException>().WithMessage("*SerialHandshake*");
    }

    /// <summary>
    /// 0 and the .NET "infinite" -1 do not mean the same thing to a socket, a SerialPort and a
    /// deadline computed from them; a command whose answer is never waited for would be reported as
    /// ambiguous on a device that answered.
    /// </summary>
    [Theory]
    [InlineData("ConnectTimeoutMs")]
    [InlineData("SendTimeoutMs")]
    [InlineData("ReceiveTimeoutMs")]
    public void FromConfiguration_RejectsATimeoutTheScuCannotWaitFor(string setting)
    {
        var act = () => PosNetConfiguration.FromConfiguration(new Dictionary<string, object>
        {
            ["DeviceUrl"] = "tcp://192.168.1.50:6666",
            [setting] = -1,
        });

        act.Should().Throw<PLValidationException>().WithMessage($"*{setting}*");
    }

    /// <summary>A parameter of the wrong shape is a configuration mistake, and is reported as one.</summary>
    [Fact]
    public void FromConfiguration_WithAParameterOfTheWrongShape_FailsAsAValidationError()
    {
        var act = () => PosNetConfiguration.FromConfiguration(new Dictionary<string, object>
        {
            ["DeviceUrl"] = "tcp://192.168.1.50:6666",
            ["VatRateTable"] = "[{\"PtuSlot\":\"A\",\"VatRatePercent\":23}]",
        });

        act.Should().Throw<PLValidationException>().WithMessage("*PosNet SCU configuration could not be read*");
    }

    /// <summary>The serial settings of a serial address, the way the transport factory reads them.</summary>
    private static PosNetSerialSettings SerialSettingsOf(PosNetConfiguration configuration)
        => PosNetSerialSettings.From((PosNetDeviceAddress.Serial)configuration.ParseDeviceAddress(), configuration);
}
