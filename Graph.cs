using System;
using System.Collections.Generic;

public class Graph<NData,EData> {
	public class Node {
		public NData nodeData;
		public List<Edge> edges = new List<Edge>();
		public Edge AddNeighbor(Node n) {
			Edge e = Connects(n);
			if (e != null) return e;
			e = new Edge(this, n);
			edges.Add(e);
			return e;
		}
		public int GetNeighborCount() { return edges.Count; }
		public int GetNeighborCountNotIncluding(IList<Node> ignored) {
			int count = 0;
			for(int i = 0; i < edges.Count; ++i) {
				if(ignored.IndexOf(edges[i]._to) < 0) { ++count; }
			}
			return count;
		}
		public Node GetNeighborNotIncluding(int index, List<Node> ignored, Dictionary<string,float> keys) {
			int count = 0;
			List<Edge> validEdges = GetValidEdges(keys);
			for(int i = 0; i < validEdges.Count; ++i) {
				Node n = validEdges[i]._to;
				if (ignored.IndexOf(n) < 0) {
					if(count == index) { return n; }
					++count;
				}
			}
			return null;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="keys">if keys dictionary is null, all edges are traversable</param>
		/// <returns></returns>
		public List<Edge> GetValidEdges(Dictionary<string,float> keys) {
			List<Edge> validEdges = new List<Edge>();
			for(int i = 0; i < edges.Count; ++i) {
				if(keys == null || edges[i].IsTraversableBy(keys)) {
					validEdges.Add(edges[i]);
				}
			}
			return validEdges;
		}
		public int GetNeighborCount(Dictionary<string,float> keys) {
			int count = 0;
			for(int i = 0; i < edges.Count; ++i) {
				if(keys == null || edges[i].IsTraversableBy(keys)) {
					++count;
				}
			}
			return count;
		}
		public Node GetNeighbor(int index, Dictionary<string,float> keys) {
			int count = 0;
			for(int i = 0; i < edges.Count; ++i) {
				if(edges[i].IsTraversableBy(keys)) {
					if (index == count) return edges[i]._to;
					++count;
				}
			}
			return null;
		}
		public Node GetNeighbor(int index) { return edges[index]._to; }
		public Edge Connects(Node n) { return edges.Find(e => e.Connects(n)); }
		public Edge EdgeTo(Node n) => Connects(n);
		public Edge GetEdgeTo(Node n) => Connects(n);
		public void ForEachEdge(Action<Edge> edgeAction) {
			edges.ForEach(edgeAction);
		}
		public void ForEachNeighbor(Action<Node> neighborAction) {
			edges.ForEach(e=>neighborAction(e._to));
		}
		public void AddEdges(IList<Edge> a_edges) {
			for(int i = 0; i < a_edges.Count; ++i) {
				Edge edge = a_edges[i];
				Edge found = GetEdgeTo(edge.Other(this));
				if(found == null) {
					edges.Add(edge);
				} else {
					found.cost = edge.cost;
				}
			}
		}
	}
	public class Edge {
		public EData edgeData;
		public Node _from,_to;
		public Dictionary<string, float> cost = new Dictionary<string, float>();
		public Edge(Node a_from, Node a_to) { _from = a_from; _to = a_to; }
		public Node Other(Node n) { if (n == _from) { return _to; } if (n == _to) { return _from; } throw new Exception("invalid node given"); }
		public bool Connects(Node n) { return _from == n || _to == n; }

		public bool IsTraversableBy(Dictionary<string,float> keys) {
			foreach(KeyValuePair<string,float> kvp in cost) {
				if (kvp.Key == null) continue;
				if(!keys.TryGetValue(kvp.Key, out float keyValue) || keyValue < kvp.Value) {
					return false;
				}
			}
			return true;
		}
		//public bool Equals(Edge e) { return e != null && cost == e.cost && (a == e.a && b == e.b || a == e.b && b == e.a); }
		//public override bool Equals(object obj) { return (obj.GetType() == typeof(Edge) && Equals((Edge)obj)); }
		//public static bool operator ==(Edge a, Edge b) { return (a == null) ? (b == null) : a.Equals(b); }
		//public static bool operator !=(Edge a, Edge b) { return!a.Equals(b); }
		//public override int GetHashCode() { return base.GetHashCode(); }
	}
}