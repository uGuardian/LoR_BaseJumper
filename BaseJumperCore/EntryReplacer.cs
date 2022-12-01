
using System.Collections.Generic;
using System.Reflection;
using LOR_DiceSystem;
using LOR_XML;

namespace Mod {
	class EntryReplacer {
		internal static void ReplaceBookXmlInfo(LorId oldId, LorId newId)
		{
			int index = BookXmlList.Instance.GetList().FindIndex(x => x.id == oldId);
			if (index != -1)
			{
				BookXmlInfo newItem = BookXmlList.Instance.GetList().Find(x => x.id == newId);
				if (newItem != null)
				{
					newItem._id = oldId.id;
					newItem.workshopID = oldId.packageId;
					Singleton<BookXmlList>.Instance._dictionary[oldId] = newItem;
					BookXmlList.Instance.GetList()[index] = newItem;
				}
			}
		}
		internal static void ReplaceCardInfo(LorId oldId, LorId newId)
		{
			int index = ItemXmlDataList.instance.GetCardList().FindIndex(x => x.id == oldId);
			if (index != -1)
			{
				DiceCardXmlInfo newItem = ItemXmlDataList.instance.GetCardList().Find(x => x.id == newId);
				if (newItem != null)
				{
					newItem._id = oldId.id;
					newItem.workshopID = oldId.packageId;
					ItemXmlDataList.instance._cardInfoTable[oldId] = newItem;
					ItemXmlDataList.instance.GetCardList()[index] = newItem;
				}
			}
		}
		internal static void ReplacePassiveXmlInfo(LorId oldId, LorId newId)
		{
			int index = PassiveXmlList.Instance.GetDataAll().FindIndex(x => x.id == oldId);
			if (index != -1)
			{
				PassiveXmlInfo newItem = PassiveXmlList.Instance.GetDataAll().Find(x => x.id == newId);
				if (newItem != null)
				{
					newItem._id = oldId.id;
					newItem.workshopID = oldId.packageId;
					Singleton<PassiveDescXmlList>.Instance._dictionary[oldId].name = newItem.name;
					Singleton<PassiveDescXmlList>.Instance._dictionary[oldId].desc = newItem.desc;
					PassiveXmlList.Instance.GetDataAll()[index] = newItem;
				}
			}
		}
	}
}