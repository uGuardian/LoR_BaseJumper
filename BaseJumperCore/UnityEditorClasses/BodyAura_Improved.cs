using System.Collections.Generic;
using UnityEngine;

public class BodyAura_Improved : BodyAura {
	int lastLayer;
	public override void Update() {
		if (_appearance != null) {
			string layerName = _appearance.GetLayerName();
			int currentLayer = LayerMask.NameToLayer(layerName);
			if (currentLayer == lastLayer) {return;}
			int targetLayer;
			switch (layerName) {
				case "Character":
				case "CharacterUI":
				case "CharacterAppearance":
					targetLayer = currentLayer;
					break;
				default:
					targetLayer = LayerMask.NameToLayer("Effect");
					break;
			}
			foreach (GameObject gameObject in _objectList) {
				gameObject.layer = targetLayer;
			}
			lastLayer = targetLayer;
		}
	}
}