namespace IC_Diode_Record
{
    /// <summary>填写 Azure 语音资源密钥与区域（免费层 F0）。</summary>
    internal sealed class VoiceAzureConfigForm : Form
    {
        private readonly TextBox _keyBox;
        private readonly TextBox _regionBox;

        public VoiceAzureConfigForm(AzureVoiceSettings current)
        {
            Text = "Azure 语音（免费层）";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(440, 220);

            var tip = new Label
            {
                AutoSize = false,
                Location = new Point(12, 8),
                Size = new Size(416, 44),
                Text = "在 Azure 门户创建「语音」资源，复制密钥与区域。免费层 F0 有每月免费额度（以官网为准）。\r\n配置文件保存在 %AppData%\\IC_Diode_Record\\azure_voice.json"
            };

            var keyLabel = new Label { AutoSize = true, Location = new Point(12, 58), Text = "密钥" };
            _keyBox = new TextBox
            {
                Location = new Point(12, 82),
                Size = new Size(416, 32),
                UseSystemPasswordChar = true,
                Text = current.SubscriptionKey
            };

            var regionLabel = new Label { AutoSize = true, Location = new Point(12, 118), Text = "区域（如 eastasia、chinaeast2）" };
            _regionBox = new TextBox
            {
                Location = new Point(12, 142),
                Size = new Size(200, 32),
                Text = string.IsNullOrWhiteSpace(current.Region) ? "eastasia" : current.Region
            };

            var ok = new Button { Text = "保存", DialogResult = DialogResult.OK, Location = new Point(248, 182), Size = new Size(88, 32) };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(340, 182), Size = new Size(88, 32) };
            AcceptButton = ok;
            CancelButton = cancel;

            Controls.Add(tip);
            Controls.Add(keyLabel);
            Controls.Add(_keyBox);
            Controls.Add(regionLabel);
            Controls.Add(_regionBox);
            Controls.Add(ok);
            Controls.Add(cancel);
        }

        public AzureVoiceSettings GetSettings()
        {
            return new AzureVoiceSettings
            {
                SubscriptionKey = _keyBox.Text.Trim(),
                Region = _regionBox.Text.Trim()
            };
        }
    }
}
