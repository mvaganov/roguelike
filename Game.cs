using System;
using System.Collections.Generic;
using System.Linq;
using MazeGeneration;

public class Game : GameBase {
	public static int Main(string[] args) {
		Game g = new Game();
		g.Init(new Coord (40,15));
		while(g.IsRunning()) {
			g.Draw();
			g.UserInput();
			g.Update();
		}
		g.Release();
		return 0;
	}

	Entity2D maze;
	EntityBase player, goal;
	List<EntityBase> playerFireballs = new List<EntityBase>();
	Dictionary<string, float> playerKeys = new Dictionary<string, float>();
	string blockingWalls = "#+-|";
	Coord mazeSize;
	Maze mazeGen;
	Random rng = new Random(0);

	RoguelikeVisibility playerVisibilityAlgorithm;
	System.Collections.BitArray playerScreenVisibility, playerSeenIt;
	int playerSightRange = 15;
	string playerCanSeeThrough = " `'.,*\'\"01234567890ABCDEFG"; // see through if there is no non-default background

	protected override void Init(Coord screenSize) {
		base.Init(screenSize);
		maze = new Entity2D("maze", '/');
		//try {
			mazeGen = new Maze(new Coord(30, 20), 0, 2);
			mazeGen.graph.DebugPrint(Coord.Zero);
			string theMap = mazeGen.ToString();
			maze.LoadFromString(theMap);
			string serializedEntities = mazeGen.GetSerializedEntities(blockingWalls, maze);
			System.IO.File.WriteAllText(@"../../maze_map.txt", theMap);
			System.IO.File.WriteAllText(@"../../maze_entities.txt", serializedEntities);
		//} catch (Exception e) {
		//	Console.WriteLine(e);
		//	Console.ReadKey();
		//}
		entities.Add(maze);
		LoadEntities(serializedEntities);

		mazeSize = maze.GetSize();
		scrollMax = mazeSize + maze.position - screenSize;
		playerSeenIt = new System.Collections.BitArray(mazeSize.X * mazeSize.Y);
		playerScreenVisibility = new System.Collections.BitArray(screen.Width * screen.Height);
		playerVisibilityAlgorithm = new MilazzoVisibility(PlayerVisionBlockedAt, MarkVisibleByPlayer, ManhattanDistance);
	}

	public void LoadEntities(string serializedEntities) {
		List<EntityBase> keyList = new List<EntityBase>();
		List<EntityBase> doorList = new List<EntityBase>();
		List<EntityBase> npcs = new List<EntityBase>();
		string[] lines = serializedEntities.Split('\n');
		for(int i = 0; i < lines.Length; ++i) {
			string[] line = lines[i].Split(' ');
			switch (line[0]) {
				case "player": player = CreatePlayer(line);     break;
				case "npc":	   npcs.Add(CreateNPC(line));       break;
				case "key":    keyList.Add(CreateKey(line));    break;
				case "door":   doorList.Add(CreateDoor(line));  break;
				case "goal":   goal = CreateArea(line);         break;
			}
		}
		if (player != null) {
			entities.Add(player);
			colliders.Add(player);
		}
		if (goal != null) {
			entities.Add(goal);
		}
		entities.AddRange(npcs);
		colliders.AddRange(npcs);
		entities.AddRange(keyList);
		entities.AddRange(doorList);
		colliders.AddRange(doorList);
	}

	public EntityBasic CreatePlayer(string[] line) {
		EntityBasic player = PopulateEntityFrom(line, new EntityBasic()) as EntityBasic;
		player.onUpdate += () => {
			if (!playerMoveArrowKeyConversion.TryGetValue(keyIn.Key, out char userMoveInput)) {
				userMoveInput = keyIn.KeyChar;
			}
			if (player.MoveDirection(userMoveInput) != Coord.Zero) {
				EntityMoveBlockedByMaze(player, userMoveInput);
				SetScrollToKeepVisiblity(player.position);
			}
			if (userMoveInput == ' ' && playerFireballs.Count < 3) {
				ShootFireball(player);
			}
			if (player.IsIntersecting(goal)) {
				PlayerWins();
			}
		};
		return player;
	}
	private static Dictionary<ConsoleKey, char> playerMoveArrowKeyConversion = new Dictionary<ConsoleKey, char>() {
		{ConsoleKey.LeftArrow, 'a' },
		{ConsoleKey.UpArrow,   'w' },
		{ConsoleKey.RightArrow,'d' },
		{ConsoleKey.DownArrow, 's' },
	};

	public EntityBasic CreateKey(string[] line) {
		EntityBasic key = PopulateEntityFrom(line, new EntityBasic()) as EntityBasic;
		key.onUpdate = () => {
			if (key.IsIntersecting(player, null)) {
				playerKeys.TryGetValue(key.name, out float keyCount);
				playerKeys[key.name] = keyCount + 1;
				Destroy(key);
			}
		};
		return key;
	}

	public EntityMobileObject CreateNPC(string[] line) {
		EntityMobileObject npc = PopulateEntityFrom(line, new EntityMobileObject()) as EntityMobileObject;
		npc.onUpdate += () => {
			EntityRandomMove(npc);
			if (npc.IndexOfIntersecting(playerFireballs, null) >= 0) {
				Destroy(npc);
			}
		};
		return npc;
	}

	public Entity2D CreateDoor(string[] line) {
		Entity2D door = PopulateEntityFrom(line, new Entity2D()) as Entity2D;
		door.onTrigger = triggeringEntity => {
			if(triggeringEntity == player) {
				if(playerKeys.TryGetValue(door.name, out float keyCount)) {
					Destroy(door);
					Console.Write($"\rusing {door.name}");
				} else {
					Console.Write($"\rneed {door.name}");
				}
				Console.ReadKey();
				Console.Write("\r" + new string(' ', door.name.Length+8) + "\r");
			}
		};
		return door;
	}

	public Entity2D CreateArea(string[] line) {
		return PopulateEntityFrom(line, new Entity2D()) as Entity2D;
	}

	private EntityBase PopulateEntityFrom(string[] line, EntityBase entity) {
		entity.name = line[1];
		char letter = line[2][0];
		int fore = int.Parse(line[3]);
		if (fore < 0) fore = ConsoleTile.DefaultTile.fore;
		int back = int.Parse(line[4]);
		if (back < 0) back = ConsoleTile.DefaultTile.back;
		entity.position = EntityEntryToCoord(line[5]);
		if (entity is EntityBasic eb) {
			eb.icon = new ConsoleTile(letter, (ConsoleColor)fore, (ConsoleColor)back);
		} else if(entity is Entity2D e2d) {
			Coord size = EntityEntryToCoord(line[6]);
			if (line.Length > 7) {
				ConsoleTile ct = new ConsoleTile(letter, (ConsoleColor)fore, (ConsoleColor)back);
				e2d.map = new Map2D('\0', size);
				e2d.map.transparentLetter = '\0';
				for (int i = 7; i < line.Length; ++i) {
					Coord pos = EntityEntryToCoord(line[i]);
					e2d[pos] = ct;
				}
			} else {
				e2d.map = new Map2D(letter, size);
			}
		} else {
			throw new Exception($"unable to populate {entity.GetType()} with {string.Join(", ", line)}");
		}
		return entity;
	}

	private Coord EntityEntryToCoord(string text) {
		string[] pos = text.Split(',');
		return new Coord(int.Parse(pos[0]), int.Parse(pos[1]));
	}

	protected override void Draw() {
		DrawEntities();
		ColorScreenByPlayerVisibility();
		Render();
	}

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

	// could be replaced with: coord => maze[coord] == '#'
	public bool IsMazeWall(Coord coord) { return blockingWalls.IndexOf(maze[coord]) >= 0; }

	public bool IsMazeWall(Rect rect) { return rect.ForEach(IsMazeWall); }

	// TODO implement a collision matrix for characters
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
				playerFireballs.Remove(fireball);
				entities.Remove(fireball);
			}
		};
		playerFireballs.Add(fireball);
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