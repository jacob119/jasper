﻿using System;
using System.Linq.Expressions;
using Jasper.Conneg;
using Jasper.EnvironmentChecks;
using Jasper.Http;
using Jasper.Http.ContentHandling;
using Jasper.Http.Routing;
using Jasper.Http.Transport;
using Jasper.Messaging;
using Jasper.Messaging.Configuration;
using Jasper.Messaging.Durability;
using Jasper.Messaging.Logging;
using Jasper.Messaging.Runtime.Serializers;
using Jasper.Messaging.Runtime.Subscriptions;
using Jasper.Messaging.Sagas;
using Jasper.Messaging.Transports;
using Jasper.Messaging.Transports.Tcp;
using Lamar;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;

namespace Jasper
{

    internal class JasperServiceRegistry : ServiceRegistry
    {
        public JasperServiceRegistry(JasperRegistry parent)
        {
            Policies.Add<JasperResolverSet>();

            For<IMetrics>().Use<NulloMetrics>();
            For<IHostedService>().Use<MetricsCollector>();

            this.AddLogging();

            For<IMessageLogger>().Use<MessageLogger>().Singleton();
            For<ITransportLogger>().Use<TransportLogger>().Singleton();


            this.AddSingleton(parent.CodeGeneration);

            For<IHostedService>().Use<NodeRegistration>();
            For<IHostedService>().Use<BackPressureAgent>();

            conneg(parent);
            messaging(parent);

            aspnetcore(parent);
        }

        private void aspnetcore(JasperRegistry parent)
        {
            For<ITransport>()
                .Use<HttpTransport>();


            this.AddSingleton<ConnegRules>();

            this.AddScoped<IHttpContextAccessor>(x => new HttpContextAccessor());
            this.AddSingleton(parent.HttpRoutes.Routes.Router);
            this.AddSingleton(parent.HttpRoutes.Routes);
            ForSingletonOf<IUrlRegistry>().Use(parent.HttpRoutes.Routes.Router.Urls);

            this.AddSingleton<IServiceProviderFactory<IServiceCollection>>(new DefaultServiceProviderFactory());
        }

        private void conneg(JasperRegistry parent)
        {
            this.AddOptions();

            var forwarding = new Forwarders();
            For<Forwarders>().Use(forwarding);

            Scan(_ =>
            {
                _.Assembly(parent.ApplicationAssembly);
                _.AddAllTypesOf<IMessageSerializer>();
                _.AddAllTypesOf<IMessageDeserializer>();
                _.With(new ForwardingRegistration(forwarding));
            });
        }

        private void messaging(JasperRegistry parent)
        {
            ForSingletonOf<MessagingSerializationGraph>().Use<MessagingSerializationGraph>();

            For<IEnvelopePersistor>().Use<NulloEnvelopePersistor>();
            this.AddSingleton<InMemorySagaPersistor>();

            this.AddSingleton(parent.Messaging.Graph);
            this.AddSingleton<IChannelGraph>(parent.Messaging.Channels);
            this.AddSingleton<ILocalWorkerSender>(parent.Messaging.LocalWorker);

            this.AddSingleton<IRetries, EnvelopeRetries>();

            For<ITransport>()
                .Use<LoopbackTransport>();

            For<ITransport>()
                .Use<TcpTransport>();


            ForSingletonOf<IMessagingRoot>().Use<MessagingRoot>();

            ForSingletonOf<ObjectPoolProvider>().Use(new DefaultObjectPoolProvider());



            MessagingRootService(x => x.Workers);
            MessagingRootService(x => x.Pipeline);

            MessagingRootService(x => x.Router);
            MessagingRootService(x => x.Lookup);
            MessagingRootService(x => x.ScheduledJobs);

            For<IMessageContext>().Use(new MessageContextInstance());

            ForSingletonOf<ITransportLogger>().Use<TransportLogger>();

            ForSingletonOf<INodeDiscovery>().UseIfNone(new InMemoryNodeDiscovery(parent.MessagingSettings));
            ForSingletonOf<ISubscriptionsRepository>().UseIfNone<DefaultSubscriptionsRepository>();


            For<IUriLookup>().Use<ConfigUriLookup>();

            For<IEnvironmentRecorder>().Use<EnvironmentRecorder>();
        }

        public void MessagingRootService<T>(Expression<Func<IMessagingRoot, T>> expression) where T : class
        {
            For<T>().Use(new MessagingRootInstance<T>(expression));
        }
    }
}
