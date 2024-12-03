using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE;

public static class XmlHelpers
{
    public static string? GetXmlEnumAttributeValueFromEnum<TEnum>(this TEnum value) where TEnum : struct, IConvertible
    {
        var enumType = typeof(TEnum);
        if (!enumType.IsEnum)
        {
            return null;//or string.Empty, or throw exception
        }

        var member = enumType.GetMember(value.ToString() ?? "").FirstOrDefault();
        if (member == null)
        {
            return null;//or string.Empty, or throw exception
        }

        var attribute = member.GetCustomAttributes(false).OfType<XmlEnumAttribute>().FirstOrDefault();
        if (attribute == null)
        {
            return null;//or string.Empty, or throw exception
        }

        return attribute.Name;
    }
}
