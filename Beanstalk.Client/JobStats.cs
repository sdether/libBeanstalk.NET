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
using Droog.Beanstalk.Client.Protocol;
using Droog.Beanstalk.Client.Util;

namespace Droog.Beanstalk.Client {
    public class JobStats : StatsBase {
        public JobStats(IDictionary<string, string> dictionary) : base(dictionary) { }

        public uint Id { get { return this["id"].As<uint>(); } }
        public string Tube { get { return this["tube"]; } }
        public JobState State { get { return this["state"].As<JobState>(); } }
        public TimeSpan Age { get { return TimeSpan.FromSeconds(this["age"].As<uint>()); } }
        public uint Priority { get { return this["pri"].As<uint>(); } }
        public TimeSpan TimeLeft { get { return TimeSpan.FromSeconds(this["time-left"].As<uint>()); } }
        public uint Timeouts { get { return this["timeouts"].As<uint>(); } }
        public uint Releases { get { return this["releases"].As<uint>(); } }
        public uint Buries { get { return this["buries"].As<uint>(); } }
        public uint Kicks { get { return this["kicks"].As<uint>(); } }
    }
}