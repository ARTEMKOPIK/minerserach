using MSearch.Models;
using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MSearch.Services;

public class QuarantineService : IQuarantineService
{
    private readonly string _quarantinePath;
    private readonly string _indexPath;
    private List<QuarantineItem> _items = new();

    public QuarantineService()
    {
        _quarantinePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MinerSearch", "Quarantine");
        
        _indexPath = Path.Combine(_quarantinePath, "index.json");
        
        Directory.CreateDirectory(_quarantinePath);
        LoadIndex();
    }

    private void LoadIndex()
    {
        try
        {
            if (File.Exists(_indexPath))
            {
                var json = File.ReadAllText(_indexPath);
                _items = JsonConvert.DeserializeObject<List<QuarantineItem>>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            App.Logger?.Error(ex, "Failed to load quarantine index");
            _items = new();
        }
    }

    private void SaveIndex()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_items, Formatting.Indented);
            File.WriteAllText(_indexPath, json);
        }
        catch (Exception ex)
        {
            App.Logger?.Error(ex, "Failed to save quarantine index");
        }
    }

    public Task<List<QuarantineItem>> GetQuarantineItemsAsync()
    {
        return Task.FromResult(_items.ToList());
    }

    public async Task<bool> QuarantineFileAsync(ThreatInfo threat)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(threat.FilePath))
                {
                    App.Logger?.Warning("File not found for quarantine: {Path}", threat.FilePath);
                    return false;
                }

                var quarantineId = Guid.NewGuid().ToString();
                var quarantinedFileName = $"{quarantineId}.quar";
                var quarantinedPath = Path.Combine(_quarantinePath, quarantinedFileName);

                // Move file to quarantine
                File.Move(threat.FilePath, quarantinedPath);

                // Create quarantine item
                var item = new QuarantineItem
                {
                    Id = quarantineId,
                    OriginalPath = threat.FilePath,
                    QuarantinePath = quarantinedPath,
                    FileName = threat.FileName,
                    FileSize = new FileInfo(quarantinedPath).Length,
                    ThreatType = threat.Type,
                    QuarantinedAt = DateTime.Now
                };

                _items.Add(item);
                SaveIndex();

                threat.Status = QuarantineStatus.Quarantined;
                threat.QuarantinePath = quarantinedPath;

                App.Logger?.Information("File quarantined: {OriginalPath} -> {QuarantinePath}", 
                    threat.FilePath, quarantinedPath);
                
                return true;
            }
            catch (Exception ex)
            {
                App.Logger?.Error(ex, "Failed to quarantine file: {Path}", threat.FilePath);
                return false;
            }
        });
    }

    public async Task<bool> RestoreFileAsync(string quarantineId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var item = _items.FirstOrDefault(x => x.Id == quarantineId);
                if (item == null)
                {
                    App.Logger?.Warning("Quarantine item not found: {Id}", quarantineId);
                    return false;
                }

                if (!File.Exists(item.QuarantinePath))
                {
                    App.Logger?.Warning("Quarantined file not found: {Path}", item.QuarantinePath);
                    return false;
                }

                // Create original directory if needed
                var originalDir = Path.GetDirectoryName(item.OriginalPath);
                if (!string.IsNullOrEmpty(originalDir) && !Directory.Exists(originalDir))
                {
                    Directory.CreateDirectory(originalDir);
                }

                // Restore file
                File.Move(item.QuarantinePath, item.OriginalPath);

                _items.Remove(item);
                SaveIndex();

                App.Logger?.Information("File restored: {OriginalPath}", item.OriginalPath);
                return true;
            }
            catch (Exception ex)
            {
                App.Logger?.Error(ex, "Failed to restore file: {Id}", quarantineId);
                return false;
            }
        });
    }

    public async Task<bool> DeleteFileAsync(string quarantineId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var item = _items.FirstOrDefault(x => x.Id == quarantineId);
                if (item == null)
                {
                    App.Logger?.Warning("Quarantine item not found: {Id}", quarantineId);
                    return false;
                }

                if (File.Exists(item.QuarantinePath))
                {
                    File.Delete(item.QuarantinePath);
                }

                _items.Remove(item);
                SaveIndex();

                App.Logger?.Information("File deleted from quarantine: {Id}", quarantineId);
                return true;
            }
            catch (Exception ex)
            {
                App.Logger?.Error(ex, "Failed to delete file from quarantine: {Id}", quarantineId);
                return false;
            }
        });
    }

    public async Task<bool> DeleteAllAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                foreach (var item in _items)
                {
                    if (File.Exists(item.QuarantinePath))
                    {
                        File.Delete(item.QuarantinePath);
                    }
                }

                _items.Clear();
                SaveIndex();

                App.Logger?.Information("All quarantine items deleted");
                return true;
            }
            catch (Exception ex)
            {
                App.Logger?.Error(ex, "Failed to delete all quarantine items");
                return false;
            }
        });
    }
}
