using DOT_User_Interface.Models.Routines.Util;
using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Components.Forms;

namespace DOT_User_Interface.Models.Routines
{
    public class RoutineReplaceImages : RoutineSettings, IRoutine
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool ApplyOnElements { get; set; } = true;
        public bool ApplyOnHeaders { get; set; } = true;
        public bool ApplyOnFooters { get; set; } = true;

        // Lista de imagens de origem (nomes armazenados relativos à pasta de dados da aplicação)
        public List<string> SourceImages { get; set; } = new();

        // Imagem alvo (nome armazenado relativo à pasta de dados da aplicação)
        public string? TargetImage { get; set; }

        public string Endpoint => ProcessEndpoints.ReplaceImages;
        public RoutineTypes Type => RoutineTypes.ReplaceImages;

        public IRoutine CloneBase()
        {
            return new RoutineReplaceImages
            {
                Name = this.Name,
                Description = this.Description,
                ApplyOnElements = this.ApplyOnElements,
                ApplyOnHeaders = this.ApplyOnHeaders,
                ApplyOnFooters = this.ApplyOnFooters,
                // copia listagens para evitar referências compartilhadas
                SourceImages = new List<string>(this.SourceImages),
                TargetImage = this.TargetImage
            };
        }

        private static string ImagesFolderName => "replace_images";

        private static string EnsureImagesFolder()
        {
            var dir = Path.Combine(FileSystem.AppDataDirectory, ImagesFolderName);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        private static string StoreFileName(string originalName)
        {
            var safe = Path.GetFileName(originalName) ?? "file";
            return $"{Guid.NewGuid():N}_{safe}";
        }

        private static string GetStoredPath(string storedFileName) =>
            Path.Combine(FileSystem.AppDataDirectory, ImagesFolderName, storedFileName);

        /// <summary>
        /// Adiciona uma imagem de origem a partir de um IBrowserFile (InputFile).
        /// O arquivo é salvo em disco e o nome salvo em SourceImages.
        /// </summary>
        public async Task<string> AddSourceImageAsync(IBrowserFile file, long maxSizeBytes = 10 * 1024 * 1024)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            var dir = EnsureImagesFolder();
            var storedName = StoreFileName(file.Name);
            var path = Path.Combine(dir, storedName);

            // Usa o stream do browser (limitar tamanho)
            using var stream = file.OpenReadStream(maxAllowedSize: maxSizeBytes);
            using var fs = File.Create(path);
            await stream.CopyToAsync(fs);
            fs.Flush();

            SourceImages.Add(storedName);
            return storedName;
        }

        /// <summary>
        /// Define a imagem alvo a partir de um IBrowserFile (InputFile).
        /// O arquivo é salvo em disco e TargetImage recebe o nome armazenado.
        /// </summary>
        public async Task<string> SetTargetImageAsync(IBrowserFile file, long maxSizeBytes = 10 * 1024 * 1024)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            var dir = EnsureImagesFolder();
            var storedName = StoreFileName(file.Name);
            var path = Path.Combine(dir, storedName);

            using var stream = file.OpenReadStream(maxAllowedSize: maxSizeBytes);
            using var fs = File.Create(path);
            await stream.CopyToAsync(fs);
            fs.Flush();

            TargetImage = storedName;
            return storedName;
        }

        /// <summary>
        /// Adiciona uma imagem já vinda de stream (ex.: MAUI FileResult.OpenReadAsync).
        /// </summary>
        public async Task<string> AddSourceImageAsync(Stream inputStream, string originalFileName)
        {
            if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
            var dir = EnsureImagesFolder();
            var storedName = StoreFileName(originalFileName);
            var path = Path.Combine(dir, storedName);

            using var fs = File.Create(path);
            await inputStream.CopyToAsync(fs);
            fs.Flush();

            SourceImages.Add(storedName);
            return storedName;
        }

        public void RemoveSourceImage(string storedFileName)
        {
            if (string.IsNullOrWhiteSpace(storedFileName)) return;
            SourceImages.Remove(storedFileName);

            try
            {
                var path = GetStoredPath(storedFileName);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // não propagar: falhas de I/O não bloqueiam remoção da referência
            }
        }

        public void ClearSourceImages()
        {
            foreach (var name in SourceImages.ToArray())
            {
                try
                {
                    var path = GetStoredPath(name);
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch
                {
                    // ignore
                }
            }
            SourceImages.Clear();
        }

        public void ClearTargetImage()
        {
            if (!string.IsNullOrWhiteSpace(TargetImage))
            {
                try
                {
                    var path = GetStoredPath(TargetImage);
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch
                {
                    // ignore
                }
            }
            TargetImage = null;
        }

        public string ProcessingArgs(bool applyFilter = false)
        {
            var styles = new Dictionary<string, object?>();

            // monta lista de imagens com nomes (o envio real dos bytes pode ser feito pelo fluxo de upload separado)
            var sourceFiles = new List<object?>();
            foreach (var stored in SourceImages)
            {
                sourceFiles.Add(new { file = stored });
            }

            object? target = TargetImage is null ? null : new { file = TargetImage };

            var config = new Dictionary<string, object?>
            {
                ["apply_on_elements"] = ApplyOnElements,
                ["apply_on_headers"] = ApplyOnHeaders,
                ["apply_on_footers"] = ApplyOnFooters,
                ["source_images"] = sourceFiles,
                ["target_image"] = target
            };

            var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var suffixMap = new Dictionary<string, string>
            {
                ["en"] = " edited",
                ["pt"] = " editado",
                ["es"] = " editado",
                ["fr"] = " édité"
            };
            var suffix = suffixMap.ContainsKey(lang) ? suffixMap[lang] : suffixMap["en"];

            var payload = new
            {
                model = "edit",
                action = new { config, styles },
                suffix
            };

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
        }

        public async Task ShareAsync(I18n I18n)
        {
            try
            {
                var fileName = $"{Name.Replace(' ', '_')}_{Type}.json";
                var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                await File.WriteAllTextAsync(tempPath, json);

                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = I18n.T("RoutineFactory.Routine.Share") + $" \"{Name}\"",
                    File = new ShareFile(tempPath)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(I18n.T("RoutineFactory.Routine.Share_erro") + $" {ex.Message}");
            }
        }
    }
}
