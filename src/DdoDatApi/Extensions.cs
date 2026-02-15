using System;
using VoK.Sdk.Ddo.Enums;
using VoK.Sdk.Enums;
using VoK.Sdk.Properties;

namespace DdoDatApi;

public static class Extensions
{
    public static T GetAttributeOfType<T>(this Enum enumVal) where T : System.Attribute
    {
        var type = enumVal.GetType();
        var memInfo = type.GetMember(enumVal.ToString());
        var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
        return (attributes.Length > 0) ? (T)attributes[0] : null;
    }

    public static uint GetWeenieType(this IPropertyCollection propertyCollection)
    {
        if (propertyCollection == null) return 0;
        var prop = propertyCollection.GetEnumProperty((uint)DdoProperty.WeenieType);
        return prop?.UInt32Value ?? 0;
    }

}
