/*
 * libBeanstalk.NET 
 * Copyright (C) 2011 Arne F. Claassen
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
using System.Linq;
using System.Text;
using Droog.Beanstalk.Client.Net;
using Droog.Beanstalk.Client.Net.Helper;
using NUnit.Framework;

namespace Droog.Beanstalk.Client.Test {

    [TestFixture]
    public class PoolSocketTests {

        [Test]
        public void Dispose_calls_reclaim_callback_with_encapsulated_socket() {
            var mock = new MockSocket();
            ISocket reclaimed = null;
            var socket = new PoolSocket(mock, (s) => reclaimed = s);
            socket.Dispose();
            Assert.AreSame(mock, reclaimed);
        }

        [Test]
        public void Dispose_does_not_dispose_of_encapsulated_socket() {
            var mock = new MockSocket();
            ISocket reclaimed = null;
            var socket = new PoolSocket(mock, (s) => reclaimed = s);
            socket.Dispose();
            Assert.AreEqual(0, mock.DisposeCalled);
        }
    }
}
