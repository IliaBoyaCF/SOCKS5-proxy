# SOCKS5-proxy

General
----

Application implements proxy-server working on SOCKS protocol version 5
(https://www.ietf.org/rfc/rfc1928.txt).

Implemented features
----

- Negotiation without sub-negotiation(NO AUTHENTICATION REQUIRED).
- CONNECT requests.
- IPv4 and domain name support in requests.

Usage
----
    socks-proxy <PORT>
Starts a proxy server running using protocol SOCKS-5 on a specified port number.
Starting without specifying PORT will use the default port: 1080.


