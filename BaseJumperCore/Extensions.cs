namespace BaseJumperAPI {
	public static class Extensions {
		public static string TrimEnd(this string source, string value) {
			if (!source.EndsWith(value))
				return source;
			return source.Remove(source.LastIndexOf(value));
		}
		public static string GetName(this ResourceInfoManager manager, int id) {
			if (manager._characterResourceTableById.TryGetValue(id, out ResourceXmlInfo resourceXmlInfo)) {
				return resourceXmlInfo.fileName;
			}
			return null;
		}
	}
}