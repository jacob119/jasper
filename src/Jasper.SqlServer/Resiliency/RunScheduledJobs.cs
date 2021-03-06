﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Jasper.Messaging.Durability;
using Jasper.Messaging.Logging;
using Jasper.Messaging.Runtime;
using Jasper.Messaging.Transports;
using Jasper.Messaging.Transports.Configuration;
using Jasper.Messaging.WorkerQueues;
using Jasper.SqlServer.Persistence;
using Jasper.SqlServer.Util;

namespace Jasper.SqlServer.Resiliency
{
    public class RunScheduledJobs : IMessagingAction
    {
        private readonly string _findReadyToExecuteJobs;
        private readonly IWorkerQueue _workers;
        private readonly SqlServerSettings _mssqlSettings;
        private readonly ITransportLogger _logger;
        private readonly IRetries _retries;
        private readonly MessagingSettings _settings;
        public static readonly int ScheduledJobLockId = "scheduled-jobs".GetHashCode();
        private readonly IEnvelopePersistor _persistor;

        public RunScheduledJobs(IWorkerQueue workers, SqlServerSettings mssqlSettings, ITransportLogger logger, IRetries retries, MessagingSettings settings)
        {
            _workers = workers;
            _mssqlSettings = mssqlSettings;
            _logger = logger;
            _retries = retries;
            _settings = settings;

            _persistor = new SqlServerEnvelopePersistor(_mssqlSettings);

            _findReadyToExecuteJobs = $"select body from {mssqlSettings.SchemaName}.{SqlServerEnvelopePersistor.IncomingTable} where status = '{TransportConstants.Scheduled}' and execution_time <= @time";

        }

        public async Task Execute(SqlConnection conn, ISchedulingAgent agent)
        {
            var utcNow = DateTimeOffset.UtcNow;;

            await ExecuteAtTime(conn, utcNow);
        }

        public async Task<List<Envelope>> ExecuteAtTime(SqlConnection conn, DateTimeOffset utcNow)
        {
            if (!await conn.TryGetGlobalLock(ScheduledJobLockId))
            {
                return null;
            }

            var tx = conn.BeginTransaction();

            try
            {


                List<Envelope> readyToExecute = null;

                try
                {
                    readyToExecute = await conn
                        .CreateCommand(tx, _findReadyToExecuteJobs)
                        .With("time", utcNow, SqlDbType.DateTimeOffset)
                        .ExecuteToEnvelopes(tx);

                    if (!readyToExecute.Any())
                    {
                        tx.Rollback();
                        return readyToExecute;
                    }

                    await markOwnership(conn, tx, readyToExecute);

                    tx.Commit();
                }
                catch (Exception)
                {
                    tx.Rollback();
                    throw;
                }

                _logger.ScheduledJobsQueuedForExecution(readyToExecute);

                foreach (var envelope in readyToExecute)
                {
                    envelope.Callback = new DurableCallback(envelope, _workers, _persistor, _retries, _logger);

                    await _workers.Enqueue(envelope);
                }

                return readyToExecute;
            }
            finally
            {
                await conn.ReleaseGlobalLock(ScheduledJobLockId);
            }
        }

        private async Task markOwnership(SqlConnection conn, SqlTransaction tx, List<Envelope> incoming)
        {
            var cmd = conn.CreateCommand($"{_mssqlSettings.SchemaName}.uspMarkIncomingOwnership");
            cmd.Transaction = tx;
            cmd.CommandType = CommandType.StoredProcedure;
            var list = cmd.Parameters.AddWithValue("IDLIST", SqlServerEnvelopePersistor.BuildIdTable(incoming));
            list.SqlDbType = SqlDbType.Structured;
            list.TypeName = $"{_mssqlSettings.SchemaName}.EnvelopeIdList";
            cmd.Parameters.AddWithValue("owner", _settings.UniqueNodeId).SqlDbType = SqlDbType.Int;



            await cmd.ExecuteNonQueryAsync();
        }
    }


}
