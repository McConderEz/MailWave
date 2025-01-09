using MailWave.Mail.Domain.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MailWave.Mail.Infrastructure.Converters;

/// <summary>
/// Конвертер вложений в json и обратно
/// </summary>
public class AttachmentJsonConverter : JsonConverter<Attachment>
{
    public override void WriteJson(JsonWriter writer, Attachment value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("FileName");
        serializer.Serialize(writer, value.FileName);
        writer.WritePropertyName("Content");
        using (var memoryStream = new MemoryStream())
        {
            value.Content.CopyTo(memoryStream);
            writer.WriteValue(Convert.ToBase64String(memoryStream.ToArray()));
        }

        writer.WriteEndObject();
    }

    public override Attachment ReadJson(
        JsonReader reader, Type objectType, Attachment existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        string fileName = jo.Value<string>("FileName");
        byte[] content = Convert.FromBase64String(jo.Value<string>("Content"));
        return new Attachment { FileName = fileName, Content = new MemoryStream(content) };
    }
}

