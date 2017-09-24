using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace contradictions
{
	public enum Op { before, after, notbefore, notafter, during, unless }; // "always/never" need not have its changes sorted because there arent any.
		// eventually is unary; I'd prefer "eventually X" be changed to a before/after because "eventually" is a long, long time. End of program??
		// still, makes a great sanity-checking tool for the user
		// during = while; !while = unless; unless = !while
		// except = unless

	// how to temporal connectives & loops interact? are loops repeated time?
	// No. Loops are shorthand for writing work(item.1), work(item.2), work(item.3), etc. 
	// before/after work(items) means before/after the whole loop.
	// else, before/after need specify work(an item) and let the connected op be broken apart & placed inside the loop body
 
	public class Node
	{
		public string term;
		public Node(string t) { term = t; }
		// only for visit()
		public uint index = 0;
		public uint lowlink;
		public List<Edge> meAsIndependent = new List<Edge>();
	};

	public class Edge
	{
		public Op op;
		public Node independent, dependent;
		public bool swapped = false;
		public Edge(Node from, Op o, Node to) { independent = from; dependent = to; op = o; from.meAsIndependent.Add(this); }
	};

	public class TopologicalSort // depth-first, with Tarjan's algorithm
	{
		// can be output
		public bool Success { get { return circularLogicErrors.Count == 0; } }
		public List<Node> sorted = new List<Node>(); // Empty list that will contain the sorted elements
		public List<List<Node>> circularLogicErrors = new List<List<Node>>();

		public TopologicalSort Sort(Tuple<List<Node>, List<Edge>> graph)
		{
			return Sort(graph.Item1, graph.Item2);
		}

		public TopologicalSort Sort(List<Node> nodes, List<Edge> edges)
		{
			this.edges = edges; // pass to visit() by member field, because visit() is recursive

			// topological sort of the nodes, remembering cycles ("strongly connected groups")
			foreach (Node node in nodes)
				if (node.index == 0)
					visit(node);

			return this;
		}

		// used by hidden visit()
		protected List<Edge> edges;// = parameter
		protected Stack<Node> S = new Stack<Node>();
		protected uint indexCreator = 1;

		// recursive 
		protected void visit(Node v)
		{
			// init
			v.index = indexCreator;
			v.lowlink = indexCreator;
			indexCreator++;
			S.Push(v);

			// examine children
			//foreach (Edge edge in edges.Where(e => e.independent == v))
			foreach (Edge edge in v.meAsIndependent)
			{
				if (edge.dependent.index == 0)
				{
					visit(edge.dependent);
					edge.independent.lowlink = Math.Min(edge.independent.lowlink, edge.dependent.lowlink);
				}
				else if (S.Contains(edge.dependent))
					edge.independent.lowlink = Math.Min(edge.independent.lowlink, edge.dependent.index);
			}

			// insert parent into Sorted
			sorted.Insert(0, v);

			// check for end of group
			if (v.lowlink != v.index) return;

			// possible cycle; definitely a group found. Check.
			var cycle = new List<Node>();
			while (v != cycle.And(S.Pop()));
			if (cycle.Count > 1)
				circularLogicErrors.Add(cycle);
		}


	}
}
