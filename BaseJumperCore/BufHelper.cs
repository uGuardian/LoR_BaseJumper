using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Workshop;
using Mod;
using Mod.XmlExtended;
using LOR_XML;
using XmlLoaders;
using BaseJumperAPI.Harmony;

namespace BaseJumperAPI {
	public partial class BaseJumperCore : BaseJumperModule {
		public static Dictionary<string, string> bufTextAddedByDic = new Dictionary<string, string>();
		public void AddBufText(BattleEffectText bufXml, string language) {
			// TODO - Add language and XML file support
			var xmlListInstance = Singleton<BattleEffectTextsXmlList>.Instance;
			var id = bufXml.ID;
			string debugText = null;
			int debugLevel = 0;
			if (bufTextAddedByDic.ContainsKey(id)) {
				debugLevel = 1;
				debugText = $"{packageId} has overridden {bufTextAddedByDic[id]} for buf EffectText {id}";
			} else if (!xmlListInstance.GetEffectText(id).ID.Equals("NOT_FOUND", StringComparison.Ordinal)) {
				debugLevel = 1;
				debugText = $"{packageId} has overridden an unknown mod for buf EffectText {id}";
			} else {
				bool patchedName = false;
				bool patchedDesc = false;
				if (!string.IsNullOrEmpty(xmlListInstance.GetEffectTextName(id))) {
					patchedName = true;
				}
				if (!string.IsNullOrEmpty(xmlListInstance.GetEffectTextDesc(id))) {
					patchedDesc = true;
				}
				if (patchedName || patchedDesc) {
					string substring;
					if (patchedName && patchedDesc) {
						substring = "Name and Desc";
					} else if (patchedName) {
						substring = "Name";
					} else if (patchedDesc) {
						substring = "Desc";
					} else {
						// Shut up compiler, this can't happen.
						throw new Exception("This should be LITERALLY impossible");
					}
					debugLevel = 2;
					debugText = $"A mod patch is altering the results of {substring} for buf {id}";
				}
			}
			var bufXmlDic = xmlListInstance._dictionary;
			bufXmlDic[id] = bufXml;
			if (xmlListInstance.GetEffectText(id) != bufXml) {
				debugLevel = 2;
				debugText = $"A mod patch is altering the EffectText of buf {id}";
			} else {
				bufTextAddedByDic[id] = packageId;
			}
			switch (debugLevel) {
				case 2:
					AddErrorLog(debugText);
					break;
				case 1:
					AddWarningLog(debugText);
					break;
				default:
					break;
			}
		}
	}
}