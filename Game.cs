using System;
using System.Collections.Generic;
using System.Linq;
using MazeGeneration;

public class Game : GameBase {
	public static int Main(string[] args) {
		Game g = new Game();
		g.Init(new Coord { row = 15, col = 40});
		while(g.IsRunning()) {
			g.Draw();
			g.UserInput();
			g.Update();
		}
		g.Release();
		return 0;
	}

	Entity2D maze;
	EntityBase player, goal, npc;
	List<EntityBase> fireballs = new List<EntityBase>();
	Random rng = new Random(0);
	string blockingWalls = "#+-|", erodedDebris = "    `   '  . .,;:=";
	Coord mazeSize;
	Maze mazeGen;
	Dictionary<string,float> keys = new Dictionary<string, float>();

	RoguelikeVisibility playerVisibilityAlgorithm;
	System.Collections.BitArray playerScreenVisibility, playerSeenIt;
	int playerSightRange = 15;
	string playerCanSeeThrough = " `'.,*\'\"01234567890ABCDEFG";

	protected override void Init(Coord screenSize) {
		base.Init(screenSize);
		maze = new Entity2D("maze", '/');//, new Coord(2,1), new Coord { row = 10, col = 30 });
		//try {
			mazeGen = new Maze(new Coord(30, 20), 2, 5);
			mazeGen.graph.DebugPrint(Coord.Zero);
			maze.LoadFromString(mazeGen.ToString());//maze.LoadFromFile("bigmaze.txt");
//			MazeErosion(maze.map, blockingWalls, erodedDebris);
			System.IO.File.WriteAllText(@"../../mazeout.txt", maze.map.ToString());
		//} catch (Exception e) {
		//	Console.WriteLine(e);
		//	Console.ReadKey();
		//}
		entities.Add(maze);
		foreach (var kvp in mazeGen.graph.keysAndDoors) {
			string name = kvp.Key;
			char lastLetter = name[name.Length - 1];
			MazeGraph.Node n = kvp.Value.Key;
			MazeGraph.Edge e = kvp.Value.Value;
			Coord position = n.nodeData.GetCoord().Scale(mazeGen.tileSize) + Coord.One;
			EntityBasic key = new EntityBasic(name, new ConsoleTile(lastLetter, ConsoleColor.Yellow), position);
			key.onUpdate = () => {
				if(key.IsIntersecting(player, null)) {
					keys.TryGetValue(name, out float keyCount);
					keys[name] = keyCount + 1;
					entities.Remove(key);
				}
			};
			entities.Add(key);
			//EntityComposite entireDoor = new EntityComposite();
			List<Coord> doorPositions = (e.edgeData as MazePath).path;
			for(int i = 0; i < doorPositions.Count; ++i) {
				position = doorPositions[i].Scale(mazeGen.tileSize);
				Entity2D door = new Entity2D("door"+name, new ConsoleTile(lastLetter, ConsoleColor.Black, ConsoleColor.DarkYellow), position, mazeGen.tileSize);
				door.GetMinMax(out Coord min, out Coord max);
				Coord.ForEach(min, max, c => {
					if(blockingWalls.IndexOf(maze[c]) >= 0) {
						door[c] = '\0';
					}
				});
				door.onTrigger = triggeringEntity => {
					if(triggeringEntity == player) {
						if(keys.TryGetValue(name, out float keyCount)) {
							Destroy(door);
							Console.WriteLine($"using {name}");
						} else {
							Console.WriteLine($"need {name}");
						}
					}
				};
				colliders.Add(door);
				entities.Add(door);
				//entireDoor.AddPart(door);
			}
			//entities.Add(entireDoor);
			//collisionLayer.Add(entireDoor);
			Console.WriteLine($"added {name} to {key.position}, and door to {string.Join(", ",doorPositions)}");
		}
		Console.ReadKey();

		player = new EntityBasic("player", '@', new Coord { row = 3, col = 8 });
		goal = new Entity2D("goal", new ConsoleTile('G', ConsoleColor.Green), new Coord { row = 7, col = 34}, new Coord(2,2));
		npc = new EntityMobileObject("npc", new ConsoleTile('M', ConsoleColor.Magenta), new Coord { row = 9, col = 12 });
		entities.Add(goal);
		entities.Add(player);
		entities.Add(npc);
		colliders.Add(player);
		colliders.Add(npc);

		Dictionary<ConsoleKey, char> arrowKeyConversion = new Dictionary<ConsoleKey, char>() {
			{ConsoleKey.LeftArrow, 'a' },
			{ConsoleKey.UpArrow,   'w' },
			{ConsoleKey.RightArrow,'d' },
			{ConsoleKey.DownArrow, 's' },
		};
		player.onUpdate += () => {
			if(!arrowKeyConversion.TryGetValue(keyIn.Key, out char userMoveInput)) {
				userMoveInput = keyIn.KeyChar;
			}
			if (player.MoveDirection(userMoveInput) != Coord.Zero) {
				EntityMoveBlockedByMaze(player, userMoveInput);
				KeepVisiblity(player.position);
			}
			if (userMoveInput == ' ' && fireballs.Count < 3) {
				ShootFireball(player);
			}
			if (player.IsIntersecting(goal)) {
				PlayerWins();
			}
		};
		npc.onUpdate += () => {
			EntityRandomMove(npc);
			if(npc.IndexOfIntersecting(fireballs, null) >= 0) {
				entities.Remove(npc);
			}
		};
		mazeSize = maze.GetSize();
		scrollMax = mazeSize + maze.position - screenSize;
		playerSeenIt = new System.Collections.BitArray(mazeSize.X * mazeSize.Y);
		playerScreenVisibility = new System.Collections.BitArray(screen.Width * screen.Height);
		playerVisibilityAlgorithm = new MilazzoVisibility(PlayerVisionBlockedAt, MarkVisibleByPlayer, ManhattanDistance);
	}

	protected override void Draw() {
		DrawEntities();
		ColorScreenByPlayerVisibility();
		//DrawRoomExits();
		Render();
		Console.Write(fireballs.Count);
	}

	public void DrawRoomExits() {
		IList<MazeGraph.Edge> edges = mazeGen.GetEdgesForNodeAt(player.position);
		for(int e = 0; e < edges.Count; ++e) {
			MazeGraph.Edge edge = edges[e];
			MazePath mp = edge.edgeData as MazePath;
			mp.ForEach(c => {
				c = c.Scale(mazeGen.tileSize);
				Coord s = c - screenOffset;
				Coord.ForEach(s + Coord.One, s + mazeGen.tileSize, p => {
					if (screen.Contains(p)) {
						ConsoleTile ct = screen[p];
						ct.Back = ConsoleColor.Yellow;
						screen[p] = ct;
					}
				});
			});
		}
	}


	public bool PlayerVisionBlockedAt(Coord screenCoord) {
		if (!screenCoord.IsWithin(screen.Size)) return true;
		ConsoleTile ct = screen[screenCoord];
		if (ct.back != (byte)Console.BackgroundColor) return true;
		return playerCanSeeThrough.IndexOf(ct.letter) < 0;
	}

	public void SetPlayerHasSeen(Coord c, bool value) {
		int i = c.Y * mazeSize.X + c.X; if (i < 0 || i >= playerSeenIt.Count) return;
		playerSeenIt.Set(i, value);
	}
	public bool GetPlayerHasSeen(Coord c) {
		int i = c.Y * mazeSize.X + c.X; if (i < 0 || i >= playerSeenIt.Count) return false;
		return playerSeenIt.Get(i);
	}

	public void MarkVisibleByPlayer(Coord screenCoord) {
		if (!screenCoord.IsWithin(screen.Size)) return;
		playerScreenVisibility.Set(screenCoord.Y * screen.Width + screenCoord.X, true);
		SetPlayerHasSeen(screenCoord + screenOffset, true);
	}

	public static int ManhattanDistance(Coord delta) => Math.Abs(delta.X) + Math.Abs(delta.Y);

	protected void ColorScreenByPlayerVisibility() {
		playerScreenVisibility.SetAll(false);
		playerVisibilityAlgorithm.Compute(player.position - screenOffset, playerSightRange);
		ConsoleTile hiddenTile = new ConsoleTile('.', ConsoleColor.Black, ConsoleColor.DarkGray);
		ConsoleTile fog = new ConsoleTile('?', ConsoleColor.Black, ConsoleColor.DarkGray);
		int index = 0;
		Coord.ForEach(Coord.Zero, screen.Size, c => {
			if(!GetPlayerHasSeen(c + screenOffset)) {
				screen[c] = fog;
			} else if (!playerScreenVisibility.Get(index)) {
				hiddenTile = screen[c];
				hiddenTile.Fore = hiddenTile.Fore == ConsoleColor.Gray ? (GetPlayerHasSeen(c + screenOffset) ? ConsoleColor.DarkGray : ConsoleColor.Black) : ConsoleColor.Black;
				screen[c] = hiddenTile;
			}
			++index;
		});
	}

	// could be replaced with: coord => maze[coord] == '#'
	public bool IsMazeWall(Coord coord) { return blockingWalls.IndexOf(maze[coord]) >= 0; }

	public bool IsMazeWall(Rect rect) { return rect.ForEach(IsMazeWall); }

	public void MazeErosion(Map2D map, string erodable, string erosionOrder) {
		for (int i = 0; i < erosionOrder.Length; ++i) {
			ErodeMaze(maze.map, erodable, new ConsoleTile(erosionOrder[i], ConsoleColor.DarkGray));
		}
	}

	public void ErodeMaze(Map2D map, string wallsToReplace, ConsoleTile replacement) {
		byte[,] neighborCount = new byte[map.Height, map.Width];
		Coord s = map.Size;
		Coord.ForEach(Coord.Zero, s, coord => {
			if (wallsToReplace.IndexOf(map[coord].letter) < 0) return;
			byte n = 0;
			if (coord.row > 0       && wallsToReplace.IndexOf(map[coord + Coord.Up]) >= 0) { ++n; }
			if (coord.col > 0       && wallsToReplace.IndexOf(map[coord + Coord.Left]) >= 0) { ++n; }
			if (coord.row < s.row-1 && wallsToReplace.IndexOf(map[coord + Coord.Down]) >= 0) { ++n; }
			if (coord.col < s.col-1 && wallsToReplace.IndexOf(map[coord + Coord.Right]) >= 0) { ++n; }
			neighborCount[coord.row, coord.col] = n;
		});
		Coord.ForEach(Coord.Zero, s, coord => {
			if (neighborCount[coord.row, coord.col] == 1) {
				map[coord] = replacement;
			}
		});
	}

	public void EntityRandomMove(EntityBase npc) {
		EntityMobileObject mob = npc as EntityMobileObject;
		if(mob == null) { throw new Exception("can't randomly move "+npc); }
		if (rng.Next() % 2 == 0) {
			int randomNum = rng.Next() % npc.moves.Count;
			mob.currentMove = npc.moves.ElementAt(randomNum).Key;
		}
		EntityMoveBlockedByMaze(mob, mob.currentMove);
	}

	/// <returns>true if entity moved, then collided with walls in a way that is defined by whatTriggersCollision, and was pushed back</returns>
	public bool EntityMoveBlockedByMaze(EntityBase entity, char move) {
		Coord oldPosition = entity.position;
		entity.Move(move);
		if (!IsInScrollArea(entity.position)) {
			entity.position = oldPosition;
			return true;
		}
		if(entity.IsIntersecting(maze, IsMazeWall)) {
			entity.position = oldPosition;
			return true;
		}
		return false;
	}

	public void ShootFireball(EntityBase player) {
		char shootDirection = Console.ReadKey().KeyChar;
		if (!player.moves.ContainsKey(shootDirection)) return;
		EntityMobileObject fireball = new EntityMobileObject("fireball", new ConsoleTile('*', ConsoleColor.Red), player.position);
		fireball.currentMove = shootDirection;
		fireball.onUpdate += () => {
			fireball.Move(fireball.currentMove);
			if (!screen.Contains(fireball.position) || fireball.IsIntersecting(maze, IsMazeWall)) {
				fireballs.Remove(fireball);
				entities.Remove(fireball);
			}
		};
		fireballs.Add(fireball);
		entities.Add(fireball);
	}

	public void PlayerWins() {
		Console.SetCursorPosition(0, screen.Height);
		Console.WriteLine("You Win!\n(press escape to quit)");
		do { Console.SetCursorPosition(0, screen.Height + 2);
		} while (Console.ReadKey().KeyChar != 27);
		isRunning = false;
	}
}