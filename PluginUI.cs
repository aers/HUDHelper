using ImGuiNET;
using System;
using System.Runtime.InteropServices;

namespace HUDHelper
{
    public unsafe class PluginUI
    {

        private bool visible = false;
        public bool IsVisible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private Plugin _p;

        public PluginUI(Plugin p)
        {
            _p = p;
        }
        public void Draw()
        {
            if (!IsVisible)
                return;

            if (_p.hudLayoutManager.GetCurrentAddon() == null)
                return;

            var addon = _p.hudLayoutManager.GetCurrentAddon();

            if (_p.config.HideOverlay)
                _p.hudLayoutManager.HideCurrentOverlay();
            else
                _p.hudLayoutManager.ShowCurrentOverlay();

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 120));
            if (ImGui.Begin("HUDHelper", ref visible, ImGuiWindowFlags.NoResize))
            {
                //ImGui.Text($"AddonHudLayoutScreen(ptr = {(long)_p.hudLayoutManager.hudLayoutScreen:X})");
                //ImGui.Text($"Selected Addon(ptr = {(long)_p.hudLayoutManager.hudLayoutScreen->SelectedAddon:X}) - {Marshal.PtrToStringAnsi(new IntPtr(addon->Name))}");
                var X = (int) addon->X;
                if (ImGui.InputInt("X", ref X))
                {
                    _p.hudLayoutManager.SetPosition((short)X, addon->Y);
                }
                var Y = (int)addon->Y;
                if (ImGui.InputInt("Y", ref Y))
                {
                    _p.hudLayoutManager.SetPosition(addon->X, (short)Y);
                }
                bool hideOverlay = _p.config.HideOverlay;
                if (ImGui.Checkbox("Hide Addon Overlay", ref hideOverlay))
                {
                    _p.config.HideOverlay = hideOverlay;
                    if (hideOverlay)
                        _p.hudLayoutManager.HideCurrentOverlay();
                    else
                        _p.hudLayoutManager.ShowCurrentOverlay();
                }
            }
        }
    }
}
