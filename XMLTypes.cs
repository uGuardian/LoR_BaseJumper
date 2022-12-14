using System;

namespace BaseJumperAPI {
	// This is for convienience, and is not actually a shared type between the assemblies, and must be parsed as a ulong.
	[Flags] public enum XMLTypes : ulong {
		NONE =				0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000,

		// Default Serializers (Currently unused entries are reserved)
	//	Stage =				0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0001,
	//	EnemyUnit =			0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0010,
		EquipPage =			0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0100,
	//	Book =				0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0100,
	//	CardDropTable =		0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_1000,
	//	DropBook =			0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0001_0000,
	//	BookUse =			0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0001_0000,
	//	CardInfo =			0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0010_0000,
	//	DiceCard =			0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0010_0000,
	//	Deck =				0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0100_0000,
	//	BattleDialog =		0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_1000_0000,
	//	BookStory =			0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0001_0000_0000,
	//	BookDesc =			0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0001_0000_0000,
	//	Passive =			0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0010_0000_0000,

		// Custom Serializers
	//	EmotionCard =		0b_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0100_0000_0000,

		// All Serializers implemented in current version
		ALL =
	//	Stage |
	//	EnemyUnit |
		EquipPage
	//	CardDropTable |
	//	DropBook |
	//	CardInfo |
	//	Deck |
	//	BattleDialog |
	//	BookStory |
	//	Passive |

	//	EmotionCard
	}
}