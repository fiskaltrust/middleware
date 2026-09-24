using System.Threading;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

/// <summary>
/// Position of a document in the RT numbering: the Z (daily closing) number it belongs to and its
/// progressive number inside that Z.
/// </summary>
public sealed record DocPosition(long ZNumber, long DocNumber);

/// <summary>
/// Holds the last known <see cref="DocPosition"/> of an RT device.
/// <para>
/// The value is the baseline the network-error recovery compares against: if the document counter of
/// the printer has moved past it, the printer did print the receipt that the caller believes failed.
/// </para>
/// <para>
/// The slot exists as a separate object so its lifetime can be decoupled from the SCU instance. On-prem
/// the SCU lives as long as its host, so the default (a slot of its own) is enough; in the hosted
/// CloudRTDevice path a new SCU is built for every HTTP request, and the host keeps one slot per cashbox
/// alive across requests instead.
/// </para>
/// <para>
/// The reference is read and written through <see cref="Volatile"/> so two concurrent requests on the same
/// cashbox always see a whole value: a struct field would be written non-atomically and a reader could pick
/// up the Z number of one document together with the doc number of another.
/// </para>
/// </summary>
public sealed class LastDocSlot
{
    private DocPosition? _value;

    public DocPosition? Value
    {
        get => Volatile.Read(ref _value);
        set => Volatile.Write(ref _value, value);
    }
}
