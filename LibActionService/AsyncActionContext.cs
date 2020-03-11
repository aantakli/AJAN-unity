using System;
using System.IO;
using System.Net;
using NLog;
using VDS.RDF;
using VDS.RDF.Writing;

namespace ActionService
{
	public class AsyncActionContext : AbstractActionContext
	{
		private readonly Uri CallbackUri;
		private static Logger Log = LogManager.GetCurrentClassLogger();
		internal bool SyncPartCompleted = false;

		public AsyncActionContext(IGraph input, Uri callback)
			: base(input)
		{
			CallbackUri = callback;
		}

		public override void Complete(IGraph response)
		{
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(CallbackUri);
			request.Method = "POST";
			request.ContentType = "text/turtle";
			CompressingTurtleWriter writer = new CompressingTurtleWriter();
			Stream requestStream = request.GetRequestStream();
			StreamWriter requestWriter = new StreamWriter(requestStream);
			writer.Save(response, requestWriter);
			requestStream.Close();
			try
			{
				request.GetResponse();
			}
			catch (Exception ex)
			{
				Log.Warn(ex, "Could not perform completion request");
			}
		}

		public override bool IsAsynchronous()
		{
			return true;
		}

		public override void Fail(string description)
		{
			if (SyncResponseSent)
			{
				Complete(FailGraph(description));
			}
			else
			{
				Failed = true;
				SyncOutput = FailGraph(description);
			}
		}
	}
}