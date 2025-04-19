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
        Task Bind(); 
        void HandlePostRequest(string content);
    }


    abstract class Proxy : IProxy
    {
        private protected string _bindAddr;
        private protected int _bindPort;
        private protected string _targethost;
        private protected HttpListener _listener;

        public async Task Bind()
        {
            try
            {
                _listener = new HttpListener();

                _listener.Prefixes.Add($"https://{_bindAddr}:{_bindPort}/");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                _listener.Start();
                Console.WriteLine("Proxy started");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start listener: " + ex.Message);
                return;
            }

            while (true)
            {
                try
                {
                    await ProccessRequests();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in Bind loop: " + ex.Message);
                }
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
                    await incomingRequest.InputStream.CopyToAsync(memoryStream);

                    // Reset the position of the memory stream to the beginning
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    using (StreamReader reader = new StreamReader(memoryStream, incomingRequest.ContentEncoding))
                    {
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
                    await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
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
                    if (IsRestrictedHeader(header))
                        continue;

                    context.Response.Headers[header] = webResponse.Headers[header];
                }

                context.Response.StatusCode = (int)webResponse.StatusCode;
                context.Response.ContentLength64 = webResponse.ContentLength;

                using (Stream rawStream = webResponse.GetResponseStream())
                {
                    Stream responseStream = rawStream;

                    // Check for gzip encoding
                    if (webResponse.ContentEncoding.ToLower().Contains("gzip"))
                    {
                        responseStream = new System.IO.Compression.GZipStream(rawStream, System.IO.Compression.CompressionMode.Decompress);
                    }

                    // Buffer the response so we can both read and forward it
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        await responseStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;

                        // Correct the Content-Length before sending it to the client
                        context.Response.ContentLength64 = memoryStream.Length;

                        // Try to read as text if content type is text or json or xml
                        string contentType = webResponse.ContentType.ToLower();
                        if (contentType.Contains("text") || contentType.Contains("json") || contentType.Contains("xml"))
                        {
                            using (StreamReader reader = new StreamReader(memoryStream, Encoding.UTF8, true, 1024, true))
                            {
                                string responseBody = reader.ReadToEnd();
                                Console.WriteLine("=== Response Body ===");
                                Console.WriteLine(responseBody);
                                Console.WriteLine("=====================");
                            }
                        }

                        // Rewind and write to response
                        memoryStream.Position = 0;
                        using (Stream outputStream = context.Response.OutputStream)
                        {
                            await memoryStream.CopyToAsync(outputStream);
                            await outputStream.FlushAsync();  // Ensure everything is written before closing
                        }
                    }
                }
            }

            // Ensure the response stream is properly closed only after everything has been written.
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
            // Implementation to handle POST requests can be customized here
        }
    }
}
