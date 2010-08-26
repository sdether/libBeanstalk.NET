using System;
using System.IO;
using Droog.Beanstalk.Client.Protocol;
using NUnit.Framework;

namespace Droog.Beanstalk.Client.Test {
    public class MockSocket : ISocket {
        public int CloseCalled;
        private readonly MemoryStream _sentData = new MemoryStream();
        private MemoryStream _receivedData = new MemoryStream();
        private string _sent;

        public void Close() {
            CloseCalled++;
            Connected = false;
        }

        public bool Connected { get; set; }

        public int Send(byte[] buffer, int offset, int size) {
            _sentData.Write(buffer, offset, size);
            return size;
        }

        public int Receive(byte[] buffer, int offset, int size) {
            return _receivedData.Read(buffer, offset, size);
        }

        public void Expect(string sent, string received) {
            _receivedData = received.AsStream();
            _sent = sent;
        }

        public void Verify() {
            Assert.AreEqual(_sent, _sentData.AsText());
        }
    }
}