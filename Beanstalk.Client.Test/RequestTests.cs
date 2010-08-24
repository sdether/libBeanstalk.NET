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

using Droog.Beanstalk.Client.Protocol;
using NUnit.Framework;

namespace Droog.Beanstalk.Client.Test {
    [TestFixture]
    public class RequestTests {

        [Test]
        public void Can_render_simple_command() {
            var request = Request.Create(RequestCommand.Use);
            var data = request.GetData();
            Assert.AreEqual(1, data.Length);
            Assert.IsTrue(data[0].HasData);
            Assert.AreEqual("use\r\n", data[0].AsText());
        }

        [Test]
        public void Can_render_command_with_uint_arg() {
            var request = Request.Create(RequestCommand.Use).AppendArgument(123);
            var data = request.GetData();
            Assert.AreEqual(1, data.Length);
            Assert.AreEqual(1, data.Length);
            Assert.IsTrue(data[0].HasData);
            Assert.AreEqual("use 123\r\n", data[0].AsText());
        }

        [Test]
        public void Can_render_command_with_string_arg() {
            var request = Request.Create(RequestCommand.Use).AppendArgument("123");
            var data = request.GetData();
            Assert.AreEqual(1, data.Length);
            Assert.IsTrue(data[0].HasData);
            Assert.AreEqual("use 123\r\n", data[0].AsText());
        }

        [Test]
        public void Can_render_command_with_string_and_uint_arg() {
            var request = Request.Create(RequestCommand.Use).AppendArgument("123").AppendArgument(456);
            var data = request.GetData();
            Assert.AreEqual(1, data.Length);
            Assert.IsTrue(data[0].HasData);
            Assert.AreEqual("use 123 456\r\n", data[0].AsText());
        }

        [Test]
        public void Can_render_command_with_data_as_three_request_data_chunks() {
            var d = "foo".AsStream();
            var request = Request.Create(RequestCommand.Use).WithData(d, d.Length);
            var data = request.GetData();
            Assert.AreEqual(3, data.Length);
            Assert.IsTrue(data[0].HasData);
            Assert.AreEqual("use 3\r\n", data[0].AsText());
            Assert.IsTrue(data[1].HasData);
            Assert.AreEqual("foo", data[1].AsText());
            Assert.IsTrue(data[2].HasData);
            Assert.AreEqual("\r\n", data[2].AsText());
        }
    }
}
