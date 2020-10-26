using System;

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

	public static Coord SizeOf(System.Array map) {
		return new Coord { row = (short)map.GetLength(0), col = (short)map.GetLength(1) };
	}

	public static bool IsRectIntersect(Coord aMin, Coord aMax, Coord bMin, Coord bMax) {
		return aMin.col < bMax.col && bMin.col < aMax.col && aMin.row < bMax.row && bMin.row < aMax.row;
	}

	public static bool IsSizeRectIntersect(Coord aMin, Coord aSize, Coord bMin, Coord bSize) {
		return IsRectIntersect(aMin, aMin+aSize, bMin, bMin+bSize);
	}

	public static bool GetRectIntersect(Coord aMin, Coord aMax, Coord bMin, Coord bMax, out Coord oMin, out Coord oMax) {
		oMin = new Coord { col = System.Math.Max(aMin.col, bMin.col), row = System.Math.Max(aMin.row, bMin.row) };
		oMax = new Coord { col = System.Math.Min(aMax.col, bMax.col), row = System.Math.Min(aMax.row, bMax.row) };
		return oMin.col < oMax.col && oMin.row < oMax.row;
	}

	public static bool GetSizeRectIntersect(Coord aMin, Coord aSize, Coord bMin, Coord bSize, out Coord oMin, out Coord oSize) {
		bool result = GetRectIntersect(aMin, aMin + aSize, bMin, bMin + bSize, out oMin, out oSize);
		oSize -= oMin;
		return result;
	}

	/// <param name="nMin">needle min corner</param>
	/// <param name="nMax">needle max corner</param>
	/// <param name="hMin">haystack min corner</param>
	/// <param name="hMax">haystack max corner</param>
	/// <returns></returns>
	public static bool IsRectContained(Coord nMin, Coord nMax, Coord hMin, Coord hMax) {
		return nMin.col >= hMin.col && hMax.col >= nMax.col && nMin.row >= hMin.row && hMax.row >= nMax.row;
	}

	public static bool IsSizeRectContained(Coord nMin, Coord nSize, Coord hMin, Coord hSize) {
		return IsRectContained(nMin, nMin + nSize, hMin, hMin + hSize);
	}

	public static void ForEach(Coord min, Coord max, System.Action<Coord> action) {
		Coord cursor = min;
		for(cursor.row = min.row; cursor.row < max.row; ++cursor.row) {
			for(cursor.col = min.col; cursor.col < max.col; ++cursor.col) {
				action(cursor);
			}
		}
	}

	public void ForEach(System.Action<Coord> action) => ForEach(Zero, this, action);

	/// <summary>
	/// stops iterating as soon as action returns true
	/// </summary>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <param name="action"></param>
	/// <returns>true if action returned true even once</returns>
	public static bool ForEach(Coord min, Coord max, System.Func<Coord, bool> action) {
		Coord cursor = min;
		for(cursor.row = min.row; cursor.row < max.row; ++cursor.row) {
			for(cursor.col = min.col; cursor.col < max.col; ++cursor.col) {
				if(action(cursor)) { return true; }
			}
		}
		return false;
	}

	public bool ForEach(System.Func<Coord, bool> action) => ForEach(Zero, this, action);

	public static void ForEachInclusive(Coord start, Coord end, System.Action<Coord> action) {
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

	public static void ExpandRectangle(Coord pMin, Coord pMax, ref Coord min, ref Coord max) {
		if (pMin.col < min.col) { min.col = pMin.col; }
		if (pMin.row < min.row) { min.row = pMin.row; }
		if (pMax.col > max.col) { max.col = pMax.col; }
		if (pMax.row > max.row) { max.row = pMax.row; }
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