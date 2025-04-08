using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace One.Inception.Api.Converters;

public class StringTenantIdConverter : GenericJsonConverter<string, AggregateRootId>
{
    public override bool CanConvertValue(Type valueType)
    {
        return typeof(IConvertible).IsAssignableFrom(valueType);
    }

    public override AggregateRootId Convert(string value, Type objectType, IEnumerable<System.Security.Claims.Claim> claims)
    {
        if (string.IsNullOrEmpty(value.ToString()))
            return null;

        string urn = value;
        if (urn.CanUrlDecode())
            urn = urn.UrlDecode();

        if (urn.IsBase64String())
            urn = urn.Base64Decode();

        var urnParsed = AggregateRootId.Parse(urn);
        return new AggregateRootId(urnParsed.NID, urnParsed.AggregateRootName, urnParsed.NSS);
    }

    public override object GetValue(AggregateRootId instance)
    {
        return instance.Value;
    }
}

public abstract class GenericJsonConverter<TFrom, TObject> : JsonConverter
{
    public abstract object GetValue(TObject instance);

    public abstract TObject Convert(TFrom jObject, Type objectType, IEnumerable<System.Security.Claims.Claim> claims);

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JToken token = JToken.FromObject(value);

        if (token.Type != JTokenType.Object)
        {
            token.WriteTo(writer);
        }
        else
        {
            object id = null;
            if (!ReferenceEquals(null, value))
            {
                id = GetValue((TObject)value);
            }
            writer.WriteValue(id);
        }

    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var value = reader.Value;
        if (CanConvertValue(reader.ValueType) == false)
        {
            return null;
        }
        return Convert((TFrom)System.Convert.ChangeType(reader.Value, typeof(TFrom), System.Globalization.CultureInfo.InvariantCulture), objectType, System.Security.Claims.ClaimsPrincipal.Current.Claims);
    }

    public virtual bool CanConvertValue(Type valueType)
    {
        return valueType == typeof(TFrom);
    }

    public override bool CanRead
    {
        get { return true; }
    }

    public override bool CanWrite
    {
        get { return true; }
    }

    public Type BindingType
    {
        get { return typeof(TObject); }
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(TObject).IsAssignableFrom(objectType);
    }
}
