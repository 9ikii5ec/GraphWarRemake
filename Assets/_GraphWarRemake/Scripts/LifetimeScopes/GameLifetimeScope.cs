using GraphWarRemake.Logging;
using GraphWarRemake.Math;
using VContainer;
using VContainer.Unity;

namespace GraphWarRemake.LifetimeScopes
{
    public class GameLifetimeScope : LifetimeScope
    {
        public static IObjectResolver GlobalContainer { get; private set; }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(container =>
            {
                GlobalContainer = container;
            });

            builder.Register<IGameLogger>(c => new GameLogger(), Lifetime.Singleton)
                .AsSelf();

            builder.Register<MathEngine>(c =>
            {
                var logger = c.Resolve<IGameLogger>();
                return new MathEngine(logger);
            }, Lifetime.Singleton).AsSelf();
        }

        protected override void OnDestroy()
        {
            GlobalContainer = null;
            base.OnDestroy();
        }
    }
}
