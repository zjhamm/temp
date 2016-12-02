using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace CS422
{
	public class WebRequest
	{
		private string _HTTPMethod;
		private string _URI;
		private string _HTTPVersion;
		private string _length;
		private Dictionary<string, string> _headers;
		private ConcatStream _body;

		private NetworkStream _response;


		public WebRequest(string requestString)
		{
			this._HTTPMethod = "";
			this._URI = "";
			this._HTTPVersion = "";
			this._headers = new Dictionary<string, string>();

			buildRequest(requestString);
		}

        public WebRequest(string requestString, NetworkStream requestStream)
        {
            this._HTTPMethod = "";
            this._URI = "";
            this._HTTPVersion = "";
            this._headers = new Dictionary<string, string>();
            this._response = requestStream;

            buildRequest(requestString);
        }

		// If the content-length header is there, use the ConcatStream(Stream, Stream, length) constructor
		// If it is not, use the ConcatStream(Stream, Stream) constructor

		public void WriteNotFoundResponse(string pageHTML)
		{
            string responseTemplate = String.Format(
                "HTTP/1.1 404 Not Found\r\n" +
                "Content-Type: text/html\r\n" +
                "Content-Length: {0}\r\n" +
                "\r\n" + pageHTML, pageHTML.Length);

            byte[] response = System.Text.Encoding.ASCII.GetBytes(responseTemplate);

            this._response.Write(response, 0, response.Length);
		}

		public bool WriteHTMLResponse(string htmlString)
		{
            string responseTemplate = String.Format(
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html\r\n" +
                "Content-Length: {0}\r\n" +
                "\r\n" + htmlString, htmlString.Length);
            responseTemplate += "</html>";

            byte[] response = System.Text.Encoding.ASCII.GetBytes(responseTemplate);

            this._response.Write(response, 0, response.Length);

            return true;
        }

        public bool WriteStreamResponse(Stream stream, string ContentType)
        {
            string responseTemplate = String.Format(
                    "HTTP/1.1 200 OK\r\n" +
                    "Content-Type: {0}\r\n" +
                    "Content-Length: {1}\r\n" +
                    "\r\n",
                    ContentType,
                    stream.Length);

            // Write the response template to get the headers going and what not
            byte[] response = Encoding.ASCII.GetBytes(responseTemplate);
            this._response.Write(response, 0, response.Length);

            // The stream could not be done writing content so keep reading until
            // there is no more content left.
            byte[] buffer = new byte[4096];
            int numBytesRead = stream.Read(buffer, 0, buffer.Length);
            while (numBytesRead > 0)
            {
                this._response.Write(buffer, 0, buffer.Length);
                numBytesRead = stream.Read(buffer, 0, buffer.Length);
            }

            return true; 
        }

        private void buildRequest(string requestString)
        {
            int i = 0;

            if (requestString == null || requestString == "")
            {
                return;
            }

            // Save the HTTP Method, I know if will only be GET
            // for now, but if I do not hard code it I will not have
            // to change it later if the specs change.
            while (requestString[i] != ' ')
            {
                this._HTTPMethod += requestString[i++];
            }

            // Increment over space
            i++;

            // Need to loop over the URI and parse it out
            while (requestString[i] != ' ')
            {
                _URI += requestString[i++];
            }

            // Increment over space
            i++;

            // Save the HTTP version
            while (requestString[i] != '\r')
            {
                this._HTTPVersion += requestString[i++];
            }

            //Skip over \r\n
            i += 2;

            //Start with the headers
            while (true)
            {
                StringBuilder header = new StringBuilder();
                StringBuilder value = new StringBuilder();
				StringBuilder bodyTester = new StringBuilder();

                //Loop through the word and break at the colon
				while (requestString[i] != ':')
                {
                    header.Append(requestString[i++]);
                }

                // Skip the space
                i++;

                while(requestString[i] != '\r')
                {
                    value.Append(requestString[i++]);
                }

				//this._headers.Add(header.ToString(), value.ToString().Trim());

				while (i < requestString.Length && (requestString[i] == '\r' || requestString[i] == '\n'))
				{
					bodyTester.Append(requestString[i++]);
				}

				// Check to see if we found the \r\n\r\n
				if (bodyTester.ToString() == "\r\n\r\n")
				{
					break;
				}
            }

            // Get to start of the body
            i++;

            // Save the body
			if (this._headers.ContainsKey("Content-Length".ToLower()))
            {
				this._length = _headers ["Content-Length"];
                MemoryStream tempBodyStream = new MemoryStream(Encoding.ASCII.GetBytes(requestString.Substring(i)));
                this._body = new ConcatStream(tempBodyStream, this._response, Convert.ToInt64(this._length));
            }
            else
            {
				this._length = "Unknown";
                MemoryStream tempBodyStream = new MemoryStream(Encoding.ASCII.GetBytes(requestString.Substring(i - 1)));
                this._body = new ConcatStream(tempBodyStream, this._response);
            }
        }

		public string Length 
		{
			get 
			{
				return this._length;
			}
		}

		public string URI
		{
			get 
			{
				return this._URI;
			}
		}

		public string HTTPMethod
		{
			get 
			{
				return this._HTTPMethod;
			}
		}
	}
}

