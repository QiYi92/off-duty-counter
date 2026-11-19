using System.Text.Json;

namespace OffDutyCounter;

internal sealed record Preferences(Schedule? Schedule = null, int Theme = 0, bool Topmost = false, bool Compact = false)
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OffDutyCounter", "settings.json");

    public static Preferences Load()
    {
        try { return JsonSerializer.Deserialize<Preferences>(File.ReadAllText(FilePath)) ?? new(); }
        catch (IOException) { return new(); }
        catch (UnauthorizedAccessException) { return new(); }
        catch (JsonException) { return new(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(this));
            File.Move(FilePath + ".tmp", FilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show("设置无法保存，本次运行仍可使用。", "牛马计数器");
        }
    }
}
