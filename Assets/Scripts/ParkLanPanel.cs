using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PeacePark
{
    internal sealed class ParkLanPanel : IDisposable
    {
        readonly ParkPlayground park;
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        ParkLan.Host host;
        IPAddress remote;
        IPAddress joining;
        Task<byte> send;
        Task<IPAddress> probe;
        CancellationTokenSource probeCancellation;
        byte[] lastFront, lastBack;
        AnimalKind lastKind;
        Guid draftId;
        string hostAddresses;
        internal string HostAddresses => hostAddresses;
        internal bool IsJoining => probe != null;
        public bool IsHosting => host != null;
        public bool IsJoined => remote != null;
        public bool LastSendAccepted { get; private set; }

        public ParkLanPanel(ParkPlayground park) => this.park = park;

        internal bool StartHost()
        {
            if (host != null || remote != null) return false;
            try
            {
                host = new ParkLan.Host();
                hostAddresses = GetAddresses();
                return true;
            }
            catch (SocketException)
            {
                park.Toast.Show("놀이 공간을 열 수 없어요. 다른 창을 확인해 주세요.");
                return false;
            }
        }

        static string GetAddresses()
        {
            try
            {
                var addresses = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
                    .Select(entry => entry.Address)
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    .Select(ip => ip.ToString())
                    .Distinct()
                    .ToArray();
                return addresses.Length == 0 ? "같은 PC  127.0.0.1"
                    : "다른 기기  " + string.Join(" · ", addresses) + "\n같은 PC  127.0.0.1";
            }
            catch (NetworkInformationException) { return "같은 PC  127.0.0.1"; }
        }

        internal bool BeginJoin(string value)
        {
            if (!park.IsConnecting || probe != null || host != null || remote != null) return false;
            value = value.Trim();
            if (string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase)) value = "127.0.0.1";
            if (!IPAddress.TryParse(value, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            {
                park.Toast.Show("호스트 화면에 표시된 IPv4 주소를 입력하세요.");
                return false;
            }
            joining = ip;
            StartProbe(ip);
            return true;
        }

        internal bool BeginDiscovery()
        {
            if(!park.IsConnecting||IsJoining||host!=null||remote!=null)return false;
            StartProbe(null);
            return true;
        }

        void StartProbe(IPAddress address)
        {
            joining=address;
            park.Toast.Show(address==null?"같은 Wi-Fi에서 놀이 공간을 찾는 중…":"놀이 공간에 연결하는 중…");
            probeCancellation=CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
            probe=FindAndConnect(address,probeCancellation.Token);
        }

        static async Task<IPAddress> FindAndConnect(IPAddress address,CancellationToken token)
        {
            address??=await ParkLanDiscovery.Find(token).ConfigureAwait(false);
            if(address==null)return null;
            return await ParkLan.Probe(address,token).ConfigureAwait(false)==ParkLan.Accepted?address:null;
        }

        bool SendDrawing()
        {
            if (remote == null || send != null) return false;
            var front = park.Drawing.Front.Paint.GetRawTextureData();
            var back = park.Drawing.Back.Paint.GetRawTextureData();
            // A retry of the same artwork uses the same ID if the reply was lost.
            var kind=(AnimalKind)park.Drawing.SelectedAnimal;
            if (lastFront == null || kind!=lastKind || !front.SequenceEqual(lastFront) || !back.SequenceEqual(lastBack))
                draftId = Guid.NewGuid();
            lastKind=kind;
            lastFront = front;
            lastBack = back;
            LastSendAccepted = false;
            park.Drawing.Sending = true;
            park.Toast.Show("놀이 공간에 그림을 보내는 중…");
            send = ParkLan.Send(remote, draftId, front, back, stop.Token,kind);
            return true;
        }

        static T Result<T>(Task<T> task)
        {
            if (task.Status == TaskStatus.RanToCompletion) return task.Result;
            _ = task.Exception; // A closed connection is displayed as a retryable failure.
            return default;
        }

        public void Update()
        {
            host?.Pump(park);
            if (probe != null && probe.IsCompleted)
            {
                var found=Result(probe);
                probe = null;
                probeCancellation.Dispose();
                probeCancellation = null;
                if (found!=null)
                {
                    remote = found;
                    park.ShowDrawing();
                    park.Drawing.RemoteApply = SendDrawing;
                    park.Toast.Show("놀이 공간에 연결됐어요");
                }
                else park.Toast.Show(joining==null?"놀이 공간을 찾지 못했어요. IP로도 연결할 수 있어요."
                    :"연결할 수 없어요. 주소와 호스트를 확인하세요.");
            }
            if (send == null || !send.IsCompleted) return;
            byte reply = Result(send);
            send = null;
            park.Drawing.Sending = false;
            LastSendAccepted = reply == ParkLan.Accepted;
            if (LastSendAccepted)
            {
                lastFront = lastBack = null;
                park.Drawing.SelectAnimal(park.Drawing.SelectedAnimal);
                park.Toast.Show("놀이 공간에 보냈어요! 새 동물을 색칠해 보세요");
            }
            else if (reply == ParkLan.Full)
                park.Toast.Show("이 호스트의 놀이 공간이 가득 찼어요");
            else if (reply == ParkLan.Busy)
                park.Toast.Show("기다리는 친구가 많아요. 잠시 후 다시 적용해 주세요");
            else park.Toast.Show("전송을 확인하지 못했어요. 다시 적용해 주세요");
        }

        internal void CancelJoin()
        {
            CancelProbe();
            joining = null;
            park.Toast.Clear();
        }

        public void ResetSession()
        {
            CancelJoin();
            host?.Dispose();
            host = null;
            hostAddresses = null;
            remote = null;
            lastFront = lastBack = null;
            LastSendAccepted = false;
            park.Drawing.RemoteApply = null;
        }

        void CancelProbe()
        {
            if (probe == null) return;
            _ = probe.ContinueWith(task => { _ = task.Exception; },
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            probe = null;
            probeCancellation.Cancel();
            probeCancellation.Dispose();
            probeCancellation = null;
        }

        public void Dispose()
        {
            CancelProbe();
            host?.Dispose();
            stop.Cancel();
            stop.Dispose();
        }
    }
}
