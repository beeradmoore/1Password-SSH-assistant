using System.Text.Json.Serialization;
using SSHAssistantFor1Password.Core.Data;

namespace SSHAssistantFor1Password.Core;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<Vault>))]
[JsonSerializable(typeof(List<Account>))]
[JsonSerializable(typeof(List<Item>))]
[JsonSerializable(typeof(PublicKey))]
public partial class SourceGenerationContext : JsonSerializerContext
{

}
