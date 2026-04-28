using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WIM;

public class ItemEntry
{
	public string Filename { get; set; } = "";

	[JsonPropertyName("display_name")]
	public string DisplayNameEn { get; set; } = "";

	[JsonIgnore]
	public string DisplayNameRu { get; set; } = "";

	[JsonPropertyName("description")]
	public string DescriptionEn { get; set; } = "";

	[JsonIgnore]
	public string DescriptionRu { get; set; } = "";

	[JsonPropertyName("vanity_text")]
	public string VanityTextEn { get; set; } = "";

	[JsonIgnore]
	public string VanityTextRu { get; set; } = "";

	[JsonIgnore]
	public string DisplayName => DisplayNameEn;

	[JsonIgnore]
	public string Description => DescriptionEn;

	[JsonIgnore]
	public string VanityText => VanityTextEn;

	public string ItemTag { get; set; } = "";

	public string ItemType { get; set; } = "";

	public string Rarity { get; set; } = "";

	public string Category { get; set; } = "";

	public string IconRef { get; set; } = "";

	public string ItemParamsPath { get; set; } = "";

	public string MainStat { get; set; } = "";

	public List<string> SecondaryStats { get; set; } = new List<string>();

	public int MaxLevel { get; set; }

	public int MaxQualityLevel { get; set; }

	public int MaxCountInSlot { get; set; } = 9999;

	public float Weight { get; set; }

	public bool KeepOnDeath { get; set; }

	public StatCurveRef? MainStatCurve { get; set; }

	public List<StatCurveRef>? SecondaryStatCurves { get; set; }

	public List<StatCurveRef>? AddlStatCurves { get; set; }

	public List<DescCurveRef>? DescCurves { get; set; }

	public List<string>? EffectsTexts { get; set; }

	[JsonIgnore]
	public List<string>? EffectsTextsRu { get; set; }

	public List<SetEffectEntry>? SetEffects { get; set; }

	[JsonIgnore]
	public List<string>? EffectsTextsLocalized => EffectsTexts;
}
