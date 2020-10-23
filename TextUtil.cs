public static class TextUtil {
	/// <summary>
	/// searches for the given file name in the exe's directory, as well as parent directories
	/// </summary>
	/// <param name="filePathAndName"></param>
	/// <param name="numDirectoriesBackToLook">how many parent directories backward to look</param>
	public static string StringFromFile(string filePathAndName, int numDirectoriesBackToLook = 3) {
		string text = null;
		int directoriesSearched = 0;
		string originalDir = System.IO.Directory.GetCurrentDirectory();
		do {
			try {
				text = System.IO.File.ReadAllText(filePathAndName);
			} catch (System.IO.FileNotFoundException e) {
				if (directoriesSearched < numDirectoriesBackToLook) {
					System.IO.Directory.SetCurrentDirectory("..");
					directoriesSearched++;
				} else {
					System.Console.WriteLine($"{filePathAndName} not found @{originalDir}");
					throw e;
				}
			}
		} while (text == null);
		text = text.Replace("\r\n", "\n");
		text = text.Replace("\n\r", "\n");
		text = text.Replace("\r", "\n");
		return text;
	}
}