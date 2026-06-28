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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ImageGlass.Common.ServiceProviders;
using ImageGlass.Common.Types;
using ImageGlass.Tools;
using ImageGlass.UI;
using ImageGlass.UI.Windowing;
using ImageGlass.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGlass.Common.Windows;


public partial class MainWindow : PhWindow
{
    private readonly AppStatusInfo _status;
    private bool _isClosingHandled; // to handle closing for saving configs

    public MainWindowModel VM => (MainWindowModel)DataContext!;


    public MainWindow()
    {
        InitializeComponent();
        _status = new AppStatusInfo(PART_MainView.PART_Viewer);


        // load window size & position
        Width = Core.Config.MainWindowBounds.Width;
        Height = Core.Config.MainWindowBounds.Height;
        Position = new((int)Core.Config.MainWindowBounds.X, (int)Core.Config.MainWindowBounds.Y);

        if (!Core.Config.EnableWindowFit)
        {
            // load window state
            if (Core.Config.EnableMainWindowMaximized) WindowState = WindowState.Maximized;
        }

        // set zoom lock
        if (Core.Config.ZoomMode == UI.Viewer.ZoomMode.LockZoom)
        {
            PART_MainView.PART_Viewer.ZoomFactor = Core.Config.ZoomLockValue / 100f;
        }

        // events
        Core.AppInstance.InstanceInvoked += AppInstance_InstanceInvoked;
    }



    #region Override Methods


    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (Core.Config.EnableWindowFit)
        {
            // load Window fit
            _ = await Core.API.RunApiAsync(API.IG_ToggleWindowFit, "true");
        }
        else
        {
            // load full screen
            if (Core.Config.EnableFullScreen) WindowState = WindowState.FullScreen;
        }

        // load color profile
        Core.UpdateDestColorProfile();

        // restore last opened tool
        _ = await Core.API.RunApiAsync(API.IG_OpenTool, Core.Config.LastOpenedTool);
    }


    protected override async void OnLoaded(RoutedEventArgs e)
    {
        // check if the config loading was failed
        if (Config.LoadingException is not null)
        {
            var isContinue = await ModalWindow.ShowUnhandledErrorAsync(
                Config.LoadingException, this,
                "IGE: There was an error while loading user settings");
            if (!isContinue) return;
        }

        base.OnLoaded(e);

        // register app hotkeys
        Core.API.RegisterHotkeys();

        // build the macOS menu bar (window-level) from the existing main menu;
        // the application (⌘) menu is defined in App.axaml
        if (OperatingSystem.IsMacOS())
        {
            NativeMenu.SetMenu(this, PART_MainView.PART_Toolbar.BuildNativeWindowMenu());
        }

        // control events
        _status.Changed += Status_Changed;
        PART_MainView.PART_Toolbar.ItemClicked += PART_Toolbar_ItemClicked;
        PART_MainView.PART_Gallery.ItemClicked += PART_Gallery_ItemClicked;
    }


    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        // allow the close through on the second pass after async work is done
        if (_isClosingHandled)
        {
            base.OnClosing(e);
            return;
        }

        // cancel the close so async work can complete before the app tears down
        e.Cancel = true;
        _isClosingHandled = true;

        // control events
        _status.Changed -= Status_Changed;
        _status.Dispose();

        PART_MainView.PART_Toolbar.ItemClicked -= PART_Toolbar_ItemClicked;
        PART_MainView.PART_Gallery.ItemClicked -= PART_Gallery_ItemClicked;


        // stop slideshow so pre-slideshow config values are restored before saving
        _ = await Core.API.RunApiAsync(API.IG_ToggleSlideshow, "false");

        // stop all external tool processes before saving config
        await Core.ToolRegistry.ExternalTools.StopAllAsync();

        // only save config here, do NOT dispose resources yet
        await SaveConfigOnClosingAsync();

        // now close for real — _isClosingHandled lets the second pass through
        Close();
    }


    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Dispose after the window (and its render loop) is fully closed
        Core.Dispose();
    }


    protected override async void OnKeyDown(KeyEventArgs e)
    {
        // Let text input controls handle keys normally via base first.
        if (e.Source is TextBox
            or NumericUpDown
            or MaskedTextBox
            or AutoCompleteBox)
        {
            base.OnKeyDown(e);
            return;
        }

        // Run app hotkeys BEFORE base so Alt+key combos (WM_SYSKEYDOWN) are not
        // swallowed by Avalonia's system-key handling (e.g. Alt+C for crop).
        var hk = new Hotkey(e);
        if (hk.IsSame(Key.Escape) && Core.Slideshow?.IsRunning == true)
        {
            _ = await Core.API.RunApiAsync(API.IG_ToggleSlideshow, "false");
            e.Handled = true;
            return;
        }

        await Core.API.HandleKeyDownAsync(e);
        if (e.Handled) return;

        base.OnKeyDown(e);
    }


    protected override async void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Handled) return;

        // process app hotkeys
        await Core.API.HandleKeyUpAsync(e);
        if (e.Handled) return;
    }


    #endregion // Override Methods



    #region Control Events

    private async void AppInstance_InstanceInvoked(AppInstance sender, InstanceInvokedEventArgs e)
    {
        // handle single instance command
        if (e.Command.Equals(ExeParams.SINGLE_INSTANCE))
        {
            if (WindowState == Avalonia.Controls.WindowState.Minimized)
            {
                WindowState = Avalonia.Controls.WindowState.Normal;
            }

            // set instance arguments
            var modulePath = Core.Args.ElementAtOrDefault(0) ?? string.Empty;
            Core.Args = [modulePath, .. e.Arguments];
            Core.UpdateInitImagePath();

            // apply any -p: config overrides from the forwarded args
            Config.ApplyCliOverrides(Core.Config, e.Arguments);

            // Refresh lock manager after CLI overrides
            ServiceProviders.FeatureManager.Refresh();

            // load image path
            _ = await Core.API.RunApiAsync(API.IG_OpenPath, Core.InputImagePathFromArgs);

            Activate();
            Topmost = true;
            Topmost = Core.Config.EnableWindowTopMost;
        }
    }


    private void Status_Changed(object? sender, EventArgs e)
    {
        VM.Title = _status.Text;
    }


    private void PART_Toolbar_ItemClicked(object sender, ToolbarItemClickEventArgs e)
    {
        _ = Core.API.RunActionAsync(e.VM.OnClick);
    }


    private async void PART_Gallery_ItemClicked(GalleryItem sender, GalleryItemClickEventArgs e)
    {
        var photoIndex = Core.Photos.IndexOf(sender.VM.FilePath);
        _ = await Core.API.RunApiAsync(API.IG_ViewByIndex, photoIndex.ToString());
    }


    #endregion Control Events




    private async Task SaveConfigOnClosingAsync()
    {
        // 1. save window maximized state
        Core.Config.EnableMainWindowMaximized = WindowState == Avalonia.Controls.WindowState.Maximized;
        Core.Config.EnableFullScreen = WindowState == WindowState.FullScreen;

        // 2. save window bounds
        if (WindowState == Avalonia.Controls.WindowState.Normal)
        {
            var size = ClientSize;
            Core.Config.MainWindowBounds = new(Position.X, Position.Y,
                (int)size.Width,
                (int)size.Height);
        }


        // fullscreen mode: use the backup value
        if (Core.Config.EnableFullScreen)
        {
            // TODO:
            //Core.Config.ShowToolbar = _showToolbar;
            //Core.Config.ShowGallery = _showGallery;
        }


        Core.Config.LastSeenImagePath = Core.Photos.CurrentFilePath;
        Core.Config.ZoomLockValue = PART_MainView.PART_Viewer.ZoomFactor * 100f;

        // save current tool setting if it's open
        var currentToolId = PART_MainView.PART_ToolHost.Tool?.ToolId ?? string.Empty;
        var currentTool = Core.ToolRegistry.Get(currentToolId);
        Core.Config.LastOpenedTool = (currentTool is not null && !currentTool.IsHosted)
            ? string.Empty
            : currentToolId;

        // save settings for the current hosted tool
        if (PART_MainView.PART_ToolHost.Tool is ITool toolToSave)
        {
            _ = await Core.API.RunApiAsync(API.IG_CloseTool, toolToSave.ToolId);
        }


        // hide the window
        Hide();

        // save config to file
        var taskConfig = Core.Config.SaveAsync();


        // permanently adds the data that is on the Clipboard so that it is available
        // after the data's original application closes.
        if (Clipboard is not null)
        {
            try
            {
                await Clipboard.FlushAsync();
            }
            catch { }
        }

        await taskConfig;

        // cleaning
        try
        {
            // delete trash
            var tempDir = BHelper.ConfigDir(Dir.Temporary);
            Directory.Delete(tempDir, true);
        }
        catch { }
    }



}