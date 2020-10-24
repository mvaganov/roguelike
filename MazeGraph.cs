
using System;
using System.Collections.Generic;

namespace MazeGeneration
{
	public class MazeRoom : MazeSection
	{
		public Coord min, max;

		public Coord Size => max - min;

		public override bool Contains(Coord c) { return c.IsWithin(min, max); }

		public void ForEach(Action<Coord> action) { Coord.ForEach(min, max, action); }

		// TODO don't create rooms that overlap with allRooms
		public static List<MazeRoom> Discover(Maze m, List<MazeSection> allRooms) {
			List<MazeRoom> rooms = new List<MazeRoom>();
			Coord s = m.size;
			Coord cursor = Coord.Zero;
			for(cursor.row = 0; cursor.row < s.row; ++cursor.row) {
				for (cursor.col = 0; cursor.col < s.col; ++cursor.col) {
					int roomIndex = rooms.FindIndex(r => r.Contains(cursor));
					if(roomIndex >= 0) {
						cursor.col = (short)(rooms[roomIndex].max.col-1);
					} else {
						int otherRoomindex = allRooms.FindIndex(r => r.Contains(cursor));
						if (otherRoomindex < 0) {
							MazeRoom mr = BiggestAt(m, cursor, rooms);
							rooms.Add(mr);
							cursor.col = (short)(mr.max.col - 1);
						}
					}
				}
			}
			return rooms;
		}

		public static MazeRoom BiggestAt(Maze m, Coord c, List<MazeRoom> rooms) {
			Coord size = Coord.One;
			RoomExpand r;
			int counter = 0;
			while( (r = GetRoomExpandability(m, c, size, rooms)) != RoomExpand.None) {
				switch (r) {
					case RoomExpand.RightAndDown: size += Coord.One; break;
					case RoomExpand.Right: size += Coord.Right; break;
					case RoomExpand.Down: size += Coord.Down; break;
				}
				if(counter++ > 1000) { throw new Exception("infinite loop?"); }
			}
			return new MazeRoom { min = c, max = c + size };
		}
		public enum RoomExpand { None, Right, Down, RightAndDown }
		public static bool NextIsRoom(Coord c, CellState dir, List<MazeRoom> rooms) {
			Coord next = c + WallEdge.DirToCoord(dir);
			return rooms.FindIndex(r => r.Contains(next)) >= 0;
		}
		public static RoomExpand GetRoomExpandability(Maze m, Coord c, Coord size, List<MazeRoom> rooms) {
			Coord delta = new Coord(size.col-1, size.row-1);
			bool rightConnect = true, downConnect = true;
			Coord cursor = c;
			cursor.col += delta.col;
			for(int i = 0; i < size.row; ++i) {
				if(cursor.row >= m.size.row || !m.CellConnects(cursor, CellState.Right) || NextIsRoom(cursor, CellState.Right, rooms)) { rightConnect = false; break; }
				if(i < size.row-1 && !m.CellConnects(cursor + Coord.Right, CellState.Down)) { rightConnect = false; break; }
				++cursor.row;
			}
			cursor = c;
			cursor.row += delta.row;
			for(int i = 0; i < size.col; ++i) {
				if(cursor.col >= m.size.col || !m.CellConnects(cursor, CellState.Down) || NextIsRoom(cursor, CellState.Down, rooms)) { downConnect = false; break; }
				if(i < size.col-1 && !m.CellConnects(cursor + Coord.Down, CellState.Right)) { downConnect = false; break; }
				++cursor.col;
			}
			bool bothConnect = false;
			if(rightConnect && downConnect) {
				cursor = c + size;
				bothConnect = rooms.FindIndex(r => r.Contains(cursor)) < 0 && m.CellConnects(cursor, CellState.Left) && m.CellConnects(cursor, CellState.Up);
			}
			RoomExpand result = RoomExpand.None;
			if (bothConnect) { result = RoomExpand.RightAndDown; } else
			if (rightConnect) { result = RoomExpand.Right; } else
			if (downConnect) { result = RoomExpand.Down; }
			return result;
		}
	}

	public class ChunkyRoom : MazeSection
	{
		public List<MazeRoom> chunks = new List<MazeRoom>();
		public override bool Contains(Coord c) {
			for(int i = 0; i < chunks.Count; ++i) { if (chunks[i].Contains(c)) return true; }
			return false;
		}

		public void ForEach(Action<Coord> action) {
			for(int i = 0; i < chunks.Count; ++i) {
				chunks[i].ForEach(action);
			}
		}

		public static bool IsConnected(Maze m, MazeRoom a, MazeRoom b) {
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
				|| IsMazeRoomConnecting(m, new Coord(b.min.col, b.min.row),     new Coord(b.max.col - 1, a.min.row),     CellState.Up)) {
					return true;
				}
			}
			//if (aBelow) {
			//	// check all Up col at a.min.row or all Down col at b.max.row-1
			//	if(IsMazeRoomConnecting(m, new Coord(a.min.col, a.min.row),     new Coord(a.max.col - 1, a.min.row),     CellState.Up)
			//	|| IsMazeRoomConnecting(m, new Coord(b.min.col, b.max.row - 1), new Coord(b.max.col - 1, a.max.row - 1), CellState.Down)) {
			//		return true;
			//	}
			//}
			//if (aLeft) {
			//	// check all Right row at a.max.col-1 or all Left row at b.min.col
			//	if(IsMazeRoomConnecting(m, new Coord(a.max.col - 1, a.min.row), new Coord(a.max.col - 1, a.max.row - 1), CellState.Right)
			//	|| IsMazeRoomConnecting(m, new Coord(b.min.col,     b.min.row), new Coord(b.min.col,     a.max.row - 1), CellState.Left)) {
			//		return true;
			//	}
			//}
			//if (aRight) {
			//	// check all Left row at a.min.col or all Right row at b.max.col-1
			//	if(IsMazeRoomConnecting(m, new Coord(a.min.col,     a.min.row), new Coord(a.min.col,     a.max.row - 1), CellState.Left)
			//	|| IsMazeRoomConnecting(m, new Coord(b.max.col - 1, b.min.row), new Coord(b.max.col - 1, a.max.row - 1), CellState.Right)) {
			//		return true;
			//	}
			//}
			return false;
			//return true;
		}

		// TODO check how this is working... maybe make separate loops for horizontal and vertical? i dunno. I'm sleepy.
		static bool IsMazeRoomConnecting(Maze m, Coord min, Coord max, CellState dir) {
			//Coord min = new Coord(Math.Min(a.col, b.col), Math.Min(a.row, b.row));
			//Coord max = new Coord(Math.Max(a.col, b.col), Math.Max(a.row, b.row));
			max += Coord.One;
			bool foundBlock = Coord.ForEach(min, max, c => {
				return (m[c] & dir) != 0;
			});
			return !foundBlock;
		}

		public static List<ChunkyRoom> MergeRooms(Maze m, List<MazeRoom> rooms) {
			List<ChunkyRoom> chonkies = new List<ChunkyRoom>();
			for (int i = 0; i < rooms.Count; ++i) {
				ChunkyRoom chungus = new ChunkyRoom();
				chungus.chunks.Add(rooms[i]);
				rooms.RemoveAt(i--);
				for(int j = 0; j < rooms.Count; ++j) {
					for(int k = 0; k < chungus.chunks.Count; ++k) {
						if (IsConnected(m, chungus.chunks[k], rooms[j])) {
							chungus.chunks.Add(rooms[j]);
							rooms.RemoveAt(j--);
							break;
						}
					}
				}
				chonkies.Add(chungus);
			}
			return chonkies;
		}

	}

	public class MazeGraph : Graph<MazeRoom,MazeRoom>
	{

	}
}