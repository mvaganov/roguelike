using System;
using System.Collections.Generic;

public interface IDrawable : IRect {
	void Draw(ConsoleTile[,] map, Coord offset);
}

public abstract class EntityBase : IDrawable {
	public string name;
	public Coord position;

	public Action onUpdate;
	public Action<EntityBase> onTrigger;
	public virtual void Update(GameBase game) { onUpdate?.Invoke(); }

	public abstract void Draw(ConsoleTile[,] map, Coord offset);
	public virtual Coord GetPosition() => position;
	public abstract Coord GetSize();
	public virtual Rect GetRect() => new Rect(position, position + GetSize());

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
	public ConsoleTile icon;

	public override Coord GetSize() => Coord.One;

	public EntityBasic() { }

	public EntityBasic(string name, ConsoleTile icon, Coord position) {
		this.name = name;
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
	public EntityMobileObject() { }
	public EntityMobileObject(string name, ConsoleTile icon, Coord position) : base(name, icon, position) { }
}