using OPSSHAssistant.Core.Data;

namespace OPSSHAssistant.Core;

public class PreparedExport
{
    public bool Success { get; set; } = false;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorMessageDetails { get; set; } = string.Empty;
    public List<Item> PublicKeysToExport { get; set; } = new List<Item>();
    public bool SSHConfigToBeCreated { get; set; } = false;
    public string SSHConfigToAppend { get; set; } = string.Empty;
    public bool AgentTomlToBeCreated { get; set; } = false;
    public string AgentTomlToAppend { get; set; } = string.Empty;
}