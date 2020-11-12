using System;
using System.Collections.Generic;

public static class CollisionUtil {
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="self"></param>
	/// <param name="other"></param>
	/// <param name="action"></param>
	/// <returns>true if even one intersection was found</returns>
	public static bool IsIntersecting(this EntityBase self, EntityBase other, Action<Rect> action) {
		if (self == other) return false;
		if(self.GetRect().TryGetIntersect(other.GetRect(), out Rect intersection)) {
			if (action != null) { action.Invoke(intersection); }
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
		bool intersection = false;
		IsIntersecting(self, other, rect => {
			intersection = condition == null || condition.Invoke(rect);
		});
		return intersection;
	}

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