using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpInvisibleProxy
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: SharpInvisibleProxy.exe <proxyIpAddress> <ProxyType> [hostname]");
                return;
            }

            // Parse the command line arguments            
            string proxyIpAddress = args[0];
            string proxyusage = args[1];
            string hostname = args.Length > 2 ? args[2] : ""; // Use a default value if hostname is not provided

            if (proxyusage == null)
            {
                Console.WriteLine("Proxy type is required");
            }

            else if (proxyusage == "AAD")
            {
                Proxy proxy = new AADProxy(proxyIpAddress, 443);
                proxy.Bind();
            }

            else if (proxyusage == "Custom")
            {
                if (string.IsNullOrEmpty(hostname))
                {
                    Console.WriteLine("Hostname is required when using Custom proxy type");
                }
                else
                {
                    Proxy proxy = new Custom(proxyIpAddress, 443, hostname);
                    proxy.Bind();
                }
            }

            else
            {
                Console.WriteLine("Proxy type was not recognize (AAD, Custom)");
                return;
            }
        }
    }
}
