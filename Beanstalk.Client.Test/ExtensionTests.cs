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

using System.IO;
using System.Text;
using Droog.Beanstalk.Client.Protocol;
using Moq;
using NUnit.Framework;

namespace Droog.Beanstalk.Client.Test {

    [TestFixture]
    public class ExtensionTests {

        [Test]
        public void Can_send_simple_request() {
            var buffer = new byte[1024];
            var request = Request.Create(RequestCommand.Use);
            var socket = new Mock<ISocket>();
            socket.Setup(x => x.Send(It.Is<byte[]>(b => "use\r\n" == Encoding.ASCII.GetString(b, 0, 5)), 0, 5)).Returns(5).AtMostOnce().Verifiable();
            socket.Object.SendRequest(request, buffer);
            socket.VerifyAll();
        }

        [Test]
        public void Can_send_request_with_arg() {
            var buffer = new byte[1024];
            var request = Request.Create(RequestCommand.Use).AppendArgument(123);
            var socket = new Mock<ISocket>();
            socket.Setup(x => x.Send(It.Is<byte[]>(b => "use 123\r\n" == Encoding.ASCII.GetString(b, 0, 9)), 0, 9)).Returns(9).AtMostOnce().Verifiable();
            socket.Object.SendRequest(request, buffer);
            socket.VerifyAll();
        }

        [Test]
        public void Can_send_request_with_data() {
            var buffer = new byte[1024];
            var capture = new MemoryStream();
            var d = "foo".AsStream();
            var request = Request.Create(RequestCommand.Use).WithData(d, d.Length);
            var socket = new Mock<ISocket>();
            var count = 0;
            socket.Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback<byte[], int, int>((b, o, s) => {
                    capture.Write(b, o, s);
                    count = s;
                })
                .Returns(() => count)
                .AtMostOnce()
                .Verifiable();
            socket.Object.SendRequest(request, buffer);
            socket.VerifyAll();
            capture.Position = 0;
            Assert.AreEqual("use 3\r\nfoo\r\n", new StreamReader(capture).ReadToEnd());
        }

        [Test]
        public void Can_send_request_with_arguments_and_data() {
            var buffer = new byte[1024];
            var capture = new MemoryStream();
            var d = "foo".AsStream();
            var request = Request.Create(RequestCommand.Put)
                .AppendArgument(100)
                .AppendArgument(0)
                .AppendArgument(120)
                .WithData(d, d.Length);
            var socket = new Mock<ISocket>();
            var count = 0;
            socket.Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback<byte[], int, int>((b, o, s) => {
                    capture.Write(b, o, s);
                    count = s;
                })
                .Returns(() => count)
                .AtMostOnce()
                .Verifiable();
            socket.Object.SendRequest(request, buffer);
            socket.VerifyAll();
            capture.Position = 0;
            Assert.AreEqual("put 100 0 120 3\r\nfoo\r\n", new StreamReader(capture).ReadToEnd());
        }

        [Test]
        public void Can_receive_simple_response() {
            var buffer = new byte[1024];
            var socket = new Mock<ISocket>();
            var responseStream = "NOT_FOUND\r\n".AsStream();
            socket.Setup(x => x.Receive(buffer, 0, buffer.Length))
                .Returns((int)responseStream.Length)
                .Callback<byte[], int, int>((b, o, s) => responseStream.Read(b, o, s))
                .AtMostOnce()
                .Verifiable();
            var response = socket.Object.ReceiveResponse(buffer);
            socket.VerifyAll();
            Assert.AreEqual(ResponseStatus.NotFound, response.Status);
            Assert.AreEqual(0, response.Arguments.Length);
            Assert.IsNull(response.Data);
            Assert.AreEqual(0, response.DataLength);
        }

        [Test]
        public void Can_receive_response_with_one_arg() {
            var buffer = new byte[1024];
            var socket = new Mock<ISocket>();
            var responseStream = "USING foo\r\n".AsStream();
            socket.Setup(x => x.Receive(buffer, 0, buffer.Length))
                .Returns((int)responseStream.Length)
                .Callback<byte[], int, int>((b, o, s) => responseStream.Read(b, o, s))
                .AtMostOnce()
                .Verifiable();
            var response = socket.Object.ReceiveResponse(buffer);
            socket.VerifyAll();
            Assert.AreEqual(ResponseStatus.Using, response.Status);
            Assert.AreEqual(1, response.Arguments.Length);
            Assert.AreEqual("foo", response.Arguments[0]);
            Assert.IsNull(response.Data);
            Assert.AreEqual(0, response.DataLength);
        }

        [Test]
        public void Can_receive_response_with_data() {
            var buffer = new byte[1024];
            var socket = new Mock<ISocket>();
            var responseStream = "RESERVED 123 3\r\nfoo\r\n".AsStream();
            socket.Setup(x => x.Receive(buffer, 0, buffer.Length))
                .Returns((int)responseStream.Length)
                .Callback<byte[], int, int>((b, o, s) => responseStream.Read(b, o, s))
                .AtMostOnce()
                .Verifiable();
            var response = socket.Object.ReceiveResponse(buffer);
            socket.VerifyAll();
            Assert.AreEqual(ResponseStatus.Reserved, response.Status);
            Assert.AreEqual(2, response.Arguments.Length);
            Assert.AreEqual(new[] { "123", "3" }, response.Arguments);
            Assert.AreEqual(3, response.DataLength);
            Assert.AreEqual("foo", new StreamReader(response.Data).ReadToEnd());
        }

        [Test]
        public void Can_receive_response_with_data_from_fragmented_reads_1() {
            var buffer = new byte[1024];
            var socket = new Mock<ISocket>();
            var responseStream = "RESERVED 123 3\r\nfoo\r\n".AsStream();
            var count = 16;
            var offset = 0;
            socket.Setup(x => x.Receive(buffer, 0, buffer.Length))
                .Returns(() => count)
                .Callback<byte[], int, int>((b, o, s) => {
                    responseStream.Read(b, 0, count);
                    offset = count;
                    count = 5;
                })
                .AtMost(2)
                .Verifiable();
            var response = socket.Object.ReceiveResponse(buffer);
            socket.VerifyAll();
            Assert.AreEqual(ResponseStatus.Reserved, response.Status);
            Assert.AreEqual(2, response.Arguments.Length);
            Assert.AreEqual(new[] { "123", "3" }, response.Arguments);
            Assert.AreEqual(3, response.DataLength);
            Assert.AreEqual("foo", new StreamReader(response.Data).ReadToEnd());
        }

        [Test]
        public void Can_receive_response_with_data_from_fragmented_reads_2() {
            var buffer = new byte[1024];
            var socket = new Mock<ISocket>();
            var responseStream = "RESERVED 123 3\r\nfoo\r\n".AsStream();
            var count = 19;
            var offset = 0;
            socket.Setup(x => x.Receive(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(() => count)
                .Callback<byte[], int, int>((b, o, s) => {
                    responseStream.Read(b, 0, count);
                    offset = count;
                    count = 2;
                })
                .AtMost(2)
                .Verifiable();
            var response = socket.Object.ReceiveResponse(buffer);
            socket.VerifyAll();
            Assert.AreEqual(ResponseStatus.Reserved, response.Status);
            Assert.AreEqual(2, response.Arguments.Length);
            Assert.AreEqual(new[] { "123", "3" }, response.Arguments);
            Assert.AreEqual(3, response.DataLength);
            Assert.AreEqual("foo", new StreamReader(response.Data).ReadToEnd());
        }
    }
}
