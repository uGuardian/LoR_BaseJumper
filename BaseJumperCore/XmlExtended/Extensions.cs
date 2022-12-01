using System.Collections.Generic;
using System.Xml.Serialization;
using LOR_DiceSystem;
using System.Linq;

namespace Mod.XmlExtended.Extensions {
	public static class BookXmlList_Extensions {
		public static List<BookXmlInfo_Extended> GetExtendedList(this BookXmlList instance, BookXmlInfo info) {
			return instance.GetList().OfType<BookXmlInfo_Extended>().ToList();
		}
	}
	public static class EmotionCardXmlList_Extensions {
		public static EmotionCardXmlInfo GetData(this EmotionCardXmlList instance, LorId id, SephirahType sephirah) {
			if (!id.IsBasic()) {
				return instance._list.OfType<EmotionCardXmlInfo_Extended>().FirstOrDefault(x => x.lorId == id && x.Sephirah == sephirah);
			}
			return instance._list.OfType<EmotionCardXmlInfo_Extended>().FirstOrDefault(x => x.id == id.id && x.Sephirah == sephirah);
		}
		public static List<EmotionCardXmlInfo> GetEnemyEmotionCardList(this EmotionCardXmlList instance, IEnumerable<LorId> ids) {
			var filter = ids.ToLookup(x => x.IsBasic());
			var basic = filter[true].Select(x => x.id).ToHashSet();
			var extended = filter[false].ToHashSet();
			return instance._list.Where(x => (x.Sephirah == SephirahType.None) && (basic.Contains(x.id) ||
				(x is EmotionCardXmlInfo_Extended e && extended.Contains(e.lorId)))).ToList();
		}
	}
}