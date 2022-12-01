
using System.Collections.Generic;
using System.Reflection;
using LOR_DiceSystem;
using LOR_XML;

namespace Mod {
	class ExampleReplacer {
		internal static void ReplaceBookXmlInfo(LorId oldId, LorId newId)
		{
			int index = BookXmlList.Instance.GetList().FindIndex(x => x.id == oldId);
			if (index != -1)
			{
				BookXmlInfo newItem = BookXmlList.Instance.GetList().Find(x => x.id == newId);
				if (newItem != null)
				{
					var dic = (Dictionary<LorId, BookXmlInfo>)typeof(BookXmlList)
						.GetField("_dictionary", BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic)
						.GetValue(BookXmlList.Instance);
					newItem._id = oldId.id;
					newItem.workshopID = oldId.packageId;
					dic[oldId] = newItem;
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
					var dic = (Dictionary<LorId, DiceCardXmlInfo>)typeof(ItemXmlDataList)
						.GetField("_cardInfoTable", BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic)
						.GetValue(ItemXmlDataList.instance);
					newItem._id = oldId.id;
					newItem.workshopID = oldId.packageId;
					dic[oldId] = newItem;
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
					var dic = (Dictionary<LorId, PassiveDesc>)typeof(PassiveDescXmlList)
						.GetField("_dictionary", BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic)
						.GetValue(PassiveDescXmlList.Instance);
					newItem._id = oldId.id;
					newItem.workshopID = oldId.packageId;
					dic[oldId].name = newItem.name;
					dic[oldId].desc = newItem.desc;
					PassiveXmlList.Instance.GetDataAll()[index] = newItem;
				}
			}
		}
		/* Example Use
		public class SC_SotC1BuffsModInitializer : ModInitializer
		{
			public override void OnInitializeMod()
			{
				ReplaceBookXmlInfo(new LorId(250002), new LorId("SotC1Buffs", 10000001));
				ReplaceBookXmlInfo(new LorId(250010), new LorId("SotC1Buffs", 10000002));
				ReplaceBookXmlInfo(new LorId(150002), new LorId("SotC1Buffs", 1));
				ReplaceBookXmlInfo(new LorId(150010), new LorId("SotC1Buffs", 2));
			}
		}
		*/
	}
}