using System;
using System.Collections.Generic;

public class Entity2D : EntityBase {
	public Map2D map = new Map2D();

	public Entity2D() { }

	public Entity2D(string name, ConsoleTile fill, Coord position, Coord size) {
		this.name = name;
		this.position = position;
		map.SetSize(size);
		map.Fill(fill);
		map.transparentLetter = 0;
	}

	public Entity2D(string name, ConsoleTile icon, Coord position) : this(name, icon, position, Coord.One) { }

	public Entity2D(string name, ConsoleTile icon) : this(name, icon, Coord.Zero) { }

	public Entity2D(string name, string filename, Coord position) {
		this.name = name;
		this.position = position;
		map.LoadFromFile(filename);
		map.transparentLetter = ' ';
	}

	public void LoadFromFile(string filename) => map.LoadFromFile(filename);
	public void LoadFromString(string text) => map.LoadFromString(text);

	public override void Draw(ConsoleTile[,] buffer, Coord offset) => map.Draw(buffer, position - offset);

	public override Coord GetSize() => map.Size;

	public void GetMinMax(out Coord min, out Coord max) { min = position; max = min + map.Size; }

	public ConsoleTile this[int row, int col] {
		get { return map[row-position.row, col-position.col]; }
		set { map[row - position.row, col - position.col] = value; }
	}

	public ConsoleTile this[Coord position] {
		get { return map[position - this.position]; }
		set { map[position - this.position] = value; }
	}

	public void ForEach(Action<Coord> action) { GetRect().ForEach(action); }

	public bool Contains(Coord position) {
		return map.Contains(position - this.position);
	}
}
