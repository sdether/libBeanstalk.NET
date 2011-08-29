using System;
using Droog.Beanstalk.Client.Net;

namespace Droog.Beanstalk.Client.Test {
    public class FakeDisposableSocket : ISocket {
        public bool Disposed;
        public void Dispose() { Disposed = true; }
        public bool Connected { get { return true; } }
        public int Send(byte[] buffer, int offset, int size) { throw new NotImplementedException(); }
        public int Receive(byte[] buffer, int offset, int size) { throw new NotImplementedException(); }
    }
}