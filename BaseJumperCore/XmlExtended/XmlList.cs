using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Mod.XmlExtended {
	[Obsolete("Unused")]
	[Flags]
	public enum XmlList {
		stageFile			= 0b_0000_0000_0001,
		enemyUnitFile		= 0b_0000_0000_0010,
		enemyEquipPage		= 0b_0000_0000_0100,
		librarianEquipPage	= 0b_0000_0000_1000,
		dropBookFile		= 0b_0000_0001_0000,
		cardDropTableFile	= 0b_0000_0010_0000,
		combatPageFile		= 0b_0000_0100_0000,
		enemyDeckFile		= 0b_0000_1000_0000,
		dialogFile			= 0b_0001_0000_0000,
		bookStoryFile		= 0b_0010_0000_0000,
		passiveFile			= 0b_0100_0000_0000,
		cardAbilityFile		= 0b_1000_0000_0000,
		UNKNOWN				= 0b_0000_0000_0000,
		AUTO				= 0b_0000_0000_0000,
	}
}
