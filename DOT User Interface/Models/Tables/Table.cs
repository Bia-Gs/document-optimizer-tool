using DOT_User_Interface.Models.Tables.Util;
using System.Text.Json;

namespace DOT_User_Interface.Models.Tables
{
    public class Table<T> : ITable where T : new()
    {
        // 🔹 Construtor sem parâmetros exigido pela desserialização
        public Table() { }

        public Table(string name, TableTypes type)
        {
            Name = name;
            Type = type;
        }

        public Table(Table<T> table)
        {
            Name = table.Name;
            Type = table.Type;
            Filter = table.Filter;
            Description = table.Description;
            Rows = table.Rows.Select(CloneRow).ToList();
        }

        public Table<T> Clone() => new(this);

        // 🔹 Propriedades com set público (ou [JsonInclude] + private set)
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Filter { get; set; } = "";

        // Se preferir manter setter privado, use:
        // [JsonInclude] public List<T> Rows { get; private set; } = new();
        public List<T> Rows { get; set; } = new();

        // Antes era private set; durante a carga precisamos setar.
        public TableTypes Type { get; set; }

        public void AddRow() => Rows.Add(new T());
        public IEnumerable<object> RowsAsObjects
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Filter))
                    return Rows.Cast<object>();

                if (typeof(IRow).IsAssignableFrom(typeof(T)))
                {
                    return Rows
                        .Cast<IRow>()
                        .Where(r => r.Matches(Filter))
                        .Cast<object>();
                }

                // Fallback: se T não implementa IFilterRow, retorna tudo
                return Rows.Cast<object>();
            }
        }
        public void RemoveRow(object row) { if (row is T typed) Rows.Remove(typed); }

        private static T CloneRow(T source)
        {
            var clone = new T();
            var props = typeof(T).GetProperties();
            foreach (var prop in props)
                if (prop.CanRead && prop.CanWrite)
                    prop.SetValue(clone, prop.GetValue(source));
            return clone;
        }

        public ITable CloneBase() => new Table<T>(this);

        public async Task ShareAsync(I18n I18n)
        {
            try
            {
                // Caminho temporário
                var fileName = $"{Name.Replace(' ', '_')}_{Type}.json";
                var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);

                // Serializa esta tabela (somente ela)
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(tempPath, json);
                // Abre o compartilhamento nativo (Windows / Android / iOS)
                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = I18n["TableFactory.Table.Share"] + $" \"{Name}\"",
                    File = new ShareFile(tempPath)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(I18n["TableFactory.Table.Share_erro"] + $" {ex.Message}");
            }
        }

        public string ProcessingArgs(bool applyFilter = false)
        {
            var src = new List<string>();
            var tgt = new List<string>();

            // Usa as linhas filtradas (RowsAsObjects) por padrão
            IEnumerable<object> rows = applyFilter ? RowsAsObjects : Rows.Cast<object>();

            if (typeof(T) == typeof(MarkerRow))
            {
                foreach (var o in rows)
                {
                    var r = (MarkerRow)o;
                    var s = r.SourceTerm?.Trim();
                    if (!string.IsNullOrWhiteSpace(s))
                        src.Add(s);
                }

                // Para Marker NÃO envia target_texts (o backend usa src_list por padrão)
                var argsMarker = new Dictionary<string, object?>
                {
                    ["source_texts"] = src,
                    ["table_name"] = Name,
                    ["type"] = "marker"
                };

                return JsonSerializer.Serialize(argsMarker);
            }
            else if (typeof(T) == typeof(GlossaryRow))
            {
                foreach (var o in rows)
                {
                    var r = (GlossaryRow)o;
                    var s = r.SourceTerm?.Trim();
                    var t = r.TargetTerm?.Trim();

                    // Mantém pares alinhados; ignora linhas totalmente vazias
                    if (string.IsNullOrWhiteSpace(s) && string.IsNullOrWhiteSpace(t))
                        continue;

                    // Se só o target vier vazio, ainda alinhamos (servidor pode tratar)
                    src.Add(s ?? string.Empty);
                    tgt.Add(t ?? string.Empty);
                }

                var argsGloss = new Dictionary<string, object?>
                {
                    ["source_texts"] = src,
                    ["target_texts"] = tgt,  // tamanhos iguais a src
                    ["table_name"] = Name,
                    ["type"] = "glossary"
                };

                return JsonSerializer.Serialize(argsGloss);
            }
            else
            {
                // Tipo desconhecido: fallback seguro (envia apenas source_texts de qualquer objeto com SourceTerm)
                foreach (var o in rows)
                {
                    var prop = o.GetType().GetProperty("SourceTerm");
                    var s = prop?.GetValue(o)?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(s))
                        src.Add(s);
                }

                var args = new Dictionary<string, object?>
                {
                    ["source_texts"] = src,
                    ["table_name"] = Name,
                    ["type"] = Type.ToString().ToLowerInvariant()
                };

                return JsonSerializer.Serialize(args);
            }
        }
    }
}
