﻿using System.Threading.Tasks;
using Jasper.Testing.FakeStoreTypes;
using Jasper.Testing.Messaging.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Jasper.Testing.Messaging.Compilation
{
    public class use_wrappers : CompilationContext
    {
        public use_wrappers()
        {
            theRegistry.Handlers.IncludeType<TransactionalHandler>();

            theRegistry.Services.AddSingleton(theTracking);
            theRegistry.Services.ForSingletonOf<IFakeStore>().Use<FakeStore>();
        }

        private readonly FakeStoreTypes.Tracking theTracking = new FakeStoreTypes.Tracking();

        [Fact]
        public async Task wrapper_applied_by_generic_attribute_executes()
        {
            var message = new Message2();

            await Execute(message);

            theTracking.DisposedTheSession.ShouldBeTrue();
            theTracking.OpenedSession.ShouldBeTrue();
            theTracking.CalledSaveChanges.ShouldBeTrue();
        }


        [Fact]
        public async Task wrapper_executes()
        {
            var message = new Message1();

            await Execute(message);

            theTracking.DisposedTheSession.ShouldBeTrue();
            theTracking.OpenedSession.ShouldBeTrue();
            theTracking.CalledSaveChanges.ShouldBeTrue();
        }
    }

    [JasperIgnore]
    public class TransactionalHandler
    {
        [FakeTransaction]
        public void Handle(Message1 message)
        {
        }

        [GenericFakeTransaction]
        public void Handle(Message2 message)
        {
        }
    }
}
