using System;
using VDS.RDF;

namespace ActionService
{
	public class SyncActionContext : AbstractActionContext
	{
		public SyncActionContext(IGraph input) : base(input)
		{
		}

		public override void Complete(IGraph response)
		{
			if (SyncResponseSent)
			{
				throw new InvalidOperationException("Action has already been completed synchronously");
			}
			SyncOutput = response;
		}

		public override void Fail(string description)
		{
			Failed = true;
			SyncOutput = FailGraph(description);
			SyncResponseSent = true;
		}

		public override bool IsAsynchronous()
		{
			return false;
		}
	}
}

