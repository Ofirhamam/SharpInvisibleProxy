# SharpInvisibleProxy

C# Invisible HTTPS proxy  
MITM Proxy server written in C# that performs SSL inspection to obtain credentials and tokens from client connections.

## Prerequisites:

1. A trusted "Server Authentication" certificate for the requested domain.
2. On the proxy server, bind the trusted certificate to port 443 using the following `netsh` command:
   ```bash
   netsh http add sslcert ipport=0.0.0.0:443 certhash=7bf81da1a8911593d42e68db5e23ddfe49f338a3 appid={81eaa9c5-8965-4a0a-a00a-8edada28c473}
   ```
3. (Optional) When needed, modify the client `HOSTS` file to redirect it to the proxy server.

## Cleanup: 

To remove the installed certificate and the certificate port binding, use the following command:
```bash
netsh http delete sslcert ipport=0.0.0.0:443
```

## Miscellaneous:

To update the binding to another certificate, use this command:
```bash
netsh http update sslcert ipport=0.0.0.0:443 certhash=0f09ba64e065dde844d02bd3f34a64d3de46381d appid={81eaa9c5-8965-4a0a-a00a-8edada28c473}
```
