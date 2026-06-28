/*
ImageGlass - A Fast, Seamless Photo Viewer
Copyright (C) 2010 - 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using Avalonia.Input;
using ImageGlass.Common.Actions;
using ImageGlass.Common.AppThemes;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using ImageGlass.Tools;
using ImageGlass.UI;
using ImageGlass.UI.Viewer;
using ImageGlass.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MKeys = Avalonia.Input.KeyModifiers;

namespace ImageGlass.Common.ServiceProviders;

public partial class AppAPIProvider
{
    private Hotkey? _lastHotkeyPressed = null;
    private InterlockedBool _isQuickBrowsingPhotos = new(false);

    /// <summary>
    /// Gets a value indicating whether the user is holding a navigation key
    /// for rapid browsing (quick browse mode).
    /// </summary>
    public bool IsQuickBrowsing => _isQuickBrowsingPhotos;


    // list of all menu items & default action, hotkeys
    private static IReadOnlyCollection<HotkeySingleAction> _defaultMenuList => [
        new(LangId.FrmMain_MnuMain,                 API.IG_OpenMainMenu,        MKeys.Alt, Key.F),

        // File
        new(LangId.FrmMain_MnuOpenFile,             API.IG_OpenFile,            Hotkey.Ctrl, Key.O),
        new(LangId.FrmMain_MnuNewWindow,            API.IG_NewWindow,           Hotkey.Ctrl, Key.N),
        new(LangId.FrmMain_MnuSave,                 API.IG_Save,                Hotkey.Ctrl, Key.S),
        new(LangId.FrmMain_MnuSaveAs,               API.IG_SaveAs,              Hotkey.Ctrl | MKeys.Shift, Key.S),
        new(LangId.FrmMain_MnuExportFrames,         API.IG_ExportImageFrames,   Hotkey.Ctrl, Key.J),
        new(LangId.FrmMain_MnuPrint,                API.IG_Print,               Hotkey.Ctrl, Key.P),
        new(LangId.FrmMain_MnuOpenWith,             API.IG_OpenWith,            Key.D),
        new(LangId.FrmMain_MnuShare,                API.IG_Share,               Key.S),
        new(LangId.FrmMain_MnuOpenLocation,         API.IG_OpenLocation,        Key.L),
        new(LangId.FrmMain_MnuRename,               API.IG_Rename,              Key.F2),
        new(LangId.FrmMain_MnuMoveToRecycleBin,     API.IG_Delete, "true",      [new(Hotkey.Delete)]),
        new(LangId.FrmMain_MnuDeleteFromHardDisk,   API.IG_Delete, "false",     [new(MKeys.Shift, Hotkey.Delete)]),
        new(LangId.FrmMain_MnuImageProperties,      API.IG_OpenProperties,      MKeys.Alt, Key.Enter),


        // Navigation
        new(LangId.FrmMain_MnuViewNext,             API.IG_ViewNext,            Key.Right),
        new(LangId.FrmMain_MnuViewPrevious,         API.IG_ViewPrevious,        Key.Left),
        new(LangId.FrmMain_MnuGoTo,                 API.IG_Goto,                Key.F),
        new(LangId.FrmMain_MnuGoToFirst,            API.IG_GotoFirst,           Key.Home),
        new(LangId.FrmMain_MnuGoToLast,             API.IG_GotoLast,            Key.End),
        new(LangId.FrmMain_MnuViewNextFrame,        API.IG_ViewNextFrame,       Hotkey.Ctrl, Key.Right),
        new(LangId.FrmMain_MnuViewPreviousFrame,    API.IG_ViewPreviousFrame,   Hotkey.Ctrl, Key.Left),
        new(LangId.FrmMain_MnuViewFirstFrame,       API.IG_ViewFirstFrame,      Hotkey.Ctrl, Key.Up),
        new(LangId.FrmMain_MnuViewLastFrame,        API.IG_ViewLastFrame,       Hotkey.Ctrl, Key.Down),


        // Zoom
        new(LangId.FrmMain_MnuCustomZoom,       API.IG_CustomZoom,          Key.Z),
        new(LangId.FrmMain_MnuActualSize,       API.IG_SetZoom, "1",        [new(Key.D0), new(Key.NumPad0)]),
        new(LangId.FrmMain_MnuZoomIn,           API.IG_ZoomIn,              Key.Add),
        new(LangId.FrmMain_MnuZoomOut,          API.IG_ZoomOut,             Key.Subtract),
        new(LangId.FrmMain_MnuAutoZoom,         API.IG_SetZoomMode, nameof(ZoomMode.AutoZoom),      [new(Key.D1), new(Key.NumPad1)]),
        new(LangId.FrmMain_MnuLockZoom,         API.IG_SetZoomMode, nameof(ZoomMode.LockZoom),      [new(Key.D2), new(Key.NumPad2)]),
        new(LangId.FrmMain_MnuScaleToWidth,     API.IG_SetZoomMode, nameof(ZoomMode.ScaleToWidth),  [new(Key.D3), new(Key.NumPad3)]),
        new(LangId.FrmMain_MnuScaleToHeight,    API.IG_SetZoomMode, nameof(ZoomMode.ScaleToHeight), [new(Key.D4), new(Key.NumPad4)]),
        new(LangId.FrmMain_MnuScaleToFit,       API.IG_SetZoomMode, nameof(ZoomMode.ScaleToFit),    [new(Key.D5), new(Key.NumPad5)]),
        new(LangId.FrmMain_MnuScaleToFill,      API.IG_SetZoomMode, nameof(ZoomMode.ScaleToFill),   [new(Key.D6), new(Key.NumPad6)]),


        // Panning
        new(LangId.FrmMain_MnuPanLeft,          API.IG_PanLeft,             [new(MKeys.Alt, Key.Left)]),
        new(LangId.FrmMain_MnuPanRight,         API.IG_PanRight,            [new(MKeys.Alt, Key.Right)]),
        new(LangId.FrmMain_MnuPanUp,            API.IG_PanUp,               [new(MKeys.Alt, Key.Up)]),
        new(LangId.FrmMain_MnuPanDown,          API.IG_PanDown,             [new(MKeys.Alt, Key.Down)]),
        new(LangId.FrmMain_MnuPanToLeftSide,    API.IG_PanToLeft,           [new(Hotkey.Ctrl | MKeys.Alt, Key.Left)]),
        new(LangId.FrmMain_MnuPanToRightSide,   API.IG_PanToRight,          [new(Hotkey.Ctrl | MKeys.Alt, Key.Right)]),
        new(LangId.FrmMain_MnuPanToTop,         API.IG_PanToTop,            [new(Hotkey.Ctrl | MKeys.Alt, Key.Up)]),
        new(LangId.FrmMain_MnuPanToBottom,      API.IG_PanToBottom,         [new(Hotkey.Ctrl | MKeys.Alt, Key.Down)]),


        // Image
        new(LangId.FrmMain_MnuRefresh,              API.IG_Refresh,             Key.R),
        new(LangId.FrmMain_MnuReload,               API.IG_Reload,              Hotkey.Ctrl, Key.R),
        new(LangId.FrmMain_MnuReloadImageList,      API.IG_ReloadList,          Hotkey.Ctrl | MKeys.Shift, Key.R),
        new(LangId.FrmMain_MnuUnload,               API.IG_Unload,              Key.U),
        new(LangId.FrmSettings_EnableExplorerSortOrder,  API.IG_ToggleExplorerSortOrder),
        new(LangId.ImageOrderBy_Name,               API.IG_ToggleLoadingOrderBy,    nameof(ImageOrderBy.Name),         [new(Hotkey.Ctrl | MKeys.Alt, Key.A)]),
        new(LangId.ImageOrderBy_Random,             API.IG_SetLoadingOrderBy,       nameof(ImageOrderBy.Random)),
        new(LangId.ImageOrderBy_FileSize,           API.IG_SetLoadingOrderBy,       nameof(ImageOrderBy.FileSize)),
        new(LangId.ImageOrderBy_Extension,          API.IG_SetLoadingOrderBy,       nameof(ImageOrderBy.Extension)),
        new(LangId.ImageOrderBy_DateCreated,        API.IG_SetLoadingOrderBy,       nameof(ImageOrderBy.DateCreated)),
        new(LangId.ImageOrderBy_DateAccessed,       API.IG_SetLoadingOrderBy,       nameof(ImageOrderBy.DateAccessed)),
        new(LangId.ImageOrderBy_DateModified,       API.IG_ToggleLoadingOrderBy,    nameof(ImageOrderBy.DateModified), [new(Hotkey.Ctrl | MKeys.Alt, Key.D)]),
        new(LangId.ImageOrderBy_ExifDateTaken,      API.IG_SetLoadingOrderBy,       nameof(ImageOrderBy.ExifDateTaken)),
        new(LangId.ImageOrderBy_ExifRating,         API.IG_SetLoadingOrderBy,       nameof(ImageOrderBy.ExifRating)),
        new(LangId.ImageOrderType_Asc,              API.IG_SetLoadingOrderType,     nameof(ImageOrderType.Asc)),
        new(LangId.ImageOrderType_Desc,             API.IG_SetLoadingOrderType,     nameof(ImageOrderType.Desc)),
        new(LangId.FrmMain_MnuEdit,                 API.IG_OpenEditingApp,          Key.E),
        new(LangId.FrmMain_MnuInvertColors,         API.IG_InvertColors,            Hotkey.Ctrl, Key.I),
        new(LangId.FrmMain_MnuToggleImageAnimation, API.IG_ToggleImageAnimation,    Hotkey.Ctrl, Key.Space),
        new(LangId.FrmMain_MnuRotateLeft,           API.IG_Rotate,              nameof(RotateOption.Left),          [new(Hotkey.Ctrl, Key.OemPeriod)]),
        new(LangId.FrmMain_MnuRotateRight,          API.IG_Rotate,              nameof(RotateOption.Right),         [new(Hotkey.Ctrl, Key.OemQuestion)]),
        new(LangId.FrmMain_MnuFlipHorizontal,       API.IG_FlipImage,           nameof(FlipOptions.Horizontal),     [new(Hotkey.Ctrl, Key.OemSemicolon)]),
        new(LangId.FrmMain_MnuFlipVertical,         API.IG_FlipImage,           nameof(FlipOptions.Vertical),       [new(Hotkey.Ctrl, Key.OemQuotes)]),
        new(LangId.FrmMain_MnuSetDesktopBackground, API.IG_SetDesktopBackground),
        new(LangId.FrmMain_MnuSetLockScreen,        API.IG_SetLockScreenImage),


        // Clipboard
        new(LangId.FrmMain_MnuPasteImage,       API.IG_PasteImage,          Hotkey.Ctrl, Key.V),
        new(LangId.FrmMain_MnuCopyImagePixels,  API.IG_CopyImagePixels,     Hotkey.Ctrl | MKeys.Shift, Key.C),
        new(LangId.FrmMain_MnuCopyFile,         API.IG_CopyFiles,           Hotkey.Ctrl, Key.C),
        new(LangId.FrmMain_MnuCutFile,          API.IG_CutFiles,            Hotkey.Ctrl, Key.X),
        new(LangId.FrmMain_MnuCopyPath,         API.IG_CopyImagePath,       Hotkey.Ctrl, Key.L),
        new(LangId.FrmMain_MnuClearClipboard,   API.IG_ClearClipboard,      Hotkey.Ctrl, Key.OemTilde),


        // Window modes
        new(LangId.FrmMain_MnuWindowFit,            API.IG_ToggleWindowFit,             Key.F9),
        new(LangId.FrmMain_MnuFrameless,            API.IG_ToggleFrameless,             Key.F10),
        new(LangId.FrmMain_MnuFullScreen,           API.IG_ToggleFullScreen,            Key.F11),
        new(LangId.FrmMain_MnuSlideshow,            API.IG_ToggleSlideshow,             Key.F12),
        new(LangId.FrmSlideshow_MnuPauseResumeSlideshow,    API.IG_ToggleSlideshowPlayback,     Key.Space),


        // Layout
        new(LangId.FrmMain_MnuToggleTopMost,            API.IG_ToggleWindowTopMost),
        new(LangId.FrmMain_MnuToggleToolbar,            API.IG_ToggleToolbar,           Key.T),
        new(LangId.FrmMain_MnuToggleGallery,            API.IG_ToggleGallery,           Key.G),
        new(LangId.FrmMain_MnuToggleCheckerboard,       API.IG_ToggleCheckerboard,      Key.B),
        new(LangId.FrmMain_MnuChangeBackgroundColor,    API.IG_SetBackgroundColor,      Key.M),


        // Plugins
        new(LangId.FrmMain_MnuColorPicker,          API.IG_ToggleTool, ColorPickerToolControl.TOOL_ID,    [new(MKeys.Alt, Key.K)]),
        new(LangId.FrmMain_MnuCropTool,             API.IG_ToggleTool, CropImageToolControl.TOOL_ID,      [new(Hotkey.Ctrl | MKeys.Alt, Key.C)]),
        new(LangId.FrmMain_MnuFrameNav,             API.IG_ToggleTool, FrameNavToolControl.TOOL_ID,       [new(MKeys.Alt, Key.P)]),
        new(LangId.FrmMain_MnuResizeTool,           API.IG_OpenTool, ImageResizerTool.TOOL_ID,            [new(MKeys.Alt, Key.R)]),
        new(LangId.FrmMain_MnuLosslessCompression,  API.IG_OpenTool, LosslessCompressionTool.TOOL_ID,     [new(MKeys.Alt, Key.S)]),
        new(LangId.FrmMain_MnuGetMoreTools,         API.IG_GetMoreTools),


        // Settings
        new(LangId.FrmMain_MnuSettings,                     API.IG_OpenSettings,    Hotkey.Ctrl, Key.OemComma),


        // Help
        new(LangId.FrmMain_MnuAbout,                        API.IG_OpenAboutWindow, Key.F1),
        new(LangId._CheckForUpdate,                         API.IG_CheckForUpdate),
        new(LangId.FrmMain_MnuReportIssue,                  API.IG_ReportIssue),
        new(LangId.FrmMain_MnuSetDefaultPhotoViewer,        API.IG_SetDefaultPhotoViewer),
        new(LangId.FrmMain_MnuRemoveDefaultPhotoViewer,     API.IG_RemoveDefaultPhotoViewer),


        // Exit
        new(LangId.FrmMain_MnuExit,                         API.IG_Exit,            [new(Key.Escape), new(Hotkey.Ctrl, Key.W)]),
    ];


    // a map of menu and its action.
    private static Dictionary<LangId, HotkeySingleAction> _menuMap { get; set; }
        = new(_defaultMenuList.Select(ac => new KeyValuePair<LangId, HotkeySingleAction>(Lang.GetKey(ac.LangKey)!.Value, ac)));


    /// <summary>
    /// Gets the map of hotkeys (string) and actions.
    /// </summary>
    public static Dictionary<string, SingleAction> AppHotkeysMap { get; private set; } = new();



    /// <summary>
    /// Registers app's hotkeys
    /// </summary>
    public void RegisterHotkeys()
    {
        // 0. load main menu button hotkey text
        var mainMenuHotkeys = Core.Config.MenuHotkeys.GetValueOrDefault(LangId.FrmMain_MnuMain)
            ?? _menuMap.GetValueOrDefault(LangId.FrmMain_MnuMain)?.Hotkeys
            ?? [];
        var mainMenuHotkeyText = string.Join(", ", mainMenuHotkeys.Select(hk => hk.KeyString));

        // Use the shared static toolbar VM directly
        MainWindowViewModel.ToolbarVM.ButtonMenuVM = new ToolbarItemModel()
        {
            Image = nameof(IgThemeIcon.MainMenu),
            Text = nameof(LangId.FrmMain_MnuMain),
            HotkeyText = mainMenuHotkeyText,
        };


        // 1. register toolbar hotkeys from user-config
        foreach (var item in Core.Config.ToolbarButtons)
        {
            if (item.IsSeparator || item.OnClick is null) continue;

            // 1.1 save the button text
            if (string.IsNullOrWhiteSpace(item.OnClick.LangKey))
            {
                item.OnClick.LangKey = item.Text;
            }


            // 1.2 get hotkey text
            var hotkeyTextList = item.OnClick.Hotkeys.Select(hk => hk.KeyString) ?? [];
            if (!hotkeyTextList.Any())
            {
                var langKey = Lang.GetKey(item.OnClick.LangKey);
                if (langKey is not null)
                {
                    // try get menu hotkeys from user-config
                    var menuHotkeys = Core.Config.MenuHotkeys.GetValueOrDefault(langKey.Value) ?? [];
                    if (menuHotkeys.Length == 0)
                    {
                        // try get default menu hotkeys
                        var menuAction = _menuMap.GetValueOrDefault(langKey.Value);
                        menuHotkeys = menuAction?.Hotkeys ?? [];
                    }

                    hotkeyTextList = menuHotkeys.Select(hk => hk.KeyString) ?? [];
                }
            }
            item.HotkeyText = string.Join(", ", hotkeyTextList);


            // 1.3 register custom hotkey
            foreach (var hk in item.OnClick.Hotkeys)
            {
                // save custom hotkey to the map
                _ = AppHotkeysMap.TryAdd(hk.KeyString, item.OnClick);
            }
        }


        // 2. load menu hotkeys from user-config
        foreach (var item in Core.Config.MenuHotkeys)
        {
            if (_menuMap.TryGetValue(item.Key, out var action))
            {
                action.Hotkeys = item.Value;
            }
        }


        // 3. register hotkeys of menu
        foreach (var item in _menuMap)
        {
            foreach (var hk in item.Value.Hotkeys)
            {
                // save to the maps
                AppHotkeysMap.TryAdd(hk.KeyString, item.Value);
            }
        }


        // 4. register hotkeys of external tools (Config.Tools)
        foreach (var tool in Core.Config.Tools)
        {
            if (tool.Hotkeys.Length == 0) continue;

            // build a single action that opens the tool by its ToolId
            var toolAction = new SingleAction(API.IG_OpenTool, tool.ToolId);

            foreach (var hk in tool.Hotkeys)
            {
                if (hk is null) continue;
                AppHotkeysMap.TryAdd(hk.KeyString, toolAction);
            }
        }
    }


    /// <summary>
    /// Handles keydown event.
    /// </summary>
    public async Task HandleKeyDownAsync(KeyEventArgs e)
    {
        // 1. get hotkey action
        var hotkey = new Hotkey(e);
        var action = AppHotkeysMap.GetValueOrDefault(hotkey.KeyString);
        if (action is null) return;

        // Check if the action is locked - silently ignore locked hotkeys
        if (FeatureManager.IsLocked(action.Executable))
        {
            e.Handled = true;
            return;
        }

        var isHotkeyPressMultiTimes = hotkey.IsSame(_lastHotkeyPressed);
        var executable = action.Executable ?? string.Empty;

        // save the last hotkey pressed
        _lastHotkeyPressed = hotkey;

        // mark the hotkey handled
        e.Handled = action != null;


        // 2. handle special hotkeys
        // 2.1 animate Zoom In/Out
        if (executable == nameof(API.IG_ZoomIn))
        {
            if (Viewer.ZoomLevels.Length == 0)
            {
                Viewer.StartDrawingAnimation(AnimationSources.ZoomIn);
                return;
            }
        }
        else if (executable == nameof(API.IG_ZoomOut))
        {
            if (Viewer.ZoomLevels.Length == 0)
            {
                Viewer.StartDrawingAnimation(AnimationSources.ZoomOut);
                return;
            }
        }

        // 2.2 animate Panning
        if (executable == nameof(API.IG_PanLeft))
        {
            Viewer.StartDrawingAnimation(AnimationSources.PanLeft);
            return;
        }
        if (executable == nameof(API.IG_PanRight))
        {
            Viewer.StartDrawingAnimation(AnimationSources.PanRight);
            return;
        }
        if (executable == nameof(API.IG_PanUp))
        {
            Viewer.StartDrawingAnimation(AnimationSources.PanUp);
            return;
        }
        if (executable == nameof(API.IG_PanDown))
        {
            Viewer.StartDrawingAnimation(AnimationSources.PanDown);
            return;
        }


        // 2.3. quick browsing, only load photo preview
        var isBrowsingAction = executable.Equals(nameof(API.IG_ViewByStep), StringComparison.Ordinal)
            || executable.Equals(nameof(API.IG_ViewNext), StringComparison.Ordinal)
            || executable.Equals(nameof(API.IG_ViewPrevious), StringComparison.Ordinal);
        _isQuickBrowsingPhotos.Set(isBrowsingAction && isHotkeyPressMultiTimes);
        if (_isQuickBrowsingPhotos)
        {
            Viewer.ShouldLoadFullResolution.SetFalse();
        }


        // 3. run the action
        await RunActionAsync(action);
    }


    /// <summary>
    /// Handles keyup event.
    /// </summary>
    public async Task HandleKeyUpAsync(KeyEventArgs e)
    {
        // 1. clear early to prevent a subsequent key press from being
        // falsely detected as quick browsing while we await below
        _lastHotkeyPressed = null;


        // 2. stop animation source
        // 2.1 Zoom In/Out
        if (Viewer.AnimationSource.HasFlag(AnimationSources.ZoomIn))
        {
            Viewer.StopDrawingAnimation(AnimationSources.ZoomIn);
            return;
        }
        if (Viewer.AnimationSource.HasFlag(AnimationSources.ZoomOut))
        {
            Viewer.StopDrawingAnimation(AnimationSources.ZoomOut);
            return;
        }

        // 2.2 Panning
        if (Viewer.AnimationSource.HasFlag(AnimationSources.PanLeft))
        {
            Viewer.StopDrawingAnimation(AnimationSources.PanLeft);
            return;
        }
        if (Viewer.AnimationSource.HasFlag(AnimationSources.PanRight))
        {
            Viewer.StopDrawingAnimation(AnimationSources.PanRight);
            return;
        }
        if (Viewer.AnimationSource.HasFlag(AnimationSources.PanUp))
        {
            Viewer.StopDrawingAnimation(AnimationSources.PanUp);
            return;
        }
        if (Viewer.AnimationSource.HasFlag(AnimationSources.PanDown))
        {
            Viewer.StopDrawingAnimation(AnimationSources.PanDown);
            return;
        }


        // 3. handle quick browsing end: start loading full resolution
        if (_isQuickBrowsingPhotos)
        {
            _isQuickBrowsingPhotos.SetFalse();
            Viewer.Photo?.CancelLoading();
            Viewer.ShouldLoadFullResolution.SetTrue();

            await Task.Delay(50);
            await Viewer.LoadPhotoAsync(false, true);

            // start caching adjacent photos now that quick browsing ended
            Core.Photos.RequestCacheAround(Core.Photos.CurrentIndex);
        }
    }


    /// <summary>
    /// Gets the menu action.
    /// </summary>
    public static HotkeySingleAction? GetMenuAction(LangId? langKey)
    {
        if (langKey is null) return null;

        var action = _menuMap.GetValueOrDefault(langKey.Value);

        return action;
    }


    /// <summary>
    /// Gets the hotkey text of menu.
    /// </summary>
    public static string GetMenuHotkeyText(LangId? langKey)
    {
        var action = GetMenuAction(langKey);
        if (action is null) return string.Empty;

        var hotkeyText = String.Join(", ", action.Hotkeys);
        return hotkeyText;
    }


}
