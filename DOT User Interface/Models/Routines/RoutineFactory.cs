using DOT_User_Interface.Models.Routines.Util;
using Microsoft.AspNetCore.Components.Forms;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace DOT_User_Interface.Models.Routines
{
    public class RoutineFactory
    {
        public RoutineFactory(I18n I18n)
        {
            this.I18n = I18n;
            Load();
        }

        private readonly I18n I18n;

        private const string RoutinesFileName = "routines.json";

        public List<RoutineColorGap> RoutinesColorGaps { get; set; } = new();
        public List<RoutineEditParagraphs> RoutinesEditParagraphs { get; set; } = new();
        public List<RoutineEditTexts> RoutinesEditTexts { get; set; } = new();
        public List<RoutineReplaceImages> RoutinesReplaceImages { get; set; } = new();

        public IEnumerable<IRoutine> Routines =>
            RoutinesColorGaps.Cast<IRoutine>()
                .Concat(RoutinesEditParagraphs)
                .Concat(RoutinesEditTexts)
                .Concat(RoutinesReplaceImages);

        public (IRoutine? Original, IRoutine Working)? TargetRoutine { get; private set; }
        public IRoutine? EditRoutine => TargetRoutine?.Working;
        public IRoutine? OriginalRoutine => TargetRoutine?.Original;

        private string DefaultName => I18n["RoutineFactory.Default.Name"];

        // ====== Criação ======
        public void Create(RoutineTypes type)
        {
            switch (type)
            {
                case RoutineTypes.EditColorGap:
                    TargetRoutine = (null, new RoutineColorGap());
                    break;
                case RoutineTypes.EditParagraphs:
                    TargetRoutine = (null, new RoutineEditParagraphs());
                    break;
                case RoutineTypes.EditTexts:
                    TargetRoutine = (null, new RoutineEditTexts());
                    break;
                case RoutineTypes.ReplaceImages:
                    TargetRoutine = (null, new RoutineReplaceImages());
                    break;
            }
            if (EditRoutine != null)
                EditRoutine.Name = DefaultName;
        }

        // ====== Edição ======
        public IRoutine Edit(IRoutine routine)
        {
            var clone = routine.CloneBase();
            TargetRoutine = (routine, clone);
            return clone;
        }

        public void Add(IRoutine routine)
        {
            var original = TargetRoutine?.Original;
            var working = TargetRoutine?.Working;

            if (original is not null && working == routine)
            {
                // substitui o original pela versão editada
                if (!ReplaceInList(original, routine))
                {
                    // se não encontrou, tenta adicionar normalmente
                    AddToList(routine);
                }
            }
            else
            {
                if (!ContainsRoutine(routine))
                    AddToList(routine);
            }

            Cancel();
            Save(); // salva automaticamente ao adicionar/atualizar
        }

        public void Remove(IRoutine routine)
        {
            RemoveFromList(routine);
            if (TargetRoutine is { } tt)
            {
                if (ReferenceEquals(tt.Working, routine) || ReferenceEquals(tt.Original, routine))
                    TargetRoutine = null;
            }
            Save(); // salva automaticamente ao remover
        }

        public void Cancel() => TargetRoutine = null;

        // ====== Persistência ======
        private static string GetSavePath() =>
            Path.Combine(FileSystem.AppDataDirectory, RoutinesFileName);

        private static JsonSerializerOptions CreateSerializerOptionsForLoadSave()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            // Enum as string for readability (desserialização ainda aceita números)
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            // Conversor polimórfico para IRoutine (preserva propriedades concretas)
            options.Converters.Add(new RoutineJsonConverter());
            return options;
        }

        public void Save()
        {
            var path = GetSavePath();

            var options = CreateSerializerOptionsForLoadSave();

            // serializa a lista combinada (o RoutineJsonConverter garante que os campos concretos serão escritos)
            var list = Routines.ToList();
            var json = JsonSerializer.Serialize(list, options);
            File.WriteAllText(path, json);
        }

        private void Load()
        {
            var path = GetSavePath();
            if (!File.Exists(path)) return;

            var text = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                var options = CreateSerializerOptionsForLoadSave();
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return;

                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    // usa desserialização para IRoutine (converter polimórfico irá escolher o tipo)
                    IRoutine? r = JsonSerializer.Deserialize<IRoutine>(el.GetRawText(), options);
                    if (r != null)
                        AddToList(r);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(I18n["RoutineFactory.Load_fail"] + $" {ex}");
                // não propagar — mantemos inicialização resiliente
            }
        }

        // ====== Importação ======
        public IRoutine ImportNewFromJson(string json, string? newName = null, RoutineTypes? overrideType = null)
        {
            var options = CreateSerializerOptionsForLoadSave();

            IRoutine? imported = null;

            // aceita array com um elemento ou objeto único
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                JsonElement element;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    if (root.GetArrayLength() == 0) throw new InvalidDataException(I18n["RoutineFactory.Invalid_file"]);
                    element = root[0];
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    element = root;
                }
                else
                {
                    throw new InvalidDataException(I18n["RoutineFactory.Invalid_file"]);
                }

                // desserializa de forma tolerante e inferindo tipo se necessário
                imported = JsonSerializer.Deserialize<IRoutine>(element.GetRawText(), options);
            }
            catch (JsonException)
            {
                // formato inválido
                throw new InvalidDataException(I18n["RoutineFactory.Invalid_file"]);
            }

            if (imported is null)
                throw new InvalidDataException(I18n["RoutineFactory.Invalid_file"]);

            // Decide tipo final
            var finalType = overrideType ?? imported.Type;

            // Escolhe nome
            var name = string.IsNullOrWhiteSpace(newName) ? imported.Name : newName;
            if (string.IsNullOrWhiteSpace(name)) name = I18n["RoutineFactory.Imported_name"];

            // Se for mesmo tipo, apenas ajusta nome e usa como working
            IRoutine working;
            if (finalType == imported.Type)
            {
                imported.Name = name;
                working = imported;
            }
            else
            {
                // converte por reflexão copiando propriedades compatíveis
                working = CreateConcreteRoutine(finalType);
                CopySharedProperties(imported, working);
                // garante nome escolhido
                working.Name = name;
            }

            // entra em modo edição
            TargetRoutine = (null, working);
            return working;
        }

        public async Task<IRoutine> ImportNewFromBrowserFileAsync(IBrowserFile file, string? newName = null, RoutineTypes? overrideType = null, long maxSizeBytes = 10 * 1024 * 1024)
        {
            using var stream = file.OpenReadStream(maxAllowedSize: maxSizeBytes);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return ImportNewFromJson(json, newName, overrideType);
        }

        private IRoutine CreateConcreteRoutine(RoutineTypes type) => type switch
        {
            RoutineTypes.EditTexts => new RoutineEditTexts(),
            RoutineTypes.EditParagraphs => new RoutineEditParagraphs(),
            RoutineTypes.EditColorGap => new RoutineColorGap(),
            RoutineTypes.ReplaceImages => new RoutineReplaceImages(),
            _ => throw new NotSupportedException(I18n["RoutineFactory.Unsupported_type"] + $" {type}")
        };

        private static void CopySharedProperties(object src, object dst)
        {
            var srcProps = src.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);
            var dstProps = dst.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var sp in srcProps)
            {
                if (dstProps.TryGetValue(sp.Name, out var dp))
                {
                    try
                    {
                        var val = sp.GetValue(src);
                        // evita atribuir coleções complexas inesperadas (mantém compatível)
                        if (val != null && dp.PropertyType.IsAssignableFrom(sp.PropertyType))
                        {
                            dp.SetValue(dst, val);
                        }
                        else if (val == null && (!dp.PropertyType.IsValueType || Nullable.GetUnderlyingType(dp.PropertyType) != null))
                        {
                            dp.SetValue(dst, null);
                        }
                    }
                    catch
                    {
                        // ignore propriedades que não podem ser copiadas
                    }
                }
            }
        }

        // ====== Helpers para listas tipadas ======
        private bool ReplaceInList(IRoutine original, IRoutine replacement)
        {
            switch (original)
            {
                case RoutineEditTexts o when replacement is RoutineEditTexts r:
                    var idxT = RoutinesEditTexts.IndexOf(o);
                    if (idxT >= 0) { RoutinesEditTexts[idxT] = r; return true; }
                    break;
                case RoutineEditParagraphs o when replacement is RoutineEditParagraphs r:
                    var idxP = RoutinesEditParagraphs.IndexOf(o);
                    if (idxP >= 0) { RoutinesEditParagraphs[idxP] = r; return true; }
                    break;
                case RoutineColorGap o when replacement is RoutineColorGap r:
                    var idxC = RoutinesColorGaps.IndexOf(o);
                    if (idxC >= 0) { RoutinesColorGaps[idxC] = r; return true; }
                    break;
                case RoutineReplaceImages o when replacement is RoutineReplaceImages r:
                    var idxR = RoutinesReplaceImages.IndexOf(o);
                    if (idxR >= 0) { RoutinesReplaceImages[idxR] = r; return true; }
                    break;
            }
            return false;
        }

        private bool ContainsRoutine(IRoutine routine) =>
            routine switch
            {
                RoutineEditTexts r => RoutinesEditTexts.Contains(r),
                RoutineEditParagraphs r => RoutinesEditParagraphs.Contains(r),
                RoutineColorGap r => RoutinesColorGaps.Contains(r),
                RoutineReplaceImages r => RoutinesReplaceImages.Contains(r),
                _ => false
            };

        private void AddToList(IRoutine routine)
        {
            switch (routine)
            {
                case RoutineEditTexts r: RoutinesEditTexts.Add(r); break;
                case RoutineEditParagraphs r: RoutinesEditParagraphs.Add(r); break;
                case RoutineColorGap r: RoutinesColorGaps.Add(r); break;
                case RoutineReplaceImages r: RoutinesReplaceImages.Add(r); break;
            }
        }

        private void RemoveFromList(IRoutine routine)
        {
            switch (routine)
            {
                case RoutineEditTexts r: RoutinesEditTexts.Remove(r); break;
                case RoutineEditParagraphs r: RoutinesEditParagraphs.Remove(r); break;
                case RoutineColorGap r: RoutinesColorGaps.Remove(r); break;
                case RoutineReplaceImages r: RoutinesReplaceImages.Remove(r); break;
            }
        }
    }
}
