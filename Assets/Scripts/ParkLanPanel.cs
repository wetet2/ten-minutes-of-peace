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
        Task<byte> probe, send;
        CancellationTokenSource probeCancellation;
        byte[] lastFront, lastBack;
        AnimalKind lastKind;
        Guid draftId;
        string hostAddresses;
        string message = "";
        internal string Message => message;
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
                message = "공원을 열 수 없어요. 다른 창에서 열려 있는지 확인하세요.";
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
                message = "공원 화면에 표시된 IPv4 주소를 입력하세요.";
                return false;
            }
            joining = ip;
            message = "공원에 연결하는 중…";
            probeCancellation = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
            probe = ParkLan.Probe(ip, probeCancellation.Token);
            return true;
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
            park.Drawing.ConnectionStatus = "공원에 그림을 보내는 중…";
            send = ParkLan.Send(remote, draftId, front, back, stop.Token,kind);
            return true;
        }

        static byte Result(Task<byte> task)
        {
            if (task.Status == TaskStatus.RanToCompletion) return task.Result;
            _ = task.Exception; // A closed connection is displayed as a retryable failure.
            return 0;
        }

        public void Update()
        {
            host?.Pump(park);
            if (probe != null && probe.IsCompleted)
            {
                byte result = Result(probe);
                probe = null;
                probeCancellation.Dispose();
                probeCancellation = null;
                if (result == ParkLan.Accepted)
                {
                    remote = joining;
                    park.ShowDrawing();
                    park.Drawing.RemoteApply = SendDrawing;
                    park.Drawing.ConnectionStatus = "공원에 연결됐어요";
                }
                else message = "연결할 수 없어요. 주소와 공원 호스트를 확인하세요.";
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
                park.Drawing.ConnectionStatus = "공원에 보냈어요! 새 동물을 색칠해 보세요";
            }
            else if (reply == ParkLan.Full)
                park.Drawing.ConnectionStatus = "동물이 24마리에 도달했어요. 공원 앱을 다시 열어 주세요";
            else if (reply == ParkLan.Busy)
                park.Drawing.ConnectionStatus = "기다리는 친구가 많아요. 잠시 후 다시 적용해 주세요";
            else park.Drawing.ConnectionStatus = "전송을 확인하지 못했어요. 다시 적용해 주세요";
        }

        public void ClearMessage() => message = "";

        internal void CancelJoin()
        {
            CancelProbe();
            joining = null;
            ClearMessage();
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
            park.Drawing.ConnectionStatus = null;
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
