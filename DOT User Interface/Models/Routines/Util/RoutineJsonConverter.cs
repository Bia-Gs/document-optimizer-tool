using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DOT_User_Interface.Models.Routines;

namespace DOT_User_Interface.Models.Routines.Util
{
    public class RoutineJsonConverter : JsonConverter<IRoutine>
    {
        public override IRoutine? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var element = doc.RootElement;
            var raw = element.GetRawText();

            // 1) tenta ler propriedade "Type" / "type" (string ou número)
            if (element.TryGetProperty("Type", out var tp) || element.TryGetProperty("type", out tp))
            {
                if (tp.ValueKind == JsonValueKind.String)
                {
                    var s = tp.GetString();
                    if (!string.IsNullOrWhiteSpace(s) && Enum.TryParse<RoutineTypes>(s, ignoreCase: true, out var parsed))
                        return DeserializeByType(parsed, raw, options);
                }
                else if (tp.ValueKind == JsonValueKind.Number && tp.TryGetInt32(out var n) && Enum.IsDefined(typeof(RoutineTypes), n))
                {
                    return DeserializeByType((RoutineTypes)n, raw, options);
                }
            }

            // 2) heurística por propriedades presentes
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in element.EnumerateObject()) names.Add(p.Name);

            if (names.Overlaps(new[] { "SourceText", "TargetText", "original-text", "original_text", "new-text", "new_text", "source_text", "target_text", "sourceText", "targetText" }))
                return JsonSerializer.Deserialize<RoutineEditTexts>(raw, options);
            if (names.Overlaps(new[] { "IgnoreNumbers", "DuplicateInParagraph", "ignore_numbers", "duplicate_in_paragraph", "ignoreNumbers", "duplicateInParagraph" }))
                return JsonSerializer.Deserialize<RoutineEditParagraphs>(raw, options);
            if (names.Overlaps(new[] { "StartColorGap", "EndColorGap", "start_color", "end_color", "startColorGap", "endColorGap" }))
                return JsonSerializer.Deserialize<RoutineColorGap>(raw, options);
            if (names.Overlaps(new[] { "SourceImages", "TargetImage", "source_images", "target_image", "sourceImages", "targetImage" }))
                return JsonSerializer.Deserialize<RoutineReplaceImages>(raw, options);

            // 3) não foi possível inferir
            return null;
        }

        public override void Write(Utf8JsonWriter writer, IRoutine value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            // Serializa usando o tipo concreto para preservar todas as propriedades
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }

        private static IRoutine? DeserializeByType(RoutineTypes type, string raw, JsonSerializerOptions options)
        {
            return type switch
            {
                RoutineTypes.EditTexts => JsonSerializer.Deserialize<RoutineEditTexts>(raw, options),
                RoutineTypes.EditParagraphs => JsonSerializer.Deserialize<RoutineEditParagraphs>(raw, options),
                RoutineTypes.EditColorGap => JsonSerializer.Deserialize<RoutineColorGap>(raw, options),
                RoutineTypes.ReplaceImages => JsonSerializer.Deserialize<RoutineReplaceImages>(raw, options),
                _ => null
            };
        }
    }
}
