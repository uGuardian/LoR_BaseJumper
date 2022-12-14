using System.Collections.Generic;

public static class EmotionCardXmlList_Extensions {
	public static EmotionCardXmlInfo GetData(this EmotionCardXmlList instance, LorId id, SephirahType sephirah) =>
		Mod.XmlExtended.Extensions.EmotionCardXmlList_Extensions.GetData(instance, id, sephirah);

	// Due to technical limitations, can't handle mods with alternative LorId implementations.
	public static List<EmotionCardXmlInfo> GetEnemyEmotionCardList(this EmotionCardXmlList instance, params LorId[] ids) =>
		Mod.XmlExtended.Extensions.EmotionCardXmlList_Extensions.GetEnemyEmotionCardList(instance, ids);
	public static List<EmotionCardXmlInfo> GetEnemyEmotionCardList(this EmotionCardXmlList instance, IEnumerable<LorId> ids) =>
		Mod.XmlExtended.Extensions.EmotionCardXmlList_Extensions.GetEnemyEmotionCardList(instance, ids);
}