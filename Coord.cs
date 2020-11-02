using System;

public interface IPosition { Coord GetPosition(); }

public struct Coord {
	public short row, col;

	public Coord(int col, int row) {
		this.col = (short)col;
		this.row = (short)row;
	}

	public int X => col;
	public int Y => row;

	public static readonly Coord Zero = new Coord(0, 0);
	public static readonly Coord One = new Coord(1, 1);
	public static readonly Coord Two = new Coord(2, 2);
	public static readonly Coord Up = new Coord(0, -1);
	public static readonly Coord Left = new Coord(-1, 0);
	public static readonly Coord Down = new Coord(0, 1);
	public static readonly Coord Right = new Coord(1, 0);

	public override string ToString() => $"[{row},{col}]";
	public override int GetHashCode() => row * 0x00010000 + col;
	public override bool Equals(object o) {
		return (o == null || o.GetType() != typeof(Coord)) ? false : Equals((Coord)o);
	}
	public bool Equals(Coord c) => row == c.row && col == c.col;

	public static bool operator ==(Coord a, Coord b) =>  a.Equals(b);
	public static bool operator !=(Coord a, Coord b) => !a.Equals(b);
	public static Coord operator +(Coord a, Coord b) => new Coord(a.col + b.col, a.row + b.row);
	public static Coord operator -(Coord a, Coord b) => new Coord(a.col - b.col, a.row - b.row);
	public static Coord operator -(Coord a)          => new Coord(       -a.col,       - a.row);

	public Coord Scale(Coord scale) { col *= scale.col; row *= scale.row; return this; }
	public Coord InverseScale(Coord scale) { col /= scale.col; row /= scale.row; return this; }

	/// <param name="min">inclusive starting point</param>
	/// <param name="max">exclusive limit</param>
	/// <returns>if this is within the given range</returns>
	public bool IsWithin(Coord min, Coord max) {
		return row >= min.row && row < max.row && col >= min.col && col < max.col;
	}

	/// <param name="max">exclusive limit</param>
	/// <returns>IsWithin(<see cref="Coord.Zero"/>, max)</returns>
	public bool IsWithin(Coord max) => IsWithin(Zero, max);

	public void Clamp(Coord min, Coord max) {
		col = (col < min.col) ? min.col : (col > max.col) ? max.col : col;
		row = (row < min.row) ? min.row : (row > max.row) ? max.row : row;
	}

	public static Coord SizeOf(Array map) {
		return new Coord { row = (short)map.GetLength(0), col = (short)map.GetLength(1) };
	}

	public static void ForEach(Coord min, Coord max, Action<Coord> action) {
		Coord cursor = min;
		for(cursor.row = min.row; cursor.row < max.row; ++cursor.row) {
			for(cursor.col = min.col; cursor.col < max.col; ++cursor.col) {
				action(cursor);
			}
		}
	}

	public void ForEach(Action<Coord> action) => ForEach(Zero, this, action);

	/// <summary>
	/// stops iterating as soon as action returns true
	/// </summary>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <param name="action"></param>
	/// <returns>true if action returned true even once</returns>
	public static bool ForEach(Coord min, Coord max, Func<Coord, bool> action) {
		Coord cursor = min;
		for(cursor.row = min.row; cursor.row < max.row; ++cursor.row) {
			for(cursor.col = min.col; cursor.col < max.col; ++cursor.col) {
				if(action(cursor)) { return true; }
			}
		}
		return false;
	}

	public bool ForEach(Func<Coord, bool> action) => ForEach(Zero, this, action);

	public static void ForEachInclusive(Coord start, Coord end, Action<Coord> action) {
		Coord cursor = start;
		cursor.row = start.row;
		do {
			cursor.col = start.col;
			do {
				action(cursor);
				if (cursor.col == end.col) { break; }
				if (cursor.col < end.col) { ++cursor.col; } else { --cursor.col; }
			} while (true);
			if (cursor.row == end.row) { break; }
			if (cursor.row < end.row) { ++cursor.row; } else { --cursor.row; }
		} while (true);
	}

	public static int ManhattanDistance(Coord a, Coord b) {
		Coord delta = b - a;
		return Math.Abs(delta.col) + Math.Abs(delta.row);
	}
}

public static class CoordExtension {
	public static TYPE At<TYPE>(this TYPE[,] matrix, Coord coord) {
		return matrix[coord.row, coord.col];
	}
	public static void SetAt<TYPE>(this TYPE[,] matrix, Coord coord, TYPE value) {
		matrix[coord.row, coord.col] = value;
	}
}