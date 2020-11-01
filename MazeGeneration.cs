using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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

	public class Maze {
		public readonly CellState[,] cells;
		public int[,] marks;
		public readonly Coord size;
		public readonly Random rng;
		public List<MazeRoomBase> allRooms = new List<MazeRoomBase>();
		public List<MazeRoomBase> terminals = new List<MazeRoomBase>();
		public List<MazeRoomBase> hallways = new List<MazeRoomBase>();
		public List<MazeRoomBase> closets = new List<MazeRoomBase>();
		public List<MazeRoomBase> aabbRooms = new List<MazeRoomBase>();
		int deadEnds;
		bool showMarks = true;

		public MazeGraph graph = new MazeGraph();

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
			aabbRooms = MazeAabbRoom.Discover(this, allRooms); // TODO when looking for rooms, don't intersect dead end paths or tunnel paths.

			for (int i = 0; i < aabbRooms.Count; ++i) {
				aabbRooms[i].ForEach(c => marks.SetAt(c, i + allRooms.Count));
			}

			//if (true) return; // ignore the room merge for now... still working on individual small room generation
			List<MazeRoomBase> aggregateRooms = MazeRoomAggregate.MergeRooms(this, aabbRooms);
			for (int i = 0; i < aggregateRooms.Count; ++i) {
				int id = allRooms.Count + i + 1;
				aggregateRooms[i].ForEach(c => marks.SetAt(c, id));
			}
			//if (true) return; // ignore the final room merge for now... still working on individual room merging
			List<MazeRoomBase> merged = MazeRoomAggregate.MergeRooms(this, aggregateRooms);
			for (int i = 0; i < merged.Count; ++i) {
				allRooms.Add(merged[i]);
				merged[i].ForEach(c => marks.SetAt(c, allRooms.Count));
			}

			graph.Generate(this, allRooms);

			//List<MazeGraph.Node> path = graph.A_Star(GetNodeAt(new Coord(0, 0)), GetNodeAt(new Coord(6, 6)));
			//Console.WriteLine(string.Join(" -> ", path.Select(n => graph.GetId(n).ToString("X"))));
		}

		public MazeGraph.Node GetNodeAt(Coord coord) => graph.GetNodeAt(coord);

		public IList<MazeGraph.Edge> GetEdgesForNodeAt(Coord coord) => graph.GetEdgesForNodeAt(coord.InverseScale(tileSize));

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
		/// as <see cref="CellConnects(Coord, CellState, out Coord)"/>, except it also includes blocked edges
		public bool CellBorders(Coord cell, CellState dir, out Coord next) {
			next = cell + WallEdge.DirToCoord(dir);
			return cell.IsWithin(size); //&& !this[cell].HasFlag(dir);
		}

		public Coord tileSize = new Coord { col = 3, row = 2 };
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
					if(showMarks && marks != null) {
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
				MazePath newPath = new MazePath(end, this);
				allRooms.Add(newPath);
				terminals.Add(newPath);
				if(newPath.path.Count == 1) { closets.Add(newPath); }
				MarkPath(newPath.path, allRooms.Count);
			}
			deadEnds = allRooms.Count;
			// now find tunnels (single-cell width paths that are not dead-ends)
			List<Coord> minorPath = new List<Coord>();
			size.ForEach(c => {
				if (marks.At(c) != 0) return;
				int edgeCount = GetEdgeCount(c);
				if(edgeCount == 2) {
					if(MazePath.FindStartOfPath(c, this, minorPath)) {
						minorPath.Reverse();
						//marks.SetAt(minorPath[0], minorPath.Count);// paths.Count + 1);
						MazePath newPath = new MazePath(minorPath, this);
						if (newPath.path.Count == 0) { newPath.path.Add(minorPath[0]); } // 1-unit hallways need an extra push
						allRooms.Add(newPath);
						hallways.Add(newPath);
						MarkPath(newPath.path, allRooms.Count);
					}
				}
			});
		}
	}
}