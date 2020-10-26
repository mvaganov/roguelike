using System;
using System.Collections.Generic;

namespace MazeGeneration
{
		public abstract class MazeRoomBase {
		public abstract bool Contains(Coord c);
		public abstract void ForEach(Action<Coord> action);
		public virtual List<WallEdge> GetEdges(Maze m) { return GetEdges(m, false); }
		public abstract List<WallEdge> GetEdges(Maze m, bool includeUnlikelyConnections);
		public abstract int GetPerimeter();
		public abstract bool GetMinMax(out Coord min, out Coord max);
		public abstract int GetArea();
		public abstract Coord GetCoord();
	}

	public class MazePath : MazeRoomBase {
		public List<Coord> path = new List<Coord>();

		public override bool Contains(Coord c) { return path.IndexOf(c, 0, path.Count) >= 0; }

		public override void ForEach(Action<Coord> action) { path.ForEach(action); }

		public bool Add(Coord coord) {
			if (path.Contains(coord)) return false;
			path.Add(coord);
			return true;
		}

		public override Coord GetCoord() {
			if(path.Count >= 1) {
				return path[path.Count/2];
			}
			return Coord.Zero;
		}
		public override List<WallEdge> GetEdges(Maze m) => GetEdges(m, false);

		public override List<WallEdge> GetEdges(Maze m, bool includeUnlikelyConnections) {
			List<WallEdge> edges = new List<WallEdge>();
			for(int i = 0; i < path.Count; ++i) {
				for(int e = 0; e < WallEdge.directions.Length; ++e) {
					CellState dir = WallEdge.directions[e];
					Coord next;
					bool includeNext = !includeUnlikelyConnections
						? m.CellConnects(path[i], dir, out next)
						: m.CellBorders(path[i], dir, out next);
					if (includeNext && !Contains(next)) {
						edges.Add(new WallEdge { dir = dir, next = next });
					}
				}
			}
			return edges;
		}

		public override bool GetMinMax(out Coord min, out Coord max) {
			if (path == null || path.Count == 0) { min = max = Coord.Zero; return false; }
			min = max = path[0];
			for (int i = 0; i < path.Count; ++i) {
				Coord p = path[i];
				Coord.ExpandRectangle(p, p, ref min, ref max);
			}
			return true;
		}

		public override int GetArea() { return path.Count; }

		public override int GetPerimeter() { return path.Count * 2 + 2; }

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

		private List<MazePath> OtherPathsRightHere(List<MazeRoomBase> allPaths, Coord c) {
			List<MazePath> pathsRightHere = new List<MazePath>();
			foreach (MazePath p in allPaths) {
				if (p.path.Contains(c)) { pathsRightHere.Add(p); }
			}
			return pathsRightHere;
		}

		private bool GetNextNotOnPaths(List<MazePath> otherPaths, Maze maze, Coord c, out Coord next, out int connectionsFromHere) {
			connectionsFromHere = 0;
			next = c;
			for (int i = 0; i < 4; ++i) {
				if (maze.CellConnects(c, i, out Coord n)) {
					++connectionsFromHere;
					MazePath neighborPathClaimsThis = otherPaths.Find(p => p.path.IndexOf(n) >= 0);
					if(neighborPathClaimsThis == null) {
						next = n;
					}
				}
			}
			return next != c;
		}

		public static bool FindStartOfPath(Coord start, Maze m, List<Coord> path, CellState[] travelOrder = null) {
			if (travelOrder == null) travelOrder = WallEdge.directions;
			int edges = 0, moves = 0;
			Coord c = start, validNext = start, previousValidNext = start, possibleAlternate = start;
			path.Clear();
			do {
				if(m.marks.At(c) != 0) { break; }
				edges = 0;
				for (int i = 0; i < travelOrder.Length; ++i) {
					Coord next;
					if (m.CellConnects(c, travelOrder[i], out next) && !path.Contains(next)) {
						previousValidNext = validNext;
						validNext = next;
						edges++;
					}
				}
				if (edges == 0) { break; }
				// the following 2 if statements will restart the path on the other side of the first valid cell if the initial direction is bad
				if (edges == 2 && moves == 0) { possibleAlternate = previousValidNext; }
				if (edges > 1 && moves == 1 && possibleAlternate != start) { c = possibleAlternate; possibleAlternate = start; continue; }
				if (edges > 1 && moves > 0) { break; }
				if (CheckOpenArea(m, c, CellState.Down, CellState.Right)) { break; }
				if (CheckOpenArea(m, c, CellState.Down, CellState.Left)) { break; }
				if (CheckOpenArea(m, c, CellState.Up, CellState.Left)) { break; }
				if (CheckOpenArea(m, c, CellState.Up, CellState.Right)) { break; }
				path.Add(c);
				c = validNext;
				++moves;
				if (moves > 10000) throw new Exception("too much pathing");
			} while (true);
//			Console.Write("");
			return path.Count > 0;
		}

		private static bool CheckOpenArea(Maze m, Coord c, CellState dirA, CellState dirB) {
			CellState flag = (dirA | dirB), opposite = (dirA.OppositeWall() | dirB.OppositeWall());
			CellState thisOne = m[c];
			if ((thisOne & flag) != 0) return false;
			Coord kittyCornerLoc = c + WallEdge.DirToCoord(dirA) + WallEdge.DirToCoord(dirB);
			CellState kittyCorner = m[kittyCornerLoc];
			return (kittyCorner & opposite) == 0;
		}

		public MazePath() { }

		public MazePath(IList<Coord> seedCells, Maze m) {
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
		public MazePath(Coord cursor, Maze m) {
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

	public class MazeAabbRoom : MazeRoomBase
	{
		public Coord min, max;

		public Coord Size => max - min;

		public override string ToString() { return "{" + Size + " @" + min + "}"; }

		public override bool Contains(Coord c) { return c.IsWithin(min, max); }

		public override void ForEach(Action<Coord> action) { Coord.ForEach(min, max, action); }

		public override int GetPerimeter() { return Size.col * 2 + Size.row * 2; }

		public override int GetArea() { Coord s = Size; return s.col * s.row; }

		public override bool GetMinMax(out Coord min, out Coord max) { min = this.min; max = this.max; return true; }

		public override Coord GetCoord() => new Coord((min.X + max.X) / 2, (min.Y + max.Y) / 2);

		public override List<WallEdge> GetEdges(Maze m, bool includeUnlikelyConnections) {
			List<WallEdge> edges = new List<WallEdge>();
			Coord start, end;
			CellState dir;
			CellState[] dirs = new CellState[] { CellState.Up, CellState.Right, CellState.Down, CellState.Left };
			Coord[] corners = new Coord[] {
				new Coord { col =         min.col,      row =         min.row },
				new Coord { col = (short)(max.col - 1), row =         min.row },
				new Coord { col = (short)(max.col - 1), row = (short)(max.row - 1) },
				new Coord { col =         min.col,      row = (short)(max.row - 1) }
			};
			for (int i = 0; i < dirs.Length; ++i) {
				dir = dirs[i];
				start = corners[i];
				end = corners[(i + 1) % corners.Length];
				Coord.ForEachInclusive(start, end, p => {
					Coord next;
					if (!includeUnlikelyConnections ? m.CellConnects(p, dir, out next) : m.CellBorders(p, dir, out next)) {
						edges.Add(new WallEdge { dir = dir, next = next });
					}
				});
			}
			return edges;
		}

		public static List<MazeRoomBase> Discover(Maze m, List<MazeRoomBase> allRooms) {
			List<MazeRoomBase> rooms = new List<MazeRoomBase>();
			Coord s = m.size;
			Coord cursor = Coord.Zero;
			for(cursor.row = 0; cursor.row < s.row; ++cursor.row) {
				for (cursor.col = 0; cursor.col < s.col; ++cursor.col) {
					int roomIndex = rooms.FindIndex(r => r.Contains(cursor));
					if(roomIndex >= 0) {
						MazeAabbRoom mr = rooms[roomIndex] as MazeAabbRoom;
						cursor.col = (short)(mr.max.col-1);
					} else {
						int otherRoomindex = allRooms.FindIndex(r => r.Contains(cursor));
						if (otherRoomindex < 0) {
							MazeAabbRoom mr = BiggestAt(m, cursor, rooms);
							rooms.Add(mr);
							cursor.col = (short)(mr.max.col - 1);
						}
					}
				}
			}
			return rooms;
		}

		public static MazeAabbRoom BiggestAt(Maze m, Coord c, List<MazeRoomBase> rooms) {
			Coord size = Coord.One;
			RoomExpand r;
			int counter = 0;
			
			while( (r = GetRoomExpandability(m, c, size, rooms)) != RoomExpand.None) {
				Coord change = Coord.Zero;
				switch (r) {
					case RoomExpand.RightAndDown: change = Coord.One; break;
					case RoomExpand.Right: change = Coord.Right; break;
					case RoomExpand.Down: change = Coord.Down; break;
				}
				size += change;
				// if a thin strip of a room is growing, stop this one's growth if it has a chance of interrupting another room in the future
				if (size.col == 1 && size.row > 1) {
					if (!IsWallConsistent(m, c, size.row, Coord.Down, CellState.Left, CellState.Right)) {
						size -= change;
						break;
					}
				}
				if (size.row == 1 && size.col > 1) {
					// check horizontal neighbors. if there is an inconsistency, undo the growth and stop growing
					if (!IsWallConsistent(m, c, size.col, Coord.Right, CellState.Up, CellState.Down)) {
						size -= change;
						break;
					}
				}
				if (counter++ > 1000) { throw new Exception("infinite loop?"); }
			}
			return new MazeAabbRoom { min = c, max = c + size };
		}

		private static bool IsWallConsistent(Maze m, Coord c, int size, Coord direction, CellState dirA, CellState dirB) {
			// check horizontal/vertical neighbors, if there is an inconsistency in neighbor type
			bool wallShouldbeLeft = (m[c] & dirA) != 0, wallShouldbeRight = (m[c] & dirB) != 0;
			Coord p = c;
			bool consistent = true;
			for (int i = 1; i < size; ++i) {
				p += direction;
				bool wallOnLeft = (m[p] & dirA) != 0, wallOnRight = (m[p] & dirB) != 0;
				if (wallShouldbeLeft != wallOnLeft || wallOnRight != wallShouldbeRight) {
					consistent = false;
					break;
				}
			}
			return consistent;
		}

		public enum RoomExpand { None, Right, Down, RightAndDown }
		public static bool NextIsRoom(Coord c, CellState dir, List<MazeRoomBase> rooms) {
			Coord next = c + WallEdge.DirToCoord(dir);
			return rooms.FindIndex(r => r.Contains(next)) >= 0;
		}
		public static RoomExpand GetRoomExpandability(Maze m, Coord c, Coord size, List<MazeRoomBase> rooms) {
			Coord delta = new Coord(size.col-1, size.row-1);
			bool rightIsClear = true, downIsClear = true;
			Coord cursor = c;
			cursor.col += delta.col;
			for(int i = 0; i < size.row; ++i) {
				if(cursor.row >= m.size.row || !m.CellConnects(cursor, CellState.Right) || NextIsRoom(cursor, CellState.Right, rooms)) { rightIsClear = false; break; }
				if(i < size.row-1 && !m.CellConnects(cursor + Coord.Right, CellState.Down)) { rightIsClear = false; break; }
				++cursor.row;
			}
			cursor = c;
			cursor.row += delta.row;
			for(int i = 0; i < size.col; ++i) {
				if(cursor.col >= m.size.col || !m.CellConnects(cursor, CellState.Down) || NextIsRoom(cursor, CellState.Down, rooms)) { downIsClear = false; break; }
				if(i < size.col-1 && !m.CellConnects(cursor + Coord.Down, CellState.Right)) { downIsClear = false; break; }
				++cursor.col;
			}
			bool bothConnect = false;
			if(rightIsClear && downIsClear) {
				cursor = c + size;
				bothConnect = rooms.FindIndex(r => r.Contains(cursor)) < 0 && m.CellConnects(cursor, CellState.Left) && m.CellConnects(cursor, CellState.Up);
			}
			RoomExpand result = RoomExpand.None;
			if (bothConnect) { result = RoomExpand.RightAndDown; } else
			if (rightIsClear) { result = RoomExpand.Right; } else
			if (downIsClear) { result = RoomExpand.Down; }
			return result;
		}

		public static bool IsConnected(Maze m, MazeAabbRoom a, MazeAabbRoom b) {
			bool hMatch = (a.min.col >= b.min.col && a.max.col <= b.max.col) || (b.min.col >= a.min.col && b.max.col <= a.max.col);
			bool vMatch = (a.min.row >= b.min.row && a.max.row <= b.max.row) || (b.min.row >= a.min.row && b.max.row <= a.max.row);
			if (!hMatch && !vMatch) return false;
			bool aAbove = false, aBelow = false, aLeft = false, aRight = false;
			if (hMatch) { aAbove = a.max.row == b.min.row; aBelow = a.min.row == b.max.row; }
			if (vMatch) { aLeft = a.max.col == b.min.col; aRight = a.min.col == b.max.col; }
			if (!aAbove && !aBelow && !aLeft && !aRight) { return false; }
			if (aAbove) {
				// check all Down col at a.max.row-1 or all Up col at b.min.row
				if(IsMazeRoomConnecting(m, new Coord(a.min.col, a.max.row - 1), new Coord(a.max.col - 1, a.max.row - 1), CellState.Down)
				|| IsMazeRoomConnecting(m, new Coord(b.min.col, b.min.row),     new Coord(b.max.col - 1, b.min.row),     CellState.Up)) {
					return true;
				}
			}
			if (aBelow) {
				// check all Up col at a.min.row or all Down col at b.max.row-1
				if(IsMazeRoomConnecting(m, new Coord(a.min.col, a.min.row),     new Coord(a.max.col - 1, a.min.row),     CellState.Up)
				|| IsMazeRoomConnecting(m, new Coord(b.min.col, b.max.row - 1), new Coord(b.max.col - 1, b.max.row - 1), CellState.Down)) {
					return true;
				}
			}
			if (aLeft) {
				// check all Right row at a.max.col-1 or all Left row at b.min.col
				if(IsMazeRoomConnecting(m, new Coord(a.max.col - 1, a.min.row), new Coord(a.max.col - 1, a.max.row - 1), CellState.Right)
				|| IsMazeRoomConnecting(m, new Coord(b.min.col,     b.min.row), new Coord(b.min.col,     b.max.row - 1), CellState.Left)) {
					return true;
				}
			}
			if (aRight) {
				// check all Left row at a.min.col or all Right row at b.max.col-1
				if(IsMazeRoomConnecting(m, new Coord(a.min.col,     a.min.row), new Coord(a.min.col,     a.max.row - 1), CellState.Left)
				|| IsMazeRoomConnecting(m, new Coord(b.max.col - 1, b.min.row), new Coord(b.max.col - 1, b.max.row - 1), CellState.Right)) {
					return true;
				}
			}
			return false;
			//return true;
		}

		private static bool IsMazeRoomConnecting(Maze m, Coord min, Coord max, CellState dir) {
			max += Coord.One;
			bool isBlocked = Coord.ForEach(min, max, c => {
				CellState cell = m[c];
				return (cell & dir) != 0; // if there is a wall in this direction
			});
			return !isBlocked;
		}
	}

	public class MazeRoomAggregate : MazeRoomBase
	{
		public int id;
		public List<MazeRoomBase> parts = new List<MazeRoomBase>();
		public override bool Contains(Coord c) {
			for(int i = 0; i < parts.Count; ++i) { if (parts[i].Contains(c)) return true; }
			return false;
		}

		public override void ForEach(Action<Coord> action) {
			for(int i = 0; i < parts.Count; ++i) {
				parts[i].ForEach(action);
			}
		}

		public override List<WallEdge> GetEdges(Maze m, bool includeUnlikelyConnections) {
			List<WallEdge> outerEdges = new List<WallEdge>();
			for(int room = 0; room < parts.Count; ++room) {
				List<WallEdge> roomEdges = parts[room].GetEdges(m, includeUnlikelyConnections);
				//Console.WriteLine("room "+room+" "+parts[room]+" has " + roomEdges.Count+" edges total");
				for (int e = 0; e < roomEdges.Count; ++e) {
					if (!Contains(roomEdges[e].next)) {
						//Console.Write(roomEdges[e] + " ");
						outerEdges.Add(roomEdges[e]);
					}
				}
			}
			return outerEdges;
		}

		public override Coord GetCoord() { return parts[0].GetCoord(); }

		public override int GetArea() {
			int sum = 0;
			parts.ForEach(r => sum += r.GetArea());
			return sum;
		}

		public override int GetPerimeter() {
			if(!GetMinMax(out Coord min, out Coord max)) { return 0; }
			Coord delta = max - min;
			return delta.col * 2 + delta.row * 2;
		}

		public override bool GetMinMax(out Coord min, out Coord max) {
			if(parts == null || parts.Count == 0) { min = max = Coord.Zero; return false; }
			parts[0].GetMinMax(out min, out max);
			for(int i = 1; i < parts.Count; ++i) {
				parts[i].GetMinMax(out Coord partMin, out Coord partMax);
				Coord.ExpandRectangle(partMin, partMax, ref min, ref max);
			}
			return true;
		}


		public static MazeRoomBase WantsToMerge(Maze m, MazeRoomBase a, MazeRoomBase b) {
			if (WantsTomergeIndividualCheck(m, a, b)) return a;
			if (WantsTomergeIndividualCheck(m, b, a)) return b;
			return null;
		}

		private static bool WantsTomergeIndividualCheck(Maze m, MazeRoomBase a, MazeRoomBase b) {
			int aPer = a.GetPerimeter();
			List<WallEdge> aEdges = a.GetEdges(m);
			for(int i = aEdges.Count-1; i >= 0; --i) {
				if (!b.Contains(aEdges[i].next)) { // remove edges that arent connected to the other room
					aEdges.RemoveAt(i);
				}
			}
			return (aEdges.Count >= aPer / 4f);
		}

		public static List<MazeRoomBase> MergeRooms(Maze m, List<MazeRoomBase> rooms) {
			List<MazeRoomBase> chonkies = new List<MazeRoomBase>();
			for (int a = 0; a < rooms.Count; ++a) {
				MazeRoomBase roomA = rooms[a];
				rooms.RemoveAt(a--);
				for (int b = 0; b < rooms.Count; ++b) {
					MazeRoomBase roomB = rooms[b];
					if (WantsToMerge(m, roomA, roomB) != null) {
						rooms.RemoveAt(b--);
						if (roomA is MazeRoomAggregate crA) {
							if(roomB is MazeRoomAggregate crB) {
								crA.parts.AddRange(crB.parts);
							} else if(roomB is MazeAabbRoom mr) {
								crA.parts.Add(mr);
							}
							else { throw new Exception("don't know how to merge "+roomA.GetType()+" with "+roomB.GetType()); }
						} else if(roomA is MazeAabbRoom mrA) {
							if(roomB is MazeAabbRoom mrB) {
								MazeRoomAggregate cr = new MazeRoomAggregate();
								cr.parts.Add(mrA);
								cr.parts.Add(mrB);
								roomA = cr;
							} else if(roomB is MazeRoomAggregate crB) {
								crB.parts.Add(mrA);
								roomA = crB;
							}
							else { throw new Exception("don't know how to merge "+roomA.GetType()+" with "+roomB.GetType()); }
						}
						else { throw new Exception("don't know how to merge "+roomA.GetType()+" with "+roomB.GetType()); }
					}
				}
				chonkies.Add(roomA);
			}
			return chonkies;
		}

		public static List<MazeRoomAggregate> MergeRooms(Maze m, List<MazeRoomAggregate> rooms) {
			List<MazeRoomAggregate> chonkies = new List<MazeRoomAggregate>();
			for (int i = 0; i < rooms.Count; ++i) {
				MazeRoomAggregate chungus = rooms[i];
				rooms.RemoveAt(i--);
				for (int j = 0; j < rooms.Count; ++j) {
					if (WantsToMerge(m, chungus, rooms[j]) != null) {
						chungus.parts.AddRange(rooms[j].parts);
						rooms.RemoveAt(j--);
					}
				}
				chonkies.Add(chungus);
			}
			return chonkies;
		}
	}
}