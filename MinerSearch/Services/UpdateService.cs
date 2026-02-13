using MSearch.Models;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;

namespace MSearch.Services;

public class UpdateService : IUpdateService
{
    private readonly IConfigService _configService;
    private readonly HttpClient _httpClient;
    
    private const string ApiBaseUrl = "https://api.minersearch.com/v1";
    
    public string CurrentDatabaseVersion { get; private set; } = "1.0.0";
    public DateTime LastUpdateCheck { get; private set; }
    
    public event EventHandler<SignatureDatabase>? DatabaseUpdated;

    public UpdateService(IConfigService configService)
    {
        _configService = configService;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        CurrentDatabaseVersion = _configService.GetValue("signatureVersion", "1.0.0");
    }

    public async Task<SignatureDatabase?> CheckForUpdatesAsync()
    {
        try
        {
            App.Logger?.Information("Checking for signature updates...");
            
            var url = $"{ApiBaseUrl}/signatures?current={CurrentDatabaseVersion}";
            var response = await _httpClient.GetAsync(url);
            
            LastUpdateCheck = DateTime.Now;
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
            {
                App.Logger?.Information("Signatures are up to date");
                return null;
            }
            
            if (!response.IsSuccessStatusCode)
            {
                App.Logger?.Warning("Failed to check updates: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var json = await response.Content.ReadAsStringAsync();
            var database = JsonConvert.DeserializeObject<SignatureDatabase>(json);
            
            if (database != null && IsNewerVersion(database.Version, CurrentDatabaseVersion))
            {
                App.Logger?.Information("New signature database available: {Version}", database.Version);
                return database;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            App.Logger?.Error(ex, "Error checking for updates");
            return null;
        }
    }

    public async Task<bool> DownloadUpdateAsync(SignatureDatabase database)
    {
        try
        {
            App.Logger?.Information("Downloading signature database {Version}...", database.Version);
            
            // Save to local storage
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MinerSearch", "signatures.json");
            
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            
            var json = JsonConvert.SerializeObject(database, Formatting.Indented);
            await File.WriteAllTextAsync(dbPath, json);
            
            CurrentDatabaseVersion = database.Version;
            _configService.SetValue("signatureVersion", database.Version);
            
            DatabaseUpdated?.Invoke(this, database);
            
            App.Logger?.Information("Signature database updated to {Version}", database.Version);
            return true;
        }
        catch (Exception ex)
        {
            App.Logger?.Error(ex, "Error downloading update");
            return false;
        }
    }

    private bool IsNewerVersion(string newVersion, string currentVersion)
    {
        try
        {
            var newParts = newVersion.Split('.').Select(int.Parse).ToArray();
            var currentParts = currentVersion.Split('.').Select(int.Parse).ToArray();
            
            for (int i = 0; i < Math.Max(newParts.Length, currentParts.Length); i++)
            {
                var newPart = i < newParts.Length ? newParts[i] : 0;
                var currentPart = i < currentParts.Length ? currentParts[i] : 0;
                
                if (newPart > currentPart) return true;
                if (newPart < currentPart) return false;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
}
