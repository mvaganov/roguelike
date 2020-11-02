
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

			Dictionary<string, float> keys = null;// new Dictionary<string, float>();

			//Edge e = nodes[0x48].GetEdgeTo(nodes[0x21]);
			//if(e == null)
			//{
			//	throw new Exception("was not expecting that");
			//}
			//e.cost["final key"] = 1;
			//if (e.IsTraversableBy(keys))
			//{
			//	throw new Exception("not expected");
			//}

			// discover node depth
			nodeDepthScore = CalculateNodeWeight(start, (node, depth) => {
				if (depth > maxDepth) maxDepth = depth;
				if(!depthScore.TryGetValue(node, out float thisDepth) || depth < thisDepth) { return depthScore[node] = depth; }
				return thisDepth;
			}, keys);
			int idealPuzzleRoomRangeMin = maxDepth / 4;
			int idealPuzzleRoomRangeMax = maxDepth / 2;
			int idealPuzzleRoomRangeTarget = idealPuzzleRoomRangeMax - idealPuzzleRoomRangeMin;
			mainPuzzleItemScore = CalculateNodeWeight(start, (node, depth) => {
				if (node.nodeData is MazePath) return 0;
				if (depth < idealPuzzleRoomRangeMin || depth > idealPuzzleRoomRangeMax) return 0;
				int area = node.nodeData.GetArea();
				if(area >= 10) {
					area += (10 - area);
				}
				int distance = depth;
				if(distance > idealPuzzleRoomRangeTarget) {
					distance += idealPuzzleRoomRangeTarget - distance;
				}
				float score = area + distance;
				return score;
			}, keys);
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
			}, keys);

			CalculateEndLocations(start, keys);

			shortcutScore = CalculateNodeWeight(start, (node, depth) => {
				Edge bestEdge = GetBestShortcutBackwardFor(node);
				Node otherNode = bestEdge._to;
				//depthScore.TryGetValue(otherNode, out float otherDepth);
				bool isThisTerminal = IsTerminal(node);
				bool isThisTunnel = IsTunnel(node);
				bool isOtherTerminal = IsTerminal(otherNode);
				bool isOtherTunnel = IsTunnel(otherNode);
				List<Node> pathBetween = A_Star(node, otherNode, keys);
				if(pathBetween == null) { return -1; } // values sorted biggest first. 
				int weightModification = 0;
				if (isThisTerminal) weightModification += 10;
				if (isOtherTerminal) weightModification += 20;
				if (isThisTunnel) weightModification -= 30;
				if (isOtherTunnel) weightModification -= 30;
				//return depth - otherDepth + weightModification;
				return pathBetween.Count + weightModification;
			}, keys);

			List<Node> blocks = new List<Node>();
			Node goal = bossRoomScore[0].Key;
			finalGoal = (endLocationScore[0].Key.nodeData as MazePath).path[0];
			//Console.Write($"goal is at {GetName(goal)}\n");
			Edge edgeToBlock = GetNextBlockedEdge(goal, keys), whereTheDoorIs;
			Node nodeBlocked = edgeToBlock._to;
			//Console.Write($"door@{GetName(nodeBlocked)}, ");
			whereTheDoorIs = edgeToBlock;

			for (int i = 0; i < endLocationScore.Count; ++i) {
				Node possibleKeyLocation = endLocationScore[i].Key;
				//Console.WriteLine($"Key could go {GetName(keyLocation)}");
				blocks.Add(nodeBlocked);
				if (IsNodeBehind(possibleKeyLocation, blocks, keys)) {
					blocks.Remove(nodeBlocked);
					//Console.WriteLine("that would be bad, it's blocked.");
					continue;
				}
				string nextKey = "key" + blocks.Count;
				Edge thisEdgeToBlock = edgeToBlock;

				//Console.WriteLine("that seems like a good plan!");
				goal = possibleKeyLocation;
				//Console.Write($"{nextKey}@{GetName(goal)}");
				edgeToBlock = GetNextBlockedEdge(goal, keys);

				if(edgeToBlock == null) {
				//	Console.Write("\n");
					break;
				}
				if(edgeToBlock._to == possibleKeyLocation)
				{
				//	Console.Write("-");
					blocks.Remove(nodeBlocked);
					continue;
				}
				keysAndDoors[nextKey] = new KeyValuePair<Node,Edge>(possibleKeyLocation,whereTheDoorIs);
				//lockedDoors[nextKey] = whereTheDoorIs;
				//keyLocation[nextKey] = possibleKeyLocation;
				thisEdgeToBlock.cost[nextKey] = 1;
				

				CalculateEndLocations(start, keys); i = 0;

				nodeBlocked = edgeToBlock._to;
				//Console.Write($"\ndoor {GetName(edgeToBlock._from)}->{GetName(nodeBlocked)}, ");
				whereTheDoorIs = edgeToBlock;
			}
			//int kvpIndex = 0;
			//foreach(KeyValuePair<string,Node> kvp in keyLocation) {
			//	Coord keyLoc = kvp.Value.nodeData.GetCoord();
			//	maze.marks.SetAt(keyLoc, 0xa0 + kvpIndex);
			//	List<Coord> doorLocs = (lockedDoors[kvp.Key].edgeData as MazePath).path;
			//	for(int i = 0; i < doorLocs.Count; ++i) {
			//		maze.marks.SetAt(doorLocs[i], 0xa0 + kvpIndex);
			//	}
			//	kvpIndex++;
			//}
		}

		public void CalculateEndLocations(Node start, Dictionary<string,float> keys) {
			endLocationScore = CalculateNodeWeight(start, (node, depth) => {
				//if (maze.terminals.IndexOf(node.nodeData) < 0) return 0; // only terminals
				if (node.GetNeighborCount(keys) > 1) return 0; // only terminals
				float bestBossScoreSoFar = float.NegativeInfinity;
				int areaTraversed = 0, area, bestArea = 0;
				// back travel till there is a fork, remembering the best bossRoomScore along the way, and the total area traversed till that bossroom.
				List<Node> traveledPath = new List<Node>();
				Node cursor = node;
				do {
					traveledPath.Add(cursor);
					area = cursor.nodeData.GetArea();
					if(area > bestArea) { bestArea = area; }
					areaTraversed += area;
					float thisBossScore = bossRoomScore.Find(kvp => kvp.Key == cursor).Value;
					if (thisBossScore > bestBossScoreSoFar) { bestBossScoreSoFar = thisBossScore; }
					cursor = cursor.GetNeighborNotIncluding(0, traveledPath, keys);
					if(cursor != null && cursor.GetNeighborCountNotIncluding(traveledPath) > 1) {
						break;
					}
				} while (cursor != null);
				// subtract the total area traversed from the boss room score
				return bestBossScoreSoFar;
			}, keys);
		}

		Edge GetNextBlockedEdge(Node node, Dictionary<string,float> keys) {
			Node prev = node, next;
			int counter = 0;
			do
			{
				next = PreviousRoom(prev, keys);
				if (next == null) return null;
				// find the location of the next door. travel along
				int validNeighbors = 0;
				List<Edge> validEdges = next.GetValidEdges(keys);
				for(int i = 0; i < validEdges.Count; ++i) {
					Node neighbor = validEdges[i]._to;
					if(maze.closets.IndexOf(neighbor.nodeData) < 0) { ++validNeighbors; }
				}
				if (validNeighbors > 2)
				{
					break;
				}
				prev = next;
				if (++counter > 1000) { throw new Exception("looks like an infinite loop"); }
			} while (true);
			//Console.Write("door should be between " + GetName(next) + " and " + GetName(prev));
			return next.EdgeTo(prev);
		}

		bool IsNodeBehind(Node testNode, List<Node> possibleGateKeepers, Dictionary<string, float> keys) {
			for(int i = 0; i < possibleGateKeepers.Count; ++i) {
				if (IsNodeBehind(testNode, possibleGateKeepers[i], keys)) return true;
			}
			return false;
		}
		bool IsNodeBehind(Node testNode, Node possibleGateKeeper, Dictionary<string,float> keys) {
			depthScore.TryGetValue(testNode, out float testDepth);
			depthScore.TryGetValue(possibleGateKeeper, out float gateDepth);
			Node prev = testNode;
			do {
				if (prev == possibleGateKeeper) { return true; }
				if (testDepth < gateDepth) return false;
				prev = PreviousRoom(prev, keys);
				--testDepth;
			} while (prev != null);
			return false;
		}

		Node PreviousRoom(Node cursor, Dictionary<string,float> keys) {
			float depth = depthScore[cursor];
			List<Edge> validEdges = cursor.GetValidEdges(keys);
			for(int i = 0; i < validEdges.Count; ++i) {
				Node next = validEdges[i]._to;
				float neighborDepthScore = depthScore[next];
				if(neighborDepthScore < depth) {
					return next;
				}
			}
			return null;
		}

		Dictionary<Node, float> depthScore = new Dictionary<Node, float>();
		int maxDepth = 0;
		List<KeyValuePair<Node, float>> 
			nodeDepthScore,
			bossRoomScore, // best place for an epic final encounter
			shortcutScore, // best shortcut period
			endLocationScore, // the last tile, the true 'end point', with the final goal item
			mainPuzzleItemScore;
		public Dictionary<string,KeyValuePair<Node, Edge>> keysAndDoors = new Dictionary<string,KeyValuePair<Node, Edge>>();
		public Coord finalGoal;

		public Edge GetBestShortcutBackwardFor(Node node) {
			//Console.WriteLine(GetName(node));
			float thisDepth = depthScore[node];
			List<Edge> allEdges = GenerateEdgesFor(node, true);
			allEdges.Sort((a, b) => {
				depthScore.TryGetValue(a._to, out float aScore);
				depthScore.TryGetValue(b._to, out float bScore);
				aScore = thisDepth - aScore;
				bScore = thisDepth - bScore;
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="start">where weight calculations start from</param>
		/// <param name="weightCalculation">what the weight calculation actually is</param>
		/// <param name="keys">what 'keys' are available to pass through locked edges. null to ignore special edge costs</param>
		/// <returns></returns>
		public List<KeyValuePair<Node, float>> CalculateNodeWeight(Node start, Func<Node, int, float> weightCalculation, Dictionary<string, float> keys) {
			Dictionary<Node, float> ledger = new Dictionary<Node, float>();
			ledger[start] = weightCalculation(start, 0);
			BfsWightCalculation(start, weightCalculation, ledger, keys);
			List<KeyValuePair<Node, float>> list = ledger.ToList();
			list.Sort((KeyValuePair<Node, float> pair1, KeyValuePair<Node, float> pair2) => -pair1.Value.CompareTo(pair2.Value));
			return list;
		}

		public void BfsWightCalculation(Node node, Func<Node,int,float> weightCalculation, Dictionary<Node, float> ledger, Dictionary<string,float> keys) {
			List<Node> Q = new List<Node>();
			int currentDepth = 0, nodesAtCurrentDepth = 1, nodesAtNextDepth = 0;
			Q.Add(node);
			ledger[node] = weightCalculation(node, currentDepth);
			//Console.Write(currentDepth + ": " + GetName(node)+"\n");
			while (Q.Count > 0) {
				Node v = Q[0];
				Q.RemoveAt(0);
				for (int i = 0; i < v.edges.Count; ++i) {
					Edge edge = v.edges[i];
					if (keys != null && !edge.IsTraversableBy(keys)) continue;
					Node w = edge._to;
					if(!ledger.TryGetValue(w, out float weight)) {
						ledger[w] = weightCalculation(w, currentDepth+1);
						Q.Add(w);
						nodesAtNextDepth++;
						//Console.Write(GetName(w) + " ");
					}
				}
				--nodesAtCurrentDepth;
				if(nodesAtCurrentDepth <= 0) {
					currentDepth++;
					nodesAtCurrentDepth = nodesAtNextDepth;
					nodesAtNextDepth = 0;
					//Console.Write("\n"+currentDepth + ": ");
				}
			}
		}

		public void DebugPrint(Coord start) {
			Node n = GetNodeAt(start);
			List<Node> visited = new List<Node>();
			DebugPrint(n, 0, visited, 0);
		}
		public int DebugPrint(Node n) {
			int id = GetId(n);
			Console.Write(id.ToString("X"));
			bool isTunnel = IsTunnel(n);
			bool isTerminal = IsTerminal(n);
			bool isRoom = !isTunnel && !isTerminal;
			int area = n.nodeData.GetArea();
			Console.Write(" ");
			if (isTerminal) { Console.Write("End"); } else if (isTunnel) { Console.Write("Tunnel"); } else if (isRoom) { Console.Write("Room"); }
			Console.Write(" a"+area+" ");
			int bossIndex = bossRoomScore.FindIndex(kvp => kvp.Key == n);
			if (bossIndex >= 0 && bossIndex < 10) { Console.Write($" boss#{bossIndex}:{bossRoomScore[bossIndex].Value}"); }
			int shortcutIndex = shortcutScore.FindIndex(kvp => kvp.Key == n);
			if (shortcutIndex >= 0 && shortcutIndex < 10) {
				//Edge wall = GetBestShortcutBackwardFor(n);
				Console.Write($" short#{shortcutIndex}:{shortcutScore[shortcutIndex].Value}");//->{GetName(wall._to)}
			}
			int puzzleItemIndex = mainPuzzleItemScore.FindIndex(kvp => kvp.Key == n);
			if(puzzleItemIndex >= 0 && puzzleItemIndex < 10 && mainPuzzleItemScore[puzzleItemIndex].Value != 0) {
				Console.Write($" I#{puzzleItemIndex}:{mainPuzzleItemScore[puzzleItemIndex].Value}");
			}
			int finalIndex = endLocationScore.FindIndex(kvp => kvp.Key == n);
			if(finalIndex >= 0 && finalIndex < 10) {
				Console.Write($" Fi${finalIndex}:{endLocationScore[finalIndex].Value}");
			}
			return id;
		}

		public int GetId(Node n) => nodes.IndexOf(n) + 1;
		public string GetName(Node n) => GetId(n).ToString("X2");

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

		//function reconstruct_path(cameFrom, current)
		List<Node> reconstruct_path(Dictionary<Node,Node> cameFrom, Node current) {
			List<Node> total_path = new List<Node>(); total_path.Add(current); //total_path := {current}
			while(cameFrom.TryGetValue(current, out Node beforeCurrent)){//while current in cameFrom.Keys:
				current = beforeCurrent; //current := cameFrom[current]
				total_path.Insert(0, beforeCurrent); //total_path.prepend(current)
			}
			return total_path;
		}

		private static float DistanceBetween(Node a, Node b) {
			Coord pA = a.nodeData.GetCoord();
			Coord pB = b.nodeData.GetCoord();
			return Coord.ManhattanDistance(pA, pB);
		}

		// A* finds a path from start to goal.
		// h is the heuristic function. h(n) estimates the cost to reach goal from node n.
		//function A_Star(start, goal, h)
		public List<Node> A_Star(Node start, Node goal, Dictionary<string,float> keys) {
			// The set of discovered nodes that may need to be (re-)expanded.
			// Initially, only the start node is known.
			// This is usually implemented as a min-heap or priority queue rather than a hash-set.
			
			List<Node> openSet = new List<Node>(); openSet.Add(start); //openSet := {start}

			// For node n, cameFrom[n] is the node immediately preceding it on the cheapest path from start
			// to n currently known.
			Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>(); //cameFrom := an empty map

			// For node n, gScore[n] is the cost of the cheapest path from start to n currently known.
			Dictionary <Node, float> gScore = new Dictionary<Node, float>(); //gScore := map with default value of Infinity

			//gScore[start] := 0
			gScore[start] = 0;

			// For node n, fScore[n] := gScore[n] + h(n). fScore[n] represents our current best guess as to
			// how short a path from start to finish can be if it goes through n.
			Dictionary<Node, float> fScore = new Dictionary<Node, float>(); //fScore := map with default value of Infinity
			fScore[start] = DistanceBetween(start, goal); //fScore[start] := h(start)

			while (openSet.Count > 0) { //while openSet is not empty
				// This operation can occur in O(1) time if openSet is a min-heap or a priority queue
				Node current = openSet[openSet.Count-1]; //current := the node in openSet having the lowest fScore[] value
				if (current == goal) {//if current = goal
					return reconstruct_path(cameFrom, current);// return reconstruct_path(cameFrom, current)
				}
				openSet.RemoveAt(openSet.Count - 1); //openSet.Remove(current)
				current.ForEachEdge(edge=>{ Node neighbor = edge._to;//for each neighbor of current
					// d(current,neighbor) is the weight of the edge from current to neighbor
					// tentative_gScore is the distance from start to the neighbor through current
					if(edge.cost.Count > 0 && !edge.IsTraversableBy(keys)) return;
					if(!edge.cost.TryGetValue("", out float standardCost)) { standardCost = 1; }
					float tentative_gScore = gScore[current] + standardCost; //tentative_gScore := gScore[current] + d(current, neighbor)
					if(!gScore.ContainsKey(neighbor) || tentative_gScore < gScore[neighbor]) {//if tentative_gScore < gScore[neighbor]
						// This path to neighbor is better than any previous one. Record it!
						cameFrom[neighbor] = current; //cameFrom[neighbor] := current
						gScore[neighbor] = tentative_gScore; //gScore[neighbor] := tentative_gScore
						fScore[neighbor] = gScore[neighbor] + DistanceBetween(neighbor, goal); //fScore[neighbor] := gScore[neighbor] + h(neighbor)
						if(!openSet.Contains(neighbor)) {//if neighbor not in openSet
							openSet.Add(neighbor);//openSet.add(neighbor)
							openSet.Sort((a, b) => -fScore[a].CompareTo(fScore[b]));
						}
					}
				});
			}
			// Open set is empty but goal was never reached
			return null;//return failure
		}

		// TODO create a PathTravelled algorithm, which keeps a queue of subsequent forks to travel. first, plot a path for the early-treasure room. then plot a path for a tunnel, which was blocked till now. then from that tunnel forward, pick a next-leaf to travel to, where a key of some kind is. then back to another tunnel, and repeat.

		// TODO rate the rooms for different purposes
			// early-treasure room, where the special dungeon puzzle-solving artifact is: early-to-mid leaf
			// final treasure area: terminal or leaf room after the boss monster room
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