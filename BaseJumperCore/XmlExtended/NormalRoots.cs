using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using LOR_XML;

namespace Mod.XmlExtended {
	[Obsolete("Deprecated All-In-One XML Root")]
	public partial class AllRoots {
		[XmlElement("Version")]
		public string version = "1.1";
		[XmlElement("Stage")]
		public List<StageClassInfo> Stage;
		[XmlElement("Enemy")]
		public List<EnemyUnitClassInfo> Enemy;
		[XmlElement("Book")]
		public List<BookXmlInfo> Book;
		[XmlElement("BookUse")]
		public List<DropBookXmlInfo> BookUse;
		[XmlElement("DropTable")]
		public List<CardDropTableXmlInfo> DropTable;
		[XmlElement("Card")]
		public List<DiceCardXmlInfo_Extended> Card;
		[XmlElement("Deck")]
		public List<DeckXmlInfo> Deck;
		[XmlElement("GroupName")]
		public string GroupName;

		[XmlElement("Character")]
		public List<BattleDialogCharacter> Character = new List<BattleDialogCharacter>();
		public List<BookDesc> bookDescList;
		[XmlElement("Passive")]
		public List<PassiveXmlInfo> Passive;
		[XmlElement("BattleCardAbility")]
		public List<BattleCardAbilityDesc> BattleCardAbility;
	}
}