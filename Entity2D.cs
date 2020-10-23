public class Entity2D : EntityBase {
	public Map2D map = new Map2D();

	public Entity2D(ConsoleTile fill, Coord position, Coord size) {
		this.position = position;
		map.SetSize(size);
		map.Fill(fill);
	}

	public Entity2D(ConsoleTile icon, Coord position) : this(icon, position, Coord.One) { }

	public Entity2D(ConsoleTile icon) : this(icon, Coord.Zero) { }

	public Entity2D(string filename, Coord position) {
		this.position = position;
		map.LoadFromFile(filename);
	}

	public void LoadFromFile(string filename) => map.LoadFromFile(filename);
	public void LoadFromString(string text) => map.LoadFromString(text);

	public override void Draw(ConsoleTile[,] buffer, Coord offset) => map.Draw(buffer, position - offset);

	public override Coord GetSize() => map.Size;

	public ConsoleTile this[int row, int col] {
		get { return map[row-position.row, col-position.col]; }
		set { map[row - position.row, col - position.col] = value; }
	}

	public ConsoleTile this[Coord position] {
		get { return map[position - this.position]; }
		set { map[position - this.position] = value; }
	}

	public bool Contains(Coord position) {
		return map.Contains(position - this.position);
	}
}