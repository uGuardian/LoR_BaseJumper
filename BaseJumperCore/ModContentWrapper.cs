using System;
using System.Collections.Generic;
using System.IO;
#pragma warning restore CS0618

namespace Mod {
	/// <summary>
	/// Provides an easy reflection-less way to access all the private fields of a ModContent object
	/// </summary>
	public class ModContentWrapper {
		#pragma warning disable IDE1006
		public ModContentWrapper(ModContent instance) {
			this.instance = instance;
		}

		private readonly ModContent instance;
		
		public ref ModContentInfo _modInfo => ref instance._modInfo;
		public ref DirectoryInfo _dirInfo => ref instance._dirInfo;
		public ref Dictionary<string, string> _storyStadingPaths => ref instance._storyStadingPaths;
		public ref Dictionary<string, string> _storyCgPaths => ref instance._storyCgPaths;
		public ref Dictionary<string, string> _storyBgmPaths => ref instance._storyBgmPaths;
		public ref string _itemUniqueId => ref instance._itemUniqueId;
		public ref string _stageXmlPath => ref instance._stageXmlPath;
		public ref string _enemyUnitXmlPath => ref instance._enemyUnitXmlPath;
		public ref string _equipPageLibrarianXmlPath => ref instance._equipPageLibrarianXmlPath;
		public ref string _equipPageEnemyXmlPath => ref instance._equipPageEnemyXmlPath;
		public ref string _dropBookXmlPath => ref instance._dropBookXmlPath;
		public ref string _cardDropTableXmlPath => ref instance._cardDropTableXmlPath;
		public ref string _cardInfoXmlPath => ref instance._cardInfoXmlPath;
		public ref string _deckXmlPath => ref instance._deckXmlPath;
		public ref string _dialogXmlPath => ref instance._dialogXmlPath;
		public ref string _bookStoryXmlPath => ref instance._bookStoryXmlPath;
		public ref string _passiveXmlPath => ref instance._passiveXmlPath;
		public ref string _cardAbilityXmlPath => ref instance._cardAbilityXmlPath;

		[Obsolete("You should call this via instance.GetStoryStandingSet() instead")]
		public Dictionary<string, string> GetStoryStandingSet() => instance.GetStoryStandingSet();
		[Obsolete("You should call this via instance.GetStoryCgSet() instead")]
		public Dictionary<string, string> GetStoryCgSet() => instance.GetStoryCgSet();
		[Obsolete("You should call this via instance.GetStoryBgmSet() instead")]
		public Dictionary<string, string> GetStoryBgmSet() => instance.GetStoryBgmSet();
		[Obsolete("You should call this via ModContent.LoadModContent(modContentInfo) instead")]
		public static ModContent LoadModContent(ModContentInfo modContentInfo)
			=> ModContent.LoadModContent(modContentInfo);
		[Obsolete("You should call this via instance.Load_Custom() instead")]
		public void Load_Custom(FileInfo file) => instance.Load_Custom(file);

		#pragma warning restore IDE1006
	}
}