using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Reflection;
using LOR_DiceSystem;
using LOR_XML;
using UnityEngine;
using Workshop;
using Mod;
using Mod.XmlExtended;
using BaseJumperAPI;

namespace XmlLoaders {
	public partial class CustomLoader {
		public readonly static Dictionary<(int id, SephirahType sephirah), EmotionCardXmlInfo_Extended> vanillaOverrides =
			new Dictionary<(int id, SephirahType sephirah), EmotionCardXmlInfo_Extended>();
		public readonly static Dictionary<(LorId id, SephirahType sephirah), EmotionCardXmlInfo_Extended> newEmotionCards =
			new Dictionary<(LorId id, SephirahType sephirah), EmotionCardXmlInfo_Extended>();
		readonly static Dictionary<(LorId id, SephirahType sephirah), int> addStatuses =
			new Dictionary<(LorId id, SephirahType sephirah), int>();
		public readonly static Dictionary<(LorId id, SephirahType sephirah), int> normalizedIds =
			new Dictionary<(LorId id, SephirahType sephirah), int>();
		public List<EmotionCardXmlInfo_Extended> LoadEmotionCard(BaseJumperModule module, EmotionCardXmlRoot_Extended emotionCardInfo) {
			var content = module.modContent;
			List<EmotionCardXmlInfo_Extended> emotionCardInfoList = new List<EmotionCardXmlInfo_Extended>();
			try {
				emotionCardInfoList = emotionCardInfo.emotionCardXmlList_extended;
				foreach (var emotionCard in emotionCardInfoList) {
					emotionCard.lorId = LorId.MakeLorId(new LorIdXml(emotionCard.pid, emotionCard.id), content._itemUniqueId);
				}
			}
			catch (Exception ex) {
				Singleton<ModContentManager>.Instance.AddErrorLog(ex.Message);
			}
			return emotionCardInfoList;
		}
		public void AddEmotionCardByMod(BaseJumperModule module, List<EmotionCardXmlInfo_Extended> list) {
			foreach (var emotionCard in list) {
				var id = emotionCard.lorId;
				if (!id.IsBasic()) {
					var tuple = (emotionCard.lorId, emotionCard.Sephirah);
					if (newEmotionCards.ContainsKey(tuple)) {
						module.AddWarningLog($"EmotionCard {tuple.lorId} for Sephirah {tuple.Sephirah} is added by more than one BaseJumper mod");
						if (addStatuses[tuple] == 1) {
							addStatuses[tuple] = 2;
						}
					} else {
						addStatuses[tuple] = 0;
					}
					newEmotionCards[tuple] = emotionCard;
				} else {
					var tuple = (emotionCard.id, emotionCard.Sephirah);
					if (vanillaOverrides.ContainsKey(tuple)) {
						module.AddWarningLog($"EmotionCard {tuple.id} for Sephirah {tuple.Sephirah} is overridden by more than one BaseJumper mod");
					}
					vanillaOverrides[tuple] = emotionCard;
				}
			}
		}
		public void AddEmotionCardByModFinalizer(BaseJumperModule _) {
			var originList = Singleton<EmotionCardXmlList>.Instance._list;
			foreach (var emotionCard in vanillaOverrides.Values) {
				int cardIndex = originList.FindIndex(c => c.id == emotionCard.id && c.Sephirah == emotionCard.Sephirah);
				originList[cardIndex] = emotionCard;
			}
			foreach (var keyValuePair in newEmotionCards) {
				var tuple = keyValuePair.Key;
				var emotionCard = keyValuePair.Value;
				switch (addStatuses[tuple]) {
					case 0:
						var newId = emotionCard.lorId.GetHashCode();
						var filterList = originList.FindAll(c => c.Sephirah == emotionCard.Sephirah);
						var existingCard = filterList.Exists(c => c.id == newId);
						while (existingCard) {
							newId++;
							existingCard = filterList.Exists(c => c.id == newId);
						}
						emotionCard.id = newId;
						originList.Add(emotionCard);
						var description = emotionCard.Default_Description;
						if (description != null) {
							var id = emotionCard.Name;
							description.id = id;
							var descDic = Singleton<AbnormalityCardDescXmlList>.Instance._dictionary;
							if (!descDic.ContainsKey(id)) {
								descDic.Add(id, description);
							}
						}
						normalizedIds[tuple] = newId;
						addStatuses[tuple] = 1;
						break;
					case 1:
						break;
					case 2:
						int cardIndex = originList.FindIndex(c => c.Sephirah == emotionCard.Sephirah &&
							c.id == normalizedIds[tuple]);
						originList[cardIndex] = emotionCard;
						addStatuses[tuple] = 1;
						break;
				}
			}
		}
	}
}