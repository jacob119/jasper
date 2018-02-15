﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Baseline;
using Jasper.Messaging.Runtime;
using Jasper.Messaging.Transports.Sending;
using Jasper.Messaging.WorkerQueues;

namespace Jasper.Messaging.Transports.Stub
{
    public static class StubTransportExtensions
    {
        public static StubTransport GetStubTransport(this JasperRuntime runtime)
        {
            return runtime.Container.GetAllInstances<ITransport>().OfType<StubTransport>().Single();
        }

        public static void ClearStubTransportSentList(this JasperRuntime runtime)
        {
            runtime.GetStubTransport().Channels.Each(x => x.Sent.Clear());
        }

        public static Envelope[] AllSentThroughTheStubTransport(this JasperRuntime runtime)
        {
            return runtime.GetStubTransport().Channels.SelectMany(x => x.Sent).ToArray();
        }

    }

    public class StubTransport : ITransport
    {
        public LightweightCache<Uri, StubChannel> Channels;

        public StubTransport()

        {
            LocalReplyUri = new Uri($"stub://replies");
        }

        public void Dispose()
        {
            WasDisposed = true;
        }

        public bool WasDisposed { get; set; }

        public string Protocol { get; } = "stub";
        public IList<StubMessageCallback> Callbacks { get; } = new List<StubMessageCallback>();

        public ISendingAgent BuildSendingAgent(Uri uri, IMessagingRoot root, CancellationToken cancellation)
        {
            return Channels[uri];
        }

        public Uri LocalReplyUri { get; }

        public void StartListening(IMessagingRoot root)
        {
            var pipeline = root.Workers.As<WorkerQueue>().Pipeline;

            Channels =
                new LightweightCache<Uri, StubChannel>(uri => new StubChannel(uri, pipeline, this));


            var incoming = root.Settings.Listeners.Where(x => x.Scheme == "stub");
            foreach (var uri in incoming)
            {
                Channels.FillDefault(uri);
            }
        }

        public void Describe(TextWriter writer)
        {
            writer.WriteLine("'Stub' transport is active");
        }

        public StubMessageCallback LastCallback()
        {
            return Callbacks.LastOrDefault();
        }
    }
}