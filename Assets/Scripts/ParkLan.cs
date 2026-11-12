using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PeacePark
{
    internal static class ParkLan
    {
        public const int Port = 42731;
        public const int PaintBytes = AnimalCanvas.Width * AnimalCanvas.Height * 4;
        const int MaxPayload = PaintBytes * 2 + 65536;
        const int Version = 0x31504b50;
        const byte Ping = 1, Submit = 2, SubmitAnimal = 3;
        public const byte Accepted = 1, Busy = 2, Full = 3;

        public static Task<byte> Probe(IPAddress address, CancellationToken token) =>
            Request(address, Ping, Guid.Empty, null, null, token, AnimalKind.Rabbit);

        public static Task<byte> Send(IPAddress address, Guid id, byte[] front, byte[] back, CancellationToken token, AnimalKind kind=AnimalKind.Rabbit) =>
            Request(address, kind==AnimalKind.Rabbit?Submit:SubmitAnimal, id, front, back, token, kind);

        static Task<byte> Request(IPAddress address, byte command, Guid id, byte[] front, byte[] back, CancellationToken token, AnimalKind kind)
        {
            return Task.Run(async () =>
            {
                using var client = new TcpClient(AddressFamily.InterNetwork);
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
                deadline.CancelAfter(TimeSpan.FromSeconds(8));
                using var close = deadline.Token.Register(client.Dispose);
                await client.ConnectAsync(address, Port).ConfigureAwait(false);
                using var stream = client.GetStream();
                using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);
                writer.Write(Version);
                writer.Write(command);
                if (command != Ping)
                {
                    using var payload = new MemoryStream();
                    using (var compressor = new DeflateStream(payload, CompressionLevel.Fastest, true))
                    {
                        compressor.Write(front, 0, front.Length);
                        compressor.Write(back, 0, back.Length);
                    }
                    writer.Write(id.ToByteArray());
                    if(command==SubmitAnimal)writer.Write((byte)kind);
                    writer.Write((int)payload.Length);
                    writer.Flush();
                    payload.WriteTo(stream);
                }
                writer.Flush();
                using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
                if (reader.ReadInt32() != Version) throw new InvalidDataException("Unsupported park protocol.");
                return reader.ReadByte();
            }, token);
        }

        static void ReadExactly(Stream stream, byte[] target)
        {
            int offset = 0;
            while (offset < target.Length)
            {
                int count = stream.Read(target, offset, target.Length - offset);
                if (count == 0) throw new EndOfStreamException();
                offset += count;
            }
        }

        internal sealed class Host : IDisposable
        {
            readonly TcpListener listener = new TcpListener(IPAddress.Any, Port);
            readonly CancellationTokenSource stop = new CancellationTokenSource();
            readonly SemaphoreSlim slots = new SemaphoreSlim(4);
            readonly Queue<Arrival> inbox = new Queue<Arrival>();
            readonly HashSet<Guid> accepted = new HashSet<Guid>();
            bool disposed;

            sealed class Arrival
            {
                public Guid Id;
                public AnimalKind Kind;
                public byte[] Front, Back;
                public readonly TaskCompletionSource<byte> Reply = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public Host()
            {
                listener.ExclusiveAddressUse = true;
                listener.Start(8);
                _ = Listen();
            }

            async Task Listen()
            {
                try
                {
                    while (true)
                    {
                        await slots.WaitAsync(stop.Token).ConfigureAwait(false);
                        var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                        _ = Task.Run(() => Handle(client));
                    }
                }
                catch (OperationCanceledException) { }
                catch (SocketException) { }
                catch (ObjectDisposedException) { }
                finally
                {
                    listener.Stop();
                }
            }

            async Task Handle(TcpClient client)
            {
                using (client)
                using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop.Token))
                {
                    deadline.CancelAfter(TimeSpan.FromSeconds(8));
                    using var close = deadline.Token.Register(client.Dispose);
                    try
                    {
                        using var stream = client.GetStream();
                        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
                        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);
                        if (reader.ReadInt32() != Version) return;
                        byte command = reader.ReadByte();
                        byte result;
                        if (command == Ping) result = Accepted;
                        else if (command == Submit || command == SubmitAnimal)
                        {
                            var idBytes = new byte[16];
                            ReadExactly(stream, idBytes);
                            int animal=command==SubmitAnimal?reader.ReadByte():(int)AnimalKind.Rabbit;
                            if(!AnimalKinds.IsValid(animal))return;
                            int size = reader.ReadInt32();
                            if (size <= 0 || size > MaxPayload) return;
                            var compressed = new byte[size];
                            ReadExactly(stream, compressed);
                            using var payload = new MemoryStream(compressed);
                            using var decompressor = new DeflateStream(payload, CompressionMode.Decompress);
                            var arrival = new Arrival { Id = new Guid(idBytes), Kind=(AnimalKind)animal, Front = new byte[PaintBytes], Back = new byte[PaintBytes] };
                            if (arrival.Id == Guid.Empty) return;
                            ReadExactly(decompressor, arrival.Front);
                            ReadExactly(decompressor, arrival.Back);
                            if (decompressor.ReadByte() != -1) return;
                            using var cancel = deadline.Token.Register(() => arrival.Reply.TrySetCanceled());
                            lock (inbox)
                            {
                                if (inbox.Count < 4 && !deadline.IsCancellationRequested) inbox.Enqueue(arrival);
                                else arrival.Reply.TrySetResult(Busy);
                            }
                            result = await arrival.Reply.Task.ConfigureAwait(false);
                        }
                        else return;
                        writer.Write(Version);
                        writer.Write(result);
                        writer.Flush();
                    }
                    catch (IOException) { }
                    catch (SocketException) { }
                    catch (InvalidDataException) { }
                    catch (ObjectDisposedException) { }
                    catch (OperationCanceledException) { }
                    finally { slots.Release(); }
                }
            }

            // Unity scene and texture work is confined to the frame thread.
            public void Pump(ParkPlayground park)
            {
                Arrival arrival;
                lock (inbox)
                {
                    if (inbox.Count == 0) return;
                    arrival = inbox.Dequeue();
                }
                if (arrival.Reply.Task.IsCanceled) return;
                if (accepted.Contains(arrival.Id))
                {
                    arrival.Reply.TrySetResult(Accepted);
                    return;
                }
                byte result = park.SubmitLan(arrival.Kind,arrival.Front,arrival.Back);
                if (result == Accepted) accepted.Add(arrival.Id);
                arrival.Reply.TrySetResult(result);
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                stop.Cancel();
                listener.Stop();
                lock (inbox) inbox.Clear();
            }
        }
    }
}
