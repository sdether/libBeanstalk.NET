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
using System.IO;
using Droog.Beanstalk.Client.Protocol;
using NUnit.Framework;

namespace Droog.Beanstalk.Client.Test {
    public class MockSocket : ISocket {
        public int CloseCalled;
        private MemoryStream _sentData = new MemoryStream();
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
            _sentData = new MemoryStream();
        }

        public void Verify() {
            Assert.AreEqual(_sent, _sentData.AsText());
        }
    }
}