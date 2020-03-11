using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ActionService
{
	public class InvalidRequestException : Exception
	{
		public InvalidRequestException()
			: base()
		{
		}

		public InvalidRequestException(string message)
			: base(message)
		{
		}

		public InvalidRequestException(string message, Exception inner)
			: base(message, inner)
		{
		}

		protected InvalidRequestException(System.Runtime.Serialization.SerializationInfo info,
										  System.Runtime.Serialization.StreamingContext context) { }
	}
}