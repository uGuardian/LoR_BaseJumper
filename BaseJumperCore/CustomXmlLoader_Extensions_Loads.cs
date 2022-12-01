using LOR_DiceSystem;
using LOR_XML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using Workshop;
using Mod.XmlExtended;

namespace Mod {
	[Obsolete("Use XmlLoader methods instead")]
	public static partial class CustomXmlLoader_Extensions_Obsolete {

		public static List<StageClassInfo> LoadStage(this ModContent content) {
			List<StageClassInfo> stageClassInfoList = new List<StageClassInfo>();
			try {
				string dataFilePath = content.GetDataFilePath(content._stageXmlPath);
				if (File.Exists(dataFilePath)) {
					using (StreamReader streamReader = new StreamReader(dataFilePath)) {
						stageClassInfoList = (new XmlSerializer(typeof (StageXmlRoot)).Deserialize(streamReader) as StageXmlRoot).list;
						foreach (StageClassInfo stageClassInfo in stageClassInfoList) {
							stageClassInfo.workshopID = content._itemUniqueId;
							stageClassInfo.InitializeIds(content._itemUniqueId);
							foreach (StageStoryInfo story in stageClassInfo.storyList) {
								story.packageId = content._itemUniqueId;
								story.valid = true;
							}
						}
					}
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return stageClassInfoList;
		}

		public static List<EnemyUnitClassInfo> LoadEnemyUnit(this ModContent content) {
			List<EnemyUnitClassInfo> enemyUnitClassInfoList = new List<EnemyUnitClassInfo>();
			try {
				string dataFilePath = content.GetDataFilePath(content._enemyUnitXmlPath);
				if (File.Exists(dataFilePath)) {
					using (StreamReader streamReader = new StreamReader(dataFilePath)) {
						enemyUnitClassInfoList = (new XmlSerializer(typeof (EnemyUnitClassRoot)).Deserialize(streamReader) as EnemyUnitClassRoot).list;
						foreach (EnemyUnitClassInfo enemyUnitClassInfo in enemyUnitClassInfoList)
							enemyUnitClassInfo.workshopID = content._itemUniqueId;
					}
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return enemyUnitClassInfoList;
		}

		public static List<BookXmlInfo> LoadEquipLibrarianPage(this ModContent content) {
			List<BookXmlInfo> bookXmlInfoList = new List<BookXmlInfo>();
			try {
				string dataFilePath = content.GetDataFilePath(content._equipPageLibrarianXmlPath);
				if (File.Exists(dataFilePath)) {
					using (StreamReader streamReader = new StreamReader(dataFilePath)) {
						BookXmlRoot bookXmlRoot = new XmlSerializer(typeof (BookXmlRoot)).Deserialize(streamReader) as BookXmlRoot;
						bookXmlInfoList = bookXmlRoot.bookXmlList;
						foreach (BookXmlInfo bookXmlInfo in bookXmlInfoList) {
							bookXmlInfo.workshopID = content._itemUniqueId;
							if (ModContentInfo.ConvertModVer(bookXmlRoot.version) > ModContentInfo.ConvertModVer("1.0"))
								LorId.InitializeLorIds(bookXmlInfo.EquipEffect._PassiveList, bookXmlInfo.EquipEffect.PassiveList, content._itemUniqueId);
							else
								LorId.InitializeLorIds(bookXmlInfo.EquipEffect._PassiveList, bookXmlInfo.EquipEffect.PassiveList, "");
							if (!string.IsNullOrEmpty(bookXmlInfo.skinType)) {
								if (bookXmlInfo.skinType == "UNKNOWN")
									bookXmlInfo.skinType = "Lor";
								else if (bookXmlInfo.skinType == "CUSTOM")
									bookXmlInfo.skinType = "Custom";
								else if (bookXmlInfo.skinType == "LOR")
									bookXmlInfo.skinType = "Lor";
							}
						}
					}
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return bookXmlInfoList;
		}

		public static List<BookXmlInfo> LoadEquipEnemyPage(this ModContent content) {
			List<BookXmlInfo> bookXmlInfoList = new List<BookXmlInfo>();
			try {
				string dataFilePath = content.GetDataFilePath(content._equipPageEnemyXmlPath);
				if (File.Exists(dataFilePath)) {
					using (StreamReader streamReader = new StreamReader(dataFilePath)) {
						BookXmlRoot bookXmlRoot = new XmlSerializer(typeof (BookXmlRoot)).Deserialize(streamReader) as BookXmlRoot;
						bookXmlInfoList = bookXmlRoot.bookXmlList;
						foreach (BookXmlInfo bookXmlInfo in bookXmlInfoList) {
							bookXmlInfo.workshopID = content._itemUniqueId;
							if (ModContentInfo.ConvertModVer(bookXmlRoot.version) > ModContentInfo.ConvertModVer("1.0"))
								LorId.InitializeLorIds(bookXmlInfo.EquipEffect._PassiveList, bookXmlInfo.EquipEffect.PassiveList, content._itemUniqueId);
							else
								LorId.InitializeLorIds(bookXmlInfo.EquipEffect._PassiveList, bookXmlInfo.EquipEffect.PassiveList, "");
							if (!string.IsNullOrEmpty(bookXmlInfo.skinType)) {
								if (bookXmlInfo.skinType == "UNKNOWN")
									bookXmlInfo.skinType = "Lor";
								else if (bookXmlInfo.skinType == "CUSTOM")
									bookXmlInfo.skinType = "Custom";
								else if (bookXmlInfo.skinType == "LOR")
									bookXmlInfo.skinType = "Lor";
							}
						}
					}
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return bookXmlInfoList;
		}

		public static List<DropBookXmlInfo> LoadDropBook(this ModContent content) {
			List<DropBookXmlInfo> dropBookXmlInfoList = new List<DropBookXmlInfo>();
			try {
				string dataFilePath = content.GetDataFilePath(content._dropBookXmlPath);
				if (File.Exists(dataFilePath)) {
					using (StreamReader streamReader = new StreamReader(dataFilePath)) {
						dropBookXmlInfoList = (new XmlSerializer(typeof (BookUseXmlRoot)).Deserialize(streamReader) as BookUseXmlRoot).bookXmlList;
						foreach (DropBookXmlInfo dropBookXmlInfo in dropBookXmlInfoList) {
							dropBookXmlInfo.workshopID = content._itemUniqueId;
							dropBookXmlInfo.InitializeDropItemList(content._itemUniqueId);
						}
					}
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return dropBookXmlInfoList;
		}

		public static List<CardDropTableXmlInfo> LoadCardDropTable(this ModContent content) {
			List<CardDropTableXmlInfo> dropTableXmlInfoList = new List<CardDropTableXmlInfo>();
			try {
				string dataFilePath = content.GetDataFilePath(content._cardDropTableXmlPath);
				if (File.Exists(dataFilePath)) {
					using (StreamReader streamReader = new StreamReader(dataFilePath))
						dropTableXmlInfoList = (new XmlSerializer(typeof (CardDropTableXmlRoot)).Deserialize(streamReader) as CardDropTableXmlRoot).dropTableXmlList;
				}
				foreach (CardDropTableXmlInfo dropTableXmlInfo in dropTableXmlInfoList) {
					dropTableXmlInfo.workshopId = content._itemUniqueId;
					dropTableXmlInfo.cardIdList.Clear();
					LorId.InitializeLorIds<LorIdXml>(dropTableXmlInfo._cardIdList, dropTableXmlInfo.cardIdList, content._itemUniqueId);
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return dropTableXmlInfoList;
		}
		/* ANCHOR Reenable Later
		public static List<XmlExtended.DiceCardXmlInfo_Extended> LoadCardInfo(this ModContent content, System.IO.FileInfo file)
			=> LoadCardInfo(content, new StreamReader(file.FullName));
		public static List<XmlExtended.DiceCardXmlInfo_Extended> LoadCardInfo(this ModContent content, StreamReader streamReader) {
			List<XmlExtended.DiceCardXmlInfo_Extended> diceCardXmlInfoList = new List<XmlExtended.DiceCardXmlInfo_Extended>();
			try {
				using (streamReader) {
					diceCardXmlInfoList = (new XmlSerializer(typeof(XmlExtended.DiceCardXmlRoot_Extended)).Deserialize(streamReader) as DiceCardXmlRoot_Extended).cardXmlList;
					foreach (XmlExtended.DiceCardXmlInfo_Extended diceCardXmlInfo in diceCardXmlInfoList) {
						string workshopID = content._itemUniqueId;
						if (!string.IsNullOrEmpty(diceCardXmlInfo.pid)) {
							if (diceCardXmlInfo.pid == "@origin") {
								workshopID = string.Empty;
							} else {
								workshopID = diceCardXmlInfo.pid;
							}
						}
						diceCardXmlInfo.workshopID = workshopID;
					}
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return diceCardXmlInfoList;
		}
		*/
		public static List<XmlExtended.DiceCardXmlInfo_Extended> LoadCardInfo(this ModContent content, List<XmlExtended.DiceCardXmlInfo_Extended> diceCardXmlInfoList) {
			try {
				foreach (XmlExtended.DiceCardXmlInfo_Extended diceCardXmlInfo in diceCardXmlInfoList) {
					string workshopID = content._itemUniqueId;
					if (!string.IsNullOrEmpty(diceCardXmlInfo.pid)) {
						if (diceCardXmlInfo.pid == "@origin") {
							workshopID = string.Empty;
						} else {
							workshopID = diceCardXmlInfo.pid;
						}
					}
					diceCardXmlInfo.workshopID = workshopID;
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return diceCardXmlInfoList;
		}


		public static List<DeckXmlInfo> LoadDeck(this ModContent content) {
			List<DeckXmlInfo> deckXmlInfoList = new List<DeckXmlInfo>();
			try {
				string dataFilePath = content.GetDataFilePath(content._deckXmlPath);
				if (File.Exists(dataFilePath)) {
					using (StreamReader streamReader = new StreamReader(dataFilePath))
						deckXmlInfoList = (new XmlSerializer(typeof (DeckXmlRoot)).Deserialize(streamReader) as DeckXmlRoot).deckXmlList;
				}
				foreach (DeckXmlInfo deckXmlInfo in deckXmlInfoList) {
					deckXmlInfo.workshopId = content._itemUniqueId;
					LorId.InitializeLorIds(deckXmlInfo._cardIdList, deckXmlInfo.cardIdList, content._itemUniqueId);
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return deckXmlInfoList;
		}

		public static List<BattleDialogCharacter> LoadDialog(this ModContent content) {
			List<BattleDialogCharacter> battleDialogCharacterList = new List<BattleDialogCharacter>();
			try {
				if (content._dialogXmlPath != null) {
					string dataFilePath = content.GetDataFilePath(content._dialogXmlPath);
					if (File.Exists(dataFilePath)) {
						using (StreamReader streamReader = new StreamReader(dataFilePath))
							battleDialogCharacterList = (new XmlSerializer(typeof (BattleDialogRoot)).Deserialize(streamReader) as BattleDialogRoot).characterList;
					}
					foreach (BattleDialogCharacter battleDialogCharacter in battleDialogCharacterList) {
						battleDialogCharacter.workshopId = content._itemUniqueId;
						battleDialogCharacter.bookId = int.Parse(battleDialogCharacter.characterID);
					}
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return battleDialogCharacterList;
		}

		public static List<BookDesc> LoadBookStory(this ModContent content) {
			List<BookDesc> bookDescList = new List<BookDesc>();
			try {
				if (content._bookStoryXmlPath != null) {
					string dataFilePath = content.GetDataFilePath(content._bookStoryXmlPath);
					if (File.Exists(dataFilePath)) {
						using (StreamReader streamReader = new StreamReader(dataFilePath))
							bookDescList = (new XmlSerializer(typeof (BookDescRoot)).Deserialize(streamReader) as BookDescRoot).bookDescList;
					}
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return bookDescList;
		}

		public static List<PassiveXmlInfo> LoadPassive(this ModContent content) {
			List<PassiveXmlInfo> passiveXmlInfoList = new List<PassiveXmlInfo>();
			try {
				string dataFilePath = content.GetDataFilePath(content._passiveXmlPath);
				if (File.Exists(dataFilePath)) {
					using (StreamReader streamReader = new StreamReader(dataFilePath))
						passiveXmlInfoList = (new XmlSerializer(typeof (PassiveXmlRoot)).Deserialize(streamReader) as PassiveXmlRoot).list;
				}
				foreach (PassiveXmlInfo passiveXmlInfo in passiveXmlInfoList)
					passiveXmlInfo.workshopID = content._itemUniqueId;
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return passiveXmlInfoList;
		}

		public static List<BattleCardAbilityDesc> LoadAbilityText(this ModContent content) {
			List<BattleCardAbilityDesc> battleCardAbilityDescList = new List<BattleCardAbilityDesc>();
			try {
				if (content._cardAbilityXmlPath != null) {
					if (File.Exists(content.GetDataFilePath(content._cardAbilityXmlPath))) {
						using (StreamReader streamReader = new StreamReader(content._cardAbilityXmlPath))
							battleCardAbilityDescList = (new XmlSerializer(typeof (BattleCardAbilityDescRoot)).Deserialize(streamReader) as BattleCardAbilityDescRoot).cardDescList;
					}
					foreach (BattleCardAbilityDesc battleCardAbilityDesc in battleCardAbilityDescList)
						;
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return battleCardAbilityDescList;
		}

		public static void LoadArtworks(this ModContent content) {
			try {
				string path = Path.Combine(content._dirInfo.FullName, "Resource\\CombatPageArtwork");
				if (!Directory.Exists(path))
					return;
				string[] files = Directory.GetFiles(path);
				List<ArtworkCustomizeData> list = new List<ArtworkCustomizeData>();
				for (int index = 0; index < files.Length; ++index) {
					if (files[index].Contains(".png") || files[index].Contains(".jpg"))
						list.Add(new ArtworkCustomizeData()
						{
							spritePath = files[index],
							name = new DirectoryInfo(files[index]).Name
						});
				}
				if (list.Count <= 0)
					return;
				Singleton<CustomizingCardArtworkLoader>.Instance.AddArtworkData(content._itemUniqueId, list);
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
		}

		public static void LoadBookSkins(this ModContent content) {
			try {
				string path = Path.Combine(content._dirInfo.FullName, "Resource\\CharacterSkin");
				List<WorkshopSkinData> list = new List<WorkshopSkinData>();
				if (!Directory.Exists(path))
					return;
				string[] directories = Directory.GetDirectories(path);
				for (int index = 0; index < directories.Length; ++index) {
					WorkshopAppearanceInfo workshopAppearanceInfo = WorkshopAppearanceItemLoader.LoadCustomAppearance(directories[index], true);
					if (workshopAppearanceInfo != null) {
						string[] strArray = directories[index].Split('\\');
						string str = strArray[strArray.Length - 1];
						workshopAppearanceInfo.path = directories[index];
						workshopAppearanceInfo.uniqueId = content._itemUniqueId.ToString();
						workshopAppearanceInfo.bookName = str;
						Debug.Log("workshop bookName : " + workshopAppearanceInfo.bookName);
						if (workshopAppearanceInfo.isClothCustom)
							list.Add(new WorkshopSkinData()
							{
								dic = workshopAppearanceInfo.clothCustomInfo,
								dataName = workshopAppearanceInfo.bookName,
								contentFolderIdx = workshopAppearanceInfo.uniqueId,
								id = index
							});
					}
				}
				Singleton<CustomizingBookSkinLoader>.Instance.AddBookSkinData(content._itemUniqueId, list);
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
		}
	}
}
