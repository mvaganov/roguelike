using System;

public class ShowChars {
	public static int Main_(string[] args) {
		ConsoleKeyInfo k;
		char letter = (char)0;
		do {
			int x = (letter % 16) * 5;
			int y = (letter / 16) % 16;
			Console.SetCursorPosition(x, y);
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(letter);
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write(((int)letter).ToString("X"));
			++letter;
			if(letter % 256 == 0) {
				k = Console.ReadKey();
				switch (k.KeyChar) {
				case '\b':
					if (letter > 256) {
						letter -= (char)512;
						if (letter == 0) { letter = (char)1; }
					} else {
						letter = (char)1;
					}
					Console.SetCursorPosition(0, 0);
					for (int i = 0; i < 80*16; ++i) { Console.Write(' '); }
					break;
				case (char)27: letter = (char)0; break;
				}
			}
		} while (letter != 0);
		while (!Console.KeyAvailable) {
			System.Threading.Thread.Sleep(1000);
		}
		return 0;
	}
}