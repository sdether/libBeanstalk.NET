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
using Droog.Beanstalk.Client.Protocol;
using Droog.Beanstalk.Client.Util;

namespace Droog.Beanstalk.Client {
    public class ServerStats : StatsBase {

        public ServerStats(IDictionary<string, string> dictionary) : base(dictionary) { }

        public uint CurrentUrgentJobs { get { return this["current-jobs-urgent"].As<uint>(); } }
        public uint CurrentReadyJobs { get { return this["current-jobs-ready"].As<uint>(); } }
        public uint CurrentReservedJobs { get { return this["current-jobs-reserved"].As<uint>(); } }
        public uint CurrentDelayedJobs { get { return this["current-jobs-delayed"].As<uint>(); } }
        public uint CurrentBuriedJobs { get { return this["current-jobs-buried"].As<uint>(); } }
        public uint PutCommandCount { get { return this["cmd-put"].As<uint>(); } }
        public uint PeekCommandCount { get { return this["cmd-peek"].As<uint>(); } }
        public uint PeekReadyCommandCount { get { return this["cmd-peek-ready"].As<uint>(); } }
        public uint PeekDelayedCommandCount { get { return this["cmd-peek-delayed"].As<uint>(); } }
        public uint ReserveCommandCount { get { return this["cmd-reserve"].As<uint>(); } }
        public uint UseCommandCount { get { return this["cmd-use"].As<uint>(); } }
        public uint WatchCommandCount { get { return this["cmd-watch"].As<uint>(); } }
        public uint IgnoreCommandCount { get { return this["cmd-ignore"].As<uint>(); } }
        public uint DeleteCommandCount { get { return this["cmd-delete"].As<uint>(); } }
        public uint ReleaseCommandCount { get { return this["cmd-release"].As<uint>(); } }
        public uint BuryCommandCount { get { return this["cmd-bury"].As<uint>(); } }
        public uint KickCommandCount { get { return this["cmd-kick"].As<uint>(); } }
        public uint ServerStatsCommandCount { get { return this["cmd-stats"].As<uint>(); } }
        public uint JobStatsCommandCount { get { return this["cmd-stats-job"].As<uint>(); } }
        public uint TubeStatsCommandCount { get { return this["cmd-stats-tube"].As<uint>(); } }
        public uint ListTubesCommandCount { get { return this["cmd-list-tubes"].As<uint>(); } }
        public uint ListUsedTubesCommandCount { get { return this["cmd-list-tube-used"].As<uint>(); } }
        public uint ListWatchedTubesCommandCount { get { return this["cmd-list-tubes-watched"].As<uint>(); } }
        public uint JobTimeouts { get { return this["job-timeouts"].As<uint>(); } }
        public uint TotalJobs { get { return this["total-jobs"].As<uint>(); } }
        public uint MaxJobSize { get { return this["max-job-size"].As<uint>(); } }
        public uint CurrentTubeCount { get { return this["current-tubes"].As<uint>(); } }
        public uint CurrentConnectionCount { get { return this["current-connections"].As<uint>(); } }
        public uint CurrentProducerCount { get { return this["current-producers"].As<uint>(); } }
        public uint CurrentWorkerCount { get { return this["current-workers"].As<uint>(); } }
        public uint CurrentWaitingClientCount { get { return this["current-waiting"].As<uint>(); } }
        public uint TotalConnections { get { return this["total-connections"].As<uint>(); } }
        public uint Pid { get { return this["pid"].As<uint>(); } }
        public string Version { get { return this["version"]; } }
        public TimeSpan UserUsagetime { get { return TimeSpan.FromSeconds(this["rusage-utime"].As<uint>()); } }
        public TimeSpan SystemUsagetime { get { return TimeSpan.FromSeconds(this["rusage-stime"].As<uint>()); } }
        public TimeSpan Uptime { get { return TimeSpan.FromSeconds(this["uptime"].As<uint>()); } }
        public uint BinlogOldestIndex { get { return this["binlog-oldest-index"].As<uint>(); ; } }
        public uint BinlogCurrentIndex { get { return this["binlog-current-index"].As<uint>(); ; } }
        public uint BinlogMaxSize { get { return this["binlog-max-size"].As<uint>(); } }
    }
}