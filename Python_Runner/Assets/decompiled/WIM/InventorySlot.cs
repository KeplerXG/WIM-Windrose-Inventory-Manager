using System.Linq;

namespace WIM;

public class InventorySlot
{
	public int ModuleIndex { get; set; }

	public int SlotIndex { get; set; }

	public string ItemParams { get; set; } = "";

	public string ItemId { get; set; } = "";

	public int Level { get; set; }

	public int MaxLevel { get; set; } = 15;

	public int Quality { get; set; }

	public int Count { get; set; } = 1;

	/// <summary>True when there is no item path (or only whitespace) in the save data.</summary>
	public bool IsEmpty => string.IsNullOrWhiteSpace(ItemParams);

	/// <summary>True when the cell should look empty: no path, or non-positive stack count.</summary>
	public static bool IsVisuallyEmpty(InventorySlot s) => string.IsNullOrWhiteSpace(s.ItemParams) || s.Count < 1;

	public string InternalName
	{
		get
		{
			if (!ItemParams.Contains('/'))
			{
				return ItemParams;
			}
			return ItemParams.Split('/').Last().Split('.')
				.First();
		}
	}
}
