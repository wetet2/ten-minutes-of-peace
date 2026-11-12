using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PeacePark
{
    internal sealed class ParkLanPanel : IDisposable
    {
        readonly ParkPlayground park;
        readonly DrawingSkin skin = new DrawingSkin();
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        ParkLan.Host host;
        IPAddress remote;
        IPAddress joining;
        Task<byte> probe, send;
        byte[] lastFront, lastBack;
        AnimalKind lastKind;
        Guid draftId;
        string address = "127.0.0.1";
        string hostAddresses;
        string message = "";
        GUIStyle title, label, button, input;
        public bool IsOpen { get; private set; }
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
                message = "공원을 열었어요. 색칠 기기에서 위 주소로 연결하세요.";
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
                    .Distinct();
                return string.Join(" · ", addresses.Prepend("127.0.0.1"));
            }
            catch (NetworkInformationException) { return "127.0.0.1"; }
        }

        internal bool BeginJoin(string value)
        {
            if (probe != null || host != null || remote != null) return false;
            value = value.Trim();
            if (string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase)) value = "127.0.0.1";
            if (!IPAddress.TryParse(value, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            {
                message = "공원 화면에 표시된 IPv4 주소를 입력하세요.";
                return false;
            }
            joining = ip;
            message = "공원에 연결하는 중…";
            probe = ParkLan.Probe(ip, stop.Token);
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
                if (result == ParkLan.Accepted)
                {
                    remote = joining;
                    IsOpen = false;
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

        public void Disconnect()
        {
            remote = null;
            lastFront = lastBack = null;
            park.Drawing.RemoteApply = null;
            park.Drawing.ConnectionStatus = null;
        }

        public void Draw(float width)
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { font = ParkFonts.Bold, fontSize = 29, alignment = TextAnchor.MiddleCenter };
                title.normal.textColor = new Color32(80, 62, 47, 255);
                label = new GUIStyle(title) { font = ParkFonts.Regular, fontSize = 21, wordWrap = true };
                button = new GUIStyle(title) { fontSize = 22 };
                input = new GUIStyle(GUI.skin.textField) { font = ParkFonts.Regular, fontSize = 25, alignment = TextAnchor.MiddleCenter };
            }
            if (!IsOpen)
            {
                var open = new Rect(width - 415, 18, 158, 60);
                if (Button(open, host == null ? "기기 연결" : "공원 열림", false))
                {
                    IsOpen = true;
                    park.OrbitCamera.InputEnabled = false;
                }
                return;
            }

            float left = (width - 620) / 2;
            skin.Draw(new Rect(left, 240, 620, 404), new Color32(255, 247, 227, 255), 32);
            GUI.Label(new Rect(left + 20, 257, 580, 48), "같은 Wi-Fi에서 함께 색칠하기", title);
            if (host != null)
            {
                GUI.Label(new Rect(left + 28, 310, 564, 92), "공원 주소\n" + hostAddresses, label);
                if (Button(new Rect(left + 140, 423, 340, 56), "공원 호스트 끄기", false))
                {
                    host.Dispose();
                    host = null;
                    message = "";
                }
            }
            else
            {
                GUI.enabled = probe == null;
                if (Button(new Rect(left + 40, 316, 540, 57), "이 기기에서 공원 열기", true)) StartHost();
                GUI.Label(new Rect(left + 35, 388, 550, 32), "색칠 기기에서는 공원 IP 주소를 입력하세요", label);
                address = GUI.TextField(new Rect(left + 42, 428, 326, 52), address, 45, input);
                if (Button(new Rect(left + 383, 428, 195, 52), "연결하고 색칠", true)) BeginJoin(address);
                GUI.enabled = true;
            }
            GUI.Label(new Rect(left + 27, 491, 566, 64), message, label);
            GUI.enabled = probe == null;
            if (Button(new Rect(left + 215, 566, 190, 55), "닫기", false))
            {
                IsOpen = false;
                park.OrbitCamera.InputEnabled = true;
            }
            GUI.enabled = true;
        }

        bool Button(Rect rect, string text, bool accent)
        {
            skin.Draw(rect, accent ? new Color32(165, 217, 112, 255) : new Color32(245, 232, 199, 255), 23, accent);
            GUI.Label(rect, text, button);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        public void Dispose()
        {
            host?.Dispose();
            stop.Cancel();
            stop.Dispose();
            skin.Dispose();
        }
    }
}
