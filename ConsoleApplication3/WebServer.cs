using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;


namespace CS422
{
	internal class WebServer
	{
        static MyThreadPool _pool;
        static BlockingCollection<WebService> _webServices;
        static TcpListener _listener;
        static Thread _listenThread;
        static int _port;
		static DateTime startTime;

		public static bool Start(int port, int numThreads)
		{
            _port = port;
            _webServices = new BlockingCollection<WebService>();

            _pool = new MyThreadPool(numThreads);

            _listenThread = new Thread(BeginListen);
            _listenThread.Start();

            return true;
		}

        public static void BeginListen()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();

                while (true)
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    _pool.AddClient(client);
                }
            }
            catch
            {
                Console.WriteLine("Listener ending");
            }
        }

        public static void ThreadWork()
        {
            while (true)
            {
                TcpClient client = _pool.TakeClient();

                if (client != null)
                {
                    Console.WriteLine("Found client, starting");
                    WebRequest request = BuildRequest(client);

                    if (request == null)
                    {
                        client.GetStream().Dispose();
                        client.Close();
                    }
                    else
                    {
                        bool found = false;

                        foreach (WebService service in _webServices)
                        {
                            if (request.URI.StartsWith(service.ServiceURI))
                            {
                                service.Handler(request);
                                client.Close();
                                found = true;
                            }
                        }

                        if (!found)
                        {
                            request.WriteHTMLResponse("404 Page Not Found");
                        }
                    }
                }
            }
        }

        public static void Stop()
        {
            _pool.Dispose();
            _listener.Stop();
            _listenThread.Join();
            return;
        }

        public static void AddService(WebService service)
        {
            _webServices.Add(service);
        }

        private static WebRequest BuildRequest(TcpClient client)
		{
            if (client == null)
            {
                return null;
            }

            NetworkStream nwStream = client.GetStream();
            StringBuilder m_sb = new StringBuilder();
            byte[] buf = new byte[4096];
            int bytes_read = 0;
			int totalBytesRead;

			startTime = DateTime.Now;
			nwStream.ReadTimeout = 2000;

            try
            {
                 bytes_read = nwStream.Read(buf, 0, buf.Length);
            }
            catch
            {
                
            }
            
			totalBytesRead = bytes_read;

			while ((DateTime.Now - startTime < new TimeSpan(0, 0, 10)) && bytes_read != 0)
            {
                //Appends the stream bytes to our string builder
                m_sb.Append(Encoding.ASCII.GetString(buf, 0, bytes_read));

                // Tests our string, if we get a -1 it is an invalid string
                // if we get a 1 it is a valid string
                // if we get a 0 then we don't have either yet so keep going
                int result = WebServer.checkIfValidRequest(m_sb, totalBytesRead);
                if (result == -1)
                {
                    client.Close();
                    return null;
                }
                else if (result == 1)
                {
                    break;
                }

                // Read in as many bytes as we can from the stream
                bytes_read = nwStream.Read(buf, 0, buf.Length);
				totalBytesRead += bytes_read;
            }

            if (m_sb.Length == 0)
            {
                return null;
            }

            return new WebRequest(m_sb.ToString(), nwStream);
		}



		//Bulk of the assignment. Does parsing of the string and verifying
		public static int checkIfValidRequest(StringBuilder sb, int totalBytesRead)
		{
			List<StringBuilder> words_from_request = new List<StringBuilder>();
			StringBuilder temp_sb = new StringBuilder();
			int word_incrementer = 0;

			words_from_request.Add(temp_sb);

			// Go through the string checking for EVERYTHING+
			for (int i = 0; i < sb.Length; i++)
			{
				if (sb[i] == '\n') 
				{
					words_from_request[word_incrementer].Append(sb[i]);
				}

				// If we see a space we know that's the end of the word so
				// save the current word and move on
				if (sb[i] == ' ' || sb[i] == '\n')
				{
					// We need to make sure that there are not two spaces in a row
					// because it is an invalid HTTP request if there is
					if (sb.Length == (i + 2) && sb[i + 1] == ' ')
					{
						return -1;
					}

					if (words_from_request [word_incrementer].ToString () != "\r\n") {
						
						// Saves the word to our list (i.e. "GET" or "HTTP/1.1\r\n"
						// Then moves on
						temp_sb = new StringBuilder();
						words_from_request.Add(temp_sb);
						word_incrementer++;

						continue;
					}
				}

				// Adds our current letter to our string builder
				words_from_request[word_incrementer].Append(sb[i]);

				// Does our checking for a valid HTTP request
				int result = doWordChecking(words_from_request);

				// If we get a -1 back that means it is an invalid request
				// If we get a 3 back that means that all three of our validations
				// Passed so we can essentially "return true"
				if (result == -1) 
				{
					return -1;
				}
				else if (result != 3 && totalBytesRead > 2048) 
				{
					return -1;
				} 
				else if (result != 4 && totalBytesRead > (100 * 1024)) 
				{
					return -1;
				} 
				else if (result == 4) 
				{
					return 1;
				}
			}

			return 0;
		}

		/*
              This function runs our checks and increments when they come back correct
              Or returns a -1 if it was invalid  
        */
		
		public static int doWordChecking(List<StringBuilder> words)
		{
			int checks_true = 0;

			if (words == null)
			{
				return 5;
			}

			if (words.Count >= 1)
			{
				if (!checkForGet(words[0].ToString()))
				{
					return -1;
				}

				checks_true++;
			}

			if (words.Count >= 2)
			{
				if (!checkForRequestedURL(words[1].ToString()))
				{
					return -1;
				}

				checks_true++;
			}

			if (words.Count >= 3)
			{
				if (!checkForHTTP(words[2].ToString()))
				{
					return -1;
				}

				checks_true++;
			}

			if (words.Count >= 4) 
			{
				if (checkForDoubleLineBreak(words[words.Count - 1].ToString ()))
				{
					checks_true++;
				}
			}

			return checks_true;
		}


		// Pretty straight forward. Made a List of all possibilites of
		// the GET. If it doesn't match part of it we return false,
		// otherwise return ture
		public static bool checkForGet(String input)
		{
			List<String> validGetStrings = new List<String> { "G",
				"GE",
				"GET" };

			for (int i = 0; i < validGetStrings.Count; i++)
			{
				if (validGetStrings[i] == input)
				{
					return true;
				}
			}

			return false;
		}


		// Pretty straight forward. Made a List of all possibilites of
		// the HTTP. If it doesn't match part of it we return false,
		// otherwise return ture
		public static bool checkForHTTP(String input)
		{
			List<String> validHTTPStrings = new List<String> {  "H",
				"HT",
				"HTT",
				"HTTP",
				"HTTP/",
				"HTTP/1",
				"HTTP/1.",
				"HTTP/1.1",
				"HTTP/1.1\r",
				"HTTP/1.1\r\n"  };

			for (int i = 0; i < validHTTPStrings.Count; i++)
			{
				if (validHTTPStrings[i] == input)
				{
					return true;
				}
			}

			return false;
		}


		// Pretty straight forward. If the URL does not start with a
		// '/' character like it does normally return false, otherwise
		// return true
		public static bool checkForRequestedURL(String input)
		{
			if (input[0] == '/')
			{
				return true;
			}

			return false;
		}

		public static bool checkForDoubleLineBreak(String input)
		{
			if (input [0] == '\r') 
			{
				if (input.Length > 1 && input[1] == '\n') 
				{
					return true;
				}
			}

			return false;
		}
	}
}

