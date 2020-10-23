public class Rect {
	public Coord min, max;

	public Rect(Coord min, Coord max) {
		this.min = min;
		this.max = max;
	}

	public Rect(int x, int y, int width, int height) {
		min = new Coord(x, y);
		max = new Coord(x + width, y + height);
	}

	public int X { get => min.col; set => min.col = (short)value; }
	public int Y { get => min.row; set => min.row = (short)value; }
	public int Width { get => max.col - min.col; set => max.col = (short)(min.col + value); }
	public int Height { get => max.row - min.row; set => max.row = (short)(min.row + value); }

	public int Top { get => min.row; set => min.row = (short)value; }
	public int Left { get => min.col; set => min.col = (short)value; }
	public int Bottom { get => max.row; set => max.row = (short)value; }
	public int Right { get => max.col; set => max.col = (short)value; }

	public Rect Intersect(Rect r) {
		Coord.GetRectIntersect(min, max, r.min, r.max, out Coord iMin, out Coord iMax);
		return new Rect(iMin, iMax);
	}
}