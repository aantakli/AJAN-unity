using System;

namespace ActionService
{
	public interface ActionServiceRegistry
	{
		void RegisterService(string path, Action<ActionContext> handler);
		void UnregisterService(string path);
	}
}

