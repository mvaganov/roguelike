using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// start code from https://rosettacode.org/wiki/Maze_generation#C.23
/// </summary>
namespace MazeGeneration {
	[Flags] public enum CellState {
		Up = 1, Left = 2, Down = 4, Right = 8,
		None = 0, Visited = 128, Initial = Up | Right | Down | Left,
	}
	public static class CellStateExtension {
		public static CellState OppositeWall(this CellState orig) {
			return (CellState)(((int)orig >> 2) | ((int)orig << 2)) & CellState.Initial;
		}
		public static bool HasFlag(this CellState cs, CellState flag) { return ((int)cs & (int)flag) != 0; }
	}

	public struct WallEdge {
		public Coord next;
		public CellState dir;
		public override string ToString() => dir + next.ToString();
		public static WallEdge MakeEdge(Coord coord, CellState cellState) {
			return new WallEdge { next = coord + DirToCoord(cellState), dir = cellState };
		}
		public static Coord DirToCoord(CellState dir) {
			switch (dir) {
				case CellState.Up: return Coord.Up;
				case CellState.Left: return Coord.Left;
				case CellState.Down: return Coord.Down;
				case CellState.Right: return Coord.Right;
			}
			return Coord.Zero;
		}
		public static CellState[] directions = { CellState.Up, CellState.Left, CellState.Down, CellState.Right };
		public static Coord[] coordinates = { Coord.Up, Coord.Left, Coord.Down, Coord.Right };
		public static WallEdge None = new WallEdge { next = Coord.Zero, dir = CellState.None };
	}

	public abstract class MazeSection {
		public abstract bool Contains(Coord c);
	}

	public class Maze {
		public readonly CellState[,] cells;
		int[,] marks;
		public readonly Coord size;
		public readonly Random rng;
		List<MazeSection> mazeSections = new List<MazeSection>();
		List<MazeSection> terminals = new List<MazeSection>();
		List<MazeSection> hallways = new List<MazeSection>();
		int deadEnds;

		public Maze(Coord size, int seed = 0, int errode = 0) {
			this.size = size;
			cells = new CellState[size.Y, size.X];
			marks = new int[size.Y, size.X];
			size.ForEach(c => this[c] = CellState.Initial);
			rng = new Random(seed);
			Coord start = new Coord(rng.Next(size.X), rng.Next(size.Y));
			VisitCell(start);
			for(int i = 0; i < errode; ++i) { Erode(); }
			TerminalsAndHalways(); // finds terminal and tunnel paths
			List<MazeRoom> rooms = MazeRoom.Discover(this, mazeSections); // TODO when looking for rooms, don't intersect dead end paths or tunnel paths.

			mazeSections.AddRange(rooms);
			for (int i = 0; i < rooms.Count; ++i) {
//				rooms[i].ForEach(c => marks.SetAt(c, i + 1));
			}

			// TODO consolodate rooms when one of the rooms connects completely on one side
			// TODO generate edges for rooms, tunnels, and dead-ends.
			//Console.Write(rooms.Count);
			//Console.ReadKey();
		}

		public void Erode() {
			List<WallEdge> toKnockDown = new List<WallEdge>();
			Coord s = size - Coord.One;
			s.ForEach(a => {
				Coord b = a + Coord.One;
				bool top = !this[a].HasFlag(CellState.Right);
				bool left = !this[a].HasFlag(CellState.Down);
				bool down = !this[b].HasFlag(CellState.Left);
				bool right = !this[b].HasFlag(CellState.Up);
				int connections = (top ? 1 : 0) + (left ? 1 : 0) + (down ? 1 : 0) + (right ? 1 : 0);
				if (connections == 3) {
					if (!top) { toKnockDown.Add(new WallEdge { dir = CellState.Left, next = a }); }
					if (!left) { toKnockDown.Add(new WallEdge { dir = CellState.Up, next = a }); }
					if (!down) { toKnockDown.Add(new WallEdge { dir = CellState.Right, next = b }); }
					if (!right) { toKnockDown.Add(new WallEdge { dir = CellState.Down, next = b }); }
				}
			});
			foreach (WallEdge we in toKnockDown) {
				switch (we.dir) {
					case CellState.Left:  this[we.next] -= CellState.Right; this[we.next + Coord.Right] -= CellState.Left; break;
					case CellState.Up:    this[we.next] -= CellState.Down;  this[we.next + Coord.Down] -= CellState.Up; break;
					case CellState.Right: this[we.next] -= CellState.Left;  this[we.next + Coord.Left] -= CellState.Right; break;
					case CellState.Down:  this[we.next] -= CellState.Up;    this[we.next + Coord.Up] -= CellState.Down; break;
				}
			}
		}

		public CellState this[int y, int x] { get { return cells[  y,   x]; } set { cells[  y,   x] = value; } }
		public CellState this[Coord c]      { get { return cells[c.Y, c.X]; } set { cells[c.Y, c.X] = value; } }

		/// <summary>
		/// gets an edge from the given cell square
		/// </summary>
		/// <param name="p"></param>
		/// <param name="index">a number {0,1,2,3}</param>
		/// <param name="e"></param>
		/// <returns>true if this is a valid edge</returns>
		public bool TryGetEdge(Coord p, int index, out WallEdge e) {
			if (index < 0 || index >= 4 || !p.IsWithin(size)) {
				e = WallEdge.None;
				return false;
			}
			e = WallEdge.MakeEdge(p, WallEdge.directions[index]);
			return e.next.IsWithin(size);
		}

		public bool CellConnects(Coord p, int neighborIndex, out Coord neighbor) {
			if (neighborIndex < 0 || neighborIndex >= 4 || !p.IsWithin(size)) {
				neighbor = p;
				return false;
			}
			neighbor = p + WallEdge.coordinates[neighborIndex];
			return neighbor.IsWithin(size) && CellConnects(p, (CellState)(1 << neighborIndex));
				//!this[p].HasFlag((CellState)(1 << neighborIndex));
		}

		public static void Shuffle<TYPE>(IList<TYPE> list, Random rng) {
			int n = list.Count;
			while (n > 1) {
				int k = rng.Next(n--);
				TYPE value = list[k]; list[k] = list[n]; list[n] = value;
			}
		}

		public void VisitCell(Coord cell) {
			this[cell] |= CellState.Visited;
			IList<int> edges = new int[]{ 0, 1, 2, 3 };
			Shuffle(edges, rng);
			for(int i = 0; i < edges.Count; ++i) {
				if (!TryGetEdge(cell, edges[i], out WallEdge edge) || this[edge.next].HasFlag(CellState.Visited)) { continue; }
				this[cell] -= edge.dir;
				this[edge.next] -= edge.dir.OppositeWall();
				VisitCell(edge.next);
			}
		}

		public bool CellConnects(Coord cell, CellState dir) => !this[cell].HasFlag(dir);
		public bool CellConnects(int x, int y, CellState dir) => !this[y,x].HasFlag(dir);
		public bool CellConnects(Coord cell, CellState dir, out Coord next) {
			next = cell + WallEdge.DirToCoord(dir);
			return cell.IsWithin(size) && !this[cell].HasFlag(dir);
		}
		public string empty = "  ", horizontalWall = "--", verticalWall = "|", corner = "+", missingVerticalWall = " ", missingCorner = " ";
		public override string ToString() {
			bool cullSinglePillars = true;
			StringBuilder result = new StringBuilder();
			string open = missingVerticalWall + empty, wall = verticalWall+empty, topW = corner + horizontalWall, pilr = corner + empty, gone = missingCorner + empty;
			var firstLine = string.Empty;
			for (int y = 0; y < size.Y; y++) {
				var sbTop = new StringBuilder();
				var sbMid = new StringBuilder();
				for (int x = 0; x < size.X; x++) {
					if(marks != null) {
						int pathIndex = marks[y, x];
						string label = (pathIndex == 0)?"  ":pathIndex.ToString("X2");
						if (empty.Length == 1) { label = label[label.Length - 1].ToString(); }
						open = missingVerticalWall + label;
						wall = verticalWall + label;
						//clos = corner + label;
					}
					sbTop.Append(!CellConnects(x, y, CellState.Up)   ? topW : 
						((cullSinglePillars
						&& CellConnects(x, y, CellState.Left)
						&& CellConnects(x, y - 1, CellState.Left)
						&& CellConnects(x-1, y, CellState.Up)
						) ? gone : pilr));
					sbMid.Append(!CellConnects(x,y,CellState.Left) ? wall : open);
				}
				sbTop.Append("+");
				sbMid.Append("|");
				if (firstLine == string.Empty) { firstLine = sbTop.ToString(); }
				result.Append(sbTop).Append("\n");
				result.Append(sbMid).Append("\n");
			}
			result.Append(firstLine);
			return result.ToString();
		}

		public int GetEdgeCount(Coord c) {
			int count = 0;
			for(int i = 0; i < 4; ++i) {
				if( ((int)this[c] & (1 << i)) == 0) { ++count; }
			}
			return count;
		}

		public List<Coord> FindDeadEnds() {
			List<Coord> deadEnds = new List<Coord>();
			size.ForEach(c => {
				int edgeCount = GetEdgeCount(c);
				if (edgeCount <= 1) { deadEnds.Add(c); }
			});
			return deadEnds;
		}

		public void MarkPath(IList<Coord> path, int value) {
			for(int i = 0; i < path.Count; ++i) {
				Coord c = path[i];
				marks.SetAt(c, value);
			}
		}

		public void TerminalsAndHalways() {
			List<Coord> deadends = FindDeadEnds();
			foreach (Coord end in deadends) {
				Path newPath = new Path(end, this);
				mazeSections.Add(newPath);
				terminals.Add(newPath);
				MarkPath(newPath.path, mazeSections.Count);
				//Coord fin = newPath.Finish;
				//marks.SetAt(fin, 0xff);
			}
			deadEnds = mazeSections.Count;
			// now find tunnels (single-cell width paths that are not dead-ends)
			List<Coord> minorPath = new List<Coord>();
			size.ForEach(c => {
				if (marks.At(c) != 0) return;
				int edgeCount = GetEdgeCount(c);
				if(edgeCount == 2) {
					if(Path.FindStartOfPath(c, this, minorPath)) {
						minorPath.Reverse();
						//marks.SetAt(minorPath[0], minorPath.Count);// paths.Count + 1);
						Path newPath = new Path(minorPath, this);
						if (newPath.path.Count == 0) { newPath.path.Add(minorPath[0]); } // 1-unit hallways need an extra push
						mazeSections.Add(newPath);
						hallways.Add(newPath);
						MarkPath(newPath.path, mazeSections.Count);
						//Coord fin = newPath.Finish;
						//marks.SetAt(fin, 0xff);
					}
				}
			});

			//// once the dead ends are found, start from the ends of the dead ends
			//for (int i = 0; i < paths.Count; ++i) {
			//	Path p = paths[i], newPath;
			//	int counter = 0;
			//	do {
			//		newPath = new Path(p.Finish, this, paths);
			//		if (newPath.path.Count == 0) break;
			//		paths.Add(newPath);
			//		MarkPath(newPath.path, paths.Count);
			//		if (counter++ > 100000) { throw new Exception("infinite loop?"); }
			//	} while (newPath.path.Count > 0);
			//}
		}
	}

	public class Path : MazeSection {
		public List<Coord> path = new List<Coord>();

		public override bool Contains(Coord c) { return path.IndexOf(c, 0, path.Count) >= 0; }

		private bool GetNextOnPath(Maze maze, Coord c, out Coord next) {
			next = c;
			for (int i = 0; i < 4; ++i) { // check all the neighbors
				if (maze.CellConnects(c, i, out Coord n)) {
					if (path.IndexOf(n) >= 0) continue; // ignore coords already on the path
					if (next == c) {
						next = n;
					} else {
						//Console.WriteLine($"@{c}, found {n} along with {next}");
						return false; // if there is more than one next, this is the end of the path.
					}
				}
			}
			return next != c;
		}

		private List<Path> OtherPathsRightHere(List<MazeSection> allPaths, Coord c) {
			List<Path> pathsRightHere = new List<Path>();
			foreach (Path p in allPaths) {
				if (p.path.Contains(c)) { pathsRightHere.Add(p); }
			}
			return pathsRightHere;
		}

		private bool GetNextNotOnPaths(List<Path> otherPaths, Maze maze, Coord c, out Coord next, out int connectionsFromHere) {
			connectionsFromHere = 0;
			next = c;
			for (int i = 0; i < 4; ++i) {
				if (maze.CellConnects(c, i, out Coord n)) {
					++connectionsFromHere;
					Path neighborPathClaimsThis = otherPaths.Find(p => p.path.IndexOf(n) >= 0);
					if(neighborPathClaimsThis == null) {
						next = n;
					}
				}
			}
			return next != c;
		}

		// TODO discover why path 21 and 22 don't register as the same path
		public static bool FindStartOfPath(Coord start, Maze m, List<Coord> path, CellState[] travelOrder = null) {
			if (travelOrder == null) travelOrder = WallEdge.directions;
			int edges = 0, moves = 0;
			Coord c = start, validNext = start;
			path.Clear();
			do {
				edges = 0;
				for (int i = 0; i < travelOrder.Length; ++i) {
					Coord next;
					if (m.CellConnects(c, travelOrder[i], out next) && !path.Contains(next)) {
						validNext = next;
						edges++;
					}
				}
				if (edges == 0) { break; }
				if(edges > 1 && moves > 0) { break; }
				if (CheckOpenArea(m, c, CellState.Down, CellState.Right)) { break; }
				if (CheckOpenArea(m, c, CellState.Down, CellState.Left)) { break; }
				if (CheckOpenArea(m, c, CellState.Up, CellState.Left)) { break; }
				if (CheckOpenArea(m, c, CellState.Up, CellState.Right)) { break; }
				path.Add(c);
				c = validNext;
				++moves;
				if (moves > 10000) throw new Exception("too much pathing");
			} while (true);
			return path.Count > 0;
		}

		private static bool CheckOpenArea(Maze m, Coord c, CellState dirA, CellState dirB) {
			CellState flag = (dirA | dirB), opposite = (dirA.OppositeWall() | dirB.OppositeWall());
			return ((m[c] & flag) == 0 && (m[c + WallEdge.DirToCoord(dirA) + WallEdge.DirToCoord(dirB)] & opposite) == 0);
		}

		public Path(IList<Coord> seedCells, Maze m) {
			path.AddRange(seedCells);
			Coord cursor = seedCells[seedCells.Count - 1];
			int counter = 0, max = m.size.X * m.size.Y;
			Coord next;
			do {
				if (GetNextOnPath(m, cursor, out next)) {
					cursor = next;
					path.Add(cursor);
				} else {
					break;
				}
			} while (++counter < max);
			RemoveDuplicates();
			path.RemoveAt(path.Count - 1);
		}

		public void RemoveDuplicates() {
			for(int i = 0; i < path.Count; ++i) {
				for(int j = i+1; j < path.Count; ++j) {
					if(path[i] == path[j]) {
						path.RemoveAt(j--);
					}
				}
			}
		}

		// create path that avoids other paths
		public Path(Coord cursor, Maze m) {
			path.Add(cursor);
			int counter = 0, max = m.size.X * m.size.Y;
			Coord next;
			do {
				if (GetNextOnPath(m, cursor, out next)) {
					cursor = next;
					path.Add(cursor);
				} else {
					break;
				}
			} while (++counter < max);
			RemoveDuplicates();
			path.RemoveAt(path.Count - 1);
		}
	}
}