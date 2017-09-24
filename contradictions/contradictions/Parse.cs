using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace contradictions
{
	public class Parser
	{
		public Tuple<List<Node>, List<Edge>> Parse(string[] lines)
		{
			var nodes = new List<Node>();
			var edges = new List<Edge>();

			// create nodes & edges from text 
			var h = new Dictionary<string, Node>();
			foreach (string line in lines)
			{
				string[] pieces = line.Trim().ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (pieces[0].Trim().StartsWith("//")) continue;//comment removal

				string left = pieces[0].ToUpper();
				Op o = (Op)Enum.Parse(typeof(Op), pieces[1], true);
				string rght = pieces[2].ToUpper();

				var leftBegin = left + "-begins";
				var leftEnd = left + "-ends";
				var rghtBegin = rght + "-begins";
				var rghtEnd = rght + "-ends";

				if (!h.ContainsKey(leftBegin))
				{
					h.Add(leftBegin, nodes.And(new Node(leftBegin)));
					h.Add(leftEnd, nodes.And(new Node(leftEnd)));
					edges.Add(new Edge(h[leftBegin], Op.before, h[leftEnd]));
				}
				if (!h.ContainsKey(rghtBegin))
				{
					h.Add(rghtBegin, nodes.And(new Node(rghtBegin)));
					h.Add(rghtEnd, nodes.And(new Node(rghtEnd)));
					edges.Add(new Edge(h[rghtBegin], Op.before, h[rghtEnd]));
				}

				switch (o)
				{
					case Op.notafter: // left notafter right
						edges.Add(new Edge(h[leftEnd], Op.before, h[rghtEnd]));
						break;
					case Op.notbefore: // left notbefore right
						edges.Add(new Edge(h[rghtBegin], Op.before, h[leftBegin]));
						break;
					case Op.after: // right before left; strictly 
						edges.Add(new Edge(h[rghtEnd], Op.before, h[leftBegin]) { swapped = true });
						break;
					case Op.before: // left before right; strictly
						edges.Add(new Edge(h[leftEnd], Op.before, h[rghtBegin]));
						break;
					case Op.unless: // disjunctin!!!  either left before right or left after right but not left during right
						throw new NotImplementedException("requires disjunction: left before right OR left after right");
						// or requires "weak", breakable constraints
						//edges.Add(new Edge(h[rghtBegin], Op.before, h[leftBegin]));
						//edges.Add(new Edge(h[leftEnd], Op.before, h[rghtEnd]));
						//break;
					case Op.during: // left during right 
						edges.Add(new Edge(h[rghtBegin], Op.before, h[leftBegin]));
						edges.Add(new Edge(h[leftEnd], Op.before, h[rghtEnd]));
						break;
				}
			}
			return new Tuple<List<Node>, List<Edge>>(nodes, edges);
		}

	}
}
