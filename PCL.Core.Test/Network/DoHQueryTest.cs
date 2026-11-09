using System;
using System.Linq;
using System.Threading.Tasks;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.IO.Net.Dns;

namespace PCL.Core.Test.Network;

[TestClass]
public class DoHQueryTest
{
    [TestMethod]
    public async Task TestIpQuery()
    {
        var query = DnsQuery.Instance;
        var addr = await query.QueryForIpAsync("cloudflare.com", TestContext.CancellationTokenSource.Token);
        Assert.IsNotNull(addr);
        Assert.IsGreaterThan(0, addr.Length);
        Console.WriteLine(string.Join(", ", addr.Select(x => x.ToString())));
    }

    [TestMethod]
    public async Task TestSrvQuery()
    {
        var query = DnsQuery.Instance;
        var addr = await query.QueryAsync("_xmpp-server._tcp.jabber.org", DnsQueryType.SRV, TestContext.CancellationTokenSource.Token);
        Assert.IsNotNull(addr);
        Assert.IsGreaterThan(0, addr.Answers.Count);
        Assert.AreEqual(DnsQueryClass.IN, addr.Header.QueryClass);
        var record = addr.Answers.FirstOrDefault(answer => answer.Type == DnsQueryType.SRV)?.Resource as DnsUnknownResource;
        Assert.IsNotNull(record);
        var srvRecord = new DnsSrvResource();
        var offset = 0;
        srvRecord.ReadBytes(record.Raw, ref offset, record.Raw.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(srvRecord.Target));
        Assert.IsGreaterThan(0, srvRecord.Port);
        Console.WriteLine(srvRecord.Target);
        Console.WriteLine(srvRecord.Port);
    }

    public TestContext TestContext { get; set; }
}
