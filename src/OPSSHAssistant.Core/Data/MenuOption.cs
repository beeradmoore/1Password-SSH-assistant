using OPSSHAssistant.Core.Enums;

namespace OPSSHAssistant.Core.Data;

public class MenuOption
{
    public string Name { get; init; }
    public MenuMode Mode { get; init; }

    public MenuOption(MenuMode mode)
    {
        Mode = mode;
        Name = mode switch
        {
            MenuMode.ExportPPK => "Export .ppk from SSH key",
            MenuMode.ExportPubAppendSSHConfigAndAgentToml => "Export .pub, append .ssh config and agent.toml",
            _ => throw new NotImplementedException(),
        };
    }
}