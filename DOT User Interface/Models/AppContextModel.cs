using DOT_User_Interface.Models.Tables;
using System.Text.Json;

namespace DOT_User_Interface.Models
{
    public enum MainPages
    {
        Home,
        Marker,
        Editor,
        Translator,
        Help,
        Generator
    }
    public enum PageTabs
    {
        Home,
        Journal,
        Documentation,
        Create,
        Processing,
        Notifications,
        Settings
    }

    public class AppContextModel
    {
        public AppContextModel(I18n I18n)
        {
            Processor = new ProcessFiles();
            Processor.OnChange += () => OnChange?.Invoke();
            this.I18n = I18n;
        }
        public I18n I18n { get; private set; }
        public event Action? OnChange;
        public List<UploadedFile> SelectedFiles = new();
        public bool HasFiles => SelectedFiles.Count > 0;
        public TableFactory? TableFactory;
        public void UpdateInterface() => OnChange?.Invoke();

        private const string NotificationsFileName = "notifications.json";
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        // Notifications (iniciadas vazias; boas-vindas só se for a primeira execução)
        public List<Notification> Notifications { get; private set; } = new();

        public int NotificationsUnreaded
        {
            get
            {
                int count = 0;
                foreach (var notification in Notifications)
                {
                    if (!notification.IsRead)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        // Pages
        public MainPages Page { get; private set; } = MainPages.Home;
        public void PageHome() => Page = MainPages.Home;
        public void PageMarker() => Page = MainPages.Marker;
        public void PageEditor() => Page = MainPages.Editor;
        public void PageTranslator() => Page = MainPages.Translator;
        public void PageHelp() => Page = MainPages.Help;
        public void PageGenerator() => Page = MainPages.Generator;

        // Tabs (Sub-Páginas)
        public PageTabs PageTab { get; set; } = PageTabs.Home;
        public void SetupPageTabs(List<PageTabs> options)
        {
            if (options.Contains(PageTab))
            {
                return;
            }
            PageTab = PageTabs.Home;
        }
        public void PageTabHome()
        {
            PageTab = PageTabs.Home;
            OnChange?.Invoke();
        }
        public void PageTabOptions()
        {
            PageTab = PageTabs.Journal;
            OnChange?.Invoke();
        }
        public void PageTabProcessing()
        {
            PageTab = PageTabs.Processing;
            OnChange?.Invoke();
        }
        public void PageTabNotifications()
        {
            PageTab = PageTabs.Notifications;
            OnChange?.Invoke();
        }
        public void PageTabSetting()
        {
            PageTab = PageTabs.Settings;
            OnChange?.Invoke();
        }
        public void PageTabCreate()
        {
            PageTab = PageTabs.Create;
            OnChange?.Invoke();
        }
        public void PageTabDocumentation()
        {
            PageTab = PageTabs.Documentation;
            OnChange?.Invoke();
        }

        // --- Persistência de notificações ---

        private string GetNotificationsFilePath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, NotificationsFileName);
        }

        public async Task LoadNotificationsAsync()
        {
            try
            {
                var path = GetNotificationsFilePath();

                // Se não existir arquivo, é a primeira execução: criar notificação de boas-vindas e persistir.
                if (!File.Exists(path))
                {
                    Notifications = new List<Notification>
                    {
                        new() { Title = I18n["AppContextModel.Welcome"], Message = I18n["AppContextModel.NotificationsPlaceholder"], DateCreated = DateTime.Now, IsRead = false },
                    };
                    await SaveNotificationsAsync().ConfigureAwait(false);
                    OnChange?.Invoke();
                    return;
                }

                var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                var loaded = JsonSerializer.Deserialize<List<Notification>>(json, _jsonOptions);
                // Se o arquivo existir mas o conteúdo for uma lista vazia, mantemos a lista vazia (não adicionamos a boas-vindas).
                Notifications = loaded ?? [];
                OnChange?.Invoke();
            }
            catch
            {
                // Falha ao carregar: manter Notifications como está (vazia ou anterior).
            }
        }

        public async Task SaveNotificationsAsync()
        {
            try
            {
                var path = GetNotificationsFilePath();
                var json = JsonSerializer.Serialize(Notifications, _jsonOptions);
                await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
            }
            catch
            {
                // Falha ao salvar: ignorar (pode-se adicionar log conforme necessário).
            }
        }

        // Métodos que manipulam a lista e garantem persistência
        public void AddNotification(Notification notification)
        {
            if (notification == null) return;
            notification.DateCreated = notification.DateCreated == default ? DateTime.Now : notification.DateCreated;
            Notifications.Insert(0, notification);
            _ = SaveNotificationsAsync();
            OnChange?.Invoke();
        }

        public void RemoveAllNotification()
        {
            Notifications = [];
            _ = SaveNotificationsAsync();
            OnChange?.Invoke();
        }

        public void RemoveNotification(Notification notification)
        {
            if (notification == null) return;
            Notifications.Remove(notification);
            _ = SaveNotificationsAsync();
            OnChange?.Invoke();
        }

        public void MarkNotificationAsRead(Notification notification)
        {
            if (notification == null) return;
            var idx = Notifications.IndexOf(notification);
            if (idx < 0) return;
            if (!Notifications[idx].IsRead)
            {
                Notifications[idx].IsRead = true;
                _ = SaveNotificationsAsync();
                OnChange?.Invoke();
            }
        }

        public async Task MarkAllAsReadAsync()
        {
            var changed = false;
            foreach (var n in Notifications)
            {
                if (!n.IsRead)
                {
                    n.IsRead = true;
                    changed = true;
                }
            }
            if (changed)
            {
                await SaveNotificationsAsync().ConfigureAwait(false);
                OnChange?.Invoke();
            }
        }

        public async Task ClearNotificationsAsync()
        {
            Notifications.Clear();
            await SaveNotificationsAsync().ConfigureAwait(false);
            OnChange?.Invoke();
        }

        // Processamento
        private bool closeProcessing = true;
        public bool CloseProcessing
        {
            get
            {
                if (Processing)
                {
                    closeProcessing = false;
                }
                
                if (closeProcessing)
                {
                    foreach (var file in SelectedFiles)
                    {
                        file.ResetStatus();
                    }
                }
                return closeProcessing;
            }
            set
            {
                if (Processing)
                {
                    return;
                }
                closeProcessing = value;
            }
        }
        public bool Processing { get; set; } = false;
        public bool StopProcessing { get; set; } = false;
        public ProcessFiles Processor { get; }

        // Cancellation
        private CancellationTokenSource? _cts;

        public async Task ProcessFilesAsync(
            string endpoint,
            string actionJson = "{}",
            CancellationToken ct = default)
        {
            if (Processing) return;
            if (SelectedFiles.Count == 0)
            {
                AddNotification(new Notification { Title = I18n["AppContextModel.NoFiles"], Message = I18n["AppContextModel.SelectAtLeastOneFile"], IsRead = false });
                return;
            }

            Processing = true;
            StopProcessing = false;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            UpdateInterface();

            try
            {
                bool result = await Processor.ProcessAsync(SelectedFiles, endpoint, actionJson, _cts.Token);
                if (result)
                    AddNotification(new Notification { Title = I18n["AppContextModel.Processed"], Message = I18n["AppContextModel.AllFilesProcessed"], IsRead = false });
            }
            catch (OperationCanceledException)
            {
                AddNotification(new Notification { Title = I18n["AppContextModel.Canceled"], Message = I18n["AppContextModel.ProcessingCanceled"], IsRead = false });
            }
            finally
            {
                Processing = false;
                StopProcessing = false;
                try { _cts?.Dispose(); } catch { }
                _cts = null;
                UpdateInterface();
            }
        }

        public void CancelProcessing()
        {
            if (_cts == null) return;
            StopProcessing = true;
            try
            {
                _cts.Cancel();
            }
            catch { }
            UpdateInterface();
        }
    }
}
