using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeacePark
{
    internal static class ParkLanDiscovery
    {
        static readonly byte[] Signature=Encoding.ASCII.GetBytes("PeacePark/1");
        const byte Query=1,Reply=2;
        static int CommandOffset=>Signature.Length;

        static bool Matches(byte[] packet,byte command)
        {
            if(packet.Length!=Signature.Length+17||packet[CommandOffset]!=command)return false;
            for(int i=0;i<Signature.Length;i++)
                if(packet[i]!=Signature[i])return false;
            return true;
        }

        internal static UdpClient StartResponder()
        {
            try
            {
                var server=new UdpClient(new IPEndPoint(IPAddress.Any,ParkLan.Port));
                _=Respond(server);
                return server;
            }
            catch(SocketException){return null;}
        }

        static async Task Respond(UdpClient server)
        {
            try
            {
                while(true)
                {
                    var request=await server.ReceiveAsync().ConfigureAwait(false);
                    if(!Matches(request.Buffer,Query))continue;
                    request.Buffer[CommandOffset]=Reply;
                    await server.SendAsync(request.Buffer,request.Buffer.Length,request.RemoteEndPoint).ConfigureAwait(false);
                }
            }
            catch(SocketException){}
            catch(ObjectDisposedException){}
        }

        internal static Task<IPAddress> Find(CancellationToken token)=>Task.Run(async ()=>
        {
            using var client=new UdpClient(AddressFamily.InterNetwork);
            client.EnableBroadcast=true;
            client.Client.Bind(new IPEndPoint(IPAddress.Any,0));
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(3));
            using var close=deadline.Token.Register(client.Dispose);
            var query=new byte[Signature.Length+17];
            Buffer.BlockCopy(Signature,0,query,0,Signature.Length);
            query[CommandOffset]=Query;
            Buffer.BlockCopy(Guid.NewGuid().ToByteArray(),0,query,CommandOffset+1,16);
            try
            {
                foreach(var target in BroadcastAddresses())
                {
                    try
                    {
                        await client.SendAsync(query,query.Length,new IPEndPoint(target,ParkLan.Port)).ConfigureAwait(false);
                    }
                    catch(SocketException){}
                }
                while(!deadline.IsCancellationRequested)
                {
                    var response=await client.ReceiveAsync().ConfigureAwait(false);
                    if(response.RemoteEndPoint.Port!=ParkLan.Port||!Matches(response.Buffer,Reply))continue;
                    bool sameQuery=true;
                    for(int i=CommandOffset+1;i<query.Length;i++)sameQuery &= query[i]==response.Buffer[i];
                    if(sameQuery)return response.RemoteEndPoint.Address;
                }
            }
            catch(ObjectDisposedException){}
            catch(SocketException) when(deadline.IsCancellationRequested){}
            return null;
        },token);

        static HashSet<IPAddress> BroadcastAddresses()
        {
            var targets=new HashSet<IPAddress>{IPAddress.Broadcast,IPAddress.Loopback};
            try
            {
                foreach(var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if(adapter.OperationalStatus!=OperationalStatus.Up||adapter.NetworkInterfaceType==NetworkInterfaceType.Loopback)continue;
                    foreach(var entry in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if(entry.Address.AddressFamily!=AddressFamily.InterNetwork||entry.IPv4Mask==null)continue;
                        var address=entry.Address.GetAddressBytes();
                        var mask=entry.IPv4Mask.GetAddressBytes();
                        if(mask.Length!=address.Length)continue;
                        for(int i=0;i<address.Length;i++)address[i]|=(byte)~mask[i];
                        var broadcast=new IPAddress(address);
                        if(!broadcast.Equals(entry.Address))targets.Add(broadcast);
                    }
                }
            }
            catch(NetworkInformationException){}
            catch(PlatformNotSupportedException){}
            return targets;
        }
    }
}
