using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using Microsoft.Kinect;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    class Server
    {
        
        private TcpListener tcpListener;
        private Thread listenThread;
        private string name;
        private string widthRect;
        private string heightRect;

        public Server(string a, TcpListener b, string c, string d)
        {

            this.tcpListener = b;
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
            this.name = a;
            this.widthRect = c;
            this.heightRect = d;

        }
       

        private void ListenForClients()
        {
            this.tcpListener.Start();
      
                System.Console.WriteLine("Listening...");
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();
                System.Console.WriteLine("Client connected");

                //create a thread to handle communications with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);

        }
        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();
            

            byte[] message = new byte[4096];
            int bytesRead;
            string msg = name + "-" + widthRect + " cm" + "-" + heightRect + " cm";
            while (true)
            {
                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                //message has successfully been received
                ASCIIEncoding encoder = new ASCIIEncoding();
                String mes = encoder.GetString(message, 0, bytesRead);
                
                
                //Server reply to the client
                byte[] buffer = encoder.GetBytes(msg);
                Console.WriteLine(buffer.ToString());
               // var converter = new System.Drawing.ImageConverter();
               // byte[] buffer1 = (byte[])converter.ConvertTo(_CurrentBitmap, typeof(byte[]));
                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();
                clientStream.FlushAsync();
                

                //netstat -anp tcp | find “:3200”



            }
            
            

            tcpClient.Close();
            
        }
        
    
    }















}