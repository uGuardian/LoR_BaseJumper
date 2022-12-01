using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using LOR_DiceSystem;
using LOR_XML;
using UnityEngine;
using Workshop;
using System.Security.Permissions;
using Mod;
using System.Linq;
using System.IO;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace Mod {
	public static partial class CustomXmlLoader_Extensions {
		public static List<ModContent> GetLoadedContents(this ModContentManager manager)
			=> manager._loadedContents;
		public static ModContent GetModContent(this ModContentManager manager, string packageId)
			=> manager._loadedContents.Find((ModContent x) => x._modInfo.invInfo.workshopInfo.uniqueId == packageId);
		/// <summary>
		/// Provides an easy reflection-less way to access all the private fields of a ModContent object
		/// </summary>
		public static ModContentWrapper GetPublicisedModContent(this ModContentManager manager, string packageId)
			=> new ModContentWrapper(manager.GetModContent(packageId));
		/// <summary>
		/// Provides an easy reflection-less way to access all the private fields of a ModContent object
		/// </summary>
		public static ModContentWrapper GetPublicisedModContent(this ModContent content)
			=> new ModContentWrapper(content);
		public static string GetDataFilePath(this ModContent content, string fileName)
			=> content.GetDataFilePath(fileName);
		
		[Obsolete("Deprecated All-In-One XML Loader")]
		#if DEBUG
		public static XmlExtended.AllRoots Load_Custom(this ModContent content, System.IO.FileInfo file)
		#else
		public static void Load_Custom(this ModContent content, System.IO.FileInfo file)
		#endif
		{
			NormalInvitation invInfo = content._modInfo.invInfo;
			content._itemUniqueId = invInfo.workshopInfo.uniqueId;

			XmlExtended.AllRoots roots;
			using (XmlReader xmlReader = XmlReader.Create(file.FullName)) {
				roots = new XmlSerializer(typeof(XmlExtended.AllRoots)).Deserialize(xmlReader) as XmlExtended.AllRoots;
			}
			
			try
			{
				if (roots.Stage != null)
				{
					content._stageXmlPath = invInfo.fileInfo.stageFile.relativePath;
					List<StageClassInfo> list = content.LoadStage();
					// Singleton<StageClassInfoList>.Instance.AddStageByMod(content._itemUniqueId, list);
				}
			}
			catch (Exception e)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog("stage load", e);
			}
			try
			{
				if (roots.Enemy != null)
				{
					content._enemyUnitXmlPath = invInfo.fileInfo.enemyUnitFile.relativePath;
					List<EnemyUnitClassInfo> list = content.LoadEnemyUnit();
					// Singleton<EnemyUnitClassInfoList>.Instance.AddEnemyUnitByMod(content._itemUniqueId, list);
				}
			}
			catch (Exception e2)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog("enemy unit load", e2);
			}
			try
			{
				if (roots.Book != null)
				{
					content._equipPageLibrarianXmlPath = invInfo.fileInfo.librarianEquipPage.relativePath;
					List<BookXmlInfo> list = content.LoadEquipLibrarianPage();
					// Singleton<BookXmlList>.Instance.AddEquipPageByMod(content._itemUniqueId, list);
				}
			}
			catch (Exception e3)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog("core page load(1)", e3);
			}
			/*
			try
			{
				if (roots.Book != null)
				{
					content._equipPageEnemyXmlPath = invInfo.fileInfo.enemyEquipPage.relativePath;
					List<BookXmlInfo> list = content.LoadEquipEnemyPage();
					// Singleton<BookXmlList>.Instance.AddEquipPageByMod(content._itemUniqueId, list);
				}
			}
			catch (Exception e4)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog("core page load(2)", e4);
			}
			*/
			try
			{
				if (roots.BookUse != null)
				{
					content._cardDropTableXmlPath = invInfo.fileInfo.cardDropTableFile.relativePath;
					List<CardDropTableXmlInfo> list = content.LoadCardDropTable();
					// Singleton<CardDropTableXmlList>.Instance.AddCardDropTableByMod(content._itemUniqueId, list);
				}
			}
			catch (Exception e5)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog("book load(1)", e5);
			}
			try
			{
				if (roots.DropTable != null)
				{
					content._dropBookXmlPath = invInfo.fileInfo.dropBookFile.relativePath;
					List<DropBookXmlInfo> list = content.LoadDropBook();
					Singleton<DropBookXmlList>.Instance.SetDropTableByMod(list);
					// Singleton<DropBookXmlList>.Instance.AddBookByMod(content._itemUniqueId, list);
				}
			}
			catch (Exception e6)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog("book load(2)", e6);
			}
			try
			{
				if (roots.Card != null)
				{
					content._cardInfoXmlPath = invInfo.fileInfo.combatPageFile.relativePath;
					List<XmlExtended.DiceCardXmlInfo_Extended> list = content.LoadCardInfo(roots.Card);
					ItemXmlDataList.instance.AddCardInfoByMod(content._itemUniqueId, list);
				}
			}
			catch (Exception e7)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog("combat page load", e7);
			}
			try
			{
				if (roots.Deck != null)
				{
					content._deckXmlPath = invInfo.fileInfo.enemyDeckFile.relativePath;
					List<DeckXmlInfo> list = content.LoadDeck();
					Singleton<DeckXmlList>.Instance.AddDeckByMod(list);
				}
			}
			catch (Exception e8)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog("deck load", e8);
			}
			try
			{
				if (roots.Character != null)
				{
					content._dialogXmlPath = invInfo.fileInfo.dialogFile.relativePath;
					List<BattleDialogCharacter> list = content.LoadDialog();
					Singleton<BattleDialogXmlList>.Instance.AddDialogByMod(list);
				}
			}
			catch (Exception e9)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog("dialog load", e9);
			}
			try
			{
				// This currently appears to not be properly handled by the XML
				if (roots.bookDescList != null)
				{
					content._bookStoryXmlPath = invInfo.fileInfo.bookStoryFile.relativePath;
					List<BookDesc> list = content.LoadBookStory();
					Singleton<BookDescXmlList>.Instance.AddBookTextByMod(content._itemUniqueId, list);
				}
			}
			catch (Exception e10)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog("bookstory load", e10);
			}
			try
			{
				if (roots.Passive != null)
				{
					content._passiveXmlPath = invInfo.fileInfo.passiveFile.relativePath;
					List<PassiveXmlInfo> list = content.LoadPassive();
					Singleton<PassiveXmlList>.Instance.AddPassivesByMod(list);
				}
			}
			catch (Exception e11)
			{
				Singleton<ModContentManager>.Instance.AddErrorLog("passive load", e11);
			}
			// BattleCardAbility does not actually have an XML
			content.LoadArtworks();
			content.LoadBookSkins();
			content.LoadAssemblies();
			#if DEBUG
			return roots;
			#endif
		}
	}
}