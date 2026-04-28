namespace WIM;

public static class SlotConstraints
{
	private static readonly string[] _armorEn = new string[5] { "Helmet", "Chestplate", "Pants", "Gloves", "Boots" };

	private static readonly string[] _accessoryEn = new string[3] { "Ring", "Necklace", "Bag" };

	private static readonly string[] _ammoEn = new string[2] { "Bullets", "Powder" };

	public static int ArmorSlotCount => _armorEn.Length;

	public static int AccessorySlotCount => _accessoryEn.Length;

	public static int AmmoSlotCount => _ammoEn.Length;

	public static string? GetArmorName(int i)
	{
		if (i < 0 || i >= _armorEn.Length)
		{
			return null;
		}
		return _armorEn[i];
	}

	public static string? GetAccessoryName(int i)
	{
		if (i < 0 || i >= _accessoryEn.Length)
		{
			return null;
		}
		return _accessoryEn[i];
	}

	public static string? GetAmmoName(int i)
	{
		if (i < 0 || i >= _ammoEn.Length)
		{
			return null;
		}
		return _ammoEn[i];
	}
}
