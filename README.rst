libBeanstalk.NET 0.5
==========
A .NET driver for Beanstalkd.

License
=======
Apache 2.0

Status
======
Currently supports the full 1.3 beanstalkd protocol.

Usage
====

String data payload
::
  // connect to beanstalkd
  using(var client = new BeanstalkClient(host, port)) {

    // put some data
    var put = client.Put("foo");
  
    // reserve data from queue
    var reserve = client.Reserve();
    Console.Writeline("data: {0}", reserve.Data);
    
    // delete reserved data from queue
    client.Delete(reserve.JobId);
  }

Binary payload
::

  // connect to beanstalkd
  using(var client = new BeanstalkClient(host, port)) {

    // put some data
    var put = client.Put(100, 0, 120, data, data.Length);
  
    // reserve data from queue
    var reserve = client.Reserve();
    
    // delete reserved data from queue
    client.Delete(reserve.JobId);
  }

Patches
=======
Patches are welcome and will likely be accepted.  By submitting a patch you assign the copyright to me, Arne F. Claassen.  This is necessary to simplify the number of copyright holders should it become necessary that the copyright be re-assigned or the code re-licensed.  The code will always be available under an OSI approved license.

Roadmap
=======
- Add more tests
- Add connection pooling
- Add POCO producers and consumers with configurable serializers

Contributors
============
- Arne F. Claassen (sdether)


