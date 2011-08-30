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

using System.Collections.Generic;
using Droog.Beanstalk.Client.Protocol;
using Droog.Beanstalk.Client.Util;

namespace Droog.Beanstalk.Client {
    public class TubeStats : StatsBase {
        public TubeStats(IDictionary<string, string> dictionary) : base(dictionary) { }

        public string Name { get { return this["name"]; } }
        public uint CurrentUrgentJobs { get { return this["current-jobs-urgent"].As<uint>(); } }
        public uint CurrentReadyJobs { get { return this["current-jobs-ready"].As<uint>(); } }
        public uint CurrentReservedJobs { get { return this["current-jobs-reserved"].As<uint>(); } }
        public uint CurrentDelayedJobs { get { return this["current-jobs-delayed"].As<uint>(); } }
        public uint CurrentBuriedJobs { get { return this["current-jobs-buried"].As<uint>(); } }
        public uint TotalJobs { get { return this["total-jobs"].As<uint>(); } }
        public uint CurrentWaiting { get { return this["current-waiting"].As<uint>(); } }
    }
}