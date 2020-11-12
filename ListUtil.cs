//using System;
//using System.Collections.Generic;

//public class KeySortedList<KEY,VALUE> where KEY : IComparable where VALUE : struct
//{
//	public List<KeyValuePair<KEY, VALUE>> list = new List<KeyValuePair<KEY, VALUE>>();

//	public int IndexOfKey(KEY name) => list.BinarySearch(new KeyValuePair<KEY, VALUE>(name, default(VALUE)), costComparer);

//	public VALUE this[KEY name] {
//		get {
//			int index = IndexOfKey(name);
//			if(index >= 0) return list[index].Value;
//			return default(VALUE);
//		}
//		set {
//			int index = IndexOfKey(name);
//			KeyValuePair<KEY, VALUE> entry = new KeyValuePair<KEY, VALUE>(name, value);
//			if (index < 0) {
//				index = ~index;
//				list.Insert(index, entry);
//			} else {
//				list[index] = entry;
//			}
//		}
//	}
//	public int Count => list.Count;

//	public bool IsReadOnly => throw new NotImplementedException();

//	public VALUE this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

//	private class EdgeCostComparer : IComparer<KeyValuePair<KEY, VALUE>> {
//		public int Compare(KeyValuePair<KEY, VALUE> a, KeyValuePair<KEY, VALUE> b) { return a.Key.CompareTo(b.Key); }
//	}
//	private static EdgeCostComparer costComparer = new EdgeCostComparer();
//}