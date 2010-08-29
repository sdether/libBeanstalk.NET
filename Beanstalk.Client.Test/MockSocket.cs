/*
 * libBeanstalk.NET 
 * Copyright (C) 2010 Arne F. Claassen
 * geekblog [at] claassen [dot] net
 * http://github.com/sdether/libBeanstalk.NET 
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.IO;
using Droog.Beanstalk.Client.Net;
using NUnit.Framework;

namespace Droog.Beanstalk.Client.Test {
    public class MockSocket : ISocket {
        public int CloseCalled;
        private Queue<MemoryStream> _sentData = new Queue<MemoryStream>();
        private Queue<MemoryStream> _receivedData = new Queue<MemoryStream>();
        private Queue<string> _sent = new Queue<string>();

        public MockSocket() {
            Connected = true;
        }

        public void Dispose() {
            CloseCalled++;
            Connected = false;
        }

        public bool Connected { get; set; }

        public int Send(byte[] buffer, int offset, int size) {
            var data = new MemoryStream();
            data.Write(buffer, offset, size);
            _sentData.Enqueue(data);
            return size;
        }

        public int Receive(byte[] buffer, int offset, int size) {
            var data = _receivedData.Dequeue();
            return data.Read(buffer, offset, size);
        }

        public void Expect(string sent, string received) {
            _receivedData.Enqueue(received.AsStream());
            _sent.Enqueue(sent);
        }

        public void Verify() {
            Assert.AreEqual(_sent.Count, _sentData.Count);
            while(_sent.Count > 0 ) {
                var sent = _sent.Dequeue();
                var data = _sentData.Dequeue();
                Assert.AreEqual(sent, data.AsText());
            }
        }
    }
}