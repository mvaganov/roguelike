using System;
using System.Collections.Generic;

public class Graph<NData,EData> {
	public class Node {
		public NData data;
		public List<Edge> edges = new List<Edge>();
		public Edge AddNeighbor(Node n) {
			Edge e = Connects(n);
			if (e != null) return e;
			e = new Edge(this, n);
			edges.Add(e);
			return e;
		}
		public Edge Connects(Node n) { return edges.Find(e => e.Connects(n)); }
	}
	public class Edge {
		public EData data;
		public Node a,b;
		public float cost;
		public Edge(Node a, Node b, float cost = 1) { this.a = a; this.b = b; this.cost = cost; }
		public Node Other(Node n) { if (n == a) return b; if (n == b) return a; throw new Exception("invalid node given"); }
		public bool Connects(Node n) { return a == n || b == n; }
		public bool Equals(Edge e) { return cost == e.cost && (a == e.a && b == e.b || a == e.b && b == e.a); }
		public override bool Equals(object obj) { return (obj.GetType() == typeof(Edge) && Equals((Edge)obj)); }
		public static bool operator ==(Edge a, Edge b) { return a.Equals(b); }
		public static bool operator !=(Edge a, Edge b) { return!a.Equals(b); }
		public override int GetHashCode() { return base.GetHashCode(); }
	}
}