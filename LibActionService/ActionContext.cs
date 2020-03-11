using System;
using VDS.RDF;

namespace ActionService
{
	public interface ActionContext
	{
		IGraph GetInput();
		void Complete(IGraph response);
		void Fail(string description);
		bool IsAsynchronous();
	}
}

