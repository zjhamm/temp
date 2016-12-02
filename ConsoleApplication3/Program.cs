using System;
using System.IO;
using System.Net.Sockets;

namespace CS422
{
	class MainClass
	{
		public static void Main (string[] args)
		{
            StandardFileSystem fs = StandardFileSystem.Create("C:\\Users\\Zach Hamm\\Documents");

            WebServer.Start (1337, 1);
			WebService service = new FilesWebService (fs);
			WebServer.AddService (service);

			while (true) {
			}

			WebServer.Stop ();

		}
	}
}
