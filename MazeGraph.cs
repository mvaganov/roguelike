
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

	public class MazeGraph : Graph<MazeRoom,MazeRoom>
	{

	}
}