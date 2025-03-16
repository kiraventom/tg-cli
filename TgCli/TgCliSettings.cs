using SettingsManagement;

namespace TgCli;

#pragma warning disable CS0169

public partial class TgCliSettings : ISettings
{
    [SaveOnChange] private int _separatorOffset;
}

#pragma warning restore CS0169
