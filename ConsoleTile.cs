using System;

public struct ConsoleTile {
	public char letter;
	public byte fore, back;

	public ConsoleTile(char letter, ConsoleColor foreColor, ConsoleColor backColor) {
		this.letter = letter;
		fore = (byte)foreColor;
		back = (byte)backColor;
	}

	public ConsoleTile(char letter, ConsoleColor foreColor) {
		this.letter = letter;
		fore = (byte)foreColor;
		back = defaultTile.back;
	}

	public ConsoleColor Fore { get => (ConsoleColor)fore; set => fore = (byte)value; }
	public ConsoleColor Back { get => (ConsoleColor)back; set => back = (byte)value; }

	public readonly static ConsoleTile defaultTile;

	static ConsoleTile() {
		defaultTile = new ConsoleTile('?', Console.ForegroundColor, Console.BackgroundColor);
	}

	public static implicit operator ConsoleTile(char letter) {
		return new ConsoleTile { letter = letter, fore = defaultTile.fore, back = defaultTile.back };
	}

	public static implicit operator char(ConsoleTile tile) => tile.letter;

	public bool IsColorCurrent() {
		return Console.ForegroundColor == (ConsoleColor)fore && Console.BackgroundColor == (ConsoleColor)back;
	}

	public void SetColors(ConsoleColor fore, ConsoleColor back) {
		this.fore = (byte)fore; this.back = (byte)back;
	}

	public void ApplyColor() {
		Console.ForegroundColor = (ConsoleColor)fore;
		Console.BackgroundColor = (ConsoleColor)back;
	}

	public override string ToString() => $"[{letter}]";
	public override int GetHashCode() => fore * 0x00010000 + back * 0x01000000 + (int)letter;
	public override bool Equals(object o) {
		return (o == null || o.GetType() != typeof(ConsoleTile)) ? false : Equals((ConsoleTile)o);
	}
	public bool Equals(ConsoleTile ct) => fore == ct.fore && back == ct.back && letter == ct.letter;

	public static bool operator ==(ConsoleTile a, ConsoleTile b) { return a.Equals(b); }
	public static bool operator !=(ConsoleTile a, ConsoleTile b) { return !a.Equals(b); }
}
