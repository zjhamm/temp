using System;

namespace CS422
{
	internal abstract class WebService
	{
		public abstract void Handler(WebRequest req);

		// <Summary>
		// Gets the service URI. This is a string of the form:
		// MyServiceName.whatever
		// If a request hits the server and the request target starts with this
		// string then it will be routed to this service to handle.
		// </Summary>
		public abstract string ServiceURI
		{
			get;
		}
	}
}

