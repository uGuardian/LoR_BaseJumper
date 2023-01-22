using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using LOR_DiceSystem;
using LOR_XML;
using UnityEngine;
using Workshop;
using Mod;
using Mod.XmlExtended;
using BaseJumperAPI;
using BaseJumperAPI.Harmony;

namespace XmlLoaders {
	public class DefaultLoader {
		static readonly string notImplemented = " Requires Implementation";
		const string notImplementedObsoleteMessage = "Method Requires Implementation";
		
		[Obsolete(notImplementedObsoleteMessage)]
		public List<StageClassInfo> LoadStage(BaseJumperModule module, StageXmlRoot xml) {
			throw new NotImplementedException(System.Reflection.MethodBase.GetCurrentMethod().Name + notImplemented);
		}

		[Obsolete(notImplementedObsoleteMessage)]
		public List<EnemyUnitClassInfo> LoadEnemyUnit(BaseJumperModule module, EnemyUnitClassRoot xml) {
			throw new NotImplementedException(System.Reflection.MethodBase.GetCurrentMethod().Name + notImplemented);
		}

		#region BookXmlRoot
		public readonly Dictionary<LorId, BookXmlInfo_Extended> bookDic = new Dictionary<LorId, BookXmlInfo_Extended>();
		public List<BookXmlInfo_Extended> LoadEquipPage(BaseJumperModule module, BookXmlRoot_Extended bookXmlRoot) {
			// throw new NotImplementedException(System.Reflection.MethodBase.GetCurrentMethod().Name + notImplemented);
			var content = module.modContent;
			bookXmlRoot.bookXmlList = new List<BookXmlInfo>();
			var bookXmlInfoList = bookXmlRoot.bookXmlList_extended;
			try {
				foreach (var bookXmlInfo in bookXmlInfoList) {
					bookXmlRoot.bookXmlList.Add(bookXmlInfo);
					bookXmlInfo.workshopID = content._itemUniqueId;
					var equipEffect = bookXmlInfo.EquipEffect_Extended;
					bookXmlInfo.EquipEffect = equipEffect;

					var onlyCard_LorIds = equipEffect.OnlyCard_Serialized;
					LorId.InitializeLorIds(equipEffect.OnlyCard_Extended, onlyCard_LorIds, string.Empty);
					equipEffect.OnlyCard = onlyCard_LorIds.ConvertAll(x => {
						if (x.IsBasic()) {
							return x.id;
						} else {
							return x.GetHashCode();
						}
					});

					if (ModContentInfo.ConvertModVer(bookXmlRoot.version) > ModContentInfo.ConvertModVer("1.0"))
						LorId.InitializeLorIds<LorIdXml>(equipEffect._PassiveList, equipEffect.PassiveList, content._itemUniqueId);
					else
						LorId.InitializeLorIds<LorIdXml>(equipEffect._PassiveList, equipEffect.PassiveList, string.Empty);

					if (!string.IsNullOrEmpty(bookXmlInfo.skinType)) {
						switch (bookXmlInfo.skinType) {
							case "UNKNOWN":
								bookXmlInfo.skinType = "Lor";
								break;
							case "CUSTOM":
								bookXmlInfo.skinType = "Custom";
								break;
							case "LOR":
								bookXmlInfo.skinType = "Lor";
								break;
						}
					}
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return bookXmlInfoList;
		}
		public void AddEquipPageByMod(BaseJumperModule _, List<BookXmlInfo_Extended> list) {
			foreach (var book in list) {
				bookDic.Add(book.id, book);
			}
		}
		public void AddEquipPageByModFinalizer(BaseJumperModule module) {
			if (bookDic.Count <= 0) {
				return;
			}
			var content = module.modContent;
			string workshopId = content._itemUniqueId;
			var instance = Singleton<BookXmlList>.Instance;
			var _workshopBookDict = instance._workshopBookDict;
			var _list = instance._list;
			_list.RemoveAll(b => bookDic.Keys.Contains(b.id));
			var _dictionary = instance._dictionary;
			var list = bookDic.Values.ToList();
			if (_workshopBookDict == null)
				_workshopBookDict = new Dictionary<string, List<BookXmlInfo>>();
			_workshopBookDict[workshopId] = bookDic.Values.Cast<BookXmlInfo>().ToList();
			_list.AddRange(list);
			if (_dictionary == null)
				return;
			foreach (BookXmlInfo bookXmlInfo in list) {
				if (bookXmlInfo.EquipEffect.OnlyCard.Count > 0 &&
					bookXmlInfo.EquipEffect is BookEquipEffect_Extended equipEffect &&
					equipEffect.OnlyCard_Serialized.Any(i => i.IsWorkshop()))
				{
					OnlyPagePatches.SetXmlInfoPatch.Activate(module);
				}
				_dictionary[bookXmlInfo.id] = bookXmlInfo;
			}
		}

		[Obsolete(notImplementedObsoleteMessage)]
		#endregion
		public List<CardDropTableXmlInfo> LoadCardDropTable(BaseJumperModule module, CardDropTableXmlRoot xml) {
			throw new NotImplementedException(System.Reflection.MethodBase.GetCurrentMethod().Name + notImplemented);
		}

		[Obsolete(notImplementedObsoleteMessage)]
		public List<DropBookXmlInfo> LoadDropBook(BaseJumperModule module, BookUseXmlRoot xml) {
			throw new NotImplementedException(System.Reflection.MethodBase.GetCurrentMethod().Name + notImplemented);
		}

		[Obsolete(notImplementedObsoleteMessage)]
		public List<DiceCardXmlInfo> LoadCardInfo(BaseJumperModule module, DiceCardXmlRoot_Extended xml) {
			throw new NotImplementedException(System.Reflection.MethodBase.GetCurrentMethod().Name + notImplemented);
		}

		[Obsolete(notImplementedObsoleteMessage)]
		public List<DeckXmlInfo> LoadDeck(BaseJumperModule module, DeckXmlRoot xml) {
			throw new NotImplementedException(System.Reflection.MethodBase.GetCurrentMethod().Name + notImplemented);
		}

		[Obsolete(notImplementedObsoleteMessage)]
		public List<BattleDialogCharacter> LoadDialog(BaseJumperModule module, BattleDialogRoot xml) {
			throw new NotImplementedException(System.Reflection.MethodBase.GetCurrentMethod().Name + notImplemented);
		}

		[Obsolete(notImplementedObsoleteMessage)]
		public List<BookDesc> LoadBookStory(BaseJumperModule module, BookDescRoot xml) {
			throw new NotImplementedException(System.Reflection.MethodBase.GetCurrentMethod().Name + notImplemented);
		}

		[Obsolete(notImplementedObsoleteMessage)]
		public List<PassiveXmlInfo> LoadPassive(BaseJumperModule module, PassiveXmlInfo xml) {
			throw new NotImplementedException(System.Reflection.MethodBase.GetCurrentMethod().Name + notImplemented);
		}
	}
}