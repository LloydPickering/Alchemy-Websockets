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

        [TestFixtureSetUp]
        public void SetUp()
        {
            _server = new WebSocketServer(54321, IPAddress.Loopback) { OnReceive = OnServerReceive };
            _server.Start();
            _client = new WebSocketClient("ws://127.0.0.1:54321/path") { Origin = "localhost", OnReceive = OnClientReceive };
            _client.Connect();

            System.Security.Cryptography.X509Certificates.X509Certificate cert = null;
            X509Store store = new X509Store("My");
            store.Open(OpenFlags.ReadOnly);
            if (store.Certificates.Count > 0)
            {
                cert = store.Certificates[0];
            }
            else
            {
                throw new Exception("No certificates found for WSS Testing");
            }

            //System.Security.Cryptography.X509Certificates.X509Certificate cert = System.Security.Cryptography.X509Certificates.X509Certificate.CreateFromCertFile("dummycert.cer");

            _wssserver = new WebSocketServer(54320, IPAddress.Loopback, tls:true, tlscert: cert) { OnReceive = OnServerReceive };
            _wssserver.Start();
            _wssclient = new WebSocketClient("wss://127.0.0.1:54320/path", AllowUnverifiedWssCerts:true) { Origin = "localhost", OnReceive = OnClientReceive };
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
                var client2 = new WebSocketClient("ws://127.0.0.1:54321/path") { OnReceive = OnClientReceive };
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
                var client2 = new WebSocketClient("wss://127.0.0.1:54320/path", AllowUnverifiedWssCerts:true) { OnReceive = OnClientReceive };
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