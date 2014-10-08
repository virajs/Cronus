using System;
using System.Collections.Generic;
using Elders.Cronus.DomainModeling;
using Elders.Cronus.UnitOfWork;
using Elders.Cronus.IocContainer;

namespace Elders.Cronus.Pipeline.Config
{
    public class MessageProcessorSettings<TContract> : SettingsBuilder, IMessageProcessorSettings<TContract>, IHaveUnitOfWorkFactory where TContract : IMessage
    {
        public MessageProcessorSettings(ISettingsBuilder builder) : base(builder)
        {
            (this as IHaveUnitOfWorkFactory).UnitOfWorkFactory = new Lazy<UnitOfWorkFactory>(() => new UnitOfWorkFactory());
        }

        Dictionary<Type, List<Tuple<Type, Func<Type, Context, object>>>> IMessageProcessorSettings<TContract>.HandlerRegistrations { get; set; }

        Lazy<UnitOfWorkFactory> IHaveUnitOfWorkFactory.UnitOfWorkFactory { get; set; }

        public override void Build()
        {
            var builder = this as ISettingsBuilder;

            Func<IMessageProcessor<TContract>> messageHandlerProcessorFactory = () =>
            {
                var safeBatchFactory = new SafeBatchWithBatchUnitOfWorkContextFactory<TransportMessage>((this as IHaveUnitOfWorkFactory).UnitOfWorkFactory.Value);

                var handler = new MessageHandlerCollection<TContract>(safeBatchFactory);

                foreach (var reg in (this as IMessageProcessorSettings<TContract>).HandlerRegistrations)
                {
                    foreach (var item in reg.Value)
                    {
                        if (!typeof(IPort).IsAssignableFrom(item.Item1))
                            handler.RegisterHandler(reg.Key, item.Item1, item.Item2);
                    }
                }
                return handler;
            };
            builder.Container.RegisterSingleton<IMessageProcessor<TContract>>(() => messageHandlerProcessorFactory(), builder.Name);
        }
    }

    public static class MessageProcessorWithSafeBatchSettingsExtensions
    {
        public static T UseUnitOfWork<T>(this T self, UnitOfWorkFactory instance) where T : IHaveUnitOfWorkFactory
        {
            self.UnitOfWorkFactory = new Lazy<UnitOfWorkFactory>(() => instance);

            return self;
        }

        public static T UseProjections<T>(this T self, Action<MessageProcessorSettings<IEvent>> configure) where T : IConsumerSettings<IEvent>
        {
            MessageProcessorSettings<IEvent> settings = new MessageProcessorSettings<IEvent>(self);
            if (configure != null)
                configure(settings);

            (settings as ISettingsBuilder).Build();
            return self;
        }

        public static T UsePortsAndProjections<T>(this T self, Action<MessageProcessorSettings<IEvent>> configure) where T : IConsumerSettings<IEvent>
        {
            MessageProcessorSettings<IEvent> settings = new MessageProcessorSettings<IEvent>(self);
            if (configure != null)
                configure(settings);

            (settings as ISettingsBuilder).Build();
            return self;
        }

        public static T UsePorts<T>(this T self, Action<MessageProcessorSettings<IEvent>> configure) where T : IConsumerSettings<IEvent>
        {
            MessageProcessorSettings<IEvent> settings = new MessageProcessorSettings<IEvent>(self);
            if (configure != null)
                configure(settings);

            (settings as ISettingsBuilder).Build();
            return self;
        }

        public static T UseApplicationServices<T>(this T self, Action<MessageProcessorSettings<ICommand>> configure) where T : IConsumerSettings<ICommand>
        {
            MessageProcessorSettings<ICommand> settings = new MessageProcessorSettings<ICommand>(self);
            if (configure != null)
                configure(settings);

            (settings as ISettingsBuilder).Build();
            return self;
        }
    }
}