namespace OPSSHAssistant.Core;

public class ExportResult
{
    public bool PublicKeyGenerationSuccess { get; set; } = false;
    public bool AppendSSHConfigSuccess { get; set; } = false;
    public bool AppendAgentTomlSuccess { get; set; } = false;
    
    public string PublicKeyGenerationSummary { get; set; } = string.Empty;
    public string SSHConfigSummary { get; set; } = string.Empty;
    public string AgentTomlSummary { get; set; } = string.Empty;
    
    
    
}