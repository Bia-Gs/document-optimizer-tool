using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DOT_User_Interface.Models
{
    public static class ProcessEndpoints
    {
        private const string BaseUrl = "http://127.0.0.1:5000";
        public const string MarkFile = $"{BaseUrl}/mark_file";
        public const string EditTexts = $"{BaseUrl}/edit_texts";
        public const string ColorGap = $"{BaseUrl}/edit_color_gap";
        public const string EditParagraphs = $"{BaseUrl}/edit_paragraphs";
        public const string ReplaceImages = $"{BaseUrl}/replace_file_imgs";
        public const string Translator = $"{BaseUrl}/translate_file";
    }
    public class ProcessFiles()
    {
        public event Action? OnChange;
        public async Task<bool> ProcessAsync(
            IEnumerable<UploadedFile> files,
            string endpoint,
            string actionJson = "{}",
            CancellationToken ct = default)
        {
            // http client com “verify=False” equivalente
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            };
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(15) };

            foreach (var file in files)
            {
                OnChange?.Invoke();
                if (ct.IsCancellationRequested) return false;
                try
                {
                    using var response = await PostMarkFileAsync(http, endpoint, file, actionJson, ct);

                    int StatusCode = (int)response.StatusCode;

                    if (StatusCode == 413)
                    {
                        file.SetStatus("Arquivo muito grande!", FileStatus.Error);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var text = await response.Content.ReadAsStringAsync(ct);

                        try
                        {
                            // Tenta interpretar como JSON
                            var json = JsonSerializer.Deserialize<Dictionary<string, object>>(text);

                            if (json != null)
                            {
                                if (json.ContainsKey("error_code"))
                                {
                                    file.SetStatus(json["error_code"]?.ToString(), FileStatus.Error);
                                    continue;
                                }
                                if (json.ContainsKey("error"))
                                {
                                    file.SetStatus(json["error"]?.ToString(), FileStatus.Error);
                                    continue;
                                }
                            }
                        }
                        catch { }

                        file.SetStatus(text.Length > 400 ? text[..400] : text, FileStatus.Error);
                        continue;
                    }

                    var savedPath = await SaveResponseAsync(response, file.FileName, ct);
                    file.SetStatus(null, FileStatus.Success);
                }
                catch (OperationCanceledException)
                {
                    file.SetStatus("Cancelado.", FileStatus.Warning);
                    OnChange?.Invoke();
                    return false;
                }
                catch (Exception ex)
                {
                    file.SetStatus($"Erro: {ex.Message}", FileStatus.Error);
                }
            }
            OnChange?.Invoke();
            return true;
        }

        private async Task<HttpResponseMessage> PostMarkFileAsync(
            HttpClient http,
            string endpoint,
            UploadedFile file,
            string actionJson,
            CancellationToken ct)
        {
            using var form = new MultipartFormDataContent();

            // Campo "action" como JSON
            var actionContent = new StringContent(actionJson ?? "{}", Encoding.UTF8, "application/json");
            form.Add(actionContent, "action");

            // Stream garantido do UploadedFile
            await using var stream = await GetFileStreamAsync(file, ct);

            if (stream.Length == 0)
                throw new InvalidOperationException($"Arquivo vazio no cliente: {file.FileName}");

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");

            form.Add(fileContent, "file", file.FileName);

            System.Diagnostics.Debug.WriteLine($"Enviando {file.FileName} ({stream.Length} bytes) para {endpoint}");

            return await http.PostAsync(endpoint, form, ct);
        }


        private static async Task<string> SaveResponseAsync(HttpResponseMessage resp, string originalName, CancellationToken ct)
        {
            var downloadsDir = FileHelper.GetDownloadsPath();
            Directory.CreateDirectory(downloadsDir);

            var name = Path.GetFileNameWithoutExtension(originalName);
            var ext = Path.GetExtension(originalName);
            var outPath = Path.Combine(downloadsDir, $"{name}-processed{ext}");

            await using var output = File.Create(outPath);
            await resp.Content.CopyToAsync(output, ct);

            System.Diagnostics.Debug.WriteLine($"✅ Arquivo salvo em: {outPath}");
            return outPath;
        }

        private static async Task<Stream> GetFileStreamAsync(UploadedFile uf, CancellationToken ct)
        {
            try
            {
                var bytes = await uf.GetContentAsync();
                // Retorna um MemoryStream seguro e reposicionável
                var stream = new MemoryStream(bytes.ToArray(), writable: false);
                if (stream.CanSeek)
                    stream.Position = 0;

                return stream;
            }
            catch
            {
                return new MemoryStream();
            }

        }


        public static class FileHelper
        {
            public static string GetDownloadsPath()
            {
#if WINDOWS
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads"
                    );
#elif ANDROID
                    return Android.OS.Environment.GetExternalStoragePublicDirectory(
                        Android.OS.Environment.DirectoryDownloads
                    )!.AbsolutePath;
#elif MACCATALYST || MACOS
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads"
                    );
#elif IOS
                return FileSystem.AppDataDirectory;
#else
                    // fallback genérico
                    return FileSystem.AppDataDirectory;
#endif
            }
        }

    }
}
