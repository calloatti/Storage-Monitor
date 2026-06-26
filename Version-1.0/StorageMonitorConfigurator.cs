using Bindito.Core;
using Timberborn.Automation;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;

namespace Calloatti.StorageMonitor
{
  [Context("Game")]
  internal class StorageMonitorConfigurator : Configurator
  {
    private class EntityPanelModuleProvider : IProvider<EntityPanelModule>
    {
      private readonly StorageMonitorFragment _fragment;

      public EntityPanelModuleProvider(StorageMonitorFragment fragment)
      {
        _fragment = fragment;
      }

      public EntityPanelModule Get()
      {
        EntityPanelModule.Builder builder = new EntityPanelModule.Builder();
        builder.AddMiddleFragment(_fragment);
        return builder.Build();
      }
    }

    protected override void Configure()
    {
      Bind<StorageMonitor>().AsTransient();
      Bind<StorageMonitorGoodsDropdownProvider>().AsTransient();
      Bind<StorageMonitorBannerSetter>().AsTransient();
      Bind<StorageMonitorFragment>().AsSingleton();

      MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
      MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule()
    {
      TemplateModule.Builder builder = new TemplateModule.Builder();
      builder.AddDecorator<StorageMonitorSpec, StorageMonitor>();

      // THIS IS THE MISSING LINK: It attaches the Automator component to your building!
      builder.AddDecorator<StorageMonitor, Automator>();

      builder.AddDecorator<StorageMonitor, StorageMonitorGoodsDropdownProvider>();
      builder.AddDecorator<StorageMonitor, StorageMonitorBannerSetter>();
      builder.AddDecorator<StorageMonitor, AutomatorIlluminator>();
      return builder.Build();
    }
  }
}