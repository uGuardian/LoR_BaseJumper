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
		public static partial class CustomXmlLoader_Extensions {
		public static void AddCardInfoByMod(this ItemXmlDataList instance, string workshopId, List<XmlExtended.DiceCardXmlInfo_Extended> list) {
			var _workshopDict = instance.GetAllWorkshopData();
			if (!_workshopDict.ContainsKey(workshopId))
				_workshopDict.Add(workshopId, list.ConvertAll(
					new Converter<XmlExtended.DiceCardXmlInfo_Extended, LOR_DiceSystem.DiceCardXmlInfo>(ToBaseDiceCardXmlInfo)));
			var cardList = instance.GetCardList();
			foreach (XmlExtended.DiceCardXmlInfo_Extended diceCardXmlInfo in list) {
				if (diceCardXmlInfo.id.packageId != workshopId) {
					var oldId = new LorId(workshopId, diceCardXmlInfo.id.id);
					instance.GetCardList().RemoveAll(r => r.id == oldId);
					instance._cardInfoTable.Remove(oldId);
				}
				if (instance._cardInfoTable.ContainsKey(diceCardXmlInfo.id)) {
					Debug.LogWarning($"Overriding card {diceCardXmlInfo.id}");
					instance.GetCardList().Remove(diceCardXmlInfo);
				}
				instance.GetCardList().Add(diceCardXmlInfo);
				instance._cardInfoTable[diceCardXmlInfo.id] = diceCardXmlInfo;
			}
		}
		public static LOR_DiceSystem.DiceCardXmlInfo ToBaseDiceCardXmlInfo(XmlExtended.DiceCardXmlInfo_Extended list) {
			return list;
		}
	}
}
