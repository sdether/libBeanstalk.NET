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
using System.Text;

namespace Droog.Beanstalk.Client {
    public static class BeanstalkExtensions {

        public static PutResponse Put(this IBeanstalkClient client, string data) {
            using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(data))) {
                return client.Put(client.Defaults.Priority, client.Defaults.Delay, client.Defaults.TimeToRun, stream, stream.Length);
            }
        }

        public static PutResponse Put(this IBeanstalkClient client, string data, uint priority) {
            using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(data))) {
                return client.Put(priority, client.Defaults.Delay, client.Defaults.TimeToRun, stream, stream.Length);
            }
        }

        public static PutResponse Put(this IBeanstalkClient client, string data, uint priority, TimeSpan delay) {
            using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(data))) {
                return client.Put(priority, delay, client.Defaults.TimeToRun, stream, stream.Length);
            }
        }

        public static PutResponse Put(this IBeanstalkClient client, string data, uint priority, TimeSpan delay, TimeSpan timeToRun) {
            using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(data))) {
                return client.Put(priority, delay, timeToRun, stream, stream.Length);
            }
        }

        public static Job<string> Reserve(this IBeanstalkClient client) {
            var job = client.Reserve();
            using(var reader = new StreamReader(job.Data)) {
                return new Job<string>(job.Id, reader.ReadToEnd());
            }
        }

        public static Job<string> Reserve(this IBeanstalkClient client, TimeSpan timeout) {
            var job = client.Reserve(timeout);
            if(job == null) {
                return null;
            }
            using(var reader = new StreamReader(job.Data)) {
                return new Job<string>(job.Id, reader.ReadToEnd());
            }
        }
    }
}
