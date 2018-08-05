﻿using System;
using Jasper.Messaging.Runtime;
using Jasper.Messaging.Tracking;
using Jasper.Persistence.Marten;
using Marten;

namespace IntegrationTests.Persistence.Marten.Persistence
{
    [Obsolete("Moved to ST")]
    public class ItemCreatedHandler
    {
        [MartenTransaction]
        public static void Handle(ItemCreated created, IDocumentSession session, MessageTracker tracker,
            Envelope envelope)
        {
            session.Store(created);
            tracker.Record(created, envelope);
        }
    }
}
