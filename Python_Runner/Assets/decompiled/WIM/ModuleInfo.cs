namespace WIM;

public class ModuleInfo
{
	public int Index { get; set; }

	public int Capacity { get; set; }

	public int Used { get; set; }

	public int Free => Capacity - Used;

	public string Label { get; set; } = "";

	public string DisplayLabel
	{
		get
		{
			if (string.IsNullOrEmpty(Label))
			{
				return $"Module {Index}";
			}
			return Label;
		}
	}
}
