using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PeacePark
{
    // Run the two test roles in separate Windows players.
    internal sealed class ParkLanSmoke : MonoBehaviour
    {
        bool host;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            bool host = args.Contains("-peaceParkLanTestHost");
            if (!host && !args.Contains("-peaceParkLanTestClient")) return;
            Application.runInBackground = true;
            new GameObject("LAN validation").AddComponent<ParkLanSmoke>().host = host;
        }

        void OnEnable() => Application.logMessageReceived += FailOnError;
        void OnDisable() => Application.logMessageReceived -= FailOnError;
        static void FailOnError(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception) Application.Quit(1);
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        static IEnumerator WaitFor(Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + 30;
            while (!condition())
            {
                Require(Time.realtimeSinceStartup < deadline, "LAN validation timed out.");
                yield return null;
            }
        }

        IEnumerator Start()
        {
            var park = FindAnyObjectByType<ParkPlayground>();
            yield return WaitFor(() => park && park.Lan != null);
            if (host) yield return RunHost(park);
            else yield return RunClient(park);
            Application.Quit(0);
        }

        static IEnumerator RunHost(ParkPlayground park)
        {
            Require(park.Lan.StartHost(), "Host failed to listen.");
            Debug.Log("LAN TEST HOST READY");
            yield return WaitFor(() => park.SubmissionCount >= AnimalKinds.Count);
            yield return new WaitForSecondsRealtime(1);
            Require(park.SubmissionCount == AnimalKinds.Count, "Duplicate submission spawned an extra animal.");
            park.Advance(45);
            Require(park.Animals.Count == AnimalKinds.Count && park.PendingCount == 0, "Remote submissions did not spawn.");
            var received = new System.Collections.Generic.HashSet<AnimalKind>();
            foreach (var rabbit in park.Animals)
            {
                Require(rabbit.State == ParkRabbitState.Wandering && rabbit.WaveCount == 1 && rabbit.ExitedThroughOpenDoor,
                    "Network rabbit did not exit, wave and wander.");
                var material = rabbit.Character.Skin.sharedMaterials[0];
                var front = (Texture2D)material.GetTexture("_FrontTex");
                var back = (Texture2D)material.GetTexture("_BackTex");
                int frontRed = ((Color32)front.GetPixel(AnimalCanvas.Width / 2, AnimalCanvas.Height / 2)).r;
                int backRed = ((Color32)back.GetPixel(AnimalCanvas.Width / 2, AnimalCanvas.Height / 2)).r;
                Require(frontRed == RedFor(rabbit.Character.Kind) && backRed == frontRed + 10,
                    "Front and back paintings did not match the client.");
                received.Add(rabbit.Character.Kind);
            }
            Require(received.Count == AnimalKinds.Count, "Artwork or species was mixed across submissions.");
            Debug.Log($"LAN HOST PASS: {received.Count} species reached the park with independent paintings, exited, waved and wandered.");
        }

        static IEnumerator RunClient(ParkPlayground park)
        {
            var camera = park.OrbitCamera.GetComponent<Camera>();
            int parkMask = camera.cullingMask;
            bool parkMsaa = camera.allowMSAA;
            Require(park.Lan.BeginJoin("localhost"), "Client failed to start joining.");
            yield return WaitFor(() => park.Lan.IsJoined);
            Require(park.IsDrawing, "Join did not open the drawing page.");
            Require(parkMask != 0 && camera.cullingMask == 0 && !camera.allowMSAA,
                "The coloring client is still rendering the park.");
            Paint(park.Drawing, RedFor(AnimalKind.Rabbit));
            Require(park.Drawing.ApplyDrawing() && !park.Drawing.ApplyDrawing(), "Apply or cooldown failed.");
            yield return WaitFor(() => park.Lan.LastSendAccepted);
            Require(park.SubmissionCount == 0 && park.IsDrawing, "Client created an animal locally.");

            var others=Enumerable.Range(0,AnimalKinds.Count).Select(value=>(AnimalKind)value)
                .Where(kind=>kind!=AnimalKind.Rabbit).ToArray();
            for(int batch=0;batch<others.Length;batch+=3)
            {
                var pending=new System.Collections.Generic.List<Task<byte>>();
                int end=Math.Min(batch+3,others.Length);
                for(int i=batch;i<end;i++)
                {
                    var kind=others[i];
                    park.Drawing.SelectAnimal((int)kind);
                    Paint(park.Drawing,RedFor(kind));
                    var front=park.Drawing.Front.Paint.GetRawTextureData();
                    var back=park.Drawing.Back.Paint.GetRawTextureData();
                    var id=Guid.NewGuid();
                    pending.Add(ParkLan.Send(IPAddress.Loopback,id,front,back,CancellationToken.None,kind));
                    if(i==end-1)pending.Add(ParkLan.Send(IPAddress.Loopback,id,front,back,CancellationToken.None,kind));
                }
                yield return WaitFor(() => pending.All(task => task.IsCompleted));
                Require(pending.All(task => task.Result == ParkLan.Accepted), "One or more concurrent requests were not acknowledged.");
            }
            park.Lan.Disconnect();
            park.ShowPark();
            Require(camera.cullingMask == parkMask && camera.allowMSAA == parkMsaa,
                "Returning to the park did not restore its camera settings.");
            Debug.Log("LAN RENDER PASS: 3D layers and MSAA are off while coloring and restored when returning to the park.");
            Debug.Log($"LAN CLIENT PASS: joined localhost, sent all {AnimalKinds.Count} species and received acknowledgements, including repeated request IDs.");
        }

        static byte RedFor(AnimalKind kind)=>(byte)(((int)kind+1)*30);

        static void Paint(DrawingPage drawing, byte red)
        {
            var center = new Vector2(AnimalCanvas.Width / 2, AnimalCanvas.Height / 2);
            drawing.Front.Stroke(center, center, 64, new Color32(red, 180, 90, 255));
            drawing.Back.Stroke(center, center, 64, new Color32((byte)(red + 10), 90, 180, 255));
        }
    }
}
