﻿using Jasper.Marten.Persistence.DbObjects;
using Jasper.Messaging.Transports.Configuration;
using Marten;
using Marten.Schema;

namespace Jasper.Marten
{
    public class EnvelopeTables
    {

        public EnvelopeTables(MessagingSettings settings, StoreOptions storeConfiguration)
        {
            Incoming = new DbObjectName(storeConfiguration.DatabaseSchemaName,
                PostgresqlEnvelopeStorage.IncomingTableName);
            Outgoing = new DbObjectName(storeConfiguration.DatabaseSchemaName,
                PostgresqlEnvelopeStorage.OutgoingTableName);

            CurrentNodeId = settings.UniqueNodeId;

        }

        public int CurrentNodeId { get; }

        public DbObjectName Incoming { get; }

        public DbObjectName Outgoing { get; }
    }
}
