using System.Text.Json;
using System.Text.Json.Serialization;

namespace DOT_User_Interface.Models.Tables.Util
{
    public class TableJsonConverter(I18n i18n) : JsonConverter<ITable>
    {
        private I18n I18n = i18n;
        public override ITable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // Lê o campo Type (pode não existir ou ser 0 em arquivos antigos)
            TableTypes type = TableTypes.Unknown;
            if (root.TryGetProperty("Type", out var typeProp) && typeProp.ValueKind == JsonValueKind.Number)
            {
                type = (TableTypes)typeProp.GetInt32();
            }

            if (type == TableTypes.Unknown)
            {
                // 🔎 tentativa de inferência pelo conteúdo das linhas
                type = InferTypeFromRows(root);
                if (type == TableTypes.Unknown)
                {
                    // fallback seguro (evita quebrar a carga)
                    type = TableTypes.Marker;
                }
            }

            return type switch
            {
                TableTypes.Marker => JsonSerializer.Deserialize<Table<MarkerRow>>(root.GetRawText(), options),
                TableTypes.Glossary => JsonSerializer.Deserialize<Table<GlossaryRow>>(root.GetRawText(), options),
                _ => throw new JsonException(I18n["TableFactory.TableJsonConverter.Read_error"] + $" {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, ITable value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case Table<MarkerRow> marker:
                    JsonSerializer.Serialize(writer, marker, options);
                    break;
                case Table<GlossaryRow> glossary:
                    JsonSerializer.Serialize(writer, glossary, options);
                    break;
                default:
                    throw new JsonException(I18n["TableFactory.TableJsonConverter.Write_error"] + $" {value.Type}");
            }
        }

        private static TableTypes InferTypeFromRows(JsonElement root)
        {
            if (!root.TryGetProperty("Rows", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() == 0)
                return TableTypes.Unknown;

            var first = rows[0];
            // Ajuste estes “sinais” aos nomes reais das propriedades das suas linhas
            bool looksGlossary =
                first.TryGetProperty("Source", out _) || first.TryGetProperty("From", out _) ||
                first.TryGetProperty("Target", out _) || first.TryGetProperty("To", out _) ||
                first.TryGetProperty("Replace", out _);

            bool looksMarker =
                first.TryGetProperty("Term", out _) || first.TryGetProperty("Regex", out _) ||
                first.TryGetProperty("CaseSensitive", out _) || first.TryGetProperty("WholeWord", out _);

            if (looksGlossary && !looksMarker) return TableTypes.Glossary;
            if (looksMarker && !looksGlossary) return TableTypes.Marker;

            // se ambos/similares, escolha um padrão
            return TableTypes.Marker;
        }
    }
}
