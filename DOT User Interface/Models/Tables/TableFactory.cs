using DOT_User_Interface.Models.Tables.Util;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DOT_User_Interface.Models.Tables
{
    public class TableFactory
    {
        public TableFactory(I18n I18n)
        {
            this.Load();
            this.I18n = I18n;
        }
        private I18n I18n;
        private readonly List<ITable> _tables = new();
        public ReadOnlyCollection<ITable> Tables => _tables.AsReadOnly();

        public (ITable? Original, ITable Working)? TargetTables { get; private set; }
        public ITable? EditTable => TargetTables?.Working;

        private string DefaultName => I18n["TableFactory.Default_name"];
        private const string TablesFileName = "tables.json";

        public List<ITable> Glossaries => _tables.Where(t => t.Type == TableTypes.Glossary).ToList();
        public List<ITable> Markers => _tables.Where(t => t.Type == TableTypes.Marker).ToList();

        // ====== Criação ======
        public ITable Create(TableTypes type) => Create(type, DefaultName);

        public ITable Create(TableTypes type, string name)
        {
            var table = CreateConcreteTable(type, name);
            TargetTables = (null, table);
            return table;
        }

        // ====== Edição ======
        public ITable Edit(ITable table)
        {
            var clone = table.CloneBase();
            TargetTables = (table, clone);
            return clone;
        }

        public void Add(ITable table)
        {
            var original = TargetTables?.Original;
            var working = TargetTables?.Working;

            if (original is not null && working == table)
            {
                var idx = _tables.IndexOf(original);
                if (idx >= 0) _tables[idx] = table;
            }
            else if (!_tables.Contains(table))
                _tables.Add(table);

            Cancel();
            Save(); // 🔹 Salva automaticamente ao adicionar
        }

        public void Remove(ITable table)
        {
            _tables.Remove(table);
            if (TargetTables is { } tt)
            {
                if (ReferenceEquals(tt.Working, table) || ReferenceEquals(tt.Original, table))
                    TargetTables = null;
            }
            Save(); // 🔹 Salva automaticamente ao remover
        }

        public void Cancel() => TargetTables = null;

        // ====== Persistência ======
        private static string GetSavePath() =>
            Path.Combine(FileSystem.AppDataDirectory, TablesFileName);

        public void Save()
        {
            var path = GetSavePath();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new TableJsonConverter(I18n) }
            };

            var json = JsonSerializer.Serialize(_tables, options);
            File.WriteAllText(path, json);
        }

        public void Load()
        {
            var path = GetSavePath();
            if (!File.Exists(path)) return;

            try
            {
                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // tolerante
                    Converters = { new TableJsonConverter(I18n) }
                };
                var loaded = JsonSerializer.Deserialize<List<ITable>>(json, options);
                if (loaded is { Count: > 0 })
                {
                    _tables.Clear();
                    _tables.AddRange(loaded);
                    Save(); // 🔁 migra o arquivo para o novo formato
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(I18n["TableFactory.Load_fail"] + $" {ex}");
                // opcional: mover para .bak
                // File.Move(path, path + ".bak", true);
            }
        }

        // ====== Utilidades ======
        private ITable CreateConcreteTable(TableTypes type, string name) => type switch
        {
            TableTypes.Marker => new Table<MarkerRow>(name, TableTypes.Marker),
            TableTypes.Glossary => new Table<GlossaryRow>(name, TableTypes.Glossary),
            _ => throw new NotSupportedException(I18n["TableFactory.Unsupported_type"] + $" {type}")
        };

        public ITable ImportNewFromJson(string json, string? newName = null, TableTypes? overrideType = null)
        {
            // 1) Desserializa o arquivo de origem (ITable), tolerante a formato antigo
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new TableJsonConverter(I18n) }
            };

            ITable? imported = null;
            try
            {
                imported = JsonSerializer.Deserialize<ITable>(json, options);
            }
            catch
            {
                // fallback para casos muito antigos (apenas array de rows)
                imported = TryInferLegacy(json);
            }

            if (imported is null)
                throw new InvalidDataException(I18n["TableFactory.Invalid_file"]);

            // 2) Decide o tipo e o nome da nova tabela
            var type = overrideType ?? imported.Type;
            if (type == TableTypes.Unknown) type = TableTypes.Marker; // fallback seguro

            var name = string.IsNullOrWhiteSpace(newName) ? imported.Name : newName;
            if (string.IsNullOrWhiteSpace(name)) name = I18n["TableFactory.Imported_name"];

            // 3) Cria tabela do tipo correto e copia os dados
            var working = CreateConcreteTable(type, name);

            switch (type)
            {
                case TableTypes.Marker:
                    {
                        var src = imported as Table<MarkerRow>
                                  ?? ConvertAnyToMarker(imported);
                        var dst = (Table<MarkerRow>)working;

                        dst.Description = src.Description;
                        dst.Filter = ""; // inicia sem filtro ou copie src.Filter se preferir
                        dst.Rows = src.Rows
                            .Select(r => new MarkerRow { SourceTerm = r.SourceTerm })
                            .ToList();
                        break;
                    }
                case TableTypes.Glossary:
                    {
                        var src = imported as Table<GlossaryRow>
                                  ?? ConvertAnyToGlossary(imported);
                        var dst = (Table<GlossaryRow>)working;

                        dst.Description = src.Description;
                        dst.Filter = ""; // idem
                        dst.Rows = src.Rows
                            .Select(r => new GlossaryRow { SourceTerm = r.SourceTerm, TargetTerm = r.TargetTerm })
                            .ToList();
                        break;
                    }
                default:
                    throw new NotSupportedException(I18n["TableFactory.Unsupported_type"] + $" {type}");
            }

            // 4) Entra em modo de edição tal como Create(...)
            TargetTables = (null, working);
            return working;
        }

        /// <summary>
        /// Importa a partir de um IBrowserFile (InputFile) e retorna a tabela em modo de edição.
        /// </summary>
        public async Task<ITable> ImportNewFromBrowserFileAsync(IBrowserFile file, string? newName = null, TableTypes? overrideType = null, long maxSizeBytes = 10 * 1024 * 1024)
        {
            using var stream = file.OpenReadStream(maxAllowedSize: maxSizeBytes);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return ImportNewFromJson(json, newName, overrideType);
        }

        // ===== Helpers de conversão e legado =====

        private Table<MarkerRow> ConvertAnyToMarker(ITable any)
        {
            // tenta converter por reflexão de Rows
            if (any is Table<MarkerRow> m) return m;

            var mr = new Table<MarkerRow>(any.Name, TableTypes.Marker)
            {
                Description = any.Description
            };

            foreach (var o in any.RowsAsObjects)
            {
                // tenta mapear propriedades comuns
                var srcTerm = o?.GetType().GetProperty("SourceTerm")?.GetValue(o)?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(srcTerm))
                    mr.Rows.Add(new MarkerRow { SourceTerm = srcTerm });
            }
            return mr;
        }

        private Table<GlossaryRow> ConvertAnyToGlossary(ITable any)
        {
            if (any is Table<GlossaryRow> g) return g;

            var gl = new Table<GlossaryRow>(any.Name, TableTypes.Glossary)
            {
                Description = any.Description
            };

            foreach (var o in any.RowsAsObjects)
            {
                var srcTerm = o?.GetType().GetProperty("SourceTerm")?.GetValue(o)?.ToString() ?? "";
                var tgtTerm = o?.GetType().GetProperty("TargetTerm")?.GetValue(o)?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(srcTerm) || !string.IsNullOrWhiteSpace(tgtTerm))
                    gl.Rows.Add(new GlossaryRow { SourceTerm = srcTerm, TargetTerm = tgtTerm });
            }
            return gl;
        }

        /// <summary>
        /// Para arquivos bem antigos: tenta inferir apenas lendo um array de rows.
        /// </summary>
        private ITable? TryInferLegacy(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Caso seja um objeto com "Rows" dentro
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("Rows", out var rowsEl) && rowsEl.ValueKind == JsonValueKind.Array)
                {
                    return InferFromRowsElement(root, rowsEl);
                }

                // Caso seja um array de rows diretamente
                if (root.ValueKind == JsonValueKind.Array)
                {
                    return InferFromRowsElement(default, root);
                }
            }
            catch { /* ignore */ }

            return null;
        }

        private ITable InferFromRowsElement(JsonElement parent, JsonElement rows)
        {
            var first = rows.GetArrayLength() > 0 ? rows[0] : default;

            bool looksGlossary =
                first.ValueKind == JsonValueKind.Object &&
                (first.TryGetProperty("TargetTerm", out _) ||
                 first.TryGetProperty("Target", out _));

            if (looksGlossary)
            {
                var t = new Table<GlossaryRow>(I18n["TableFactory.Imported_name"], TableTypes.Glossary);
                foreach (var el in rows.EnumerateArray())
                {
                    var s = el.TryGetProperty("SourceTerm", out var sEl) ? sEl.GetString() ?? "" :
                            el.TryGetProperty("Source", out var s2) ? s2.GetString() ?? "" : "";
                    var tg = el.TryGetProperty("TargetTerm", out var tEl) ? tEl.GetString() ?? "" :
                             el.TryGetProperty("Target", out var t2) ? t2.GetString() ?? "" : "";
                    t.Rows.Add(new GlossaryRow { SourceTerm = s, TargetTerm = tg });
                }
                return t;
            }
            else
            {
                var t = new Table<MarkerRow>(I18n["TableFactory.Imported_name"], TableTypes.Marker);
                foreach (var el in rows.EnumerateArray())
                {
                    var s = el.TryGetProperty("SourceTerm", out var sEl) ? sEl.GetString() ?? "" :
                            el.TryGetProperty("Term", out var s2) ? s2.GetString() ?? "" : "";
                    t.Rows.Add(new MarkerRow { SourceTerm = s });
                }
                return t;
            }
        }
    }
}
