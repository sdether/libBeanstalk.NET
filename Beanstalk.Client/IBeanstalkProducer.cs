using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Droog.Beanstalk.Client {
    public interface IBeanstalkProducer<T> {
        PutResponse Put(uint priority, TimeSpan delay, TimeSpan timeToRun, T data);
    }

    public interface IBeanstalkConsumer<T> {
        Work<T> Reserve();
        Work<T> Reserve(TimeSpan timeout);
    }

    public class Work<T> : IDisposable{
        private readonly IBeanstalkClient _client;
        private readonly T _data;
        private readonly uint _jobId;

        public Work(IBeanstalkClient client, T data, uint jobId) {
            _client = client;
            _data = data;
            _jobId = jobId;
        }

        public uint Id { get { return _jobId; } }
        public T Data { get { return _data; } }
        public uint Priority { get; set; }
        public WorkStatus Status { get; set; }
        public void Delete();
        public void Release();
        public void Release(uint priority, TimeSpan delay);
        public void Bury();
        public void Bury(uint priority);
        public void Touch();

        public void Dispose() {
            throw new NotImplementedException();
        }
    }

    public enum WorkStatus {
        Active,
        Deleted,
        Released,
    }
}
