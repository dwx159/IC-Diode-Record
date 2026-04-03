using System.Text.Json;

namespace IC_Diode_Record
{
    /// <summary>Azure 语音服务（免费层 F0：需注册 Azure 免费账号，每月有免费识别额度）。</summary>
    internal sealed class AzureVoiceSettings
    {
        public string SubscriptionKey { get; set; } = "";
        public string Region { get; set; } = "eastasia";

        private static string ConfigPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "IC_Diode_Record",
                "azure_voice.json");

        public static AzureVoiceSettings Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    return new AzureVoiceSettings();
                var json = File.ReadAllText(ConfigPath);
                var o = JsonSerializer.Deserialize<AzureVoiceSettings>(json);
                return o ?? new AzureVoiceSettings();
            }
            catch
            {
                return new AzureVoiceSettings();
            }
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SubscriptionKey) && !string.IsNullOrWhiteSpace(Region);
    }
}
