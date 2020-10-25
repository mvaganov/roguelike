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
		public float cost;
		public Edge(Node a_from, Node a_to, float cost = 1) { _from = a_from; _to = a_to; this.cost = cost; }
		public Node Other(Node n) { if (n == _from) { return _to; } if (n == _to) { return _from; } throw new Exception("invalid node given"); }
		public bool Connects(Node n) { return _from == n || _to == n; }
		//public bool Equals(Edge e) { return e != null && cost == e.cost && (a == e.a && b == e.b || a == e.b && b == e.a); }
		//public override bool Equals(object obj) { return (obj.GetType() == typeof(Edge) && Equals((Edge)obj)); }
		//public static bool operator ==(Edge a, Edge b) { return (a == null) ? (b == null) : a.Equals(b); }
		//public static bool operator !=(Edge a, Edge b) { return!a.Equals(b); }
		//public override int GetHashCode() { return base.GetHashCode(); }
	}
}