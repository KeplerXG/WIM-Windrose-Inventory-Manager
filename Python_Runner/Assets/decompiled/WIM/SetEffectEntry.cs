using System.Text.Json.Serialization;

namespace WIM;

public class SetEffectEntry
{
	[JsonPropertyName("name")]
	public string NameEn { get; set; } = "";

	[JsonPropertyName("description")]
	public string DescriptionEn { get; set; } = "";

	public int ActivationCount { get; set; }

	[JsonIgnore]
	public string NameRu { get; set; } = "";

	[JsonIgnore]
	public string DescriptionRu { get; set; } = "";

	[JsonIgnore]
	public string Name => NameEn;

	[JsonIgnore]
	public string Description => DescriptionEn;
}
