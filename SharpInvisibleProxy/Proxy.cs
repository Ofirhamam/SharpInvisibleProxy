using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SharpInvisibleProxy
{
    interface IProxy
    {
        void Bind();
        void HandlePostRequest(string content);
    }
    abstract class Proxy : IProxy
    {
        private protected string _bindAddr;
        private protected int _bindPort;
        private protected string _targethost;
        private protected HttpListener _listener;
        public void Bind()
        {
            try
            {
                _listener.Prefixes.Add($"https://{_bindAddr}:{_bindPort}/");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                _listener.Start();
                Console.WriteLine("Proxy started");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            while (true)
            {
                Task.WaitAll(ProccessRequests());
            }
        }
        private async Task ProccessRequests()
        {
            HttpListenerContext context = await _listener.GetContextAsync();
            HttpListenerRequest incomingRequest = context.Request;

            Console.WriteLine("Request: " + incomingRequest.HttpMethod + " " + incomingRequest.Url);

            byte[] byteArray = null;
            string content = null;

            if (incomingRequest.HttpMethod == "POST")
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    // Copy the input stream to the memory stream
                    incomingRequest.InputStream.CopyTo(memoryStream);

                    // Reset the position of the memory stream to the beginning
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    using (StreamReader reader = new StreamReader(memoryStream, incomingRequest.ContentEncoding))
                    {
                        //string content = reader.ReadToEnd();
                        content = reader.ReadToEnd();
                        HandlePostRequest(content);
                        byteArray = Encoding.UTF8.GetBytes(content);

                    }
                }
            }

            IPAddress targetIpAddress = Dns.GetHostEntry(_targethost).AddressList[1];
            string targetUrl = "https://" + targetIpAddress + incomingRequest.Url.PathAndQuery;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(targetUrl);
            webRequest.Method = incomingRequest.HttpMethod;

            //debug option - redirect the proxy traffic to burp.
            //string MyProxyHostString = "127.0.0.1";
            //int MyProxyPort = 8080;
            //webRequest.Proxy = new WebProxy(MyProxyHostString, MyProxyPort);
            //debug option

            // Set the 'Host' property instead of modifying the 'Host' header
            webRequest.Host = _targethost;

            foreach (string header in incomingRequest.Headers.AllKeys)
            {
                // Skip the restricted headers
                if (IsRestrictedHeader(header))
                    continue;

                webRequest.Headers[header] = incomingRequest.Headers[header];
            }

            if (incomingRequest.HttpMethod == "POST")
            {
                using (Stream dataStream = webRequest.GetRequestStream())
                {
                    byteArray = Encoding.UTF8.GetBytes(content);
                    dataStream.Write(byteArray, 0, byteArray.Length);

                }
            }

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                return true; // Allow all certificates
            };


            using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                // Copy response headers
                foreach (string header in webResponse.Headers.AllKeys)
                {
                    // Skip the restricted headers
                    if (IsRestrictedHeader(header))
                        continue;

                    context.Response.Headers[header] = webResponse.Headers[header];
                }

                // Copy response status code
                context.Response.StatusCode = (int)webResponse.StatusCode;

                // Set the Content-Length header of the response explicitly
                context.Response.ContentLength64 = webResponse.ContentLength;

                // Copy response body
                using (Stream inputStream = webResponse.GetResponseStream())
                {
                    using (Stream outputStream = context.Response.OutputStream)
                    {
                        inputStream.CopyTo(outputStream);
                    }
                }
            }

            context.Response.Close();
        }
        private protected Dictionary<string, string> ParseRequestBody(string requestBody)
        {
            var parameters = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(requestBody))
            {
                string[] pairs = requestBody.Split('&');

                foreach (string pair in pairs)
                {
                    int equalsIndex = pair.IndexOf('=');
                    if (equalsIndex >= 0)
                    {
                        string key = Uri.UnescapeDataString(pair.Substring(0, equalsIndex));
                        string value = Uri.UnescapeDataString(pair.Substring(equalsIndex + 1));
                        parameters[key] = value;
                    }
                }
            }

            return parameters;
        }
        bool IsRestrictedHeader(string header)
        {
            // List of restricted headers
            string[] restrictedHeaders = { "Accept", "Connection", "Content-Length", "Content-Type", "Date", "Expect", "Host", "If-Modified-Since", "Range", "Referer", "Transfer-Encoding", "User-Agent", "Proxy-Connection" };

            // Check if the header is restricted
            return Array.Exists(restrictedHeaders, h => string.Equals(h, header, StringComparison.OrdinalIgnoreCase));
        }

        public virtual void HandlePostRequest(string content)
        {

        }
    }
}
