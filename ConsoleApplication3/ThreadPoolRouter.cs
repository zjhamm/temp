using System;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CS422
{
    public class MyThreadPool : IDisposable
    {
        BlockingCollection<TcpClient> m_clients;
        List<Thread> m_threads;
        int m_port;

        public MyThreadPool(int numThreads)
        {
            this.m_clients = new BlockingCollection<TcpClient>();

            /*if (numThreads <= 0)
            {
                numThreads = 64;
            }
            else { numThreads = 0; }*/
            
            this.m_threads = new List<Thread>();

            for (int i = 0; i < numThreads; i++)
            {
                Thread thread = new Thread(WebServer.ThreadWork);
                thread.Start();
                this.m_threads.Add(thread);
            }
        }
        
        public void AddClient(TcpClient client)
        {
            this.m_clients.Add(client);
        }

        public TcpClient TakeClient()
        {
            if (this.m_clients.Count == 0)
            {
                return null;
            }
                
            return this.m_clients.Take();
        }

        public void Dispose()
        {
            for (int i = 0; i < this.m_threads.Count; i++)
            {
                m_clients.Add(null);
                m_threads[i].Join();
            }
        }

        public int ThreadCount
        {
            get
            {
                return this.m_threads.Count;
            }
        }
    }
}
