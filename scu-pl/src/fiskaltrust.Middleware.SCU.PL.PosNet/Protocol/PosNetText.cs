using System.Text;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

/// <summary>
/// Prepares free text (goods and payment names) for a POSNET parameter value. The protocol has no
/// escaping: fields are separated by TAB and the frame by STX/ETX, so a control character inside a
/// value does not travel as content — a TAB would open an extra protocol field (a second
/// <c>vt</c>, i.e. a different PTU slot) and an ETX would end the frame early. Control characters
/// are therefore replaced before they ever reach <see cref="PosNetFrame.Encode"/>, which rejects
/// what slipped through.
/// </summary>
public static class PosNetText
{
    /// <summary>True if the value cannot travel inside a POSNET frame as content.</summary>
    public static bool ContainsFramingCharacter(string value)
    {
        foreach (var character in value)
        {
            if (IsFramingCharacter(character))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Replaces every framing character with a space and cuts the result to the field's maximum
    /// length. Trailing whitespace is dropped so a truncated name does not end in padding.
    /// </summary>
    public static string ToField(string? value, int maxLength)
    {
        var text = value ?? "";
        if (text.Length > maxLength)
        {
            text = text[..maxLength];
        }

        if (!ContainsFramingCharacter(text))
        {
            return text.TrimEnd();
        }

        var sanitized = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            sanitized.Append(IsFramingCharacter(character) ? ' ' : character);
        }
        return sanitized.ToString().TrimEnd();
    }

    // Everything below the printable range plus DEL: TAB (field separator), STX/ETX (frame
    // markers), CR/LF and any other control code the device would not render as text.
    private static bool IsFramingCharacter(char character) => character < ' ' || character == (char)0x7F;
}
