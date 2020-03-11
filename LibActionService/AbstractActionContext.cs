using System;
using VDS.RDF;

namespace ActionService
{
	public abstract class AbstractActionContext : ActionContext
	{
		readonly IGraph Input;
		public bool Failed { get; protected set; }
		internal protected bool SyncResponseSent;
		internal IGraph SyncOutput;

		public AbstractActionContext(IGraph input)
		{
			Input = input;
			SyncResponseSent = false;
			Failed = false;
			SyncOutput = new Graph();
		}

		public abstract void Complete(IGraph response);
		public abstract bool IsAsynchronous();
		public abstract void Fail(string description);

		public IGraph GetInput()
		{
			return Input;
		}

		internal IGraph GetSyncOutput()
		{
			SyncResponseSent = true;
			return SyncOutput;
		}

		protected IGraph FailGraph(string description)
		{
			IGraph failGraph = new Graph();
			INode type = failGraph.CreateUriNode("rdf:type");
			INode fault = failGraph.CreateUriNode(new Uri("http://www.ajan.de/actn#FAULT"));
			INode desc = failGraph.CreateUriNode(new Uri("http://purl.org/dc/terms/description"));
			INode text = failGraph.CreateLiteralNode(description);
			foreach (INode subject in Input.Triples.SubjectNodes)
			{
				var subjCopy = Tools.CopyNode(subject, failGraph);
				failGraph.Assert(new Triple(subjCopy, type, fault));
				failGraph.Assert(new Triple(subjCopy, desc, text));
			}
			return failGraph;
		}
	}
}

