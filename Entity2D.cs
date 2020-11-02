using System;
using System.Collections.Generic;

public class Entity2D : EntityBase {
	public Map2D map = new Map2D();

	public Entity2D(string name, ConsoleTile fill, Coord position, Coord size) {
		this.name = name;
		this.position = position;
		map.SetSize(size);
		map.Fill(fill);
		map.transparentLetter = 0;
	}

	public Entity2D(string name, ConsoleTile icon, Coord position) : this(name, icon, position, Coord.One) { }

	public Entity2D(string name, ConsoleTile icon) : this(name, icon, Coord.Zero) { }

	public Entity2D(string name, string filename, Coord position) {
		this.name = name;
		this.position = position;
		map.LoadFromFile(filename);
		map.transparentLetter = ' ';
	}

	public void LoadFromFile(string filename) => map.LoadFromFile(filename);
	public void LoadFromString(string text) => map.LoadFromString(text);

	public override void Draw(ConsoleTile[,] buffer, Coord offset) => map.Draw(buffer, position - offset);

	public override Coord GetSize() => map.Size;

	public void GetMinMax(out Coord min, out Coord max) { min = position; max = min + map.Size; }

	public ConsoleTile this[int row, int col] {
		get { return map[row-position.row, col-position.col]; }
		set { map[row - position.row, col - position.col] = value; }
	}

	public ConsoleTile this[Coord position] {
		get { return map[position - this.position]; }
		set { map[position - this.position] = value; }
	}

	public bool Contains(Coord position) {
		return map.Contains(position - this.position);
	}
}

//public class EntityComposite : EntityBase {
//	public EntityComposite() {
//		//onCollision
//	}

//	private List<EntityBase> part = new List<EntityBase>();
//	public override void Draw(ConsoleTile[,] map, Coord offset) {
//		part.ForEach(e => e.Draw(map, offset));
//	}

//	public override Coord GetSize() {
//		Coord min = position, max = position;
//		Coord size = Coord.Zero;
//		if(part.Count > 0) {
//			size = part[0].GetSize();
//			min = part[0].position;
//			max = min + size;
//			part.ForEach(e => {
//				Coord s = e.GetSize();
//				Coord.ExpandRectangle(e.position, s, ref min, ref max);
//			});
//			size = max - min;
//		}
//		return size;
//	}

//	public int CountParts => part.Count;

//	public void AddPart(EntityBase newPart) { part.Add(newPart); }

//	public EntityBase GetPart(int index) { return part[index]; }

//	//public bool IsColliding(EntityBase other, Func<Coord,bool> whatTriggersCollision) {
//	//	for(int i = 0; i < part.Count; ++i) {
//	//		if(part[i].Intersects(other, whatTriggersCollision)) {

//	//		}
//	//	}
//	//	part.ForEach(e => {
//	//		return true;
//	//	});
//	//}
//}