using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

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
            yield return WaitFor(() => park && park.IsReady && park.Lan != null);
            if (host) yield return RunHost(park);
            else yield return RunClient(park);
            Application.Quit(0);
        }

        static IEnumerator RunHost(ParkPlayground park)
        {
            Require(park.IsSelectingMode&&!park.Lan.IsHosting&&!park.IsDrawing, "The first screen is not the mode selector.");
            var camera=park.OrbitCamera.GetComponent<Camera>();
            var start=GameObject.Find("Start Screen").transform;
            var open=start.Find("Safe Area/놀이 공간 열기").GetComponent<Button>();
            var tiles=start.Find("Safe Area/Start Venue Tiles");
            int initialVenue=park.VenueIndex;
            Require(!tiles.gameObject.activeSelf,"Start tiles were visible before choosing host mode.");
            open.onClick.Invoke();
            Require(tiles.gameObject.activeInHierarchy&&park.IsSelectingMode&&!park.Lan.IsHosting&&
                park.VenueIndex==initialVenue&&camera.cullingMask==0,"Opening start tiles moved the park or started hosting.");
            var corners=new Vector3[4];
            var safe=Screen.safeArea;
            foreach(var venue in park.Venues)
            {
                var tile=(RectTransform)tiles.Find(venue.Name);
                Require(tile.Find("Artwork Frame/Venue Preview").GetComponent<RawImage>().texture,
                    "Missing start tile preview for "+venue.Name);
                tile.GetWorldCorners(corners);
                Require(corners[0].x>=safe.xMin-1&&corners[0].y>=safe.yMin-1&&
                    corners[2].x<=safe.xMax+1&&corners[2].y<=safe.yMax+1,
                    "Start tile extends outside the safe area: "+venue.Name);
            }
            tiles.Find("뒤로").GetComponent<Button>().onClick.Invoke();
            Require(!tiles.gameObject.activeSelf&&open.gameObject.activeInHierarchy&&!park.Lan.IsHosting,
                "Back did not restore the start controls.");
            open.onClick.Invoke();
            tiles.Find("놀이공원").GetComponent<Button>().onClick.Invoke();
            Require(park.IsViewingPark&&park.Lan.IsHosting&&park.ThemeName=="놀이공원"&&!tiles.gameObject.activeSelf,
                "Selecting a start tile did not enter the chosen venue as host.");
            Debug.Log("START VENUE TILES PASS: previews remain on the start screen, back restores the buttons, and only choosing a tile opens the selected park.");
            Require(camera.cullingMask!=0&&park.OrbitCamera.InputEnabled, "The host park is not rendered or controllable.");
            park.OpenSettings();
            string address=park.Lan.LocalIPv4();
            Require(address=="—"||(IPAddress.TryParse(address,out var ip)&&
                ip.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork&&!IPAddress.IsLoopback(ip)),
                "Settings must show a single device IPv4 address when available.");
            Require(park.IsSettingsOpen&&park.IsViewingPark&&park.Lan.IsHosting&&camera.cullingMask!=0&&
                !park.OrbitCamera.InputEnabled, "Park information did not open over the live park.");
            park.CloseDialog();
            Require(!park.IsSettingsOpen&&park.OrbitCamera.InputEnabled, "Closing settings did not restore camera controls.");
            park.OpenVenuePicker();
            Require(park.IsVenuePickerOpen&&!park.OrbitCamera.InputEnabled&&park.Venues.Any(venue=>venue.Name=="시계탑 광장")&&
                park.Venues.All(venue=>venue.Preview),"Venue picture cards are missing.");
            Debug.Log("LAN TEST HOST READY");
            yield return WaitFor(() => park.SubmissionCount > 0);
            park.Advance(.2f);
            Require(park.Animals.Count>0,"Expected a painted animal before changing venue.");
            var first=park.Animals[0];
            Require(park.SelectVenue(1)&&park.ThemeName=="숲"&&park.Lan.IsHosting&&
                park.Venues[1].Root.activeInHierarchy&&!park.Venues[0].Root.activeInHierarchy&&
                park.Animals[0]==first&&park.OrbitCamera.InputEnabled,"Switching to the forest lost host state.");
            Require(park.Venues[1].Root.transform.Find("Ultimate Nature Starter scenery").childCount>90,
                "The forest did not load its Ultimate Nature prefabs.");
            yield return WaitFor(() => park.SubmissionCount >= AnimalKinds.Count);
            yield return new WaitForSecondsRealtime(1);
            Require(park.SubmissionCount == AnimalKinds.Count, "Duplicate submission spawned an extra animal.");
            park.Advance(AnimalKinds.Count*7f);
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
            park.OpenVenuePicker();
            Require(park.SelectVenue(0)&&park.ThemeName=="공원"&&park.Fountain&&park.Animals[0]==first&&
                park.Lan.IsHosting,"Returning to the garden lost animals or disconnected the host.");
            Debug.Log("VENUES PASS: picture picker, imported forest, active door handoff, live LAN submissions and return to the garden.");
            park.ReturnToModeSelection();
            Require(park.IsSelectingMode&&!park.Lan.IsHosting&&camera.cullingMask==0,
                "Returning to the mode selector did not stop the host and park rendering.");
            Debug.Log($"LAN HOST PASS: {received.Count} species reached the park with independent paintings, exited, waved and wandered.");
        }

        static IEnumerator RunClient(ParkPlayground park)
        {
            var camera = park.OrbitCamera.GetComponent<Camera>();
            Require(park.IsSelectingMode&&!park.IsDrawing&&camera.cullingMask==0, "The first screen is not the mode selector.");
            park.OpenVenuePicker();
            Require(!park.IsVenuePickerOpen,"A non-host was able to open the venue picker.");
            park.ChooseDrawingMode();
            Require(park.IsConnecting&&park.IsSelectingMode&&!park.IsDrawing, "The connection popup replaced the start screen.");
            Require(!park.Lan.BeginJoin("invalid")&&!string.IsNullOrEmpty(park.Toast.Message), "Invalid address did not show a toast.");
            park.CloseDialog();
            Require(park.IsSelectingMode&&!park.IsConnecting&&park.Toast.Message==null, "Closing the popup did not clear its toast.");
            park.ChooseDrawingMode();
            Require(park.Lan.BeginDiscovery(),"Client failed to start automatic discovery.");
            yield return WaitFor(() => park.Lan.IsJoined);
            Require(park.IsDrawing&&!park.IsConnecting&&!park.IsSelectingMode, "Join did not open the drawing page and dismiss the popup.");
            Debug.Log("DISCOVERY PASS: a UDP reply located the host and the existing TCP connection opened drawing.");
            Require(camera.cullingMask == 0 && !camera.allowMSAA,
                "The coloring client is still rendering the park.");
            Require(park.Toast.Message==ParkLanguage.Text("놀이 공간에 연결됐어요"),"Remote drawing did not show its connection toast.");
            Paint(park.Drawing, RedFor(AnimalKind.Rabbit));
            Require(park.Drawing.ApplyDrawing() && !park.Drawing.ApplyDrawing(), "Apply or cooldown failed.");
            yield return WaitFor(() => park.Lan.LastSendAccepted);
            Require(park.SubmissionCount == 0 && park.IsDrawing, "Client created an animal locally.");
            Require(park.Toast.Message==ParkLanguage.Text("놀이 공간에 보냈어요! 새 동물을 색칠해 보세요"),
                "An acknowledged drawing did not show a toast.");
            float toastStart=Time.unscaledTime;
            yield return new WaitForSecondsRealtime(2.5f);
            Require(park.Toast.Message!=null,"The send toast expired early.");
            yield return WaitFor(() => park.Toast.Message==null);
            Require(Time.unscaledTime-toastStart<3.3f,"The send toast did not clear in three seconds.");
            Debug.Log("DRAWING TOAST PASS: remote navigation hidden, send acknowledgement displayed for three seconds.");

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
                    pending.Add(SendSample(kind,id,front,back));
                    if(i==end-1)pending.Add(SendSample(kind,id,front,back));
                }
                yield return WaitFor(() => pending.All(task => task.IsCompleted));
                var responses=pending.Select(task=>task.Result).ToArray();
                Require(responses.All(result=>result==ParkLan.Accepted),
                    "Concurrent drawing responses: "+string.Join(", ",responses));
            }
            park.ReturnToModeSelection();
            Require(park.IsSelectingMode&&!park.Lan.IsJoined&&!park.IsDrawing&&camera.cullingMask==0,
                "Disconnect did not return to mode selection.");
            Debug.Log("MODE SELECTION PASS: startup selector, connection popup, close, remote drawing and disconnect.");
            Debug.Log($"LAN CLIENT PASS: joined localhost, sent all {AnimalKinds.Count} species and received acknowledgements, including repeated request IDs.");
        }

        static async Task<byte> SendSample(AnimalKind kind,Guid id,byte[] front,byte[] back)
        {
            // The live venue may briefly fill its eight-entry queue while an animal leaves the door.
            for(int retries=0;;retries++)
            {
                byte result=await ParkLan.Send(IPAddress.Loopback,id,front,back,CancellationToken.None,kind).ConfigureAwait(false);
                if(result!=ParkLan.Busy||retries>=24)return result;
                await Task.Delay(350).ConfigureAwait(false);
            }
        }

        static byte RedFor(AnimalKind kind)=>(byte)(((int)kind+1)*21);

        static void Paint(DrawingPage drawing, byte red)
        {
            var center = new Vector2(AnimalCanvas.Width / 2, AnimalCanvas.Height / 2);
            drawing.Front.Stroke(center, center, 64, new Color32(red, 180, 90, 255));
            drawing.Back.Stroke(center, center, 64, new Color32((byte)(red + 10), 90, 180, 255));
        }
    }
}
