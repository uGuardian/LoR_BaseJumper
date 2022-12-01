using System.Collections.Generic;

public static class EmotionCardXmlList_Extensions {
	public static EmotionCardXmlInfo GetData(this EmotionCardXmlList instance, LorId id, SephirahType sephirah) =>
		Mod.XmlExtended.Extensions.EmotionCardXmlList_Extensions.GetData(instance, id, sephirah);
	public static List<EmotionCardXmlInfo> GetEnemyEmotionCardList(this EmotionCardXmlList instance, IEnumerable<LorId> ids) =>
		Mod.XmlExtended.Extensions.EmotionCardXmlList_Extensions.GetEnemyEmotionCardList(instance, ids);
}