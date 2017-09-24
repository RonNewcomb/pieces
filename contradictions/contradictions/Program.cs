using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace contradictions
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			string input = @"
X before Y
Y before Z
X before Z
//G before Y
X during E
V during E
V after G
G after Y
T before G
M after E
";
			// syntax synonyms
			input = input.Replace("while", "during").Replace("except","unless");

			var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

			var graph = new Parser().Parse(lines);

			var ordering = new TopologicalSort().Sort(graph);

			if (ordering.Success)
			{
				"In order:".Log();
				ordering.sorted.Log();
			}
			else
			{
				"Circular logic".Log();
				foreach (List<Node> cycle in ordering.circularLogicErrors)
					cycle.Log();
			}

			Console.Read();

		}


		// extension methods ///////////////////// 

		public static string Log(this List<Node> nodes)
		{
			string retval = "";
			string previous = "";
			foreach (var item in nodes)
			{
				if (item.term.EndsWith("-ends") && previous.Replace("-begins", "-ends") == item.term)
				{
					retval += (previous.Replace("-begins", "") + " ");
					previous = "";
					continue;
				}
				else if (previous != "")
				{
					retval += (previous + " ");
					previous = "";
				}
				if (item.term.EndsWith("-begins"))
					previous = item.term;
				else
					retval += (item.term + " ");
			}

			retval += "\n\n\n" + nodes.Aggregate("", (s, n) => s + " " + n.term);
			Console.WriteLine(retval);
			return retval;
		}

		public static void Log(this string s)
		{
			Console.WriteLine(s);
		}

		public static T And<T>(this List<T> l, T item) where T : class
		{
			if (item == null) return null;
			l.Add(item);
			return item;
		}

		public static TopologicalSort Sort(this Tuple<List<Node>, List<Edge>> graph)
		{
			return new TopologicalSort().Sort(graph.Item1, graph.Item2);
		}


	}
}
