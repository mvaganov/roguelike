using System;
using System.Collections.Generic;

public class GameBase {
	protected bool isRunning;
	protected ConsoleKeyInfo keyIn;
	/// <summary>
	/// drawing, updating entities
	/// </summary>
	protected List<EntityBase> entities = new List<EntityBase>();
	/// <summary>
	/// colliding entities
	/// </summary>
	protected List<EntityBase> colliders = new List<EntityBase>();
	/// <summary>
	/// entities that need to be removed from entities and colliders list
	/// </summary>
	protected List<EntityBase> toDestroy = new List<EntityBase>();
	protected Map2D screen, backBuffer;
	protected Coord screenOffset = Coord.Zero;
	protected Coord scrollMin = Coord.Zero, scrollMax, scrollBorder = new Coord(6,3), scrollJump = new Coord(8,4);

	public virtual bool IsRunning() { return isRunning; }

	protected virtual void Init(Coord screenSize) {
		isRunning = true;
		screen = new Map2D(' ', screenSize);
		backBuffer = new Map2D('\0', screenSize);
		scrollMax = screenSize;
	}

	protected virtual void Release() {
		backBuffer.Release();
		screen.Release();
	}

	protected virtual void Draw() {
		DrawEntities();
		Render();
	}

	protected virtual void DrawEntities() {
		ConsoleTile[,] map = screen.GetRawMap();
		for (int i = 0; i < entities.Count; ++i) {
			entities[i].Draw(map, screenOffset);
		}
	}

	protected virtual void Render() {
		for (int row = 0; row < screen.Height; ++row) {
			for (int col = 0; col < screen.Width; ++col) {
				if (backBuffer[row, col] != screen[row, col]) {
					ConsoleTile tile = backBuffer[row, col] = screen[row, col];
					if(Console.CursorLeft != col || Console.CursorTop != row) {
						Console.SetCursorPosition(col, row);
					}
					if(!tile.IsColorCurrent()) { tile.ApplyColor(); }
					Console.Write(screen[row, col]);
				}
			}
		}
		ConsoleTile.DefaultTile.ApplyColor();
		Console.SetCursorPosition(0, screen.Height);
	}

	protected virtual void UserInput() {
		Console.SetCursorPosition(0, screen.Height);
		keyIn = Console.ReadKey();
	}

	protected List<Coord> colliderOldPosition = new List<Coord>();
	protected List<int> collided = new List<int>();

	protected virtual void Update() {
		switch (keyIn.KeyChar) {
			case (char)27: isRunning = false; return;
			case 'i': screenOffset.row -= scrollJump.row; break;
			case 'j': screenOffset.col -= scrollJump.col; break;
			case 'k': screenOffset.row += scrollJump.row; break;
			case 'l': screenOffset.col += scrollJump.col; break;
		}
		screenOffset = ClampToScrollSpace(screenOffset);
		// before collision detection, remember the position of moving objects
		while (colliderOldPosition.Count < colliders.Count) { colliderOldPosition.Add(Coord.Zero); }
		for (int i = 0; i < colliders.Count; ++i) { colliderOldPosition[i] = colliders[i].position; }
		for (int i = 0; i < entities.Count; ++i) {
			entities[i].Update(this);
		}
		// check if collision happened, and trigger collision functions
		for (int i = 0; i < colliders.Count; ++i) {
			EntityBase entity = colliders[i];
			if (entity.position != colliderOldPosition[i]) {
				int hitIndex = colliders.IndexOfIntersect(entity, null);
				if (hitIndex >= 0) {
					collided.Add(i);
					EntityBase wasHit = colliders[hitIndex];
					wasHit.onTrigger?.Invoke(entity);
				}
			}
		}
		// if collision happened, move colliding objects back
		if(collided.Count > 0) {
			collided.ForEach(i => colliders[i].position = colliderOldPosition[i]);
			collided.Clear();
		}
		while(toDestroy.Count > 0) {
			EntityBase onChoppingBlock = toDestroy[0];
			entities.Remove(onChoppingBlock);
			colliders.Remove(onChoppingBlock);
			toDestroy.RemoveAt(0);
		}
	}

	public void Destroy(EntityBase entity) { toDestroy.Add(entity); }

	public Coord ClampToScrollSpace(Coord coord) {
		coord.Clamp(scrollMin, scrollMax);
		return coord;
	}

	public bool IsInScrollArea(Coord coord) => coord.IsWithin(scrollMin, scrollMax + screen.Size);

	public void SetScrollToKeepVisiblity(Coord target) {
		Coord jump = DeltaNeededToKeepInside(target, screenOffset + scrollBorder, screenOffset + screen.Size - scrollBorder);
		jump.Scale(scrollJump);
		screenOffset += jump;
		screenOffset = ClampToScrollSpace(screenOffset);
	}

	public static Coord DeltaNeededToKeepInside(Coord target, Coord min, Coord max) {
		Coord delta = Coord.Zero;
		max -= Coord.One;
		if (target.col < min.col) { delta.col = (short)(target.col - min.col); }
		if (target.row < min.row) { delta.row = (short)(target.row - min.row); }
		if (target.col > max.col) { delta.col = (short)(target.col - max.col); }
		if (target.row > max.row) { delta.row = (short)(target.row - max.row); }
		return delta;
	}
}