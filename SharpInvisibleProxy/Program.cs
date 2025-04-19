using System;
using System.Threading.Tasks;

namespace SharpInvisibleProxy
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: SharpInvisibleProxy.exe <proxyIpAddress> <ProxyType> [hostname]");
                return;
            }

            string proxyIpAddress = args[0];
            string proxyusage = args[1];
            string hostname = args.Length > 2 ? args[2] : "";

            if (string.IsNullOrEmpty(proxyusage))
            {
                Console.WriteLine("Proxy type is required");
                return;
            }

            Proxy proxy = null;

            if (proxyusage == "AAD")
            {
                proxy = new AADProxy(proxyIpAddress, 443);
            }
            else if (proxyusage == "Custom")
            {
                if (string.IsNullOrEmpty(hostname))
                {
                    Console.WriteLine("Hostname is required when using Custom proxy type");
                    return;
                }
                proxy = new Custom(proxyIpAddress, 443, hostname);
            }
            else
            {
                Console.WriteLine("Proxy type was not recognized (AAD, Custom)");
                return;
            }

            await proxy.Bind();
        }
    }
}
