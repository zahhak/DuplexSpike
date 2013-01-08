using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class JsonSerializerExtensions
{
    public static string Serialize(this JsonSerializer serializer, object value)
    {
        using (var writer = new StringWriter(CultureInfo.InvariantCulture))
        using (var jsonWriter = new JsonTextWriter(writer))
        {
            serializer.Serialize(jsonWriter, value);
            return writer.ToString();
        }
    }
}