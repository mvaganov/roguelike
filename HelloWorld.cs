using System;

public class HelloWorld {
	public static int Main_(string[] args) {
		Console.Write("Hello World!");

		Console.ForegroundColor = ConsoleColor.Green;
		ConsoleKeyInfo k;
		do {
			Console.SetCursorPosition(5, 1);
			k = Console.ReadKey();
		} while (k.KeyChar != 27);
		return 0;
	}
}