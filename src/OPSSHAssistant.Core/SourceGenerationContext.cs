using System.Text.Json.Serialization;
using OPSSHAssistant.Core.Data;

namespace OPSSHAssistant.Core;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<Vault>))]
[JsonSerializable(typeof(List<Account>))]
[JsonSerializable(typeof(List<Item>))]
[JsonSerializable(typeof(PublicKey))]
public partial class SourceGenerationContext : JsonSerializerContext
{

}
