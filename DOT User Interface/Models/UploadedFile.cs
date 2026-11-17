using Microsoft.AspNetCore.Components.Forms;

public class UploadedFile(IBrowserFile file)
{
    private readonly IBrowserFile? _sourceFile = file;
    private byte[]? _cachedContent;

    public string FileName { get; } = file.Name;
    public string ContentType { get; } = file.ContentType;
    public string? Errors { get; set; }
    public FileStatus Status { get; private set; } = FileStatus.Normal;

    public async Task<ReadOnlyMemory<byte>> GetContentAsync()
    {
        if (_cachedContent is not null) return _cachedContent;
        if (_sourceFile is null) throw new InvalidOperationException("Nenhum arquivo fonte associado.");

        try
        {
            using var stream = _sourceFile.OpenReadStream(50 * 1024 * 1024);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms).ConfigureAwait(false);
            _cachedContent = ms.ToArray();
            return _cachedContent; // implícito para ReadOnlyMemory<byte>
        }
        catch
        {
            Status = FileStatus.Error;
            throw;
        }
    }

    public void SetStatus(string? message, FileStatus status = FileStatus.Normal)
    {
        if (Status <= status)
        {
            Status = status;
            if (message == null && Errors != null) return;
            Errors = message;
        }
    }

    public void ResetStatus()
    {
        Status = FileStatus.Normal;
        Errors = null;
    }
}

public enum FileStatus
{
    Normal,   // estado padrão
    Success,  // processado com sucesso
    Warning,  // aviso ou validação parcial
    Error     // erro grave
}
