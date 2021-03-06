﻿using System.IO;
using System.Text;
using System.Threading.Tasks;
using Photon.Communication.Tests.Internal.TRx;
using Xunit;

namespace Photon.Communication.Tests
{
    public class StreamTests
    {
        [Fact]
        public void ReadAndWriteTextDuplexer()
        {
            using (var duplexer = new Duplexer())
            using (var writer = new StreamWriter(duplexer.StreamA, Encoding.UTF8, 64, true)) 
            using (var reader = new StreamReader(duplexer.StreamB, Encoding.UTF8, true)) {
                writer.Write("Hello ");
                writer.Write("World");
                writer.WriteLine("!");
                writer.Flush();

                var data = reader.ReadLine();
                Assert.Equal("Hello World!", data);
            }
        }

        [Fact]
        public async Task ReadAndWriteTextDuplexerAsync()
        {
            using (var duplexer = new Duplexer())
            using (var writer = new StreamWriter(duplexer.StreamA, Encoding.UTF8, 64, true)) 
            using (var reader = new StreamReader(duplexer.StreamB, Encoding.UTF8, true)) {
                var dataTask = reader.ReadLineAsync();

                await writer.WriteAsync("Hello ");
                await writer.WriteAsync("World");
                await writer.WriteLineAsync("!");
                await writer.FlushAsync();

                Assert.Equal("Hello World!", dataTask.Result);
            }
        }

        [Fact]
        public void ReadAndWriteBinaryDuplexer()
        {
            using (var duplexer = new Duplexer())
            using (var writer = new BinaryWriter(duplexer.StreamA, Encoding.UTF8, true))
            using (var reader = new BinaryReader(duplexer.StreamB, Encoding.UTF8, true)) {
                writer.Write(42);
                writer.Flush();
                var dataInt32 = reader.ReadInt32();
                Assert.Equal(42, dataInt32);

                writer.Write("Hello!");
                writer.Flush();
                var dataString = reader.ReadString();
                Assert.Equal("Hello!", dataString);
            }
        }

        [Fact]
        public void BiDirectionalTextDuplexer()
        {
            using (var duplexer = new Duplexer())
            using (var writerA = new StreamWriter(duplexer.StreamA))
            using (var readerA = new StreamReader(duplexer.StreamA))
            using (var writerB = new StreamWriter(duplexer.StreamB))
            using (var readerB = new StreamReader(duplexer.StreamB)) {
                writerA.Write("Hello ");
                writerA.Write("World");
                writerA.WriteLine("!");
                writerA.Flush();

                writerB.Write("What's");
                writerB.Write(" up ");
                writerB.Write("Doc");
                writerB.WriteLine("?");
                writerB.Flush();

                var dataA = readerA.ReadLine();
                Assert.Equal("What's up Doc?", dataA);

                var dataB = readerB.ReadLine();
                Assert.Equal("Hello World!", dataB);
            }
        }

        [Fact]
        public async Task BiDirectionalTextDuplexerAsync()
        {
            using (var duplexer = new Duplexer())
            using (var writerA = new StreamWriter(duplexer.StreamA))
            using (var readerA = new StreamReader(duplexer.StreamA))
            using (var writerB = new StreamWriter(duplexer.StreamB))
            using (var readerB = new StreamReader(duplexer.StreamB)) {
                var taskA = readerA.ReadLineAsync();
                var taskB = readerB.ReadLineAsync();

                await Task.Delay(20);

                await writerA.WriteAsync("Hello ");
                await writerA.WriteAsync("World");
                await writerA.WriteLineAsync("!");
                await writerA.FlushAsync();

                await writerB.WriteAsync("What's");
                await writerB.WriteAsync(" up ");
                await writerB.WriteAsync("Doc");
                await writerB.WriteLineAsync("?");
                await writerB.FlushAsync();

                Assert.Equal("Hello World!", taskB.Result);
                Assert.Equal("What's up Doc?", taskA.Result);
            }
        }
    }
}
