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
using Droog.Beanstalk.Client.Test;
using NUnit.Framework;

namespace Droog.Beanstalk.Client.IntegrationTest {
    [TestFixture]
    public class BeanstalkClientTests {

        [Test]
        public void PutReserveDelete() {
            using(var client = new BeanstalkClient(TestConfig.Host, TestConfig.Port)) {
                var data = "abc";
                var stream = data.AsStream();
                var put = client.Put(100, TimeSpan.Zero, TimeSpan.FromMinutes(2), stream, data.Length);
                var reserve = client.Reserve();
                Assert.AreEqual(put.JobId, reserve.JobId);
                Assert.IsTrue(client.Delete(reserve.JobId));
                using(var reader = new StreamReader(reserve.Data)) {
                    Assert.AreEqual(data, reader.ReadToEnd());
                }
            }
        }
    }
}