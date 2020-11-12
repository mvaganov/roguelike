using System;

public class Map2D : IRect {
	private ConsoleTile[,] map;
	public int transparentLetter = -1;

	public Map2D() => SetSize(Coord.Zero);

	public Map2D(Map2D toCopy) => Copy(toCopy);

	public Map2D(ConsoleTile fill, Coord size) {
		SetSize(size);
		Fill(fill);
	}

	public void SetSize(Coord newSize) {
		if (IsSize(newSize)) return;
		ConsoleTile[,] oldMap = map;
		map = new ConsoleTile[newSize.row, newSize.col];
		if (oldMap != null) {
			for (int row = 0; row < oldMap.GetLength(0) && row < Height; ++row) {
				for (int col = 0; col < oldMap.GetLength(1) && col < Width; ++col) {
					map[row, col] = oldMap[row, col];
				}
			}
		}
	}

	public void SetEach(System.Func<Coord, ConsoleTile> rowColumnValue) {
		for (int r = 0; r < Height; ++r) {
			for (int c = 0; c < Width; ++c) {
				map[r, c] = rowColumnValue(new Coord(c,r));
			}
		}
	}

	public void Fill(ConsoleTile fill) { SetEach((coord) => fill); }

	public void Copy(Map2D m) {
		SetSize(m.Size);
		SetEach(coord => m[coord]);
	}

	public Coord Size {
		get {
			if (map != null) {
				return Coord.SizeOf(map);
			}
			return Coord.Zero;
		}
		set { SetSize(value); }
	}

	public int Height => map != null ? map.GetLength(0) : 0;
	public int Width => map?.GetLength(1) ?? 0;

	public ConsoleTile this[int row, int col] {
		get { return map[row, col]; }
		set { map[row, col] = value; }
	}

	public ConsoleTile this[Coord position] {
		get => map[position.row, position.col];
		set => map[position.row, position.col] = value;
	}

	public void ForEach(Action<Coord> action) { Size.ForEach(action); }

	public bool Contains(Coord position) { return position.IsWithin(Size); }

	public ConsoleTile[,] GetRawMap() => map;

	public void Release() { map = null; }

	public bool IsSize(Coord size) { return Height == size.row && Width == size.col; }

	public void Draw(ConsoleTile[,] drawBuffer, Coord position) {
		if (!Rect.GetSizeRectIntersect(Coord.Zero, Coord.SizeOf(drawBuffer), position, Coord.SizeOf(map), out Coord min, out Coord size)) {
			return;
		}
		Coord.ForEach(min, min+size, c => {
			int y = c.row - position.row, x = c.col - position.col;
			ConsoleTile ct = map[y, x];
			if (transparentLetter == -1 || ct.letter != transparentLetter) {
				drawBuffer[c.row, c.col] = map[y, x];
			}
		});
	}

	/// <summary>
	/// draw maps: https://notimetoplay.itch.io/ascii-mapper
	/// random generator: https://thenerdshow.com/amaze.html, https://www.dcode.fr/maze-generator, http://www.delorie.com/game-room/mazes/genmaze.cgi
	/// https://codepen.io/MittenedWatchmaker/pen/xpEvXd, https://raw.githubusercontent.com/dragonsploder/ascii-map-generator/master/AMG.py,
	/// https://rosettacode.org/wiki/Maze_generation#C.23
	/// </summary>
	/// <param name="filePathAndName"></param>
	public void LoadFromFile(string filePathAndName) {
		LoadFromString(TextUtil.StringFromFile(filePathAndName));
	}

	public void LoadFromString(string text) {
		Coord size = new Coord { row = 1, col = 0 };
		int lineWidth = 0;
		for(int i = 0; i < text.Length; ++i) {
			char c = text[i];
			if(c == '\n') {
				size.row++;
				lineWidth = 0;
			} else {
				lineWidth++;
			}
			if(lineWidth > size.col) {
				size.col = (short)lineWidth;
			}
		}
		SetSize(size);
		Coord cursor = Coord.Zero;
		for(int i = 0; i < text.Length; ++i) {
			char c = text[i];
			if(c == '\n') {
				cursor.row++;
				cursor.col = 0;
			} else {
				this[cursor] = c;
				cursor.col++;
			}
		}
	}
	public override string ToString() {
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		Coord.ForEach(Coord.Zero, Size, c => {
			if(c.col == 0 && c.row > 0) { sb.Append('\n'); }
			sb.Append(this[c].letter);
		});
		return sb.ToString();
	}

	public Rect GetRect() { return new Rect(0, 0, Width, Height); }

	public Coord GetPosition() { return Coord.Zero; }
}