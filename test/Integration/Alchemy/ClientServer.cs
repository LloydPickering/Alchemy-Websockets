using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Alchemy.Classes;
using NUnit.Framework;

namespace Alchemy
{
    [TestFixture]
    public class ClientServer
    {
        private WebSocketServer _server;
        private WebSocketClient _client;
        private WebSocketServer _wssserver;
        private WebSocketClient _wssclient;
        private bool _forever;
        private bool _clientDataPass = true;
        private Int32 serverport;
        private Int32 wssserverport;

        [TestFixtureSetUp]
        public void SetUp()
        {
            //setup for Ws://
            serverport = 54321;
            _server = new WebSocketServer(serverport, IPAddress.Loopback) { OnReceive = OnServerReceive };
            _server.Start();
            _client = new WebSocketClient(String.Format("ws://127.0.0.1:{0}/path", serverport)) { Origin = "localhost", OnReceive = OnClientReceive };
            _client.Connect();


            //Setup for Wss://
            wssserverport = 54320;
            X509Certificate cert = GetCert();

            _wssserver = new WebSocketServer(wssserverport, IPAddress.Loopback, tls:true, tlscert: cert) { OnReceive = OnServerReceive };
            _wssserver.Start();
            _wssclient = new WebSocketClient(String.Format("wss://127.0.0.1:{0}/path", wssserverport), AllowUnverifiedWssCerts: true) { Origin = "localhost", OnReceive = OnClientReceive };
            _wssclient.Connect();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            _client.Disconnect();
            _server.Stop();
            _client = null;
            _server = null;

            _wssclient.Disconnect();
            _wssserver.Stop();
            _wssclient = null;
            _wssserver = null;
        }

        private static X509Certificate GetCert()
        {
            System.Security.Cryptography.X509Certificates.X509Certificate cert = null;

            //Try to get cert from file
            cert = X509Certificate.CreateFromCertFile("dummycert.cer");

            if (cert == null)
            {
                X509Store store = new X509Store("My");
                store.Open(OpenFlags.ReadOnly);

                //Try to find a valid cert for localhost in Store
                foreach (X509Certificate c in store.Certificates)
                {
                    if (c.Subject == "localhost")
                    {
                        cert = c;
                    }
                }

                //no localhost cert, so try picking the first one in the store
                if (store.Certificates.Count > 0 && cert == null)
                {
                    cert = store.Certificates[0];
                }

                //if we still dont have a cert - die
                if (cert == null)
                {
                    throw new Exception("No certificates found for WSS Testing. Please create one. To do so, see http://compilewith.net/2007/12/creating-test-x509-certificates.html");
                }
            }
            return cert;
        }

        private static void OnServerReceive(UserContext context)
        {
            var data = context.DataFrame.ToString();
            context.Send(data);
        }

        private void OnClientReceive(UserContext context)
        {
            var data = context.DataFrame.ToString();
            if (data == "Test")
            {
                if (_forever && _clientDataPass)
                {
                    context.Send(data);
                }
            }
            else
            {
                _clientDataPass = false;
            }
        }

        [Test]
        public void ClientConnect()
        {
            Assert.IsTrue(_client.Connected);
        }
        [Test]
        public void WssClientConnect()
        {
            Assert.IsTrue(_wssclient.Connected);
        }

        [Test]
        public void ClientSendData()
        {
            _forever = false;
            if (_client.Connected)
            {
                _client.Send("Test");
                Thread.Sleep(1000);
            }
            Assert.IsTrue(_clientDataPass);
        }
        [Test]
        public void WssClientSendData()
        {
            _forever = false;
            if (_wssclient.Connected)
            {
                _wssclient.Send("Test");
                Thread.Sleep(1000);
            }
            Assert.IsTrue(_clientDataPass);
        }

        [Test]
        public void ClientSendDataConcurrent()
        {
            _forever = true;
            if (_client.Connected)
            {
                var client2 = new WebSocketClient(String.Format("ws://127.0.0.1:{0}/path", serverport)) { OnReceive = OnClientReceive };
                client2.Connect();

                if (client2.Connected)
                {
                    _client.Send("Test");
                    client2.Send("Test");
                }
                else
                {
                    _clientDataPass = false;
                }
                Thread.Sleep(5000);
            }
            else
            {
                _clientDataPass = false;
            }
            Assert.IsTrue(_clientDataPass);
        }
        [Test]
        public void WssClientSendDataConcurrent()
        {
            _forever = true;
            if (_wssclient.Connected)
            {
                var client2 = new WebSocketClient(String.Format("wss://127.0.0.1:{0}/path", wssserverport), AllowUnverifiedWssCerts: true) { OnReceive = OnClientReceive };
                client2.Connect();

                if (client2.Connected)
                {
                    _wssclient.Send("Test");
                    client2.Send("Test");
                }
                else
                {
                    _clientDataPass = false;
                }
                Thread.Sleep(5000);
            }
            else
            {
                _clientDataPass = false;
            }
            Assert.IsTrue(_clientDataPass);
        }
    }
}