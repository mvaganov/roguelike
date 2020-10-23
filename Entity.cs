using System;
using System.Collections.Generic;

public abstract class EntityBase {
	public Coord position;

	public Action<EntityBase, GameBase> onUpdate;

	public abstract void Draw(ConsoleTile[,] map, Coord offset);
	public abstract Coord GetSize();

	public virtual void Update(GameBase game) 	{
		if(onUpdate != null) {
			onUpdate.Invoke(this, game);
		}
	}

	public static readonly IDictionary<char, Coord> defaultMoves = new Dictionary<char, Coord>() {
		['w'] = Coord.Up, ['a'] = Coord.Left, ['s'] = Coord.Down, ['d'] = Coord.Right,
	};

	public IDictionary<char, Coord> moves = defaultMoves;

	public Coord MoveDirection(char keyCode) {
		moves.TryGetValue(keyCode, out Coord direction);
		return direction;
	}

	public void Move(char keyCode) { position += MoveDirection(keyCode); }
}

public class EntityBasic : EntityBase {
	private ConsoleTile icon;

	public override Coord GetSize() => Coord.One;

	public EntityBasic(ConsoleTile icon, Coord position) {
		this.position = position;
		this.icon = icon;
	}

	public override void Draw(ConsoleTile[,] map, Coord offset) {
		if(position.IsWithin(offset, offset+Coord.SizeOf(map))) {
			map[position.row-offset.row, position.col-offset.col] = icon;
		}
	}
}

public class EntityMobileObject : EntityBasic {
	public char currentMove;

	public EntityMobileObject(ConsoleTile icon, Coord position) : base(icon, position) { }
}