using System.Collections.Generic;
using System.Xml.Serialization;
using LOR_DiceSystem;

namespace Mod.XmlExtended {
	[XmlRoot("DiceCardXmlRoot")]
	public class DiceCardXmlRoot_Extended : DiceCardXmlRoot, IXmlExtended {
		[XmlElement("Version")]
		public string version = "1.1";

		// [XmlElement("Card")]
		// new public List<DiceCardXmlInfo_Extended> cardXmlList = new List<DiceCardXmlInfo_Extended>();

		public static XmlAttributeOverrides GetAttributeOverrides() {
			XmlAttributeOverrides overrides = new XmlAttributeOverrides();
			overrides.Add(typeof(DiceCardXmlRoot), "DiceCardXmlRoot", new XmlAttributes { XmlIgnore = true });
			overrides.Add(typeof(DiceCardXmlInfo), "DiceCardXmlInfo", new XmlAttributes { XmlIgnore = true });
			return overrides;
		}
	}
	[XmlRoot("BookXmlRoot")]
	public class BookXmlRoot_Extended : BookXmlRoot, IXmlExtended {
		[XmlElement("Book")]
		public List<BookXmlInfo_Extended> bookXmlList_extended;

		public static XmlAttributeOverrides GetAttributeOverrides() {
			XmlAttributeOverrides overrides = new XmlAttributeOverrides();
			overrides.Add(typeof(BookXmlRoot), "BookXmlRoot", new XmlAttributes { XmlIgnore = true });
			overrides.Add(typeof(BookXmlRoot), "bookXmlList", new XmlAttributes { XmlIgnore = true });
			overrides.Add(typeof(BookXmlInfo), "EquipEffect", new XmlAttributes { XmlIgnore = true });
			overrides.Add(typeof(BookEquipEffect), "OnlyCard", new XmlAttributes { XmlIgnore = true });
			return overrides;
		}
	}
	public class BookXmlInfo_Extended : BookXmlInfo {
		[XmlElement("EquipEffect")]
		public BookEquipEffect_Extended EquipEffect_Extended = new BookEquipEffect_Extended();
	}
	public class BookEquipEffect_Extended : BookEquipEffect {
		[XmlElement("OnlyCard")]
		public List<LorIdXml> OnlyCard_Extended = new List<LorIdXml>();
		[XmlIgnore]
		public List<LorId> OnlyCard_Serialized = new List<LorId>();
	}
	
	[XmlType("DiceCardXmlInfo")]
	public class DiceCardXmlInfo_Extended : DiceCardXmlInfo {
		[XmlAttribute("Pid")]
		public string pid;
	}

	[XmlRoot("EmotionCardXmlRoot")]
	public class EmotionCardXmlRoot_Extended : EmotionCardXmlRoot, IXmlExtended {
		[XmlElement("EmotionCard")]
		public List<EmotionCardXmlInfo_Extended> emotionCardXmlList_extended;

		public static XmlAttributeOverrides GetAttributeOverrides() {
			XmlAttributeOverrides overrides = new XmlAttributeOverrides();
			overrides.Add(typeof(EmotionCardXmlRoot), "EmotionCardXmlRoot", new XmlAttributes { XmlIgnore = true });
			overrides.Add(typeof(EmotionCardXmlInfo), "EmotionCardXmlInfo", new XmlAttributes { XmlIgnore = true });
			overrides.Add(typeof(List<EmotionCardXmlInfo>), "emotionCardXmlList", new XmlAttributes { XmlIgnore = true });
			return overrides;
		}
	}

	public class EmotionCardXmlInfo_Extended : EmotionCardXmlInfo {
		[XmlAttribute("Pid")]
		public string pid;
		[XmlIgnore]
		public LorId lorId;
	}
}