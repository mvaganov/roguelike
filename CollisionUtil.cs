using System;
using System.Collections.Generic;

public static class CollisionUtil {

	public static bool Intersects(this EntityBase self, EntityBase other) {
		return Coord.IsSizeRectIntersect(self.position, self.GetSize(), other.position, other.GetSize());
	}

	public static bool Intersects(this EntityBase self, EntityBase other, out Coord outMin, out Coord outSize) {
		return Coord.GetSizeRectIntersect(self.position, self.GetSize(), other.position, other.GetSize(), out outMin, out outSize);
	}

	public static bool Intersects(this EntityBase self, EntityBase other, Action<Coord, EntityBase, EntityBase> action) {
		if (self == other || !self.Intersects(other, out Coord outMin, out Coord outSize)) return false;
		Coord.ForEach(outMin, outMin + outSize, coord => { action(coord, self, other); });
		return true;
	}

	/// <summary>
	/// if there is basic rectangular collision, each individual coordinate is checked for collision
	/// </summary>
	/// <param name="other">if self, return false. this function does not trigger collisions with self</param>
	/// <param name="action">function to determine if there is overlap at a coordinate. the parameters are: coordinate to check, this entity, and the other entity</param>
	/// <returns></returns>
	public static bool Intersects(this EntityBase self, EntityBase other, Func<Coord, EntityBase, EntityBase, bool> action) {
		if (self == other || !self.Intersects(other, out Coord outMin, out Coord outSize)) return false;
		return Coord.ForEach(outMin, outMin + outSize, coord => { return action(coord, self, other); });
	}
	/// <summary>
	/// if there is basic rectangular collision, each individual coordinate is checked for collision
	/// </summary>
	/// <param name="other">if self, return false. this function does not trigger collisions with self</param>
	/// <param name="action">function to determine if there is overlap at a coordinate</param>
	/// <returns></returns>
	public static bool Intersects(this EntityBase self, EntityBase other, Func<Coord, bool> action) {
		if (self == other || !self.Intersects(other, out Coord outMin, out Coord outSize)) return false;
		return Coord.ForEach(outMin, outMin + outSize, coord => { return action(coord); });
	}

	public static bool CollidesWith(this EntityBase entity, EntityBase other, Func<Coord, bool> whatTriggersCollision, Action whatToDoIfCollisionHappened) {
		if (entity.Intersects(other, whatTriggersCollision)) {
			whatToDoIfCollisionHappened();
			return true;
		}
		return false;
	}
	public static bool CollidesWith(this EntityBase entity, EntityBase other, Func<Coord, EntityBase, EntityBase, bool> whatTriggersCollision, Action whatToDoIfCollisionHappened) {
		if (entity.Intersects(other, whatTriggersCollision)) {
			whatToDoIfCollisionHappened();
			return true;
		}
		return false;
	}

	public static bool CollidesWith(this EntityBase entity, IList<EntityBase> otherList, Func<Coord, EntityBase, EntityBase, bool> whatTriggersCollision, Action whatToDoIfCollisionHappened) {
		for (int i = 0; i < otherList.Count; ++i) {
			if (entity != otherList[i] && entity.Intersects(otherList[i], whatTriggersCollision)) {
				whatToDoIfCollisionHappened();
				return true;
			}
		}
		return false;
	}
}