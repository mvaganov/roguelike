using System;
using System.Collections.Generic;

public static class CollisionUtil {

	//private static bool SpecializedIntersect(EntityBase a, EntityBase b, out bool specializedCollisionResult) {
	//	specializedCollisionResult = false;
	//	//if (a.isColliding != null || b.isColliding != null) {
	//	//	if (a.isColliding != null && a.isColliding(b)) { specializedCollisionResult = true; }
	//	//	else if(b.isColliding != null && b.isColliding(a)) { specializedCollisionResult = true; }
	//	//	return true;
	//	//}
	//	return false;
	//}

	//public static bool Intersects(this EntityBase self, EntityBase other) {
	//	if(SpecializedIntersect(self,other,out bool result)) { return result; }
	//	return Rect.IsSizeRectIntersect(self.position, self.GetSize(), other.position, other.GetSize());
	//}

	//public static bool Intersects(this EntityBase self, EntityBase other, out Coord outMin, out Coord outSize) {
	//	if (SpecializedIntersect(self, other, out bool result)) {
	//		Rect sum = Rect.Sum(self.GetRect(), other.GetRect());
	//		outMin = sum.min; outSize = sum.Size;
	//		return result;
	//	}
	//	return Rect.GetSizeRectIntersect(self.position, self.GetSize(), other.position, other.GetSize(), out outMin, out outSize);
	//}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="self"></param>
	/// <param name="other"></param>
	/// <param name="action"></param>
	/// <returns>true if even one intersection was found</returns>
	public static bool IsIntersecting(this EntityBase self, EntityBase other, Action<Coord> action) {
		if (self == other) return false;
		if(self.GetRect().TryGetIntersect(other.GetRect(), out Rect intersection)) {
			if (action != null) { intersection.ForEach(action); }
			return true;
		}
		return false;
	}
	
	/// <summary>
	/// checks if the two rectangles collide, and if provided, if an extra condition is also met in the overlapping area
	/// </summary>
	/// <param name="self"></param>
	/// <param name="other"></param>
	/// <param name="condition">an additional collision check, per coordinate overlapping</param>
	/// <returns>returns true as soon as an intersection is found</returns>
	public static bool IsIntersecting(this EntityBase self, EntityBase other, Func<Rect, bool> condition = null) {
		if (self == other) return false;
		Rect selfRect = self.GetRect(), otherRect = other.GetRect();
		if (selfRect.TryGetIntersect(otherRect, out Rect intersection)
		&& (condition == null || condition.Invoke(intersection))) {
			return true;
		}
		return false;
	}

	//public static bool Intersects(this EntityBase self, EntityBase other, Action<Coord, EntityBase, EntityBase> action) {
	//	if (SpecializedIntersect(self, other, out bool result)) { return result; }
	//	if (self == other || !self.Intersects(other, out Coord outMin, out Coord outSize)) return false;
	//	Coord.ForEach(outMin, outMin + outSize, coord => { action(coord, self, other); });
	//	return true;
	//}

	///// <summary>
	///// if there is basic rectangular collision, each individual coordinate is checked for collision
	///// </summary>
	///// <param name="other">if self, return false. this function does not trigger collisions with self</param>
	///// <param name="condition">function to determine if there is overlap at a coordinate. the parameters are: coordinate to check, this entity, and the other entity</param>
	///// <returns></returns>
	//public static bool Intersects(this EntityBase self, EntityBase other, Func<Coord, EntityBase, EntityBase, bool> condition) {
	//	if (self == other || !self.Intersects(other, out Coord outMin, out Coord outSize)) return false;
	//	return condition == null || Coord.ForEach(outMin, outMin + outSize, coord => { return condition.Invoke(coord, self, other); });
	//}

	///// <summary>
	///// if there is basic rectangular collision, each individual coordinate is checked for collision
	///// </summary>
	///// <param name="other">if self, return false. this function does not trigger collisions with self</param>
	///// <param name="condition">function to determine if there is overlap at a coordinate. use null for normal rect collision</param>
	///// <returns></returns>
	//public static bool Intersects(this EntityBase self, EntityBase other, Func<Coord, bool> condition) {
	//	if (SpecializedIntersect(self, other, out bool result)) {
	//		if (condition == null) return true;
	//		Rect intersection = self.GetRect().Intersect(other.GetRect());
	//		return intersection.ForEach(condition);
	//	}
	//	if (self == other || !self.Intersects(other, out Coord outMin, out Coord outSize)) return false;
	//	return condition == null || Coord.ForEach(outMin, outMin + outSize, coord => { return condition.Invoke(coord); });
	//}

	//public static bool CollidesWith(this EntityBase entity, EntityBase other, Func<Coord, bool> whatTriggersCollision) {
	//	return entity != other && entity.Intersects(other, whatTriggersCollision);
	//}

	public static int IndexOfIntersecting(this EntityBase entity, IList<EntityBase> list, Func<Rect, bool> extraCondition) {
		for (int i = 0; i < list.Count; ++i) {
			if (entity.IsIntersecting(list[i], extraCondition)) return i;
		}
		return -1;
	}

	public static int IndexOfIntersect(this IList<EntityBase> list, EntityBase entity, Func<Rect, bool> extraCondition) {
		for (int i = 0; i < list.Count; ++i) {
			if (entity.IsIntersecting(list[i], extraCondition)) return i;
		}
		return -1;
	}
}