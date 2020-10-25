
using System;
using System.Collections.Generic;
using System.Linq;

namespace MazeGeneration
{
	public class MazeGraph : Graph<MazeRoomBase,MazeRoomBase> {

		public List<Node> nodes = new List<Node>();
		public Maze maze;

		public List<Edge> GenerateEdgesFor(Node node, bool includeUnlikelyConnections) {
			List<WallEdge> wallEdges = node.nodeData.GetEdges(maze, includeUnlikelyConnections);
			List<Edge> nodeEdges = new List<Edge>();
			for (int e = 0; e < wallEdges.Count; ++e) {
				Coord next = wallEdges[e].next;
				//int idAtEdge = m.marks.At(next) - 1;
				if (!next.IsWithin(maze.size)) continue;
				Node neighbor = GetNodeAt(next);//nodes[idAtEdge];
				Edge edge = nodeEdges.Find(ed => ed._to == neighbor);//node.GetEdgeTo(neighbor);
				MazePath mp = null;
				if (edge == null) {
					edge = new Edge(node, neighbor);
					mp = new MazePath();
					edge.edgeData = mp;
					nodeEdges.Add(edge);//node.AddNeighbor(neighbor);
				} else {
					mp = edge.edgeData as MazePath;
				}
				mp.Add(next);
			}
			// sort the edges for easier semantic parsing
			nodeEdges.Sort(EdgeSort);
			return nodeEdges;
		}

		public void Generate(Maze m, List<MazeRoomBase> rooms) {
			maze = m;
			for(int i = 0; i < rooms.Count; ++i) {
				Node n = new Node();
				n.nodeData = rooms[i];
				nodes.Add(n);
			}
			for (int i = 0; i < nodes.Count; ++i) {
				Node node = nodes[i];
				node.AddEdges(GenerateEdgesFor(node, false));
			}

			Node start = GetNodeAt(Coord.Zero);

			CalculateNodeWeight(start, (node, depth) => depthScore[node] = depth);

			bossRoomScore = CalculateNodeWeight(start, (node, depth) => {
				float v = node.nodeData.GetArea() + depth;
				node.ForEachEdge(edge => {
					int width = edge.edgeData.GetArea();
					if (width > 1 && edge._to.nodeData is MazeRoomAggregate) {
						int otherRoomArea = edge._to.nodeData.GetArea();
						v += otherRoomArea - (otherRoomArea / width);
					}
				});
				return v;
			});
			shortcutScore = CalculateNodeWeight(start, (node, depth) => {
				float v = depth;
				Edge bestEdge = GetBestShortcutBackwardFor(node);
				bool isThisTerminal = IsTerminal(node);
				bool isOtherTerminal = IsTerminal(bestEdge._to);
				if (isThisTerminal) depth += 10;
				if (isOtherTerminal) depth += 20;
				return depth - depthScore[bestEdge._to];
			});
		}
		Dictionary<Node, float> depthScore = new Dictionary<Node, float>();
		List<KeyValuePair<Node, float>> bossRoomScore, shortcutScore;

		public Edge GetBestShortcutBackwardFor(Node node) {
			float thisDepth = depthScore[node];
			List<Edge> allEdges = GenerateEdgesFor(node, true);
			allEdges.Sort((a, b) => {
				float aScore = thisDepth - depthScore[a._to];
				float bScore = thisDepth - depthScore[b._to];
				return -aScore.CompareTo(bScore);
			});
			return allEdges[0];
		}

		private int EdgeSort(Edge e1, Edge e2) {
			int size1 = e1._to.nodeData.GetArea();
			int size2 = e2._to.nodeData.GetArea();
			//return size1.CompareTo(size2);
			// terminals first
			bool term1 = maze.terminals.IndexOf(e1._to.nodeData) >= 0;
			bool term2 = maze.terminals.IndexOf(e2._to.nodeData) >= 0;
			if (term1 && term2) return size1.CompareTo(size2);
			if (term1) size1 += -100; //return -1;
			if (term2) size2 += -100; //return 1;
			// tunnels last
			bool tunnel1 = maze.hallways.IndexOf(e1._to.nodeData) >= 0;
			bool tunnel2 = maze.hallways.IndexOf(e2._to.nodeData) >= 0;
			if (tunnel1 && tunnel2) return size1.CompareTo(size2);
			if (tunnel1) size1 += 1000; // return 1;
			if (tunnel2) size2 += 1000; // return -1;
			// otherwise, smaller room first
			return size1.CompareTo(size2);
		}

		public Node GetNodeAt(Coord coord) {
			int idAtPosition = maze.marks.At(coord) - 1;
			return nodes[idAtPosition];
		}

		public IList<Edge> GetEdgesForNodeAt(Coord coord) {
			Node n = GetNodeAt(coord);
			return n.edges;
		}

		public List<KeyValuePair<Node, float>> CalculateNodeWeight(Node start, Func<Node, int, float> weightCalculation) {
			Dictionary<Node, float> ledger = new Dictionary<Node, float>();
			CalculateNodeWeight(start, 0, weightCalculation, ledger);
			List<KeyValuePair<Node, float>> list = ledger.ToList();
			list.Sort((KeyValuePair<Node, float> pair1, KeyValuePair<Node, float> pair2) => -pair1.Value.CompareTo(pair2.Value));
			return list;
		}

		public void CalculateNodeWeight(Node node, int depth, Func<Node,int,float> weightCalculation, Dictionary<Node, float> ledger) {
			ledger[node] = weightCalculation(node, depth);
			for(int i = 0; i < node.edges.Count; ++i) {
				Node next = node.edges[i]._to;
				if (ledger.ContainsKey(next)) { continue; }
				CalculateNodeWeight(next, depth + 1, weightCalculation, ledger);
			}
		}

		public void DebugPrint(Coord start) {
			Node n = GetNodeAt(start);
			List<Node> visited = new List<Node>();
			DebugPrint(n, 0, visited, 0);
		}
		public int DebugPrint(Node n) {
			int id = nodes.IndexOf(n) + 1;
			Console.Write(id.ToString("X"));
			bool isTunnel = IsTunnel(n);
			bool isTerminal = IsTerminal(n);
			bool isRoom = !isTunnel && !isTerminal;
			int area = n.nodeData.GetArea();
			Console.Write(" ");
			if (isTerminal) { Console.Write("End"); } else if (isTunnel) { Console.Write("Tunnel"); } else if (isRoom) { Console.Write("Room"); }
			Console.Write(" a"+area+" ");
			int bossIndex = bossRoomScore.FindIndex(kvp => kvp.Key == n);
			if (bossIndex < 10) { Console.Write($" boss#{bossIndex}:{bossRoomScore[bossIndex].Value}"); }
			int shortcutIndex = shortcutScore.FindIndex(kvp => kvp.Key == n);
			if (shortcutIndex < 10) {
				Edge wall = GetBestShortcutBackwardFor(n);
				int otherRoomId = nodes.IndexOf(wall._to) + 1;
				Console.Write($" short#{shortcutIndex}:{shortcutScore[shortcutIndex].Value}->{otherRoomId.ToString("X")}");
			}
			return id;
		}
		public void DebugPrint(Node n, int indent, List<Node> visited, int parentId) {
			string parent = parentId.ToString("X");
			for(int i = 0; i < indent; ++i) { Console.Write(" "); }
			//Console.Write(indent+" ");
			Console.Write("(" + parent + ")->");
			int id = DebugPrint(n);
			Console.Write("\n");
			visited.Add(n);
			for(int i = 0; i < n.edges.Count; ++i) {
				Node next = n.edges[i]._to;
				if (visited.Contains(next)) { continue; }
				DebugPrint(next, indent + 1, visited, id);
			}
		}
		private bool IsTerminal(Node n) => maze.terminals.IndexOf(n.nodeData) >= 0;
		private bool IsTunnel(Node n) => maze.hallways.IndexOf(n.nodeData) >= 0;
		// TODO rate the rooms for different purposes
			// boss monster room: deep and large. add depth to size + half of adjacent room size
			// final treasure area: terminal or leaf room after the boss monster room
			// location for a sweet puzzle solving item: leaf room
			// barrier
				// locked door: tunnels
				// puzzle room: if it is adjacent to a large room
			// possible location for a key: terminal or leaf room
			// possible locations for mobs: non-leaf rooms
			// resources, story elements, minor treasure: leaf room behind mobs
			// shortcut: back-track from the end of a terminal, searching for shallowest room blocked by wall. maybe make the terminal high on a hill, and the shallower parts below
		// TODO random boss monster abilities
			// flight mode (range attacks only)
			// AoE blast
			// large AoE blast with specific safe zones
			// line blast from monster
			// line blast in environment
			// moving danger fields
			// spawning minions
			// temporary invulnerability
			// immunity from different attack types
			// speedy movement
			// speedy burst attack
			// teleportation
			// pushing players
			// enrage mode
			// life drain
			// heals whenever players heal
			// spawns a different boss on death
			// lots of targets
	}
}