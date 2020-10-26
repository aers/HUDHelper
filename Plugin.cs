using System;
using Dalamud.Plugin;
using HUDHelper.Attributes;

namespace HUDHelper
{
    public class Plugin : IDalamudPlugin
    {
        public DalamudPluginInterface pluginInterface;
        public PluginCommandManager<Plugin> commandManager;
        public Configuration config;
        public PluginUI ui;
        public HUDLayoutManager hudLayoutManager;

        public string Name => "HUDHelper";

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            this.config = (Configuration)this.pluginInterface.GetPluginConfig() ?? new Configuration();
            this.config.Initialize(this.pluginInterface);

            this.ui = new PluginUI(this);
            this.pluginInterface.UiBuilder.OnBuildUi += this.ui.Draw;

            this.commandManager = new PluginCommandManager<Plugin>(this, this.pluginInterface);

            this.hudLayoutManager = new HUDLayoutManager(this);
            this.hudLayoutManager.Init();
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            this.commandManager.Dispose();

            this.pluginInterface.SavePluginConfig(this.config);

            this.pluginInterface.UiBuilder.OnBuildUi -= this.ui.Draw;

            this.pluginInterface.Dispose();

            this.hudLayoutManager.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
