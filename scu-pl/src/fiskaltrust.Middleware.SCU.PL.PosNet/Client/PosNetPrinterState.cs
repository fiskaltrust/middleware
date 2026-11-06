using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Client;

/// <summary>
/// The printer's live state, read with the two asynchronous status commands (<c>!sprn</c>,
/// <c>!sdev</c> — POT-I-DEV-37 pp. 16, 390–391). Asynchronous commands are answered at once, also
/// while a print blocks every other command, which makes them the only way to see what a register
/// stuck on a receipt is doing. A field the register did not report (or a read that failed) is null.
/// </summary>
/// <remarks>
/// Measured on a POSNET (Thermal family, USB): with the cover open but nothing printing, the state
/// reads ready (<c>pr0 ds0 qeT</c>) — the register only notices once it tries to print. While a
/// receipt waits for the closed cover it reads <c>pr5 ds3 qeN</c>: no paper under the head, waiting
/// for the operator, the closing command still queued.
/// </remarks>
/// <param name="Mechanism"><c>sprn pr</c>: 0 ready, 1/3/10 cover or lever, 5 no paper, 6–9 power, temperature, cutter.</param>
/// <param name="DeviceStatus"><c>sdev ds</c>: 0 ready, 1 in the menu, 2 waiting for a key, 3 waiting for the operator after an error.</param>
/// <param name="QueueEmpty"><c>sdev qe</c>: whether every command sent has been executed.</param>
/// <param name="PaperNearEnd"><c>sdev pe</c>: the paper is running out (only where the near-end sensor is fitted and enabled).</param>
/// <param name="TransactionResources"><c>sdev tl</c>: 0 free, 1/2 held by a PC interface, 3 by the device itself.</param>
public sealed record PosNetPrinterState(long? Mechanism, long? DeviceStatus, bool? QueueEmpty, bool? PaperNearEnd, long? TransactionResources)
{
    /// <summary>Whether the register answered at all.</summary>
    public bool Answered => Mechanism is not null || DeviceStatus is not null;

    /// <summary>Ready to print and nothing pending: no fault, not waiting for anyone, every command executed.</summary>
    public bool IsIdle => Mechanism == 0 && DeviceStatus == 0 && QueueEmpty == true;

    public static async Task<PosNetPrinterState> ReadAsync(PosNetClient client)
    {
        var mechanism = await PosNetStatus.TryReadAsync(client, PosNetCommands.SprnAsync(), status => PosNetStatus.ReadNumber(status, "pr"));
        var device = await PosNetStatus.TryReadAsync(client, PosNetCommands.SdevAsync(), status => status);
        return new PosNetPrinterState(
            mechanism,
            device is null ? null : PosNetStatus.ReadNumber(device, "ds"),
            device is null ? null : PosNetStatus.ReadFlag(device, "qe"),
            device is null ? null : PosNetStatus.ReadFlag(device, "pe"),
            device is null ? null : PosNetStatus.ReadNumber(device, "tl"));
    }

    /// <summary>What is wrong, in words an operator can act on; null when the register is idle.</summary>
    public string? DescribeProblem()
    {
        if (!Answered)
        {
            return "the printer did not report its state";
        }
        if (Mechanism is not (0 or null))
        {
            return DescribeMechanism(Mechanism);
        }
        if (DeviceStatus is not (0 or null))
        {
            return DescribeDeviceStatus(DeviceStatus);
        }
        if (QueueEmpty == false)
        {
            return "the printer is still executing earlier commands";
        }
        return IsIdle ? null : "the printer did not report a complete state";
    }

    /// <summary>The sprn mechanism states (POT-I-DEV-37 p. 390).</summary>
    public static string DescribeMechanism(long? mechanism) => mechanism switch
    {
        0 => "ready",
        1 => "the lever is raised or the cover is open (podniesiona dźwignia / otwarta pokrywa)",
        2 => "the printing mechanism is not accessible (brak dostępu do mechanizmu)",
        3 => "the cover is raised (podniesiona pokrywa)",
        5 => "no paper under the print head — the paper is out or the cover is open (brak papieru)",
        6 => "the temperature or supply voltage is out of range (nieodpowiednia temperatura lub zasilanie)",
        7 => "the power dropped out briefly (chwilowy zanik zasilania)",
        8 => "the cutter failed (błąd obcinacza)",
        9 => "the power supply failed (błąd zasilacza)",
        10 => "the cover was raised while cutting (podniesiona pokrywa przy obcinaniu)",
        null => "not reported",
        _ => "the printing mechanism reports a fault",
    };

    /// <summary>The sdev device states (POT-I-DEV-37 p. 391).</summary>
    public static string DescribeDeviceStatus(long? deviceStatus) => deviceStatus switch
    {
        0 => "ready",
        1 => "the printer is in its menu",
        2 => "the printer is waiting for a key press",
        3 => "the printer is waiting for the operator after an error (check paper and cover)",
        null => "not reported",
        _ => "the printer reports an unknown state",
    };
}
