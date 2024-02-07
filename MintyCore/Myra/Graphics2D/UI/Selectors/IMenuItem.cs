using System.ComponentModel;
using System.Xml.Serialization;
using MintyCore.Myra.MML;

namespace MintyCore.Myra.Graphics2D.UI.Selectors
{
	public interface IMenuItem: IItemWithId
	{
		[Browsable(false)]
		[XmlIgnore]
		Menu Menu { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		char? UnderscoreChar { get; }

		[Browsable(false)]
		[XmlIgnore]
		int Index { get; set; }
	}
}
