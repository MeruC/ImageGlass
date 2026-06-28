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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using ImageGlass.Common.Windows;
using ImageGlass.Tools;
using ImageGlass.UI;
using ImageGlass.UI.Viewer;
using ImageGlass.UI.Windowing;
using ImageGlass.Windows;
using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGlass.Common.ServiceProviders;

public partial class AppAPIProvider
{
    private MainWindow _mainWindow;

    // wallpaper formats
    private static FrozenSet<string> _desktopNativeFormats => [".bmp", ".jpg", ".jpeg", ".png", ".gif"];

    // variable to back up / restore window layout when changing window mode
    private bool _isFramelessBeforeFullscreen;
    private bool _isWindowFitBeforeFullscreen;
    private bool _showToolbar = true;
    private bool _showGallery = true;
    private Rect _windowBound;
    private bool _windowMaximized = false;

    // slideshow state backup
    private bool _isFullScreenBeforeSlideshow;
    private bool _isFramelessBeforeSlideshow;
    private bool _isWindowFitBeforeSlideshow;
    private bool _showToolbarBeforeSlideshow = true;
    private bool _showGalleryBeforeSlideshow = true;
    private Rect _windowBoundBeforeSlideshow;
    private bool _windowMaximizedBeforeSlideshow;
    private DispatcherTimer? _slideshowCountdownTimer;
    private bool _slideshowIsAdvancing;


    private ViewerControl Viewer => _mainWindow.PART_MainView.PART_Viewer;
    private ToolbarControl Toolbar => _mainWindow.PART_MainView.PART_Toolbar;
    private GalleryControl Gallery => _mainWindow.PART_MainView.PART_Gallery;
    private PhGridSplitter GalleryResizer => _mainWindow.PART_MainView.PART_GalleryResizer;
    private MessageControl Message => _mainWindow.PART_MainView.PART_Message;
    private ToolHostControl ToolHost => _mainWindow.PART_MainView.PART_ToolHost;
    private SlideshowCountdownOverlay SlideshowCountdown => _mainWindow.PART_MainView.PART_SlideshowCountdown;


    /// <summary>
    /// Gets the <see cref="ViewerControl"/> for external plugin access.
    /// </summary>
    internal ViewerControl? GetViewer() => Viewer;


    public AppAPIProvider(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;

        // Register built-in hosted plugins (via ToolControlAdapter)
        Core.ToolRegistry.Register(ColorPickerToolControl.TOOL_ID,
            new ToolControlAdapter(ColorPickerToolControl.TOOL_ID, v => new ColorPickerToolControl { Viewer = v }));
        Core.ToolRegistry.Register(CropImageToolControl.TOOL_ID,
            new ToolControlAdapter(CropImageToolControl.TOOL_ID, v => new CropImageToolControl { Viewer = v }));
        Core.ToolRegistry.Register(FrameNavToolControl.TOOL_ID,
            new ToolControlAdapter(FrameNavToolControl.TOOL_ID, v => new FrameNavToolControl { Viewer = v }));

        // Register built-in non-hosted plugins
        Core.ToolRegistry.Register(ImageResizerTool.TOOL_ID, new ImageResizerTool());
        Core.ToolRegistry.Register(LosslessCompressionTool.TOOL_ID, new LosslessCompressionTool());
    }




    #region Main Menu APIs

    /// <summary>
    /// Shows main menu.
    /// </summary>
    public void IG_OpenMainMenu()
    {
        _mainWindow.PART_MainView.PART_Toolbar.PART_BtnMainMenu.OpenDropdownMenu();
    }


    /// <summary>
    /// Open app settings.
    /// </summary>
    public static async Task IG_OpenSettingsAsync()
    {
        var configPath = BHelper.ConfigDir(Config.CONFIG_USER);

        // The user config (igconfig.json) is only written on save/exit, so it can
        // be missing on a fresh run. Create it first so there is a real file to open.
        if (!File.Exists(configPath))
        {
            await Core.Config.SaveAsync();
        }

        if (BHelper.OS == OSType.Windows)
        {
            var proc = new Process();
            proc.StartInfo.FileName = configPath;
            proc.StartInfo.UseShellExecute = true;
            proc.Start();
        }
        else if (BHelper.OS == OSType.Linux)
        {
            // Most Linux desktops have no default app for ".json", so opening it
            // directly via the OpenURI portal does nothing. Reveal & select the
            // file in the file manager instead, so the user can open it with any
            // editor of their choice.
            BHelper.OpenFilePath(configPath);
        }
        else
        {
            // macOS: 'open' launches the file's default associated app.
            _ = Core.ShellProvider?.OpenDefaultEditingAppAsync(configPath);
        }
    }


    /// <summary>
    /// Exit the app.
    /// </summary>
    public static void IG_Exit()
    {
        BHelper.ExitApp(false);
    }

    #endregion // Main Menu APIs



    #region File APIs

    /// <summary>
    /// Shows file picker to open a photo.
    /// </summary>
    public async Task IG_OpenFileAsync()
    {
        var supportFileExtPatterns = Core.Config.FileFormats.Select(ext => $"*{ext}")
            .ToImmutableList();

        var files = await _mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Core.Lang[LangId.FrmMain_MnuOpenFile],
            FileTypeFilter = [
                new FilePickerFileType(Core.Lang[LangId.FrmMain_OpenFileDialog]) {
                    Patterns = supportFileExtPatterns,
                },
            ],
        });

        var file = files?.ElementAtOrDefault(0);
        var filePath = file?.TryGetLocalPath();

        IG_OpenPath(filePath);
    }


    /// <summary>
    /// Shows folder picker to open a photo folder.
    /// </summary>
    public async Task IG_OpenFolderAsync()
    {
        var dirs = await _mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());

        var dir = dirs?.ElementAtOrDefault(0);
        var dirPath = dir?.TryGetLocalPath();

        IG_OpenPath(dirPath);
    }


    /// <summary>
    /// Opens photo by the given path, shortcut for method <see cref="PrepareLoadPhotoList"/> with params:
    /// <list type="bullet">
    ///   <item><c>currentFilePath</c> = <c>null</c></item>
    ///   <item><c>disposeForegroundShell</c> = <c>true</c> when loading from a different folder</item>
    ///   <item><c>loadInitPhoto</c> = <c>true</c></item>
    /// </list>
    /// </summary>
    public void IG_OpenPath(string? path)
    {
        var fullPath = BHelper.ResolvePath(path);
        if (string.IsNullOrWhiteSpace(fullPath)) return;

        // 1. check if the path is being opened
        var imageIndex = Core.Photos.IndexOf(fullPath);

        // 2.1 The file is located another folder, load the entire folder
        if (imageIndex == -1 || (Core.ShellProvider?.CanUseForegroundShell() ?? false))
        {
            // dispose the foreground shell when loading from a different folder,
            // so that file enumeration uses the new folder instead of the stale shell
            var disposeForegroundShell = imageIndex == -1;

            _mainWindow.PART_MainView.PrepareLoadPhotoList([fullPath],
                currentFilePath: null, disposeForegroundShell, reloadInitPhoto: true);
        }
        // 2.2 The file is in current folder AND it is the viewing image
        else if (Core.Photos.CurrentIndex == imageIndex)
        {
            //do nothing
        }
        // 2.3 The file is in current folder AND it is NOT the viewing image
        else
        {
            IG_ViewByIndex(imageIndex);
        }
    }


    /// <summary>
    /// Open the current image in a new window.
    /// </summary>
    public void IG_NewWindow()
    {
        if (!Core.Config.EnableMultiInstances)
        {
            _ = ModalWindow.ShowInfoAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuNewWindow],
                Heading = Core.Lang[LangId.FrmMain_MnuNewWindow],
                Description = Core.Lang[LangId.FrmMain_MnuNewWindow_Error],
            });
            return;
        }

        var filePath = Core.Photos.CurrentFilePath;

        // get position for new window
        var posDiff = _mainWindow.DpiScale(10f);
        var newBounds = Core.Config.MainWindowBounds.WithX(Core.Config.MainWindowBounds.X + posDiff);
        newBounds = newBounds.WithY(Core.Config.MainWindowBounds.Y + posDiff);
        var boundStr = newBounds.ToStringDelimiter();
        var boundCmd = BHelper.BuildConfigCmdLine(nameof(Config.MainWindowBounds), boundStr);

        _ = BHelper.RunExeAsync(BHelper.AppExePath, $"{boundCmd} \"{filePath}\"");
    }


    /// <summary>
    /// Saves and overrides the current photo.
    /// </summary>
    public async Task IG_SaveAsync()
    {
        var srcFilePath = Core.Photos.CurrentFilePath;
        var isOpeningImageList = Core.Photos.CurrentIndex > -1;

        // use Save As if no image is opened
        if (!isOpeningImageList && Core.ClipboardImage is not null)
        {
            await IG_SaveAsAsync();
            return;
        }


        // show override warning
        if (Core.Config.EnableSaveConfirmation)
        {
            var modal = await ModalWindow.ShowWarningAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuSave],
                Heading = Core.Lang[LangId.FrmMain_MnuSave_Confirm],
                Description = srcFilePath,
                Note = Core.Lang[LangId.FrmMain_MnuSave_ConfirmDescription],
                IsRememberOptionVisible = true,
                Thumbnail = Core.Photos.Current?.GalleryThumbnail,
            }, ModalWindowButton.Yes_No);

            // update EnableSaveConfirmation setting
            Core.Config.EnableSaveConfirmation = !modal.IsRememberOptionChecked;

            if (modal.ExitCode != DialogExitCode.OK) return;
        }

        await SaveImageAsync(srcFilePath);
    }


    /// <summary>
    /// Shows save file dialog to save photo to file.
    /// </summary>
    public async Task IG_SaveAsAsync()
    {
        var srcFilePath = string.Empty;
        var srcExt = ".png";
        IStorageFolder? initSaveDir = null;


        // 1. get dest file name
        if (Core.ClipboardImage is null)
        {
            srcFilePath = Core.Photos.CurrentFilePath;
            srcExt = Core.Photos.Current?.Extension?.ToLowerInvariant() ?? srcExt;


            if (Core.Config.EnableOpenSaveAsInCurrentFolder)
            {
                var initSaveDirPath = Path.GetDirectoryName(srcFilePath);

                if (!string.IsNullOrWhiteSpace(initSaveDirPath))
                {
                    initSaveDir = await _mainWindow.StorageProvider.TryGetFolderFromPathAsync(initSaveDirPath);
                }
            }
        }

        var destFileName = string.IsNullOrEmpty(srcFilePath)
            ? $"untitle{srcExt}"
            : Path.GetFileNameWithoutExtension(srcFilePath);


        // 2. create file save picker
        var result = await _mainWindow.StorageProvider.SaveFilePickerWithResultAsync(new FilePickerSaveOptions
        {
            Title = Core.Lang[LangId.FrmMain_MnuSaveAs],
            FileTypeChoices = SavingExts.FilePickerFileTypeChoices,
            ShowOverwritePrompt = !Core.Config.EnableSaveConfirmation, // only show 1 prompt
            SuggestedStartLocation = initSaveDir,
            SuggestedFileName = destFileName,
            SuggestedFileType = SavingExts.LastSavedFileType,
        });

        SavingExts.LastSavedFileType = result.SelectedFileType;

        var destFilePath = result.File?.TryGetLocalPath() ?? string.Empty;
        if (string.IsNullOrEmpty(destFilePath)) return;


        // 3. show override warning
        if (File.Exists(destFilePath) && Core.Config.EnableSaveConfirmation)
        {
            var fi = new FileInfo(destFilePath);

            // show confirm dialog
            var modal = await ModalWindow.ShowWarningAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuSaveAs],
                Heading = Core.Lang[LangId.FrmMain_MnuSave_Confirm],
                Description = $"""
                {destFilePath}
                {BHelper.FormatSize(fi.Length)}
                """,
                Note = Core.Lang[LangId.FrmMain_MnuSave_ConfirmDescription],
                IsRememberOptionVisible = true,
            }, ModalWindowButton.Yes_No);

            // update EnableSaveConfirmation setting
            Core.Config.EnableSaveConfirmation = !modal.IsRememberOptionChecked;

            if (modal.ExitCode != DialogExitCode.OK) return;
        }


        // save file
        await SaveImageAsync(destFilePath);
    }


    /// <summary>
    /// Save the viewing image to file.
    ///   <para>
    ///     The source image is checked by this order:
    ///     <list type="number">
    ///       <item>Selected image area.</item>
    ///       <item><see cref="Core.ClipboardImage"/>.</item>
    ///       <item>Source <paramref name="destFilePath"/> file.</item>
    ///     </list>
    ///   </para>
    /// </summary>
    public async Task<bool> SaveImageAsync(string destFilePath)
    {
        var saveSource = ImageSaveSource.Undefined;
        var hasSrcPath = !string.IsNullOrEmpty(Core.Photos.CurrentFilePath);
        Exception? error = null;

        _ = Message.ShowAsync(destFilePath, Core.Lang[LangId.FrmMain_MnuSave_Saving]);


        // 1. save photo
        // 1.1 save the selection
        var hasSelection = Viewer.EnableSelection && !Viewer.SourceSelection.IsEmpty;
        if (hasSelection)
        {
            try
            {
                using var selectedBmp = Viewer.GetRenderedBitmap(true);
                var selectedImg = SkiaCodec.ToSKImage(selectedBmp)!;
                using var photo = new Photo(selectedImg);

                await photo.SaveAsAsync(destFilePath, new PhotoTransform(),
                    Core.Config.ImageEditQuality, Core.Config.EnablePreserveModifiedDate);
                saveSource = ImageSaveSource.SelectedArea;
            }
            catch (Exception ex) { error = ex; }
        }

        // 1.2 save the clipboard image
        else if (Core.ClipboardImage is not null)
        {
            try
            {
                await Core.ClipboardImage.SaveAsAsync(destFilePath, Core.ImageTransform,
                    Core.Config.ImageEditQuality, Core.Config.EnablePreserveModifiedDate);
                saveSource = ImageSaveSource.Clipboard;
            }
            catch (Exception ex) { error = ex; }
        }

        // 1.3 save the image in the list
        else if (Core.Photos.Current is not null)
        {
            try
            {
                await Core.Photos.Current.SaveAsAsync(destFilePath, Core.ImageTransform,
                    Core.Config.ImageEditQuality, Core.Config.EnablePreserveModifiedDate);
                saveSource = ImageSaveSource.CurrentFile;
            }
            catch (Exception ex) { error = ex; }
        }

        // 1.4 image is empty
        else
        {
            return false;
        }


        // 2. check for error
        if (error is not null)
        {
            await Message.ClearAsync();

            _ = await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuSave],
                Heading = Core.Lang[LangId.FrmMain_MnuSave_Error],
                Description = $"""
                {error.Source}:
                {error.Message}

                {destFilePath}
                """
            });

            return false;
        }


        // 3. check for success
        var newPhotoIndex = Core.Photos.IndexOf(destFilePath);
        if (newPhotoIndex == Core.Photos.CurrentIndex)
        {
            if (saveSource == ImageSaveSource.SelectedArea)
            {
                // reload to view the updated image
                IG_Reload();
            }
            else if (saveSource == ImageSaveSource.Clipboard)
            {
                // clear the clipboard image
                await LoadClipboardPhotoAsync(null);

                // reload to view the updated image
                IG_Reload();
            }

            // reset transformations
            Core.ImageTransform.Clear();
            Viewer.ClearPhotoTransforms();
        }


        _ = Message.ShowAsync(destFilePath, Core.Lang[LangId.FrmMain_MnuSave_Success]);


        // 4. emits saved event
        Core.OnPhotoSaved(new(Core.Photos.CurrentFilePath, destFilePath, saveSource));


        // 5. update thumbnail & metadata if file in the list was overriden
        Gallery.LoadThumbnail(newPhotoIndex, false);

        return true;
    }


    /// <summary>
    /// Exports image frames from the current photo source.
    /// </summary>
    public async Task IG_ExportImageFrames()
    {
        if (Viewer.SourceKind == PhotoSource.None) return;
        var frameCount = Core.Photos.CurrentMetadata?.FrameCount ?? 0;
        if (frameCount < 2) return;

        // 1. open folder picker
        var results = await _mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = Core.Lang[LangId.FrmExportFrames_FolderPickerTitle],
        });

        var destDirPath = results.ToArray().FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrEmpty(destDirPath)) return;


        // 2. get source file path
        var srcFilePath = Core.Photos.CurrentFilePath;

        // clipboard image
        if (Core.ClipboardImage is not null)
        {
            // save image to temp file
            srcFilePath = await Core.SavePhotoAsTempFileAsync();
        }
        if (string.IsNullOrEmpty(srcFilePath)) return;


        // 3. export frames
        Core.IsBusy = true;

        var exportWindow = new ExportFramesWindow(srcFilePath, destDirPath);
        await exportWindow.ShowAsync(_mainWindow);

        Core.IsBusy = false;
    }


    /// <summary>
    /// Shows Open With window.
    /// </summary>
    public async Task IG_OpenWithAsync()
    {
        if (BHelper.OS != OSType.Windows)
        {
            throw new NotSupportedException($"IGE: This feature is not supported on {BHelper.OS}.");
        }


        string? filePath = null;
        var isClipboardPhoto = Core.ClipboardImage is not null;


        if (isClipboardPhoto)
        {
            _ = Message.ShowAsync(Core.Lang[LangId._CreatingFile], delayMs: 500);

            // save clipboard photo as temp PNG file
            filePath = await Core.SavePhotoAsTempFileAsync();
        }
        else
        {
            filePath = Core.Photos.CurrentFilePath;
        }


        await Message.ClearAsync();
        if (!File.Exists(filePath))
        {
            _ = await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuOpenWith],
                Description = Core.Lang[LangId._CreatingFileError],
            });
        }
        else
        {
            Core.ShellProvider?.ShowOpenWith(filePath);
        }
    }


    /// <summary>
    /// Opens Print dialog to print the current photo.
    /// </summary>
    public async Task IG_PrintAsync()
    {
        if (BHelper.OS != OSType.Windows)
        {
            throw new NotSupportedException($"IGE: This feature is not supported on {BHelper.OS}.");
        }

        var fileToPrint = Core.Photos.CurrentFilePath;
        _ = Message.ShowAsync(Core.Lang[LangId._CreatingFile], delayMs: 500);


        if (string.IsNullOrEmpty(fileToPrint))
        {
            _ = await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuOpenWith],
                Description = Core.Lang[LangId._CreatingFileError],
            });
        }
        else
        {
            try
            {
                await Core.PrintProvider.OpenPrintAsync(fileToPrint,
                    Core.Photos.CurrentMetadata, Core.ClipboardImage != null);
            }
            catch (Exception ex)
            {
                _ = await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
                {
                    Title = Core.Lang[LangId.FrmMain_MnuPrint],
                    Heading = Core.Lang[LangId.FrmMain_MnuPrint_Error],
                    Description = ex.Message,
                });
            }
        }

        _ = Message.ClearAsync();
    }


    /// <summary>
    /// Shows Share dialog.
    /// </summary>
    public async Task IG_ShareAsync()
    {
        var filePath = Core.Photos.CurrentFilePath;

        // print clipboard image
        if (Core.ClipboardImage is not null)
        {
            _ = Message.ShowAsync(Core.Lang[LangId._CreatingFile], delayMs: 500);

            // save image to temp file
            filePath = await Core.SavePhotoAsTempFileAsync();
        }

        await Message.ClearAsync();

        if (!File.Exists(filePath))
        {
            _ = ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuShare],
                Description = Core.Lang[LangId._CreatingFileError],
            });
        }
        else
        {
            try
            {
                Core.ShareProvider.ShowShare(_mainWindow.Handle, [filePath]);
            }
            catch (Exception ex)
            {
                _ = ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
                {
                    Title = Core.Lang[LangId.FrmMain_MnuShare],
                    Description = $"{Core.Lang[LangId.FrmMain_MnuShare_Error]}\r\n\r\n{ex.Message}",
                });
            }
        }
    }


    /// <summary>
    /// Opens photo file location.
    /// </summary>
    public static void IG_OpenLocation()
    {
        BHelper.OpenFilePath(Core.Photos.CurrentFilePath);
    }


    /// <summary>
    /// Opens a popup to rename the current photo.
    /// </summary>
    public async Task IG_RenameAsync()
    {
        var oldFilePath = Core.Photos.CurrentFilePath;
        if (!File.Exists(oldFilePath)) return;

        var currentFolder = Path.GetDirectoryName(oldFilePath) ?? string.Empty;
        var ext = Path.GetExtension(oldFilePath);
        var newName = Path.GetFileNameWithoutExtension(oldFilePath);
        var title = Core.Lang[LangId.FrmMain_MnuRename];

        // 2. show popup
        var result = await ModalWindow.ShowInputAsync(_mainWindow, new ModalWindowOptions
        {
            Title = title,
            Description = $"""
            {oldFilePath}

            {Core.Lang[LangId.FrmMain_MnuRename_Description]}
            """,
            InputValue = newName,
            AcceptValue = TextBoxAcceptValue.FileNameValueOnly,
            ThumbnailIcon = StockIconId.Rename,
            Thumbnail = Core.Photos.Current?.GalleryThumbnail,
        });

        if (result.ExitCode != DialogExitCode.OK || string.IsNullOrWhiteSpace(result.InputValue)) return;

        // 3. get new photo name
        newName = $"{result.InputValue.Trim()}{ext}";
        var newFilePath = Path.Combine(currentFolder, newName);


        // 4. perform renaming
        try
        {
            // Issue 73: Windows ignores case-only changes
            if (string.Equals(oldFilePath, newFilePath, StringComparison.OrdinalIgnoreCase))
            {
                // user changing only the case of the filename. Need to perform a trick.
                File.Move(oldFilePath, oldFilePath + "_temp");
                File.Move(oldFilePath + "_temp", newFilePath);
            }
            else
            {
                File.Move(oldFilePath, newFilePath);
            }


            // manually update the change if FileWatcher is not enabled
            if (!Core.Config.EnableFileWatcher)
            {
                Core.Photos.SetFilePath(Core.Photos.CurrentIndex, newFilePath);
            }
        }
        catch (Exception ex)
        {
            _ = await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = title,
                Description = ex.Message,
            });
        }
    }


    /// <summary>
    /// Sends or permenantly deletes the current image.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public async Task IG_DeleteAsync(string? moveToRecycleBinStr = "true")
    {
        var moveToRecycleBin = BHelper.ConvertStringToBool(moveToRecycleBinStr) ?? true;
        await IG_DeleteAsync(moveToRecycleBin);
    }


    /// <summary>
    /// Sends or permenantly deletes the current image.
    /// </summary>
    public async Task IG_DeleteAsync(bool moveToRecycleBin = true)
    {
        var filePath = Core.Photos.CurrentFilePath;
        if (!File.Exists(filePath)) return;

        var canDelete = true;
        var title = moveToRecycleBin
            ? Core.Lang[LangId.FrmMain_MnuMoveToRecycleBin]
            : Core.Lang[LangId.FrmMain_MnuDeleteFromHardDisk];


        // 1. show confirm dialog
        if (Core.Config.EnableDeleteConfirmation)
        {
            var heading = moveToRecycleBin
                ? Core.Lang[LangId.FrmMain_MnuMoveToRecycleBin_Description]
                : Core.Lang[LangId.FrmMain_MnuDeleteFromHardDisk_Description];
            var thumbnailIcon = moveToRecycleBin
                ? StockIconId.RecycleBin
                : StockIconId.Delete;

            var modal = await ModalWindow.ShowWarningAsync(_mainWindow, new ModalWindowOptions
            {
                Title = title,
                Heading = heading,
                Description = $"""
                {filePath}
                {Core.Photos.Current?.Metadata.FileSizeFormatted}
                """,
                IsRememberOptionVisible = true,
                ThumbnailIcon = thumbnailIcon,
                Thumbnail = Core.Photos.Current?.GalleryThumbnail,
            }, ModalWindowButton.Yes_No);

            // update remember confirm setting
            Core.Config.EnableDeleteConfirmation = !modal.IsRememberOptionChecked;

            canDelete = modal.ExitCode == DialogExitCode.OK;
        }


        // 2. delete photo
        if (!canDelete) return;
        Core.IsBusy = true;

        try
        {
            await IG_UnloadAsync();
            BHelper.DeleteFile(filePath, moveToRecycleBin);

            // manually update the change because FileWatcher is disabled when IsBusy = true
            Core.Photos.Remove(Core.Photos.CurrentFilePath);
            var nextIndex = (int)Math.Min(Core.Photos.Count - 1, Core.Photos.CurrentIndex);
            var nextPhoto = Core.Photos.Select(nextIndex);
            _ = _mainWindow.PART_MainView.ViewPhotoAsync(nextPhoto);
        }
        catch (Exception ex)
        {
            await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = title,
                Description = ex.Message,
            });
        }

        Core.IsBusy = false;
    }


    /// <summary>
    /// Opens photo's Properties dialog.
    /// </summary>
    public void IG_OpenProperties()
    {
        Core.ShellProvider?.ShowFileProperties(Core.Photos.CurrentFilePath, _mainWindow.Handle);
    }

    #endregion // File APIs



    #region Navigation APIs

    /// <summary>
    /// View a photo in the list by the given index.
    /// </summary>
    public void IG_ViewByIndex(string? photoIndexStr)
    {
        if (!int.TryParse(photoIndexStr, out var photoIndex)) return;

        IG_ViewByIndex(photoIndex);
    }


    /// <summary>
    /// View a photo in the list by the given index.
    /// </summary>
    public void IG_ViewByIndex(int photoIndex)
    {
        if (photoIndex < 0) return;

        var step = photoIndex - Core.Photos.CurrentIndex;
        IG_ViewByStep(step);
    }


    /// <summary>
    /// View a photo in the list by the given step.
    /// </summary>
    public void IG_ViewByStep(string? stepStr)
    {
        if (!int.TryParse(stepStr, out var step))
        {
            throw new ArgumentException($"""
                Step '{stepStr}' is not a valid integer.

                ----------
                👉🏼 Method: {nameof(IG_ViewByStep)}
                """,
                nameof(stepStr));
        }

        IG_ViewByStep(step);
    }


    /// <summary>
    /// View a photo in the list by the given step.
    /// </summary>
    public void IG_ViewByStep(int step)
    {
        // check if can navigate to the image
        var canLoopBack = Core.Config.EnableSlideshow
            ? Core.Config.EnableLoopSlideshow
            : Core.Config.EnableLoopBackNavigation;

        if (!Core.Photos.GetByStep(step, canLoopBack, out var photo))
        {
            var isFirst = Core.Photos.CurrentIndex == 0;

            _ = Message.ShowAsync(Core.Lang[isFirst
                ? LangId.FrmMain_ReachedFirstImage
                : LangId.FrmMain_ReachedLastImage]);
            return;
        }


        _ = _mainWindow.PART_MainView.ViewPhotoAsync(photo);

        // reset slideshow interval on manual navigation
        if (Core.Config.EnableSlideshow && !_slideshowIsAdvancing)
        {
            if (Core.Slideshow is { } slideshow)
            {
                // resume if auto-paused (e.g. at end of list)
                if (slideshow.IsPaused)
                {
                    slideshow.Resume();
                }

                slideshow.ResetInterval();
            }
        }
    }


    /// <summary>
    /// View the next photo.
    /// </summary>
    public void IG_ViewNext()
    {
        IG_ViewByStep(1);
    }


    /// <summary>
    /// View the previous photo.
    /// </summary>
    public void IG_ViewPrevious()
    {
        IG_ViewByStep(-1);
    }


    /// <summary>
    /// Shows an input dialog, and opens the user-input photo.
    /// </summary>
    public async Task IG_GoToAsync()
    {
        if (Core.Photos.Count == 0) return;

        var oldIndex = Core.Photos.CurrentIndex + 1;
        var result = await ModalWindow.ShowInputAsync(_mainWindow, new ModalWindowOptions
        {
            Title = Core.Lang[LangId.FrmMain_MnuGoTo],
            Description = Core.Lang[LangId.FrmMain_MnuGoTo_Description],
            InputValue = oldIndex.ToString(),
            AcceptValue = TextBoxAcceptValue.UnsignedIntValueOnly,
        });

        if (result.ExitCode != DialogExitCode.OK) return;


        if (int.TryParse(result.InputValue, out var newIndex))
        {
            newIndex--;

            if (newIndex != Core.Photos.CurrentIndex
                && 0 <= newIndex && newIndex < Core.Photos.Count)
            {
                IG_ViewByIndex(newIndex);
            }
        }
    }


    /// <summary>
    /// Views the first photo in the list.
    /// </summary>
    public void IG_GoToFirst()
    {
        IG_ViewByIndex(0);
    }


    /// <summary>
    /// Views the last photo in the list.
    /// </summary>
    public void IG_GoToLast()
    {
        IG_ViewByIndex((int)Core.Photos.Count - 1);
    }


    /// <summary>
    /// View a frame of the current photo.
    /// </summary>
    public void IG_ViewFrame(string? frameIndexStr)
    {
        if (!int.TryParse(frameIndexStr, out var frameIndex))
        {
            throw new ArgumentException($"""
                Frame index '{frameIndexStr}' is not a valid integer.

                ----------
                👉🏼 Method: {nameof(IG_ViewFrame)}
                """,
                nameof(frameIndexStr));
        }

        IG_ViewFrame(frameIndex);
    }


    /// <summary>
    /// View a frame of the current photo.
    /// If the frame index is out of range, it will be looped.
    /// </summary>
    public void IG_ViewFrame(int frameIndex)
    {
        var frameCount = Core.Photos.CurrentMetadata?.FrameCount ?? 0;
        if (frameCount < 2) return;

        var safeFrameIndex = BHelper.ComputeIndexInRange(frameIndex, frameCount, true);

        Dispatcher.UIThread.Post(async () =>
        {
            await Viewer.ViewFrameAsync((uint)safeFrameIndex);
        });
    }


    /// <summary>
    /// View the next frame of the current photo.
    /// </summary>
    public void IG_ViewNextFrame()
    {
        var newFrameIndex = (Core.Photos.Current?.FrameIndex ?? 0) + 1;
        IG_ViewFrame(newFrameIndex);
    }


    /// <summary>
    /// View the previous frame of the current photo.
    /// </summary>
    public void IG_ViewPreviousFrame()
    {
        var newFrameIndex = (Core.Photos.Current?.FrameIndex ?? 1) - 1;
        IG_ViewFrame(newFrameIndex);
    }


    /// <summary>
    /// View the first frame of the current photo.
    /// </summary>
    public void IG_ViewFirstFrame()
    {
        IG_ViewFrame(0);
    }


    /// <summary>
    /// View the last frame of the current photo.
    /// </summary>
    public void IG_ViewLastFrame()
    {
        var lastFrameIndex = (int)(Core.Photos.CurrentMetadata?.FrameCount ?? 1) - 1;
        IG_ViewFrame(lastFrameIndex);
    }

    #endregion // Navigation APIs



    #region Zoom APIs

    /// <summary>
    /// Shows input dialog for custom zoom.
    /// </summary>
    public async Task IG_CustomZoomAsync()
    {
        var oldZoom = Math.Round(Viewer.ZoomFactor * 100f, 3);

        var result = await ModalWindow.ShowInputAsync(_mainWindow, new ModalWindowOptions
        {
            Title = Core.Lang[LangId.FrmMain_MnuCustomZoom],
            Description = Core.Lang[LangId.FrmMain_MnuCustomZoom_Description],
            InputValue = oldZoom.ToString(),
            AcceptValue = TextBoxAcceptValue.UnsignedFloatValueOnly,
            ThumbnailIcon = StockIconId.Find,
        });

        if (result.ExitCode != DialogExitCode.OK) return;

        if (float.TryParse(result.InputValue.Trim(), out var newZoom))
        {
            Viewer.ZoomFactor = newZoom / 100f;
        }
    }


    /// <summary>
    /// Zoom to the current cursor location by the given factor.
    /// </summary>
    public void IG_SetZoom(string? factorStr)
    {
        if (!float.TryParse(factorStr, out var factor))
        {
            throw new ArgumentException($"""
                Zoom factor '{factorStr}' is not a valid float.

                ----------
                👉🏼 Method: {nameof(IG_SetZoom)}
                """,
                nameof(factorStr));
        }

        IG_SetZoom(factor);
    }

    /// <summary>
    /// Zoom to the current cursor location by the given factor.
    /// </summary>
    public void IG_SetZoom(float factor)
    {
        _ = Viewer.ZoomToPoint(factor);
    }


    /// <summary>
    /// Sets zoom = 100% if zoom value is less than 100%.
    /// Otherwise, refresh the image with the current zoom mode.
    /// </summary>
    public void IG_SetZoomForMouseClick()
    {
        if (Viewer.ZoomFactor < 1)
        {
            IG_SetZoom(1);
        }
        else
        {
            IG_Refresh();
        }
    }


    /// <summary>
    /// Sets the zoom mode value.
    /// </summary>
    public void IG_SetZoomMode(string? modeStr)
    {
        if (!Enum.TryParse<ZoomMode>(modeStr, out var mode))
        {
            throw new ArgumentException($"""
                '{modeStr}' is not a valid zoom mode.

                ----------
                👉🏼 Method: {nameof(IG_SetZoomMode)}
                """,
                nameof(modeStr));
        }

        IG_SetZoomMode(mode);
    }

    /// <summary>
    /// Sets the zoom mode value.
    /// </summary>
    public void IG_SetZoomMode(ZoomMode mode)
    {
        if (mode == Core.Config.ZoomMode)
        {
            IG_Refresh();
        }
        else
        {
            Core.Config.ZoomMode = mode;
        }
    }


    /// <summary>
    /// Zooms into the image.
    /// </summary>
    public void IG_ZoomIn()
    {
        if (Viewer.ZoomLevels.Length > 0)
        {
            Viewer.ZoomIn();
            return;
        }

        // smooth zooming
        Viewer.StartDrawingAnimation(AnimationSources.ZoomIn, 100);
    }


    /// <summary>
    /// Zooms out of the image.
    /// </summary>
    public void IG_ZoomOut()
    {
        if (Viewer.ZoomLevels.Length > 0)
        {
            Viewer.ZoomOut();
            return;
        }

        // smooth zooming
        Viewer.StartDrawingAnimation(AnimationSources.ZoomOut, 100);
    }

    #endregion // Zoom APIs



    #region Panning APIs

    /// <summary>
    /// Pans the viewing image to left.
    /// </summary>
    public void IG_PanLeft()
    {
        // smooth zooming
        Viewer.StartDrawingAnimation(AnimationSources.PanLeft, 100);
    }


    /// <summary>
    /// Pans the viewing image to right.
    /// </summary>
    public void IG_PanRight()
    {
        // smooth zooming
        Viewer.StartDrawingAnimation(AnimationSources.PanRight, 100);
    }


    /// <summary>
    /// Pans the viewing image to top.
    /// </summary>
    public void IG_PanUp()
    {
        // smooth zooming
        Viewer.StartDrawingAnimation(AnimationSources.PanUp, 100);
    }


    /// <summary>
    /// Pans the viewing image to bottom.
    /// </summary>
    public void IG_PanDown()
    {
        // smooth zooming
        Viewer.StartDrawingAnimation(AnimationSources.PanDown, 100);
    }


    /// <summary>
    /// Pans the viewing image to left side.
    /// </summary>
    public void IG_PanToLeft()
    {
        var distanceX = Viewer.SrcRect.X * Viewer.ZoomFactor;
        var duration = 1000;
        Viewer.PanSpeed = distanceX / duration * 60;

        Viewer.StartDrawingAnimation(AnimationSources.PanLeft, duration, () =>
        {
            Viewer.PanSpeed = Core.Config.PanSpeed;
        });
    }


    /// <summary>
    /// Pans the viewing image to right side.
    /// </summary>
    public void IG_PanToRight()
    {
        var x = Viewer.BitmapSize.Width - Viewer.SrcRect.Width;
        var distanceX = (x + Viewer.SrcRect.X) * Viewer.ZoomFactor;
        var duration = 1000;
        Viewer.PanSpeed = distanceX / duration * 60;

        Viewer.StartDrawingAnimation(AnimationSources.PanRight, duration, () =>
        {
            Viewer.PanSpeed = Core.Config.PanSpeed;
        });
    }


    /// <summary>
    /// Pans the viewing image to top.
    /// </summary>
    public void IG_PanToTop()
    {
        var distanceY = Viewer.SrcRect.Y * Viewer.ZoomFactor;
        var duration = 1000;
        Viewer.PanSpeed = distanceY / duration * 60;

        Viewer.StartDrawingAnimation(AnimationSources.PanUp, duration, () =>
        {
            Viewer.PanSpeed = Core.Config.PanSpeed;
        });
    }


    /// <summary>
    /// Pans the viewing image to bottom.
    /// </summary>
    public void IG_PanToBottom()
    {
        var y = Viewer.BitmapSize.Height - Viewer.SrcRect.Height;
        var distanceY = (y + Viewer.SrcRect.Y) * Viewer.ZoomFactor;
        var duration = 1000;
        Viewer.PanSpeed = distanceY / duration * 60;

        Viewer.StartDrawingAnimation(AnimationSources.PanDown, duration, () =>
        {
            Viewer.PanSpeed = Core.Config.PanSpeed;
        });
    }

    #endregion // Panning APIs



    #region Image APIs

    /// <summary>
    /// Refreshes image viewport.
    /// </summary>
    public void IG_Refresh()
    {
        Viewer.Refresh(true, false, Core.Config.EnableWindowFit);
    }


    /// <summary>
    /// Reloads image file.
    /// </summary>
    public void IG_Reload()
    {
        _ = _mainWindow.PART_MainView.ViewPhotoAsync(Core.Photos.Current, useCache: false);

        // reload thumbnail
        Gallery.LoadThumbnail(Core.Photos.CurrentIndex, false);
    }


    /// <summary>
    /// Reloads images list.
    /// </summary>
    public void IG_ReloadList()
    {
        _mainWindow.PART_MainView.PrepareLoadPhotoList(Core.Photos.DistinctDirs,
            Core.Photos.CurrentFilePath, disposeForegroundShell: false, reloadInitPhoto: false);
    }


    /// <summary>
    /// Unloads the current photo.
    /// </summary>
    public async Task IG_UnloadAsync()
    {
        var args = new PhotoUnloadedEventArgs()
        {
            IsClipboardPhoto = Core.ClipboardImage is not null,
            Index = Core.Photos.CurrentIndex,
            FilePath = Core.Photos.CurrentFilePath,
        };


        // 1. unload clipboard photo
        if (args.IsClipboardPhoto)
        {
            await LoadClipboardPhotoAsync(null);

            // show the current photo in the list
            await _mainWindow.PART_MainView.ViewPhotoAsync(Core.Photos.Current);
        }

        // 2. unload photo from the list
        else
        {
            // cancel loading the current image
            Core.Photos.Current?.CancelLoading();

            await _mainWindow.PART_MainView.ViewPhotoAsync(null, false);
            Core.Photos.Current?.Unload();
        }

        // raise unloaded event
        Core.OnPhotoUnloaded(args);
    }


    /// <summary>
    /// Sets whether to use the Explorer sort order.
    /// </summary>
    public void IG_ToggleExplorerSortOrder(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleExplorerSortOrder(enabled);
    }


    /// <summary>
    /// Sets whether to use the Explorer sort order.
    /// </summary>
    public void IG_ToggleExplorerSortOrder(bool? enabled)
    {
        enabled ??= !Core.Config.EnableExplorerSortOrder;
        Core.Config.EnableExplorerSortOrder = enabled.Value;

        IG_ReloadList();
    }


    /// <summary>
    /// Sets the image loading order value.
    /// </summary>
    public void IG_SetLoadingOrderBy(string? orderByStr)
    {
        if (!Enum.TryParse<ImageOrderBy>(orderByStr, out var orderBy))
        {
            throw new ArgumentException($"""
                '{orderByStr}' is not a valid loading order.

                ----------
                👉🏼 Method: {nameof(IG_SetLoadingOrderBy)}
                """,
                nameof(orderByStr));
        }

        IG_SetLoadingOrderBy(orderBy);
    }


    /// <summary>
    /// Sets the image loading order value.
    /// </summary>
    public void IG_SetLoadingOrderBy(ImageOrderBy orderBy)
    {
        if (orderBy == Core.Config.ImageLoadingOrder) return;

        Core.Config.ImageLoadingOrder = orderBy;
        IG_ReloadList();
    }


    /// <summary>
    /// Toggles the image loading order by the given field.
    /// If already sorted by that field, flips Asc/Desc.
    /// Otherwise switches to that field with Asc.
    /// </summary>
    public void IG_ToggleLoadingOrderBy(string? orderByStr)
    {
        if (!Enum.TryParse<ImageOrderBy>(orderByStr, out var orderBy))
        {
            throw new ArgumentException($"""
                '{orderByStr}' is not a valid loading order.

                ----------
                👉🏼 Method: {nameof(IG_ToggleLoadingOrderBy)}
                """,
                nameof(orderByStr));
        }

        if (Core.Config.ImageLoadingOrder == orderBy)
        {
            // already on this field — flip direction
            var newType = Core.Config.ImageLoadingOrderType == ImageOrderType.Asc
                ? ImageOrderType.Desc
                : ImageOrderType.Asc;
            Core.Config.ImageLoadingOrderType = newType;
        }
        else
        {
            Core.Config.ImageLoadingOrder = orderBy;
            Core.Config.ImageLoadingOrderType = ImageOrderType.Asc;
        }

        IG_ReloadList();
    }


    /// <summary>
    /// Sets the image loading order value.
    /// </summary>
    public void IG_SetLoadingOrderType(string? orderTypeStr)
    {
        if (!Enum.TryParse<ImageOrderType>(orderTypeStr, out var orderType))
        {
            throw new ArgumentException($"""
                '{orderTypeStr}' is not a valid loading order.

                ----------
                👉🏼 Method: {nameof(IG_SetLoadingOrderType)}
                """,
                nameof(orderTypeStr));
        }

        IG_SetLoadingOrderType(orderType);
    }


    /// <summary>
    /// Sets the image loading order value.
    /// </summary>
    public void IG_SetLoadingOrderType(ImageOrderType orderType)
    {
        if (orderType == Core.Config.ImageLoadingOrderType) return;

        Core.Config.ImageLoadingOrderType = orderType;
        IG_ReloadList();
    }


    /// <summary>
    /// Sets the image color channels.
    /// </summary>
    public void IG_SetColorChannels(string? channelsStr)
    {
        if (!Enum.TryParse<ColorChannels>(channelsStr, out var channels))
        {
            throw new ArgumentException($"""
                '{channelsStr}' is not a valid color channel.

                ----------
                👉🏼 Method: {nameof(IG_SetColorChannels)}
                """,
                nameof(channelsStr));
        }

        IG_SetColorChannels(channels);
    }


    /// <summary>
    /// Sets the image color channels.
    /// </summary>
    public void IG_SetColorChannels(ColorChannels channels)
    {
        if (Viewer.SourceKind == PhotoSource.None || Core.IsBusy) return;

        // apply color channel filter
        if (Viewer.FilterColorChannels(channels, false))
        {
            Core.ColorChannels = channels;

            // apply transforms
            if (Core.ImageTransform.HasChanges)
            {
                _ = Viewer.RotateImage(Core.ImageTransform.Rotation, false);
                _ = Viewer.FlipImage(Core.ImageTransform.Flips, false);
            }

            Viewer.Refresh(resetZoom: false);
        }
        else
        {
            _ = Message.ShowAsync(
                Core.Lang[LangId._InvalidAction],
                Core.Lang[LangId.FrmMain_MnuViewChannels]);
        }
    }


    /// <summary>
    /// Open app for edit action.
    /// </summary>
    public async Task IG_OpenEditingAppAsync()
    {
        // get file path to edit
        string? filePath;
        if (Core.ClipboardImage != null)
        {
            _ = Message.ShowAsync(Core.Lang[LangId._CreatingFile], delayMs: 500);
            filePath = await Core.SavePhotoAsTempFileAsync();
        }
        else
        {
            filePath = Core.Photos.CurrentFilePath;
        }
        await Message.ClearAsync();


        if (!File.Exists(filePath))
        {
            _ = await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuOpenWith],
                Description = Core.Lang[LangId._CreatingFileError],
            });
            return;
        }


        // get extension
        var ext = Path.GetExtension(filePath).ToLowerInvariant();


        // get app from the extension
        if (EditingApp.GetFromExtension(ext) is EditingApp app)
        {
            try
            {
                var args = BHelper.BuildExeArgs(app.Executable, app.Argument, filePath);

                var result = await BHelper.RunExeCmd(args.Executable, args.Args, false, false, true);
                if (result == IgExitCode.Done)
                {
                    RunActionAfterEditing__();
                }
            }
            catch { }
        }
        else // edit by default associated app
        {
            Core.ShellProvider?.OpenDefaultEditingAppAsync(filePath, RunActionAfterEditing__);
        }
    }


    /// <summary>
    /// Runs the <see cref="Config.AfterEditingAction"/> action after done editing.
    /// </summary>
    private void RunActionAfterEditing__()
    {
        if (Core.Config.AfterEditingAction == AfterEditAppAction.Minimize)
        {
            _mainWindow.WindowState = WindowState.Minimized;
        }
        else if (Core.Config.AfterEditingAction == AfterEditAppAction.Close)
        {
            IG_Exit();
        }
    }


    /// <summary>
    /// Invert image colors.
    /// </summary>
    public void IG_InvertColors()
    {
        if (Viewer.SourceKind == PhotoSource.None || Core.IsBusy) return;

        // invert image colors
        if (Viewer.InvertColor(true))
        {
            Core.ImageTransform.IsColorInverted = Viewer.IsColorInverted;
        }
        else
        {
            _ = Message.ShowAsync(
                Core.Lang[LangId._InvalidAction],
                Core.Lang[LangId.FrmMain_MnuInvertColors]);
        }
    }


    /// <summary>
    /// Sets whether to play or pause the image animation.
    /// If the photo is a live/motion photo, opens the embedded video instead.
    /// </summary>
    public async Task IG_ToggleImageAnimationAsync(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        await IG_ToggleImageAnimationAsync(enabled);
    }


    /// <summary>
    /// Sets whether to play or pause the image animation.
    /// If the photo is a live/motion photo, opens the embedded video instead.
    /// </summary>
    public async Task IG_ToggleImageAnimationAsync(bool? enabled)
    {
        // if this is a motion/live photo, extract and play the embedded video
        var meta = Viewer.Photo?.Metadata;
        if (meta?.IsLivePhoto == true && meta.EmbeddedVideoOffsetFromEnd > 0)
        {
            await meta.OpenLivePhotoVideoAsync().ConfigureAwait(false);
            return;
        }

        enabled ??= !Viewer.IsImageAnimating;

        if (enabled.Value)
        {
            Viewer.StartAnimator();
        }
        else
        {
            Viewer.StopAnimator();
        }
    }


    /// <summary>
    /// Rotate the current image according to the rotation options.
    /// </summary>
    public void IG_Rotate(string? optionStr)
    {
        if (!Enum.TryParse<RotateOption>(optionStr, out var options))
        {
            throw new ArgumentException($"""
                '{optionStr}' is not a valid rotation option.

                ----------
                👉🏼 Method: {nameof(IG_Rotate)}
                """,
                nameof(optionStr));
        }

        IG_Rotate(options);
    }


    /// <summary>
    /// Rotate the current image according to the rotation options.
    /// </summary>
    public void IG_Rotate(RotateOption options)
    {
        if (Viewer.SourceKind == PhotoSource.None || Core.IsBusy) return;

        var degree = options == RotateOption.Left ? -90 : 90;

        // update rotation changes
        if (Viewer.RotateImage(degree))
        {
            var currentRotation = Core.ImageTransform.Rotation + degree;
            if (Math.Abs(currentRotation) >= 360)
            {
                currentRotation %= 360;
            }

            Core.ImageTransform.Rotation = currentRotation;
        }
        else
        {
            _ = Message.ShowAsync(
                Core.Lang[LangId._InvalidAction_Transformation],
                Core.Lang[LangId._InvalidAction]);
        }
    }


    /// <summary>
    /// Flips the current image according to the flip options.
    /// </summary>
    public void IG_FlipImage(string? optionStr)
    {
        if (!Enum.TryParse<FlipOptions>(optionStr, out var options))
        {
            throw new ArgumentException($"""
                '{optionStr}' is not a valid flip option.

                ----------
                👉🏼 Method: {nameof(IG_FlipImage)}
                """,
                nameof(optionStr));
        }

        IG_FlipImage(options);
    }


    /// <summary>
    /// Flips the current image according to the flip options.
    /// </summary>
    public void IG_FlipImage(FlipOptions options)
    {
        if (Viewer.SourceKind == PhotoSource.None || Core.IsBusy) return;

        // update flip changes
        if (Viewer.FlipImage(options))
        {
            if (options.HasFlag(FlipOptions.Horizontal))
            {
                if (Core.ImageTransform.Flips.HasFlag(FlipOptions.Horizontal))
                {
                    Core.ImageTransform.Flips ^= FlipOptions.Horizontal;
                }
                else
                {
                    Core.ImageTransform.Flips |= FlipOptions.Horizontal;
                }
            }

            if (options.HasFlag(FlipOptions.Vertical))
            {
                if (Core.ImageTransform.Flips.HasFlag(FlipOptions.Vertical))
                {
                    Core.ImageTransform.Flips ^= FlipOptions.Vertical;
                }
                else
                {
                    Core.ImageTransform.Flips |= FlipOptions.Vertical;
                }
            }
        }
        else
        {
            _ = Message.ShowAsync(
                Core.Lang[LangId._InvalidAction_Transformation],
                Core.Lang[LangId._InvalidAction]);
        }
    }


    /// <summary>
    /// Sets the viewing photo as desktop wallpaper.
    /// </summary>
    public async Task IG_SetDesktopBackgroundAsync()
    {
        await SetSystemBackgroundAsync(false);
    }


    /// <summary>
    /// Sets the viewing photo as lock screen image.
    /// </summary>
    public async Task IG_SetLockScreenImageAsync()
    {
        if (BHelper.OS != OSType.Windows)
        {
            throw new NotSupportedException($"IGE: This feature is not supported on {BHelper.OS}.");
        }

        await SetSystemBackgroundAsync(true);
    }


    /// <summary>
    /// Sets the current photo as system background.
    /// </summary>
    /// <param name="forLockScreen">
    /// <c>true</c>: For lock screen image, <c>false</c>: for desktop wallpaper
    /// </param>
    private async Task SetSystemBackgroundAsync(bool forLockScreen)
    {
        if (Viewer.SourceKind == PhotoSource.None || Core.ShellProvider is null) return;

        var filePath = Core.Photos.CurrentFilePath;
        var ext = Core.Photos.Current?.Extension.ToLowerInvariant() ?? string.Empty;
        _ = Message.ShowAsync(Core.Lang[LangId._CreatingFile], delayMs: 500);

        var title = forLockScreen
            ? Core.Lang[LangId.FrmMain_MnuSetLockScreen]
            : Core.Lang[LangId.FrmMain_MnuSetDesktopBackground];


        // 1. create temp image if needed
        if (Core.ClipboardImage is not null || !_desktopNativeFormats.Contains(ext))
        {
            // save image to temp file
            filePath = await Core.SavePhotoAsTempFileAsync(".jpg");
        }
        await Message.ClearAsync();


        // 2. check if file path is valid
        if (!File.Exists(filePath))
        {
            _ = await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = title,
                Description = Core.Lang[LangId._CreatingFileError],
            });

            return;
        }


        // 3. set background
        try
        {
            if (forLockScreen)
            {
                await Core.ShellProvider.SetLockScreenAsync(filePath);
            }
            else
            {
                Core.ShellProvider.SetWallpaper(filePath);
            }


            var successMsg = forLockScreen
                ? Core.Lang[LangId.FrmMain_MnuSetLockScreen_Success]
                : Core.Lang[LangId.FrmMain_MnuSetDesktopBackground_Success];
            _ = Message.ShowAsync(successMsg);
        }
        catch (Exception ex)
        {
            var heading = forLockScreen
                ? Core.Lang[LangId.FrmMain_MnuSetLockScreen_Error]
                : Core.Lang[LangId.FrmMain_MnuSetDesktopBackground_Error];

            _ = await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = title,
                Heading = heading,
                Description = ex.Message,
            });
        }
    }


    #endregion // Image APIs



    #region Clipboard APIs

    /// <summary>
    /// Opens image from clipboard.
    /// </summary>
    private async Task IG_PasteImageAsync()
    {
        if (_mainWindow.Clipboard is null) return;

        using var data = await _mainWindow.Clipboard.TryGetDataAsync();
        if (data is null) return;


        // 1. if clipboard contains a file
        if (data.Contains(DataFormat.File))
        {
            var fileItem = await data.TryGetFileAsync();
            var filePath = fileItem?.TryGetLocalPath();

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                IG_OpenPath(filePath);
            }
            return;
        }


        // 2. if clipboard contains image pixels
        if (data.Contains(DataFormat.Bitmap))
        {
            var abmp = await data.TryGetBitmapAsync();
            var skBmp = SkiaCodec.FromBitmap(abmp);
            if (skBmp is not null)
            {
                var photo = new Photo(skBmp);
                await LoadClipboardPhotoAsync(photo);
            }

            return;
        }


        // 3. if clipboard contains file path
        if (data.Contains(DataFormat.Text))
        {
            var text = await data.TryGetTextAsync();
            var path = BHelper.ResolvePath(text);

            // 3.1 try to get absolute path
            if (File.Exists(path) || Directory.Exists(path))
            {
                IG_OpenPath(path);
                return;
            }


            // 3.2 get photo from base64 string
            try
            {
                var photo = await MagickCodec.DecodeBase64Async(text);
                if (photo is not null)
                {
                    await LoadClipboardPhotoAsync(photo);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌❌❌ IG_PasteImageAsync: {ex.Message}");
            }
        }

    }


    public async Task LoadClipboardPhotoAsync(Photo? photo)
    {
        // cancel the current loading image
        Core.Photos.Current?.CancelLoading();

        await _mainWindow.PART_MainView.ViewPhotoAsync(photo, true, false);

        Core.ClipboardImage = photo;
    }


    /// <summary>
    /// Copies image pixels.
    /// </summary>
    public async Task IG_CopyImagePixelsAsync()
    {
        if (Viewer.SourceKind == PhotoSource.None || _mainWindow.Clipboard is null) return;

        // 1. get rendered bitmap
        var bmp = Viewer.GetRenderedBitmap(!Viewer.SourceSelection.IsEmpty);
        if (bmp.IsDisposed()) return;


        // 2. show message
        await Message.ClearAsync();
        _ = Message.ShowAsync(Core.Lang[LangId.FrmMain_MnuCopyImagePixels_Copying], delayMs: 1000);


        // 3. copy to clipboard
        try
        {
            var abmp = SkiaCodec.ToWritableBitmap(bmp);
            await _mainWindow.Clipboard.SetBitmapAsync(abmp);

            _ = Message.ShowAsync(Core.Lang[LangId.FrmMain_MnuCopyImagePixels_Success]);
        }
        catch (Exception ex)
        {
            await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.FrmMain_MnuCopyImagePixels],
                Description = ex.Message,
            });
        }
    }


    /// <summary>
    /// Copy the current image path.
    /// </summary>
    public async Task IG_CopyImagePathAsync()
    {
        if (string.IsNullOrWhiteSpace(Core.Photos.CurrentFilePath) || _mainWindow.Clipboard is null) return;

        try
        {
            await _mainWindow.Clipboard.SetTextAsync(Core.Photos.CurrentFilePath);

            // show message
            _ = Message.ShowAsync(Core.Lang[LangId.FrmMain_MnuCopyPath_Success]);
        }
        catch { }
    }


    /// <summary>
    /// Copies the current photo file.
    /// </summary>
    public async Task IG_CopyFilesAsync()
    {
        await SetFileToClipboardAsync(Core.Photos.CurrentFilePath, false);
    }


    /// <summary>
    /// Cuts the current photo file.
    /// </summary>
    public async Task IG_CutFilesAsync()
    {
        await SetFileToClipboardAsync(Core.Photos.CurrentFilePath, true);
    }


    /// <summary>
    /// Sets file to clipboard
    /// </summary>
    private async Task SetFileToClipboardAsync(string? filePath, bool forCutting)
    {
        if (_mainWindow.Clipboard is null || !File.Exists(filePath)) return;

        // 1. cut/copy single file
        if (forCutting)
        {
            if (!Core.Config.EnableCutMultipleFiles)
            {
                Core.StringClipboard.Clear();
            }
        }
        else
        {
            if (!Core.Config.EnableCopyMultipleFiles)
            {
                Core.StringClipboard.Clear();
            }
        }


        // 2. try adding current photo path to clipboard paths
        _ = Core.StringClipboard.Add(filePath);


        // 3. set files to clipboard
        try
        {
            var dt = new DataTransfer();
            foreach (var path in Core.StringClipboard)
            {
                var fi = await _mainWindow.StorageProvider.TryGetFileFromPathAsync(path);
                if (fi is null) continue;

                var dti = new DataTransferItem();
                dti.SetFile(fi);
                dt.Add(dti);
            }


            // 4. perform copy/cut
            await _mainWindow.Clipboard.SetDataAsync(dt);

            _ = Message.ShowAsync(Core.Lang[forCutting
                    ? LangId.FrmMain_MnuCutFile_Success
                    : LangId.FrmMain_MnuCopyFile_Success,
                Core.StringClipboard.Count]);
        }
        catch (Exception ex)
        {
            await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[forCutting ? LangId.FrmMain_MnuCutFile : LangId.FrmMain_MnuCopyFile],
                Description = ex.Message,
            });
        }
    }


    /// <summary>
    /// Clears clipboard.
    /// </summary>
    public async Task IG_ClearClipboardAsync()
    {
        // clear clipboard
        Core.StringClipboard.Clear();

        if (_mainWindow.Clipboard is not null)
        {
            await _mainWindow.Clipboard.ClearAsync();
        }


        // show message
        _ = Message.ShowAsync(Core.Lang[LangId.FrmMain_MnuClearClipboard_Success]);
    }

    #endregion // Clipboard APIs



    #region Window Mode APIs

    /// <summary>
    /// Toggles window fit mode.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public void IG_ToggleWindowFit(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleWindowFit(enabled);
    }


    /// <summary>
    /// Toggles window fit mode.
    /// </summary>
    public void IG_ToggleWindowFit(bool? enabled = null)
    {
        enabled ??= !Core.Config.EnableWindowFit;
        Core.Config.EnableWindowFit = enabled.Value;

        if (Core.Config.EnableWindowFit)
        {
            // exit full screen
            if (Core.Config.EnableFullScreen)
            {
                IG_ToggleFullScreen(false);
            }
        }


        // set Window Fit mode
        ApplyWindowFitMode(true);
    }


    /// <summary>
    /// Adjusts the main window size and position to fit the displayed image within the available screen area.
    /// </summary>
    public void ApplyWindowFitMode(bool resetZoomMode = true)
    {
        if (!Core.Config.EnableWindowFit || Viewer.SourceKind == PhotoSource.None) return;

        // 1. reset window state
        _mainWindow.WindowState = WindowState.Normal;


        // 2. get the size
        var dpi = _mainWindow.Dpi;
        var toolbarPos = Config.GetControlLayout(LayoutControl.Toolbar);
        var galleryPos = Config.GetControlLayout(LayoutControl.Gallery);

        var frameSize = Core.Config.EnableFrameless ? new() : new Size(2, 32);
        var gapW = 0d;
        var gapH = Toolbar.Bounds.Height;

        if (galleryPos is LayoutPosition.Left or LayoutPosition.Right)
        {
            gapW += Gallery.Bounds.Width + GalleryResizer.Bounds.Width;
        }
        else
        {
            gapH += Gallery.Bounds.Height + GalleryResizer.Bounds.Height;
        }


        // get current screen workarea
        var screen = _mainWindow.Screens.ScreenFromWindow(_mainWindow)!;
        var workArea = screen.WorkingArea.ToRect(dpi);

        // get source image size
        var srcImgW = Viewer.BitmapSize.Width;
        var srcImgH = Viewer.BitmapSize.Height;


        // 3. calculate zoom factor for the new size
        var zoomFactor = Viewer.ZoomFactor;
        if (resetZoomMode)
        {
            if (Core.Config.ZoomMode == ZoomMode.LockZoom)
            {
                Viewer.SetZoomFactor(Core.Config.ZoomLockValue / 100f, false);
            }
            else
            {
                var maxViewerWidth = workArea.Width - gapW;
                var maxViewerHeight = workArea.Height - gapH;

                // recalculate zoom factor for the new size
                zoomFactor = Viewer.CalculateZoomFactor(Core.Config.ZoomMode, srcImgW, srcImgH, maxViewerWidth, maxViewerHeight);
            }
        }


        // 4. apply zoom factor to the image size
        var zoomImgW = (int)(srcImgW * zoomFactor / dpi);
        var zoomImgH = (int)(srcImgH * zoomFactor / dpi);


        // 5. adjust the viewer size to fit the entire image
        // but not larger than desktop working area.
        var viewerBounds = new Rect(0, 0,
            Math.Min(zoomImgW, workArea.Width - gapW),
            Math.Min(zoomImgH, workArea.Height - gapH));

        var workAreaWithoutFrame = new Rect(
            workArea.X + gapW / 2 + frameSize.Width / 2,
            workArea.Y + gapH / 2 + frameSize.Height / 2,
            workArea.Width - gapW - frameSize.Width,
            workArea.Height - gapH - frameSize.Height);

        // adjust viewer size and position to the desktop working area
        viewerBounds = workAreaWithoutFrame.CenterRectEx(viewerBounds, true);

        // add the gaps to make window bound
        var winBounds = new Rect(
            viewerBounds.X - gapW / 2 - frameSize.Width / 2,
            viewerBounds.Y - gapH / 2 - frameSize.Height / 2,
            viewerBounds.Width + gapW,
            viewerBounds.Height + gapH);


        // check center window to screen option
        if (!Core.Config.EnableCenterWindowFit)
        {
            winBounds = winBounds.WithX(_mainWindow.Position.X / dpi);
            winBounds = winBounds.WithY(_mainWindow.Position.Y / dpi);
        }


        // 6. set min size for window
        _mainWindow.MinWidth = gapW + 50;
        _mainWindow.MinHeight = gapH + 50;

        // update window position and size
        _mainWindow.Position = new((int)(winBounds.X * dpi), (int)(winBounds.Y * dpi));
        _mainWindow.Width = winBounds.Width;
        _mainWindow.Height = winBounds.Height;

        if (resetZoomMode)
        {
            Viewer.SetZoomFactor(zoomFactor, false);
        }

    }


    /// <summary>
    /// Toggles frameless mode.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public void IG_ToggleFrameless(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleFrameless(enabled);
    }


    /// <summary>
    /// Toggles frameless mode.
    /// </summary>
    public void IG_ToggleFrameless(bool? enabled = null)
    {
        if (BHelper.OS == OSType.Linux)
        {
            throw new NotSupportedException($"IGE: This feature is not supported on {BHelper.OS}.");
        }


        enabled ??= !Core.Config.EnableFrameless;

        // set frameless mode
        SetFramelessMode__(enabled.Value, true);
    }
    private void SetFramelessMode__(bool enabled, bool showMessage)
    {
        Core.Config.EnableFrameless = enabled;


        // set frameless mode
        if (enabled)
        {
            // exit full screen
            if (Core.Config.EnableFullScreen) IG_ToggleFullScreen(false);

            _mainWindow.IsFrameless = true;
        }

        // restore frame
        else
        {
            _mainWindow.IsFrameless = false;
        }


        // update window fit
        if (Core.Config.EnableWindowFit)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ApplyWindowFitMode(!Viewer.IsManualZoom);
            });
        }


        // show message
        if (showMessage && enabled)
        {
            _ = Message.ShowAsync(
                Core.Lang[LangId.FrmMain_MnuFrameless_EnableDescription],
                Core.Lang[LangId.FrmMain_MnuFrameless]);
        }
    }


    /// <summary>
    /// Toggles fullscreen mode.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public void IG_ToggleFullScreen(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleFullScreen(enabled);
    }


    /// <summary>
    /// Toggles fullscreen mode.
    /// </summary>
    public void IG_ToggleFullScreen(bool? enabled = null)
    {
        enabled ??= !Core.Config.EnableFullScreen;
        SetFullScreenMode__(enabled.Value,
            !Core.Config.ShowToolbarInFullscreen, !Core.Config.ShowGalleryInFullscreen);
    }
    private void SetFullScreenMode__(bool enabled, bool hideToolbar = false, bool hideThumbnails = false)
    {
        Core.Config.EnableFullScreen = enabled;

        // enable full screen mode
        if (enabled)
        {
            // exit window fit
            if (Core.Config.EnableWindowFit)
            {
                _isWindowFitBeforeFullscreen = true;
                IG_ToggleWindowFit(false);
            }

            // exit frameless
            if (Core.Config.EnableFrameless)
            {
                _isFramelessBeforeFullscreen = true;
                SetFramelessMode__(false, false);
            }

            // back up layout & window state
            _windowBound = _mainWindow.Bounds;
            _windowMaximized = _mainWindow.WindowState == WindowState.Maximized;
            _showToolbar = Core.Config.ShowToolbar;
            _showGallery = Core.Config.ShowGallery;


            // enable fullscreen
            _mainWindow.WindowState = WindowState.FullScreen;


            // hide toolbar & gallery
            if (hideToolbar) IG_ToggleToolbar(false);
            if (hideThumbnails) _ = IG_ToggleGalleryAsync(false);
        }

        // disable full screen mode
        else
        {
            // restore layout
            IG_ToggleToolbar(_showToolbar);
            _ = IG_ToggleGalleryAsync(_showGallery);


            // restore window state, size, position
            Core.Config.EnableMainWindowMaximized = _windowMaximized;
            _mainWindow.WindowState = _windowMaximized
                ? WindowState.Maximized
                : WindowState.Normal;


            // restore frameless, window fit mode when exiting full screen
            if (_isFramelessBeforeFullscreen) SetFramelessMode__(true, false);
            if (_isWindowFitBeforeFullscreen) IG_ToggleWindowFit(true);
        }
    }


    /// <summary>
    /// Toggles slideshow mode.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public void IG_ToggleSlideshow(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleSlideshow(enabled);
    }


    /// <summary>
    /// Toggles slideshow mode.
    /// </summary>
    public void IG_ToggleSlideshow(bool? enabled = null)
    {
        enabled ??= !Core.Config.EnableSlideshow;

        if (enabled.Value)
        {
            StartSlideshow__();
        }
        else
        {
            StopSlideshow__();
        }
    }
    private void StartSlideshow__()
    {
        if (Core.Config.EnableSlideshow) return;
        if (Core.Photos.Count == 0) return;

        // 1. back up current window state
        _isFullScreenBeforeSlideshow = Core.Config.EnableFullScreen;
        _isFramelessBeforeSlideshow = Core.Config.EnableFrameless;
        _isWindowFitBeforeSlideshow = Core.Config.EnableWindowFit;
        _showToolbarBeforeSlideshow = Core.Config.ShowToolbar;
        _showGalleryBeforeSlideshow = Core.Config.ShowGallery;
        _windowBoundBeforeSlideshow = _mainWindow.Bounds;
        _windowMaximizedBeforeSlideshow = _mainWindow.WindowState == WindowState.Maximized;


        // 2. enter full screen if configured
        if (Core.Config.EnableFullscreenSlideshow && !Core.Config.EnableFullScreen)
        {
            // exit window fit and frameless first
            if (Core.Config.EnableWindowFit) IG_ToggleWindowFit(false);
            if (Core.Config.EnableFrameless) SetFramelessMode__(false, false);

            _mainWindow.WindowState = WindowState.FullScreen;
            Core.Config.EnableFullScreen = true;
        }

        // hide toolbar and gallery
        IG_ToggleToolbar(false);
        _ = IG_ToggleGalleryAsync(false);


        // 3. create and start the slideshow service
        var slideshow = new SlideshowProvider();
        slideshow.NextPhotoRequested += OnSlideshowNextPhoto__;
        Core.Slideshow = slideshow;

        Core.Config.EnableSlideshow = true;
        slideshow.Start();


        // 4. start countdown refresh timer for the viewer overlay
        SetSlideshowCountdown(Core.Config.EnableSlideshowCountdown);
    }

    private void StopSlideshow__()
    {
        if (!Core.Config.EnableSlideshow) return;

        Core.Config.EnableSlideshow = false;

        // 1. stop countdown timer
        SetSlideshowCountdown(false);


        // 2. stop and dispose the slideshow service
        if (Core.Slideshow is { } service)
        {
            service.NextPhotoRequested -= OnSlideshowNextPhoto__;
            service.Dispose();
            Core.Slideshow = null;
        }


        // 3. restore window state
        if (Core.Config.EnableFullscreenSlideshow && !_isFullScreenBeforeSlideshow)
        {
            Core.Config.EnableFullScreen = false;

            Core.Config.EnableMainWindowMaximized = _windowMaximizedBeforeSlideshow;
            _mainWindow.WindowState = _windowMaximizedBeforeSlideshow
                ? WindowState.Maximized
                : WindowState.Normal;
        }

        // restore toolbar and gallery
        IG_ToggleToolbar(_showToolbarBeforeSlideshow);
        _ = IG_ToggleGalleryAsync(_showGalleryBeforeSlideshow);

        // restore frameless and window fit
        if (_isFramelessBeforeSlideshow) SetFramelessMode__(true, false);
        if (_isWindowFitBeforeSlideshow) IG_ToggleWindowFit(true);
    }

    private void OnSlideshowNextPhoto__()
    {
        _slideshowIsAdvancing = true;
        IG_ViewNext();
        _slideshowIsAdvancing = false;
    }



    /// <summary>
    /// Toggle slideshow countdown.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public void IG_ToggleSlideshowCountdown(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleSlideshowCountdown(enabled);
    }


    /// <summary>
    /// Toggle slideshow countdown.
    /// </summary>
    public void IG_ToggleSlideshowCountdown(bool? enabled = null)
    {
        enabled ??= !Core.Config.EnableSlideshowCountdown;
        Core.Config.EnableSlideshowCountdown = enabled.Value;

        SetSlideshowCountdown(enabled.Value);
    }
    private void SetSlideshowCountdown(bool enabled)
    {
        if (enabled)
        {
            // start countdown timer
            _slideshowCountdownTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(100),
                DispatcherPriority.Render,
                (_, _) => SlideshowCountdown.InvalidateVisual());
            _slideshowCountdownTimer.Start();
        }
        else
        {
            // stop countdown timer
            _slideshowCountdownTimer?.Stop();
            _slideshowCountdownTimer = null;
            SlideshowCountdown.InvalidateVisual();
        }
    }


    /// <summary>
    /// Plays or pauses the current slideshow.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public void IG_ToggleSlideshowPlayback(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleSlideshowPlayback(enabled);
    }


    /// <summary>
    /// Plays or pauses the current slideshow.
    /// </summary>
    public void IG_ToggleSlideshowPlayback(bool? enabled = null)
    {
        if (Core.Slideshow?.IsRunning != true) return;
        var isPaused = Core.Slideshow?.IsPaused ?? false;

        if (isPaused) Core.Slideshow?.Resume();
        else Core.Slideshow?.Pause();

        _ = Message.ShowAsync(Core.Lang[isPaused
            ? LangId.FrmSlideshow_ResumeSlideshow
            : LangId.FrmSlideshow_PauseSlideshow]);
    }

    #endregion // Window Modes APIs



    #region Layout APIs

    /// <summary>
    /// Toggles visibility of toolbar.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public void IG_ToggleToolbar(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleToolbar(enabled);
    }


    /// <summary>
    /// Toggles visibility of toolbar.
    /// </summary>
    public void IG_ToggleToolbar(bool? enabled = null)
    {
        enabled ??= !Core.Config.ShowToolbar;
        Core.Config.ShowToolbar = enabled.Value;

        // update window fit
        if (Core.Config.EnableWindowFit)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ApplyWindowFitMode(!Viewer.IsManualZoom);
            });
        }
    }


    /// <summary>
    /// Toggles visibility of gallery.
    /// </summary>
    public async Task IG_ToggleGalleryAsync(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        await IG_ToggleGalleryAsync(enabled);
    }


    /// <summary>
    /// Toggles visibility of gallery
    /// </summary>
    public async Task IG_ToggleGalleryAsync(bool? enabled = null)
    {
        enabled ??= !Core.Config.ShowGallery;
        Core.Config.ShowGallery = enabled.Value;

        if (enabled.Value)
        {
            Gallery.ScrollToItem(Core.Photos.CurrentIndex);
        }

        _mainWindow.PART_MainView.ApplyAppLayout();

        // update window fit
        if (Core.Config.EnableWindowFit)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ApplyWindowFitMode(!Viewer.IsManualZoom);
            });
        }
    }


    /// <summary>
    /// Toggles the viewer's checkerboard mode.
    /// </summary>
    public static void IG_ToggleCheckerboard(string? mode = null)
    {
        var isValid = Enum.TryParse<CheckerboardType>(mode, out var wantedMode);

        IG_ToggleCheckerboard(isValid ? wantedMode : null);
    }


    /// <summary>
    /// Toggles the viewer's checkerboard mode.
    /// </summary>
    public static void IG_ToggleCheckerboard(CheckerboardType? mode = null)
    {
        var wantedMode = Core.Config.CheckerboardMode;

        if (mode is not null)
        {
            wantedMode = mode.Value;
        }
        else
        {
            var hasAlpha = Core.Photos.CurrentMetadata?.HasAlpha ?? false;

            if (Core.Config.CheckerboardMode == CheckerboardType.None)
            {
                wantedMode = hasAlpha
                    ? CheckerboardType.Image
                    : CheckerboardType.Client;
            }
            else if (Core.Config.CheckerboardMode == CheckerboardType.Image)
            {
                wantedMode = CheckerboardType.Client;
            }
            else if (Core.Config.CheckerboardMode == CheckerboardType.Client)
            {
                wantedMode = CheckerboardType.None;
            }
        }

        Core.Config.CheckerboardMode = wantedMode;
    }


    /// <summary>
    /// Toggles window top most.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public void IG_ToggleWindowTopMost(string? boolStr = null)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr);
        IG_ToggleWindowTopMost(enabled);
    }


    /// <summary>
    /// Toggles window top most.
    /// </summary>
    public void IG_ToggleWindowTopMost(bool? enabled = null)
    {
        enabled ??= !Core.Config.EnableWindowTopMost;
        Core.Config.EnableWindowTopMost = enabled.Value;

        _ = Message.ShowAsync(Core.Lang[enabled.Value
            ? LangId.FrmMain_MnuToggleTopMost_Enable
            : LangId.FrmMain_MnuToggleTopMost_Disable]);
    }


    /// <summary>
    /// Sets background color,
    /// opens Color Picker dialog if the <paramref name="hexColor"/> is <c>null</c>.
    /// </summary>
    public async Task IG_SetBackgroundColorAsync(string? hexColor = null)
    {
        Color? newColor = null;

        // 1. open Color picker to select color if hexColor not defined
        if (string.IsNullOrEmpty(hexColor))
        {
            var oldColor = Resx.Get<SolidColorBrush>(ResxId.IG_ViewerBackgroundBrush).Color;
            var defaultColor = Core.Config.EnableSlideshow
                ? Colors.Black
                : Const.COLOR_EMPTY;

            var cp = new PhColorPickerDialog(oldColor, defaultColor)
            {
                Title = Core.Lang[LangId.FrmMain_MnuChangeBackgroundColor],
            };
            var result = await cp.ShowAsync(_mainWindow);

            if (result != DialogExitCode.OK) return;
            newColor = cp.SelectedColor;
        }

        // 2. convert the input hex color string
        else
        {
            newColor = BHelper.ColorFromHex(hexColor);
        }

        if (newColor == null) return;


        // 3. set background color
        if (Core.Config.EnableSlideshow)
        {
            Core.Config.SlideshowBackgroundColor = newColor.Value.ToHex();
        }
        else
        {
            Core.Config.BackgroundColor = newColor.Value.ToHex();
        }
    }

    #endregion // Layout APIs



    #region Tools APIs

    /// <summary>
    /// Toggles a tool by ID. Non-hosted tools only support open (toggle = open).
    /// </summary>
    public void IG_ToggleTool(string? toolId)
    {
        if (string.IsNullOrEmpty(toolId)) return;
        if (Core.ToolRegistry.Get(toolId) is not { } tool) return;

        if (tool.IsHosted)
        {
            var currentToolId = ToolHost.Tool?.ToolId;

            if (string.Equals(currentToolId, toolId, StringComparison.Ordinal))
            {
                // Close: save settings first, then close UI
                if (ToolHost.Tool is { } currentTool)
                {
                    ToolRegistry.SaveToolSettings(currentTool);
                }

                ToolHost.CloseCurrentTool();
            }
            else
            {
                // Open: close current (with save), then open new
                IG_CloseTool(currentToolId);

                if (tool is ToolControlAdapter adapter)
                {
                    var control = adapter.CreateToolControl(Viewer);
                    ToolRegistry.LoadToolSettings(control);
                    ToolHost.OpenTool(control);
                }
            }
        }
        else
        {
            // Non-hosted tools can only be opened, not toggled closed
            IG_OpenTool(toolId);
        }
    }


    /// <summary>
    /// Opens a tool by ID. Handles both hosted and non-hosted plugins.
    /// Settings are loaded before the tool is opened/executed.
    /// </summary>
    public void IG_OpenTool(string? toolId)
    {
        if (string.IsNullOrEmpty(toolId)) return;
        if (Core.ToolRegistry.Get(toolId) is not { } tool) return;

        if (tool.IsHosted)
        {
            // Hosted tool: create via adapter, load settings, host in ToolHostControl
            var currentToolId = ToolHost.Tool?.ToolId;
            if (string.Equals(currentToolId, toolId, StringComparison.Ordinal)) return;

            // Close current tool (with save) before opening new one
            IG_CloseTool(currentToolId);

            if (tool is ToolControlAdapter adapter)
            {
                var control = adapter.CreateToolControl(Viewer);
                ToolRegistry.LoadToolSettings(control);
                ToolHost.OpenTool(control);
            }
        }
        else
        {
            if (tool is ExternalToolProxy)
            {
                // Only allow one instance of a non-hosted external tool
                if (Core.ToolRegistry.ExternalTools.IsRunning(toolId)) return;

                // External non-hosted tool: start process and execute
                _ = tool.ExecuteAsync(new ToolExecutionContext { Window = _mainWindow });
            }
            else
            {
                // Built-in non-hosted tool: set viewer, load settings, execute
                tool.Viewer = Viewer;
                ToolRegistry.LoadToolSettings(tool);

                var context = new ToolExecutionContext { Window = _mainWindow };
                _ = ToolRegistry.ExecuteNonHostedToolAsync(tool, context);
            }
        }
    }


    /// <summary>
    /// Closes a tool by ID. Only applicable to hosted tools.
    /// Saves settings before closing.
    /// </summary>
    public void IG_CloseTool(string? toolId)
    {
        if (string.IsNullOrEmpty(toolId)) return;

        // Save settings for the current hosted tool before closing
        if (ToolHost.Tool is ITool currentTool
            && string.Equals(currentTool.ToolId, toolId, StringComparison.Ordinal))
        {
            ToolRegistry.SaveToolSettings(currentTool);
        }

        ToolHost.CloseTool(toolId);
    }


    /// <summary>
    /// Closes the currently active tool in the tool host, if one is open.
    /// </summary>
    public void IG_CloseCurrentTool()
    {
        if (ToolHost.Tool is ITool currentTool)
        {
            ToolRegistry.SaveToolSettings(currentTool);
        }
        ToolHost.CloseCurrentTool();
    }


    /// <summary>
    /// Opens website to download more tools.
    /// </summary>
    public void IG_GetMoreTools()
    {
        _ = BHelper.OpenUrlAsync(_mainWindow, "https://imageglass.org/tools", "from_get_more_tools");
    }

    #endregion // Tools APIs



    #region Help APIs

    /// <summary>
    /// Open About window.
    /// </summary>
    public async Task IG_OpenAboutWindowAsync()
    {
        var dialog = new AboutWindow();
        _ = await dialog.ShowAsync(_mainWindow);
    }


    /// <summary>
    /// Checks for new update asynchronously with option to shows UI feedback.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public async Task IG_CheckForUpdateAsync(string? boolStr = null)
    {
        var showUI = BHelper.ConvertStringToBool(boolStr);
        await IG_CheckForUpdateAsync(showUI ?? true);
    }


    /// <summary>
    /// Checks for new update asynchronously with option to shows UI feedback.
    /// </summary>
    public async Task IG_CheckForUpdateAsync(bool showUI = true)
    {
        // silent mode: skip if disabled or checked recently
        if (!showUI)
        {
            if (string.Equals(Core.Config.AutoUpdate, "0", StringComparison.Ordinal)) return;
            if (!UpdateProvider.ShouldCheck) return;

            // delay to let the app finish starting
            await Task.Delay(TimeSpan.FromSeconds(30));
        }


        UpdateWindow? updateWindow = null;

        if (showUI)
        {
            // open UpdateWindow immediately in "checking" state
            updateWindow = new UpdateWindow();
            updateWindow.SetCheckingState();

            // show the window non-blocking, then perform the check
            _ = updateWindow.ShowAsync(_mainWindow);
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = await Core.Update.CheckForUpdateAsync(cts.Token);

        if (showUI)
        {
            // transition the already-open window to result state
            updateWindow!.SetResultState(result);
        }
        else
        {
            // silent mode: only show window if update is available
            if (result.Status == Update.UpdateCheckStatus.UpdateAvailable)
            {
                updateWindow = new UpdateWindow();
                updateWindow.SetResultState(result);
                _ = await updateWindow.ShowAsync(_mainWindow);
            }
        }
    }


    /// <summary>
    /// Opens website to report issue.
    /// </summary>
    public void IG_ReportIssue()
    {
        _ = BHelper.OpenUrlAsync(_mainWindow,
            "https://github.com/d2phap/ImageGlass/issues?q=is%3Aissue+",
            "from_report_issue");
    }


    /// <summary>
    /// Registers the app as the default photo viewer.
    /// </summary>
    public async Task IG_SetDefaultPhotoViewerAsync()
    {
        await SetDefaultPhotoViewerAsync(true);
    }


    /// <summary>
    /// Unregisters the app from the default photo viewer.
    /// </summary>
    public async Task IG_RemoveDefaultPhotoViewerAsync()
    {
        await SetDefaultPhotoViewerAsync(false);
    }


    /// <summary>
    /// Sets or removes the app as the default photo viewer for supported file formats.
    /// </summary>
    private async Task SetDefaultPhotoViewerAsync(bool enable)
    {
        if (Core.ShellProvider is null) return;

        var extensions = Core.Config.FileFormats.ToArray();

        try
        {
            await Core.ShellProvider.SetDefaultPhotoViewerAsync(extensions, enable);

            await ModalWindow.ShowInfoAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[enable
                    ? LangId.FrmMain_MnuSetDefaultPhotoViewer
                    : LangId.FrmMain_MnuRemoveDefaultPhotoViewer],
                Heading = Core.Lang[enable
                    ? LangId.FrmMain_MnuSetDefaultPhotoViewer_Success
                    : LangId.FrmMain_MnuRemoveDefaultPhotoViewer_Success],
                Note = enable ? Core.Lang[LangId.FrmSettings_UnmanagedSettingReminder] : null,
                NoteStyle = InfoBarSeverity.Warning,
            });
        }
        catch (Exception ex)
        {
            await ModalWindow.ShowErrorAsync(_mainWindow, new ModalWindowOptions
            {
                Title = Core.Lang[enable
                    ? LangId.FrmMain_MnuSetDefaultPhotoViewer
                    : LangId.FrmMain_MnuRemoveDefaultPhotoViewer],
                Heading = Core.Lang[enable
                    ? LangId.FrmMain_MnuSetDefaultPhotoViewer_Error
                    : LangId.FrmMain_MnuRemoveDefaultPhotoViewer_Error],
                Description = ex.Message,
                Details = ex.ToString(),
            });
        }
    }


    #endregion // Help APIs



    #region Other APIs

    /// <summary>
    /// Sets the real-time file update engine.
    /// </summary>
    /// <param name="boolStr">Values: <c>"true"</c>, <c>"false"</c> or empty.</param>
    public static void IG_SetFileWatcher(string? boolStr)
    {
        var enabled = BHelper.ConvertStringToBool(boolStr) ?? false;
        IG_SetFileWatcher(enabled);
    }


    /// <summary>
    /// Sets the real-time file update engine.
    /// </summary>
    public static void IG_SetFileWatcher(bool enabled)
    {
        Core.Config.EnableFileWatcher = enabled;
    }


    /// <summary>
    /// Sets the real-time file update engine without updating the setting <see cref="Config.EnableFileWatcher"/>.
    /// </summary>
    public static void SetFileWatcher(bool enabled)
    {
        if (enabled)
        {
            // always prefer the current photo list's directory so the watcher
            // follows folder changes (e.g. opening a photo in a new folder)
            var dirPath = Core.Photos.DistinctDirs.FirstOrDefault();

            if (string.IsNullOrEmpty(dirPath))
            {
                dirPath = Core.Photos.FileWatcherFolderPath;
            }

            if (!string.IsNullOrEmpty(dirPath))
            {
                Core.Photos.StartFileWatcher(dirPath);
            }
        }
        else
        {
            Core.Photos.StopFileWatcher();
        }
    }


    /// <summary>
    /// Opens the context menu associated with the viewer.
    /// </summary>
    public void IG_OpenContextMenu()
    {
        Viewer.ContextMenu?.Open();
    }


    #endregion // Other APIs

}
