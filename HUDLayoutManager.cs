using Dalamud.Hooking;
using FFXIVClientStructs.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using FFXIVClientStructs.Client.UI;
using Dalamud.Plugin;

namespace HUDHelper
{

    public unsafe class HUDLayoutManager : IDisposable
    {
        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public unsafe struct CalculateBoundsResult
        {
            [FieldOffset(0x0)] public int left;
            [FieldOffset(0x4)] public int top;
            [FieldOffset(0x8)] public int right;
            [FieldOffset(0xC)] public int bottom;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x8)]
        public unsafe struct MouseEvent
        {
            [FieldOffset(0x0)] public short X;
            [FieldOffset(0x2)] public short Y;
            [FieldOffset(0x4)] public int unk; // few bytes worth of flags 
        }

        private unsafe delegate void AtkUnitBaseSetScaleDelegate(AtkUnitBase* thisPtr, float scale, bool unk);
        private AtkUnitBaseSetScaleDelegate atkUnitBaseSetScale;
        private unsafe delegate void AtkUnitBaseSetPositionDelegate(AtkUnitBase* thisPtr, short X, short Y);
        private AtkUnitBaseSetPositionDelegate atkUnitBaseSetPosition;

        private unsafe delegate void AtkResNodeSetPositionDelegate(AtkResNode* node, short x, short y);
        private AtkResNodeSetPositionDelegate atkResNodeSetPosition;
        private unsafe delegate void AtkResNodeSetSizeDelegate(AtkResNode* node, ushort width, ushort height);
        private AtkResNodeSetSizeDelegate atkResNodeSetSize;

        private unsafe delegate bool AtkComponentButtonToggleState(AtkComponentButton* thisPtr, bool enabled);
        private AtkComponentButtonToggleState atkComponentButtonToggleState;

        private unsafe delegate void AtkUnitBaseCalculateBounds(AtkUnitBase* thisPtr, CalculateBoundsResult* result);
        private AtkUnitBaseCalculateBounds atkUnitBaseCalculateBounds;

        private unsafe delegate void ShowHUDLayoutDelegate(void* agentHudLayout);
        private Hook<ShowHUDLayoutDelegate> hookShowHUDLayout;
        private unsafe delegate void HideHUDLayoutDelegate(void* agentHudLayout);
        private Hook<HideHUDLayoutDelegate> hookHideHUDLayout;

        private unsafe delegate bool AddonHudLayoutScreenSetAddonScale(AddonHudLayoutScreen* thisPtr, MoveableAddonInfoStruct* addonStruct, byte scaleID);
        private AddonHudLayoutScreenSetAddonScale addonHudLayoutScreenSetAddonScale;

        private unsafe delegate void AddonHudLayoutScreenEventAddonIsClickedDelegate(AddonHudLayoutScreen* thisPtr, uint slot, MouseEvent* mEvent);
        private AddonHudLayoutScreenEventAddonIsClickedDelegate addonHudLayoutScreenEventAddonIsClicked;

        private unsafe delegate void AddonHudLayoutScreenEventAddonIsReleasedDelegate(AddonHudLayoutScreen* thisPtr);
        private AddonHudLayoutScreenEventAddonIsReleasedDelegate addonHudLayoutScreenEventAddonIsReleased;

        private unsafe delegate void AddonHudLayoutScreenEventAddonMovedDelegate(AddonHudLayoutScreen* thisPtr, MouseEvent* mEvent);
        private AddonHudLayoutScreenEventAddonMovedDelegate addonHudLayoutScreenEventAddonMoved;

        private Plugin _p;

        public AgentHudLayout* agentHudLayout = null;
        public AddonHudLayoutScreen* hudLayoutScreen = null;
        public AddonHudLayoutWindow* hudLayoutWindow = null;
        public float* hudScaleTable = null;

        public HUDLayoutManager(Plugin p)
        {
            _p = p;
        }

        public unsafe void Init()
        {
            var scanner = _p.pluginInterface.TargetModuleScanner;

            var showHUDLayoutAddress = scanner.ScanText("40 53 48 83 EC 20 48 8B 01 48 8B D9 FF 50 20 48 8B CB 84 C0 74 1A");
            var hideHUDLayoutAddress = scanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B D9 48 8B 49 10 48 8B 01 FF 50 40 48 8B C8");
            var atkUnitBaseSetPositionAddress = scanner.ScanText("4C 8B 89 ?? ?? ?? ?? 41 0F BF C0");
            var atkUnitBaseSetScaleAddress = scanner.ScanText("48 8B D1 45 0F B6 C8");
            var atkUnitBaseCalculateBoundsAddress = scanner.ScanText("E8 ?? ?? ?? ?? 0F B7 55 D7");
            var atkResNodeSetPositionAddress = scanner.ScanText("E8 ?? ?? ?? ?? 49 FF CC");
            var atkResNodeSetSizeAddress = scanner.ScanText("48 83 EC 38 48 85 C9 75 08");
            var atkComponentButtonToggleStateAddress = scanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 30 0F B6 FA 48 8B D9 84 D2");
            var addonHudLayoutScreenSetAddonScaleAddress = scanner.ScanText("E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? B2 01 48 8B 01 48 8B 5C 24 ?? 48 83 C4 30");
            var addonHudLayoutScreenEventAddonIsClickedAddress = scanner.ScanText("48 89 5C 24 ?? 41 56 48 83 EC 40 48 8B 81 ?? ?? ?? ??");
            var addonHudLayoutScreenEventAddonIsReleasedAddress = scanner.ScanText("E9 ?? ?? ?? ?? 48 83 BB ?? ?? ?? ?? ?? 0F 84 ?? ?? ?? ?? 48 89 6C 24 ?? 48 8B 6C 24 ??");
            var addonHudLayoutScreenEventAddonMovedAddress = scanner.ScanText("40 53 41 56 48 83 EC 38 48 83 B9 ?? ?? ?? ?? ??");

            this.hudScaleTable = (float*)scanner.GetStaticAddressFromSig("73 46 48 8D 0D ?? ?? ?? ??").ToPointer();

            this.atkUnitBaseSetPosition = Marshal.GetDelegateForFunctionPointer<AtkUnitBaseSetPositionDelegate>(atkUnitBaseSetPositionAddress);
            this.atkUnitBaseSetScale = Marshal.GetDelegateForFunctionPointer<AtkUnitBaseSetScaleDelegate>(atkUnitBaseSetScaleAddress);
            this.atkUnitBaseCalculateBounds = Marshal.GetDelegateForFunctionPointer<AtkUnitBaseCalculateBounds>(atkUnitBaseCalculateBoundsAddress);
            this.atkResNodeSetPosition = Marshal.GetDelegateForFunctionPointer<AtkResNodeSetPositionDelegate>(atkResNodeSetPositionAddress);
            this.atkResNodeSetSize = Marshal.GetDelegateForFunctionPointer<AtkResNodeSetSizeDelegate>(atkResNodeSetSizeAddress);
            this.atkComponentButtonToggleState = Marshal.GetDelegateForFunctionPointer<AtkComponentButtonToggleState>(atkComponentButtonToggleStateAddress);
            this.addonHudLayoutScreenSetAddonScale = Marshal.GetDelegateForFunctionPointer<AddonHudLayoutScreenSetAddonScale>(addonHudLayoutScreenSetAddonScaleAddress);
            this.addonHudLayoutScreenEventAddonIsClicked = Marshal.GetDelegateForFunctionPointer<AddonHudLayoutScreenEventAddonIsClickedDelegate>(addonHudLayoutScreenEventAddonIsClickedAddress);
            this.addonHudLayoutScreenEventAddonIsReleased = Marshal.GetDelegateForFunctionPointer<AddonHudLayoutScreenEventAddonIsReleasedDelegate>(addonHudLayoutScreenEventAddonIsReleasedAddress);
            this.addonHudLayoutScreenEventAddonMoved = Marshal.GetDelegateForFunctionPointer<AddonHudLayoutScreenEventAddonMovedDelegate>(addonHudLayoutScreenEventAddonMovedAddress);
            this.hookShowHUDLayout = new Hook<ShowHUDLayoutDelegate>(showHUDLayoutAddress, new ShowHUDLayoutDelegate(ShowHUDLayoutDetour), this);
            this.hookHideHUDLayout = new Hook<HideHUDLayoutDelegate>(hideHUDLayoutAddress, new HideHUDLayoutDelegate(HideHUDLayoutDetour), this);

            this.hookShowHUDLayout.Enable();
            this.hookHideHUDLayout.Enable();
        }

        private unsafe void ShowHUDLayoutDetour(void * agentHudLayout)
        {
            hookShowHUDLayout.Original(agentHudLayout);

            this.agentHudLayout = (AgentHudLayout*) agentHudLayout;

            _p.ui.IsVisible = true;

            var addon = _p.pluginInterface.Framework.Gui.GetAddonByName("_HudLayoutScreen", 1);
            if (addon != null)
                hudLayoutScreen = (AddonHudLayoutScreen*)addon.Address.ToPointer();

            addon = _p.pluginInterface.Framework.Gui.GetAddonByName("_HudLayoutWindow", 1);
            if (addon != null)
                hudLayoutWindow = (AddonHudLayoutWindow*)addon.Address.ToPointer();
        }

        private unsafe void HideHUDLayoutDetour(void * agentHudLayout)
        {
            _p.ui.IsVisible = false;

            hookHideHUDLayout.Original(agentHudLayout);

            hudLayoutScreen = null;
            hudLayoutWindow = null;
            this.agentHudLayout = null;
        }

        public unsafe AtkUnitBase * GetCurrentAddon()
        {
            if (hudLayoutScreen == null)
            {
                var addon = _p.pluginInterface.Framework.Gui.GetAddonByName("_HudLayoutScreen", 1);
                if (addon != null)
                    hudLayoutScreen = (AddonHudLayoutScreen*)addon.Address.ToPointer();
                else
                    return null;
            }

            if (hudLayoutWindow == null)
            {
                var addon = _p.pluginInterface.Framework.Gui.GetAddonByName("_HudLayoutWindow", 1);
                if (addon != null)
                    hudLayoutWindow = (AddonHudLayoutWindow*)addon.Address.ToPointer();
                else
                    return null;
            }

            if (hudLayoutScreen->SelectedAddon == null)
                return null;

            if (hudLayoutScreen->SelectedAddon->SelectedAtkUnit == null)
                return null;

            if (hudLayoutWindow->SaveButton == null)
                return null;

            return hudLayoutScreen->SelectedAddon->SelectedAtkUnit;
        }
 
        // this function simulates clicking the addon, dragging it to a new position, and releasing the mouseclick
        // this is the 'safest' way to set a direct position since it makes sure every flag the game expects to be set is set
        public void SetPositionGame(short X, short Y)
        {
            var mEvent = stackalloc MouseEvent[1];
            mEvent->unk = 0;
            mEvent->X = (short)hudLayoutScreen->SelectedOverlayNode->AtkResNode.X;
            mEvent->Y = (short)hudLayoutScreen->SelectedOverlayNode->AtkResNode.Y;
            addonHudLayoutScreenEventAddonIsClicked(hudLayoutScreen, 0, mEvent);
            mEvent->X = X;
            mEvent->Y = Y;
            addonHudLayoutScreenEventAddonMoved(hudLayoutScreen, mEvent);
            addonHudLayoutScreenEventAddonIsReleased(hudLayoutScreen);
        }

        public void SetScaleGame(float scale)
        {
            hudScaleTable[0] = scale;
            addonHudLayoutScreenSetAddonScale(hudLayoutScreen, hudLayoutScreen->SelectedAddon, 0);
            hudScaleTable[0] = 0.6F;
        }

        // unused/unsafe manual functions left here in case someone needs the knowledge
        public void SetPositionManual(short X, short Y)
        {
            if (hudLayoutScreen->SelectedAddon->XOffset != -1)
            {
                short adjustedX = (short)(X + hudLayoutScreen->SelectedAddon->XOffset * hudLayoutScreen->SelectedAddon->SelectedAtkUnit->Scale);
                short adjustedY = (short)(Y + hudLayoutScreen->SelectedAddon->YOffset * hudLayoutScreen->SelectedAddon->SelectedAtkUnit->Scale);
                atkResNodeSetPosition((AtkResNode*)hudLayoutScreen->SelectedOverlayNode, adjustedX, adjustedY);
            }
            else
            {
                atkResNodeSetPosition((AtkResNode*)hudLayoutScreen->SelectedOverlayNode, X, Y);
            }
            atkUnitBaseSetPosition(hudLayoutScreen->SelectedAddon->SelectedAtkUnit, X, Y);

            SetHasChanges();
        }

        // broken 
        public void SetScaleManual(float scale)
        {            
            short centerX = (short)(hudLayoutScreen->SelectedAddon->SelectedAtkUnit->X + ((hudLayoutScreen->SelectedAddon->SelectedAtkUnit->RootNode->Width * hudLayoutScreen->SelectedAddon->SelectedAtkUnit->Scale)/ 2));
            short centerY = (short)(hudLayoutScreen->SelectedAddon->SelectedAtkUnit->Y + ((hudLayoutScreen->SelectedAddon->SelectedAtkUnit->RootNode->Height * hudLayoutScreen->SelectedAddon->SelectedAtkUnit->Scale)/ 2));

            atkUnitBaseSetScale(hudLayoutScreen->SelectedAddon->SelectedAtkUnit, scale, true);

            ushort newWidth = 0;
            ushort newHeight = 0;

            short newX = 0;
            short newY = 0;

            if (hudLayoutScreen->SelectedAddon->XOffset != -1)
            {
                newWidth = (ushort)(hudLayoutScreen->SelectedAddon->OverlayWidth * hudLayoutScreen->SelectedAddon->SelectedAtkUnit->Scale);
                newHeight = (ushort)(hudLayoutScreen->SelectedAddon->OverlayHeight * hudLayoutScreen->SelectedAddon->SelectedAtkUnit->Scale);

                newX = (short)(hudLayoutScreen->SelectedAddon->SelectedAtkUnit->X + hudLayoutScreen->SelectedAddon->XOffset * hudLayoutScreen->SelectedAddon->SelectedAtkUnit->Scale);
                newY = (short)(hudLayoutScreen->SelectedAddon->SelectedAtkUnit->Y + hudLayoutScreen->SelectedAddon->YOffset * hudLayoutScreen->SelectedAddon->SelectedAtkUnit->Scale);
            }
            else
            {
                if ((hudLayoutScreen->SelectedAddon->Flags & 0x200) == 0x200)
                {
                    // you may be thinking "what the fuck" but this is what the game code does so whatever
                    newWidth = hudLayoutScreen->SelectedOverlayNode->Component->OwnerNode->AtkResNode.Width;
                    newHeight = hudLayoutScreen->SelectedOverlayNode->Component->OwnerNode->AtkResNode.Height;


                }
                else
                {
                    var bounds = stackalloc CalculateBoundsResult[1];
                    atkUnitBaseCalculateBounds(hudLayoutScreen->SelectedAddon->SelectedAtkUnit, bounds);

                    newWidth = (ushort)(bounds->right - bounds->left);
                    newHeight = (ushort)(bounds->bottom - bounds->top);

                }

                newX = (short)(centerX - (newWidth / 2));
                newY = (short)(centerY - (newHeight / 2));

                atkUnitBaseSetPosition(hudLayoutScreen->SelectedAddon->SelectedAtkUnit, newX, newY);
            }

            atkResNodeSetSize((AtkResNode*)hudLayoutScreen->SelectedOverlayNode, newWidth, newHeight);
            atkResNodeSetPosition((AtkResNode*)hudLayoutScreen->SelectedOverlayNode, newX, newY);

            SetHasChanges();
        }

        public void GetBounds(CalculateBoundsResult * bounds)
        {
            atkUnitBaseCalculateBounds(hudLayoutScreen->SelectedAddon->SelectedAtkUnit, bounds);
        }

        private void SetHasChanges()
        {
            hudLayoutScreen->SelectedAddon->PositionHasChanged = 1;

            atkComponentButtonToggleState(hudLayoutWindow->SaveButton, true);

            this.agentHudLayout->NeedToSave = true;
        }

        public void HideCurrentOverlay()
        {
            hudLayoutScreen->SelectedOverlayNode->AtkResNode.Flags &= ~0x10;
        }

        public void ShowCurrentOverlay()
        {
            hudLayoutScreen->SelectedOverlayNode->AtkResNode.Flags |= 0x10;
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.hookShowHUDLayout.Disable();
                    this.hookHideHUDLayout.Disable();
                    this.hookShowHUDLayout.Dispose();
                    this.hookHideHUDLayout.Dispose();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
