using Orthography.Enums;

namespace Orthography
{
	public class ModeComboBoxItem
	{
		public Mode Mode { get; private set; }
		public int ModeId
		{
			get => (int)Mode;
			set => Mode = (Mode)value;
		}
		public string Name { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}
