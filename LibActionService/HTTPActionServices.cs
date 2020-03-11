using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using NLog;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

namespace ActionService
{
	public class HTTPActionServices : ActionServiceRegistry
	{
		private HttpListener listener;
		private bool isListening;
		protected static Logger Log;
		private Dictionary<string, Action<ActionContext>> HandlerMap = new Dictionary<string, Action<ActionContext>>();
		private Uri ListenUri;

		public HTTPActionServices(Uri listenerPrefix)
		{
			Log = LogManager.GetCurrentClassLogger();
			ListenUri = listenerPrefix;
			startListening();
		}

		private void startListening()
		{
			try
			{
				listener = new HttpListener();
				listener.Prefixes.Add(ListenUri.ToString());
				listener.Start();
				isListening = true;
				this.Run();
			}
			catch (HttpListenerException ex)
			{
				Log.Warn("Could not start HTTPListener: " + ex.Message);
			}
		}

		private void Run()
		{
			var thread = new System.Threading.Thread(() =>
			{
				while (isListening)
				{
					try
					{
						handleRequest(listener.GetContext());
					}
					catch (HttpListenerException e)
					{
						Log.Warn("Action service: HTTP Listener error: " + e.Message);
					}
				}
				isListening = false;
			});
			thread.Start();
		}

		private void handleRequest(HttpListenerContext context)
		{
			try
			{
				Action<ActionContext> handler = findHandlerFor(context.Request.Url);
				if (handler == null)
				{
					buildResponse(404, context, new Graph());
					context.Response.Close();
					return;
				}
				IGraph requestGraph = ReadRequestGraph(context);
				AbstractActionContext actionContext = CreateActionContext(requestGraph);
				handler.Invoke(actionContext);
				int statusCode = actionContext.Failed ? 400 : 200;
				buildResponse(statusCode, context, actionContext.GetSyncOutput());
				context.Response.Close();
			}
			catch (ArgumentException ex)
			{
				Log.Error(ex, "Invalid argument:" + ex.Message);
				buildResponse(400, context, new Graph());
				context.Response.Close();
			}
			catch (InvalidRequestException ex)
			{
				Log.Error(ex, "Invalid request:" + ex.Message);
				buildResponse(400, context, new Graph());
				context.Response.Close();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Internal error:" + ex.Message);
				buildResponse(500, context, new Graph());
				context.Response.Close();
				return;
			}
		}

		private IGraph ReadRequestGraph(HttpListenerContext context)
		{
			IGraph requestGraph = new Graph();
			var body = new StreamReader(context.Request.InputStream);
			parseRDF(body, requestGraph);
			return requestGraph;
		}

		private AbstractActionContext CreateActionContext(IGraph requestGraph)
		{
			Uri completionLocation;
			AbstractActionContext actionContext;
			if (hasCompletionLocation(requestGraph, out completionLocation))
			{
				actionContext = new AsyncActionContext(requestGraph, completionLocation);
			}
			else
			{
				actionContext = new SyncActionContext(requestGraph);
			}

			return actionContext;
		}

		private void parseRDF(StreamReader message, IGraph graph)
		{
			TurtleParser parser = new TurtleParser();
			try
			{
				parser.Load(graph, message);
			}
			catch (Exception ex)
			{
				Log.Warn(ex, "Could not decode incoming rdf: " + ex.Message);
			}
			if (graph.IsEmpty)
			{
				throw new InvalidRequestException("Parsed graph was empty");
			}
		}

		private void buildResponse(int statusCode, HttpListenerContext context, IGraph responseGraph)
		{
			context.Response.StatusCode = statusCode;
			try
			{
				CompressingTurtleWriter writer = new CompressingTurtleWriter();
				StreamWriter responseWriter = new StreamWriter(context.Response.OutputStream);
				writer.Save(responseGraph, responseWriter);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Could not write to Response output: " + ex.Message);
			}
			finally
			{
				context.Response.OutputStream.Close();
			}
		}

		private bool hasCompletionLocation(IGraph graph, out Uri completionLocation)
		{
			var node = graph.CreateUriNode(new Uri("http://www.ajan.de/actn#asyncRequestURI"));
			var triples = graph.GetTriplesWithPredicate(node);
			if (triples.Any())
			{
				completionLocation = new Uri(triples.First().Object.ToString());
				return true;
			}
			else
			{
				completionLocation = null;
				return false;
			}
		}

		public void Stop()
		{
			Log.Info("Stopping ActionService");
			if (isListening)
			{
				isListening = false;
				listener.Stop();
			}
		}

		public void RegisterService(string path, Action<ActionContext> handler)
		{
			HandlerMap.Add(path, handler);
		}

		public void UnregisterService(string path)
		{
			HandlerMap.Remove(path);
		}

		private Action<ActionContext> findHandlerFor(Uri url)
		{
			// implicit knowledge: url coming in here can only start with ListenUri
			// (due to the way HttpListener works)
			string relativePath = url.ToString().Substring(ListenUri.ToString().Length);
			Action<ActionContext> handler;
			if (HandlerMap.TryGetValue(relativePath, out handler))
			{
				return handler;
			}
			else
			{
				return null;
			}
		}
	}
}