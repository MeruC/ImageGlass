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
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ImageGlass.Common.Localization;


[JsonSerializable(typeof(Lang))]
public partial class LangJsonContext : JsonSerializerContext { }


/// <summary>
/// ImageGlass language pack (<c>*.iglang.json</c>)
/// </summary>
public class Lang
{

    #region JSON Serializable Properties

    /// <summary>
    /// Gets, sets the language metadata.
    /// </summary>
    public LangMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Gets, sets the language string dictionary.
    /// </summary>
    public IDictionary<LangId, string> Items { get; set; } = FrozenDictionary<LangId, string>.Empty;

    #endregion // JSON Serializable Properties



    #region Non-Serializable Properties

    /// <summary>
    /// Gets the path of language file.
    /// Example: <c>C:\ImageGlass\Languages\Vietnameses.iglang.json</c>
    /// </summary>
    [JsonIgnore]
    private string FilePath { get; set; } = "English";


    /// <summary>
    /// Gets the name of language file.
    /// Example: <c>Vietnameses.iglang.json</c>
    /// </summary>
    [JsonIgnore]
    public string FileName => Path.GetFileName(FilePath);


    /// <summary>
    /// Gets the formatted language string. If not exist, returns the key name.
    /// </summary>
    /// <param name="key">The key to get the language string</param>
    /// <param name="args">The arguments to format the language string.</param>
    /// <remarks>
    /// This is a shortcut for <see cref="Get(string, object?[])"/> method.
    /// </remarks>
    [JsonIgnore]
    public string this[string? key, params object?[] args] => Get(key, args);


    /// <summary>
    /// Gets the formatted language string. If not exist, returns empty string.
    /// </summary>
    /// <param name="key">The key to get the language string</param>
    /// <param name="args">The arguments to format the language string.</param>
    /// <remarks>
    /// This is a shortcut for <see cref="Get(LangId?, object?[])"/> method.
    /// </remarks>
    [JsonIgnore]
    public string this[LangId? key, params object?[] args] => Get(key, args);

    #endregion // Non-Serializable Properties



    #region Instance Initialization

    /// <summary>
    /// Initializes a language pack.
    /// </summary>
    public Lang() { }


    /// <summary>
    /// Initializes a language pack.
    /// </summary>
    /// <param name="filePath">E.g. <c>C:\ImageGlass\Language\Vietnamese.iglang.json</c></param>
    public Lang(string filePath)
    {
        FilePath = filePath;
    }

    #endregion // Instance Initialization



    #region Public Methods

    /// <summary>
    /// Reads <see cref="FilePath"/> and loads language strings.
    /// </summary>
    public async Task LoadAsync()
    {
        if (!File.Exists(FilePath)) return;

        // 1. create json context
        var jsonOptions = BHelper.CreateJsonOptions();
        var jsonContext = new LangJsonContext(jsonOptions);

        try
        {
            // 2. load language strings
            var lang = await BHelper.ReadJsonFromFileAsync(FilePath, jsonContext.Lang);
            if (lang == null) return;

            // 3. store the language strings
            Metadata = lang.Metadata;
            Items = lang.Items.ToFrozenDictionary();
        }
        catch { }
    }


    /// <summary>
    /// Saves current language to JSON file.
    /// </summary>
    public async Task SaveAsFileAsync(string filePath)
    {
        var lang = new Lang()
        {
            Metadata = Metadata,
            Items = Items,
        };

        if (Metadata.EnglishName.Equals("English", StringComparison.OrdinalIgnoreCase))
        {
            lang.Metadata.EnglishName = "<Your_language_name_in_English>";
            lang.Metadata.LocalName = "<Local_name_of_your_language>";
            lang.Metadata.Author = "<Your_name_here>";
        }


        var jsonOptions = BHelper.CreateJsonOptions();
        var jsonContext = new LangJsonContext(jsonOptions);

        await BHelper.WriteJsonToFileAsync(filePath, lang, jsonContext.Lang);
    }


    /// <summary>
    /// Gets a valid <see cref="LangId"/> from string.
    /// </summary>
    public static LangId? GetKey(string? key)
    {
        if (Enum.TryParse<LangId>(key, out var langKey))
        {
            return langKey;
        }

        return null;
    }


    /// <summary>
    /// Gets the formatted language string. If not exist, returns the key.
    /// </summary>
    /// <param name="key">The key to get the language string</param>
    /// <param name="args">The arguments to format the language string.</param>
    public string Get(string? key, params object?[] args)
    {
        if (GetKey(key) is LangId langKey)
        {
            return Get(langKey, args);
        }

        return key ?? string.Empty;
    }


    /// <summary>
    /// Gets the formatted language string. If not exist, returns empty string.
    /// </summary>
    /// <param name="key">The key to get the language string</param>
    /// <param name="args">The arguments to format the language string.</param>
    public string Get(LangId? key, params object?[] args)
    {
        if (key is null) return string.Empty;
        string? value = null;


        // 1. try getting value from language file
        if (Items.TryGetValue(key.Value, out value))
        {
            // do nothing
        }

        // 2. try getting value from default language dictionary
        else if (DefaultLangMap.TryGetValue(key.Value, out value))
        {
            // do nothing
        }
        else
        {
            return string.Empty;
        }


        // 3. if value has arguments, return the formatted string
        if (args.Length > 0)
        {
            return string.Format(value, args);
        }

        // 4. returns the non-formatted string
        return value;
    }


    /// <summary>
    /// Gets the formatted language string. If not exist, returns the key.
    /// </summary>
    /// <param name="key">The key to get the language string</param>
    public string Get(string? key) => Get(key, []);


    /// <summary>
    /// Gets the formatted language string. If not exist, returns empty string.
    /// </summary>
    /// <param name="key">The key to get the language string</param>
    public string Get(LangId? key) => Get(key, []);

    #endregion // Public Methods



    /// <summary>
    /// Map of <see cref="LangId"/> and language key.
    /// </summary>
    public static FrozenDictionary<LangId, string> KeysMap => new Dictionary<LangId, string>(
            Enum.GetNames<LangId>()
                .Select(langKey => new KeyValuePair<LangId, string>(Enum.Parse<LangId>(langKey), langKey)))
        .ToFrozenDictionary();


    /// <summary>
    /// Map of <see cref="LangId"/> and default localization.
    /// </summary>
    public static FrozenDictionary<LangId, string> DefaultLangMap => new Dictionary<LangId, string>(_defaultLangList)
        .ToFrozenDictionary();


    // the default language list
    private static IReadOnlyCollection<KeyValuePair<LangId, string>> _defaultLangList = [

        #region General

        new(LangId._OK, "OK"), // v9.0
        new(LangId._Cancel, "Cancel"), // v9.0
        new(LangId._Apply, "Apply"), // v9.0
        new(LangId._Close, "Close"), // v9.0
        new(LangId._Yes, "Yes"), // v9.0
        new(LangId._No, "No"), // v9.0
        new(LangId._LearnMore, "Learn more…"), // v9.0
        new(LangId._Continue, "Continue"), // v9.0
        new(LangId._Quit, "Quit"), // v9.0
        new(LangId._Back, "Back"), // v9.0
        new(LangId._Next, "Next"), // v9.0
        new(LangId._Save, "Save"), // v9.0
        new(LangId._Error, "Error"), // v9.0
        new(LangId._Warning, "Warning"), // v9.0
        new(LangId._Copy, "Copy"), //v9.0
        new(LangId._Browse, "Browse…"), //v9.0
        new(LangId._Reset, "Reset"), //v9.0
        new(LangId._ResetToDefault, "Reset to default"), //v9.0
        new(LangId._CheckForUpdate, "Check for update…"), //v5.0
        new(LangId._Download, "Download"), //v9.0
        new(LangId._Update, "Update"), //v9.0
        new(LangId._Website, "Website"), //v9.0
        new(LangId._Email, "Email"), //v9.0
        new(LangId._Install, "Install…"),
        new(LangId._Refresh, "Refresh"),
        new(LangId._Delete, "Delete"),
        new(LangId._Add, "Add"),
        new(LangId._Edit, "Edit"),
        new(LangId._ID, "ID"),
        new(LangId._Name, "Name"),
        new(LangId._Hotkeys, "Hotkeys"),
        new(LangId._AddHotkey, "Add hotkey…"),
        new(LangId._Executable, "Executable"),
        new(LangId._Argument, "Argument"),
        new(LangId._CommandPreview, "Command preview"),
        new(LangId._FileExtension, "File extension"),
        new(LangId._Empty, "(empty)"),
        new(LangId._MoveUp, "Move up"),
        new(LangId._MoveDown, "Move down"),
        new(LangId._Separator, "Separator"),
        new(LangId._Icon, "Icon"),
        new(LangId._Description, "Description"),
        new(LangId._GetHelp, "Get help"),
        new(LangId._Start, "Start"),

        new(LangId._UnhandledException, "Unhandled exception"), // v9.0
        new(LangId._UnhandledException_Description, "Unhandled exception has occurred. If you click Continue, the application will ignore this error and attempt to continue. If you click Quit, the application will close immediately."), // v9.0
        new(LangId._DoNotShowThisMessageAgain, "Do not show this message again"), // v9.0
        new(LangId._CreatingFile, "Creating a temporary image file…"), //v9.0
        new(LangId._CreatingFileError, "Could not create temporary image file"), //v9.0
        new(LangId._NotSupported, "Unsupported format"), //v9.0

        new(LangId._InvalidAction, "Invalid action"), //v9.0
        new(LangId._InvalidAction_Transformation, "ImageGlass does not support rotation, flipping for this image."), //v9.0

        new(LangId._UserAction_MenuNotFound, "Cannot find menu '{0}' to invoke the action"), // v9.0
        new(LangId._UserAction_MethodNotFound, "Cannot find method '{0}' to invoke the action"), // v9.0
        new(LangId._UserAction_MethodArgumentNotSupported, "The argument type of method '{0}' is not supported"), // v9.0
        new(LangId._UserAction_Win32ExeError, "Cannot execute command '{0}'. Make sure the name is correct."), // v9.0

        // Gallery tooltip
        new(LangId._Metadata_FileSize, "File size"), //v9.0
        new(LangId._Metadata_FileCreationTime, "Date created"), //v9.0
        new(LangId._Metadata_FileLastAccessTime, "Date accessed"), //v9.0
        new(LangId._Metadata_FileLastWriteTime, "Date modified"), //v9.0
        new(LangId._Metadata_FrameCount, "Frames"), //v9.0
        new(LangId._Metadata_ExifRatingPercent, "Rating"), //v9.0
        new(LangId._Metadata_ColorSpace, "Color space"), //v9.0
        new(LangId._Metadata_ColorProfile, "Color profile"), //v9.0
        new(LangId._Metadata_ExifDateTime, "EXIF: DateTime"), //v9.0
        new(LangId._Metadata_ExifDateTimeOriginal, "EXIF: DateTimeOriginal"), //v9.0

        // image info
        new(LangId._ImageInfo_ListCount, "{0} file(s)"), //v9.0
        new(LangId._ImageInfo_FrameCount, "{0} frame(s)"), //v9.0

        // layout position
        new(LangId._Position_Left, "Left"),
        new(LangId._Position_Right, "Right"),
        new(LangId._Position_Top, "Top"),
        new(LangId._Position_Bottom, "Bottom"),

        // validation
        new(LangId._Validation_Required, "Required"),
        new(LangId._Validation_RegexPattern, "Invalid value"),
        new(LangId._Validation_IntValueOnly, "Must be an integer"),
        new(LangId._Validation_UnsignedIntValueOnly, "Must be a non-negative integer"),
        new(LangId._Validation_FloatValueOnly, "Must be a number"),
        new(LangId._Validation_UnsignedFloatValueOnly, "Must be a non-negative number"),
        new(LangId._Validation_FileNameValueOnly, "Invalid filename"),

        #endregion // General
    
        
        #region Enums

        // ImageOrderBy
        new(LangId.ImageOrderBy_Name, "Name (default)"), //v8.0
        new(LangId.ImageOrderBy_Random, "Random"), //v8.0
        new(LangId.ImageOrderBy_FileSize, "File size"), //v8.0
        new(LangId.ImageOrderBy_Extension, "Extension"), //v8.0
        new(LangId.ImageOrderBy_DateCreated, "Date created"), //v8.0
        new(LangId.ImageOrderBy_DateAccessed, "Date accessed"), //v8.0
        new(LangId.ImageOrderBy_DateModified, "Date modified"), //v8.0
        new(LangId.ImageOrderBy_ExifDateTaken, "EXIF: Date taken"), //v9.0
        new(LangId.ImageOrderBy_ExifRating, "EXIF: Rating"), //v9.0


        // ImageOrderType
        new(LangId.ImageOrderType_Asc, "Ascending"),  //v8.0
        new(LangId.ImageOrderType_Desc, "Descending"),  //v8.0

        // AfterEditAppAction
        new(LangId.AfterEditAppAction_Nothing, "Nothing"), //v8.0
        new(LangId.AfterEditAppAction_Minimize, "Minimize"), //v8.0
        new(LangId.AfterEditAppAction_Close, "Close"), //v8.0

        // ColorProfileOption
        new(LangId.ColorProfileOption_None, "None"),
        new(LangId.ColorProfileOption_CurrentMonitorProfile, "Current monitor profile"),
        new(LangId.ColorProfileOption_Custom, "Custom…"),

        // BackdropStyle
        new(LangId.BackdropStyle_None, "None"),

        // MouseWheelEvent
        new(LangId.MouseWheelEvent_Scroll, "Scroll"),
        new(LangId.MouseWheelEvent_CtrlAndScroll, "Hold Ctrl and scroll"),
        new(LangId.MouseWheelEvent_ShiftAndScroll, "Hold Shift and scroll"),
        new(LangId.MouseWheelEvent_AltAndScroll, "Hold Alt and scroll"),

        // MouseWheelAction
        new(LangId.MouseWheelAction_DoNothing, "Do nothing"),
        new(LangId.MouseWheelAction_Zoom, "Zoom in / out"),
        new(LangId.MouseWheelAction_PanVertically, "Pan up / down"),
        new(LangId.MouseWheelAction_PanHorizontally, "Pan left / right"),
        new(LangId.MouseWheelAction_BrowseImages, "View next / previous Image"),

        // ImageInterpolation
        new(LangId.ImageInterpolation_NearestNeighbor, "Nearest neighbor"),
        new(LangId.ImageInterpolation_Linear, "Linear"),
        new(LangId.ImageInterpolation_Cubic, "Cubic"),
        new(LangId.ImageInterpolation_MultiSampleLinear, "Multi-sample linear"),
        new(LangId.ImageInterpolation_Antisotropic, "Antisotropic"),
        new(LangId.ImageInterpolation_HighQualityBicubic, "High quality bicubic"),

        #endregion // Enums


        #region Main Window

        #region Main Window > General
        new(LangId.FrmMain_PicMain_ErrorText, "Could not load this image"), // v2.0 beta, updated 4.0, 9.0, 10.0
        new(LangId.FrmMain_MnuMain, "Main menu"), // v3.0
        new(LangId.FrmMain_MnuToolbarOverflow, "View more buttons"), // v10.0

        new(LangId.FrmMain_OpenFileDialog, "All supported files"),
        new(LangId.FrmMain_Loading, "Loading…"), // v3.0
        new(LangId.FrmMain_OpenWith, "Open with {0}"), //v9.0
        new(LangId.FrmMain_ReachedFirstImage, "Reached the first image"), // v4.0
        new(LangId.FrmMain_ReachedLastImage, "Reached the last image"), // v4.0
        new(LangId.FrmMain_ClipboardImage, "Clipboard image"), //v9.0
        new(LangId.FrmMain_FolderAccessPrompt, "Allow ImageGlass to access this folder to browse other images"), //v10.0

        #endregion // Main Window > General


        #region Main Window > Main Menu

        #region Main Menu > File
        new(LangId.FrmMain_MnuFile, "File"), //v7.0
        new(LangId.FrmMain_MnuOpenFile, "Open file…"), //v3.0
        new(LangId.FrmMain_MnuNewWindow, "Open new window"), //v7.0
        new(LangId.FrmMain_MnuNewWindow_Error, "Cannot open new window because only one instance is allowed"), //v7.0
        new(LangId.FrmMain_MnuSave, "Save"), //v8.1
        new(LangId.FrmMain_MnuSave_Confirm, "Are you sure you want to override this image?"), //v9.0
        new(LangId.FrmMain_MnuSave_ConfirmDescription, "ImageGlass is not a professional photo editor, please be aware of losing quality, metadata, layers,… when saving your image."), //v9.0
        new(LangId.FrmMain_MnuSave_Saving, "Saving image…"), //v9.0
        new(LangId.FrmMain_MnuSave_Success, "Image is saved"), //v9.0
        new(LangId.FrmMain_MnuSave_Error, "Could not save image"), //v9.0
        new(LangId.FrmMain_MnuSaveAs, "Save as…"), //v3.0
        new(LangId.FrmMain_MnuExportFrames, "Export image frames…"), //v7.5

        new(LangId.FrmMain_MnuOpenWith, "Open with…"), //v7.6
        new(LangId.FrmMain_MnuEdit, "Edit image {0}…"), //v3.0,
        new(LangId.FrmMain_MnuEdit_AppNotFound, "Could not find the associated app for editing. You can assign an app for editing this format in ImageGlass Settings > Edit."), //v9.0
        new(LangId.FrmMain_MnuPrint, "Print…"), //v3.0
        new(LangId.FrmMain_MnuPrint_Error, "Could not print image"), //v9.0
        new(LangId.FrmMain_MnuShare, "Share…"), //v8.6
        new(LangId.FrmMain_MnuShare_Error, "Could not open Share dialog."), //v9.0
        new(LangId.FrmMain_MnuOpenLocation, "Open image location"), //v3.0

        new(LangId.FrmMain_MnuRename, "Rename image…"), //v3.0
        new(LangId.FrmMain_MnuRename_Description, "Enter a new filename:"), // v9.0
        new(LangId.FrmMain_MnuMoveToRecycleBin, "Move to Recycle Bin"), //v3.0
        new(LangId.FrmMain_MnuMoveToRecycleBin_Description, "Do you want to move this file to Recycle bin?"), //v3.0
        new(LangId.FrmMain_MnuDeleteFromHardDisk, "Delete permanently"), //v3.0
        new(LangId.FrmMain_MnuDeleteFromHardDisk_Description, "Are you sure you want to permanently delete this file?"), //v3.0
        #endregion // Main Menu > File

        #region Main Menu > Navigation
        new(LangId.FrmMain_MnuNavigation, "Navigation"), //v3.0
        new(LangId.FrmMain_MnuViewNext, "View next image"), //v3.0
        new(LangId.FrmMain_MnuViewPrevious, "View previous image"), //v3.0

        new(LangId.FrmMain_MnuGoTo, "Go to…"), //v3.0
        new(LangId.FrmMain_MnuGoTo_Description, "Type image number to view, and then press ENTER"),
        new(LangId.FrmMain_MnuGoToFirst, "Go to first image"), //v3.0
        new(LangId.FrmMain_MnuGoToLast, "Go to last image"), //v3.0

        new(LangId.FrmMain_MnuViewNextFrame, "View next frame"),
        new(LangId.FrmMain_MnuViewPreviousFrame, "View previous frame"),
        new(LangId.FrmMain_MnuViewFirstFrame, "View first frame"),
        new(LangId.FrmMain_MnuViewLastFrame, "View last frame"),
        #endregion // Main Menu > Navigation

        #region Main Menu > Zoom
        new(LangId.FrmMain_MnuZoom, "Zoom"), //v7.0
        new(LangId.FrmMain_MnuZoomIn, "Zoom in"), //v3.0
        new(LangId.FrmMain_MnuZoomOut, "Zoom out"), //v3.0
        new(LangId.FrmMain_MnuCustomZoom, "Custom zoom…"), // v8.3
        new(LangId.FrmMain_MnuCustomZoom_Description, "Enter a new zoom value"), // v8.3
        new(LangId.FrmMain_MnuScaleToFit, "Scale to fit"), //v3.5
        new(LangId.FrmMain_MnuScaleToFill, "Scale to fill"), //v7.5
        new(LangId.FrmMain_MnuActualSize, "Actual size"), //v3.0
        new(LangId.FrmMain_MnuLockZoom, "Lock zoom ratio"), //v3.0
        new(LangId.FrmMain_MnuAutoZoom, "Auto zoom"), //v5.5
        new(LangId.FrmMain_MnuScaleToWidth, "Scale to width"), //v3.0
        new(LangId.FrmMain_MnuScaleToHeight, "Scale to height"), //v3.0
        #endregion // Main Menu > Zoom

        #region Main Menu > Panning
        new(LangId.FrmMain_MnuPanning, "Panning"), //v9.0

        new(LangId.FrmMain_MnuPanLeft, "Pan image left"), //v9.0
        new(LangId.FrmMain_MnuPanRight, "Pan image right"), //v9.0
        new(LangId.FrmMain_MnuPanUp, "Pan image up"), //v9.0
        new(LangId.FrmMain_MnuPanDown, "Pan image down"), //v9.0

        new(LangId.FrmMain_MnuPanToLeftSide, "Pan image to left edge"), //v9.0
        new(LangId.FrmMain_MnuPanToRightSide, "Pan image to right edge"), //v9.0
        new(LangId.FrmMain_MnuPanToTop, "Pan image to top"), //v9.0
        new(LangId.FrmMain_MnuPanToBottom, "Pan image to bottom"), //v9.0
        #endregion // Main Menu > Panning

        #region Main Menu > Image
        new(LangId.FrmMain_MnuImage, "Image"), //v7.0

        new(LangId.FrmMain_MnuRefresh, "Refresh"), //v3.0
        new(LangId.FrmMain_MnuReload, "Reload image"), //v5.5
        new(LangId.FrmMain_MnuReloadImageList, "Reload image list"), //v7.0
        new(LangId.FrmMain_MnuUnload, "Unload image"), //v9.0

        new(LangId.FrmMain_MnuViewChannels, "View channels"), //v7.0
        new(LangId.FrmMain_MnuLoadingOrders, "Loading orders"), //v8.0
        new(LangId.FrmMain_MnuInvertColors, "Invert colors"), // v9.3
        new(LangId.FrmMain_MnuToggleImageAnimation, "Start / stop animating image"), //v3.0

        new(LangId.FrmMain_MnuRotateLeft, "Rotate left"), //v7.5
        new(LangId.FrmMain_MnuRotateRight, "Rotate right"), //v7.5
        new(LangId.FrmMain_MnuFlipHorizontal, "Flip Horizontal"), // V6.0
        new(LangId.FrmMain_MnuFlipVertical, "Flip Vertical"), // V6.0
        
        new(LangId.FrmMain_MnuSetDesktopBackground, "Set as Desktop background"), //v3.0
        new(LangId.FrmMain_MnuSetDesktopBackground_Error, "Could not set image as desktop background"), // v6.0
        new(LangId.FrmMain_MnuSetDesktopBackground_Success, "Desktop background is updated"), // v6.0
        new(LangId.FrmMain_MnuSetLockScreen, "Set as Lock screen image"), // V6.0
        new(LangId.FrmMain_MnuSetLockScreen_Error, "Could not set image as lock screen image"), // v6.0
        new(LangId.FrmMain_MnuSetLockScreen_Success, "Lock screen image is updated"), // v6.0

        new(LangId.FrmMain_MnuImageProperties, "Image properties"), //v3.0
        #endregion // Main Menu > Image

        #region Main Menu > Clipboard
        new(LangId.FrmMain_MnuClipboard, "Clipboard"), //v3.0
        new(LangId.FrmMain_MnuCopyFile, "Copy file"), //v3.0
        new(LangId.FrmMain_MnuCopyFile_Success, "Copied {0} file(s)"), // v2.0 final
        new(LangId.FrmMain_MnuCopyImagePixels, "Copy image pixels"), //v5.0
        new(LangId.FrmMain_MnuCopyImagePixels_Copying, "Copying image pixels. It's going to take a while…"), // v9.0
        new(LangId.FrmMain_MnuCopyImagePixels_Success, "Copied image pixels"), // v5.0
        new(LangId.FrmMain_MnuCutFile, "Cut file"), //v3.0
        new(LangId.FrmMain_MnuCutFile_Success, "Cut {0} file(s)"), // v2.0 final
        new(LangId.FrmMain_MnuCopyPath, "Copy image path"), //v3.0
        new(LangId.FrmMain_MnuCopyPath_Success, "Copied image path"), // v9.0
        new(LangId.FrmMain_MnuPasteImage, "Paste image"), //v3.0
        new(LangId.FrmMain_MnuPasteImage_Error, "Could not find image data in Clipboard"), // v8.0
        new(LangId.FrmMain_MnuClearClipboard, "Clear clipboard"), //v3.0
        new(LangId.FrmMain_MnuClearClipboard_Success, "Cleared clipboard"), // v2.0 final
        #endregion // Main Menu > Clipboard

        new(LangId.FrmMain_MnuWindowFit, "Window Fit"), //v7.5
        new(LangId.FrmMain_MnuFullScreen, "Full Screen"), //v3.0
        new(LangId.FrmMain_MnuFrameless, "Frameless"), //v7.5
        new(LangId.FrmMain_MnuFrameless_EnableDescription, "Drag the top area to move the window"), // v7.5
        new(LangId.FrmMain_MnuSlideshow, "Slideshow"), //v3.0

        #region Main Menu > Layout
        new(LangId.FrmMain_MnuLayout, "Layout"), //v3.0
        new(LangId.FrmMain_MnuToggleToolbar, "Toolbar"), //v3.0
        new(LangId.FrmMain_MnuToggleGallery, "Gallery panel"), //v3.0
        new(LangId.FrmMain_MnuToggleCheckerboard, "Checkerboard background"), //v3.0, updated v5.0
        new(LangId.FrmMain_MnuToggleTopMost, "Keep window always on top"), //v3.2
        new(LangId.FrmMain_MnuToggleTopMost_Enable, "Enabled window always on top"), // v9.0
        new(LangId.FrmMain_MnuToggleTopMost_Disable, "Disabled window always on top"), // v9.0
        new(LangId.FrmMain_MnuChangeBackgroundColor, "Change background color…"), // v9.0
        #endregion // Main Menu > Layout

        #region Main Menu > Tools
        new(LangId.FrmMain_MnuTools, "Tools"), //v3.0
        new(LangId.FrmMain_MnuColorPicker, "Color picker"), //v5.0
        new(LangId.FrmMain_MnuCropTool, "Crop image"), // v7.6
        new(LangId.FrmMain_MnuResizeTool, "Resize image"), // v9.2
        new(LangId.FrmMain_MnuFrameNav, "Frame navigation"), // v7.5
        new(LangId.FrmMain_MnuGetMoreTools, "Get more tools…"), // v9.0

        new(LangId.FrmMain_MnuLosslessCompression, "Magick.NET Lossless Compression"), // v9.1
        new(LangId.FrmMain_MnuLosslessCompression_Confirm, "Are you sure you want to proceed?"), // v9.1
        new(LangId.FrmMain_MnuLosslessCompression_Description, "This tool uses Magick.NET library for lossless compression, optimizing file size. Overwrites only if the compressed file is smaller than the original."), // v9.1
        new(LangId.FrmMain_MnuLosslessCompression_Compressing, "Performing lossless compression…"), // v9.1
        new(LangId.FrmMain_MnuLosslessCompression_Done, "Done lossless compression."), // v9.1
        #endregion // Main Menu > Tools

        new(LangId.FrmMain_MnuSettings, "Settings"), // v3.0

        #region Main Menu > Help
        new(LangId.FrmMain_MnuHelp, "Help"), //v7.0
        new(LangId.FrmMain_MnuAbout, "About"), //v3.0
        new(LangId.FrmMain_MnuQuickSetup, "Open ImageGlass Quick Setup"), //v9.0
        new(LangId.FrmMain_MnuReportIssue, "Report an issue…"), //v3.0

        new(LangId.FrmMain_MnuCheckForUpdate_NewVersion, "A new update is available!"), //v5.0
        new(LangId.FrmMain_MnuCheckForUpdate_NoUpdate, "You are using the latest version!"),
        new(LangId.FrmMain_MnuCheckForUpdate_Checking, "Checking for update…"),
        new(LangId.FrmMain_MnuCheckForUpdate_Failed, "Could not check for update!"),
        new(LangId.FrmMain_MnuCheckForUpdate_SkipVersion, "Skip this version"),
        new(LangId.FrmMain_MnuCheckForUpdate_CurrentVersion, "Current version: {0}" ), //v9.0
        new(LangId.FrmMain_MnuCheckForUpdate_LatestVersion, "The latest version: {0}" ), //v9.0
        new(LangId.FrmMain_MnuCheckForUpdate_PublishedDate, "Published date: {0}" ), //v9.0

        new(LangId.FrmMain_MnuSetDefaultPhotoViewer, "Set default photo viewer"), //v9.0
        new(LangId.FrmMain_MnuSetDefaultPhotoViewer_Success, "You have successfully set ImageGlass as default photo viewer."), //v9.0
        new(LangId.FrmMain_MnuSetDefaultPhotoViewer_Error, "Could not set ImageGlass as default photo viewer."), //v9.0

        new(LangId.FrmMain_MnuRemoveDefaultPhotoViewer, "Remove default photo viewer"), //v9.0
        new(LangId.FrmMain_MnuRemoveDefaultPhotoViewer_Success, "ImageGlass is no longer the default photo viewer."), //v9.0
        new(LangId.FrmMain_MnuRemoveDefaultPhotoViewer_Error, "Could not remove ImageGlass as the default photo viewer."), //v9.0
        #endregion // Main Menu > Help

        new(LangId.FrmMain_MnuExit, "Exit"), //v7.0

        #endregion

        #endregion // Main Window

        
        #region FrmAbout
        new(LangId.FrmAbout_Slogan, "A Fast, Seamless Photo Viewer"),
        new(LangId.FrmAbout_Version, "Version:"),
        new(LangId.FrmAbout_License, "Software license"),
        new(LangId.FrmAbout_Privacy, "Privacy policy"),
        new(LangId.FrmAbout_Thanks, "Special thanks to"),
        new(LangId.FrmAbout_LogoDesigner, "Logo designer:"),
        new(LangId.FrmAbout_Collaborator, "Collaborator:"),
        new(LangId.FrmAbout_Contact, "Contact"),
        new(LangId.FrmAbout_Homepage, "Homepage"),
        new(LangId.FrmAbout_Email, "Email:"),
        new(LangId.FrmAbout_Credits, "Credits"),
        new(LangId.FrmAbout_Donate, "Donate"),
        #endregion // FrmAbout

        
        #region FrmSettings

        new(LangId.FrmSettings_ResetSettings, "Reset settings"), // v9.1
        new(LangId.FrmSettings_UnmanagedSettingReminder, "This setting is not managed by ImageGlass. Don't forget to disable it before you remove or relocate the app because ImageGlass does not handle this automatically."), // v9.1


        #region FrmSettings > Navbar
        new(LangId.FrmSettings_Nav_General, "General"),
        new(LangId.FrmSettings_Nav_Image, "Image"),
        new(LangId.FrmSettings_Nav_Slideshow, "Slideshow"),
        new(LangId.FrmSettings_Nav_Edit, "Edit"),
        new(LangId.FrmSettings_Nav_Viewer, "Viewer"),
        new(LangId.FrmSettings_Nav_Toolbar, "Toolbar"),
        new(LangId.FrmSettings_Nav_Gallery, "Gallery"),
        new(LangId.FrmSettings_Nav_Layout, "Layout"),
        new(LangId.FrmSettings_Nav_Mouse, "Mouse"),
        new(LangId.FrmSettings_Nav_Keyboard, "Keyboard"),
        new(LangId.FrmSettings_Nav_FileTypeAssociations, "File type associations"),
        new(LangId.FrmSettings_Nav_Tools, "Tools"),
        new(LangId.FrmSettings_Nav_Language, "Language"),
        new(LangId.FrmSettings_Nav_Appearance, "Appearance"),
        #endregion // FrmSettings > Navbar


        #region FrmSettings > Tab General
        // General > General
        new(LangId.FrmSettings_StartupDir, "Startup location"),
        new(LangId.FrmSettings_ConfigDir, "Configuration location"),
        new(LangId.FrmSettings_UserConfigFile, "User settings file (igconfig.json)"),

        // General > Startup
        new(LangId.FrmSettings_Startup, "Startup"),
        new(LangId.FrmSettings_EnableWelcomeImage, "Show welcome image"),
        new(LangId.FrmSettings_EnableLastSeenImage, "Open the last seen image"),

        new(LangId.FrmSettings_StartupBoost, "Startup Boost"), // v9.1
        new(LangId.FrmSettings_StartupBoost_Description, "Preload and run ImageGlass in the background for a few seconds during Windows startup to accelerate the first launch."), // v9.1
        new(LangId.FrmSettings_StartupBoost_Enabled, "Startup Boost is enabled"), // v9.1
        new(LangId.FrmSettings_StartupBoost_Disabled, "Startup Boost is disabled"), // v9.1
        new(LangId.FrmSettings_StartupBoost_Error, "Could not change Startup Boost setting"), // v9.1
        new(LangId.FrmSettings_EnableStartupBoost, "Enable Startup Boost"), // v9.1
        new(LangId.FrmSettings_DisableStartupBoost, "Disable Startup Boost"), // v9.1
        new(LangId.FrmSettings_OpenStartupAppsSetting, "Open Startup apps setting"), // v9.1

        // General > Real-time update
        new(LangId.FrmSettings_RealTimeFileUpdate, "Real-time file update"),
        new(LangId.FrmSettings_EnableFileWatcher, "Monitor file changes in the viewing folder and update in realtime"),
        new(LangId.FrmSettings_EnableAutoOpenNewAddedImage, "Open the new added image automatically"),

        // General > Others
        new(LangId.FrmSettings_Others, "Others"),
        new(LangId.FrmSettings_AutoUpdate, "Check for update automatically"),
        new(LangId.FrmSettings_EnableMultiInstances, "Allow multiple instances of the program"),
        new(LangId.FrmSettings_ShowAppIcon, "Show app icon on the title bar"),
        new(LangId.FrmSettings_InAppMessageDuration, "In-app message duration (milliseconds)"),
        new(LangId.FrmSettings_ImageInfoTags, "Image information tags"),
        new(LangId.FrmSettings_AvailableImageInfoTags, "Available tags:"),
        #endregion // FrmSettings > Tab General

            
        #region FrmSettings > Tab Image
        // Image > Image loading
        new(LangId.FrmSettings_ImageLoading, "Image loading"),
        new(LangId.FrmSettings_ImageLoadingOrder, "Image loading order"),
        new(LangId.FrmSettings_EnableExplorerSortOrder, "Use Explorer sort order"),
        new(LangId.FrmSettings_EnableSubfoldersLoading, "Load images in subfolders"),
        new(LangId.FrmSettings_EnableImageFolderGrouping, "Group images by directory"),
        new(LangId.FrmSettings_EnableHiddenImagesLoading, "Load hidden images"),
        new(LangId.FrmSettings_EnableLoopBackNavigation, "Loop back to the first image when reaching the end of the image list"),
        new(LangId.FrmSettings_EnableImagePreview, "Display image preview while it's being loaded"),

        new(LangId.FrmSettings_EmbeddedThumbnail, "Embedded thumbnail"),
        new(LangId.FrmSettings_EnableOnlyLoadRawPreview, "Load only the embedded thumbnail for RAW formats"),
        new(LangId.FrmSettings_EnableOnlyLoadNonRawPreview, "Load only the embedded thumbnail for other formats"),
        new(LangId.FrmSettings_MinEmbeddedThumbnailSize, "Minimum size of the embedded thumbnail to be loaded"),
        new(LangId.FrmSettings_MinEmbeddedThumbnailSize_Width, "Width"),
        new(LangId.FrmSettings_MinEmbeddedThumbnailSize_Height, "Height"),

        // Image > Image Booster
        new(LangId.FrmSettings_ImageBooster, "Image Booster"),
        new(LangId.FrmSettings_ImageBoosterCacheCount, "Number of images cached by Image Booster (one direction)"),
        new(LangId.FrmSettings_ImageBoosterCacheMaxDimension, "Maximum image dimension to be cached (in pixels)"),
        new(LangId.FrmSettings_ImageBoosterCacheMaxFileSizeInMb, "Maximum image file size to be cached (in megabytes)"),

        // Image > Color management
        new(LangId.FrmSettings_ColorManagement, "Color management"),
        new(LangId.FrmSettings_EnableAlwaysApplyColorProfile, "Always apply for image without embedded color profile"),
        new(LangId.FrmSettings_ColorProfile, "Color profile"),
        new(LangId.FrmSettings_CurrentMonitorProfile_Description, "ImageGlass does not auto-update the color when moving its window between monitors"),
        #endregion // FrmSettings > Tab Image


        #region FrmSettings > Tab Slideshow
        // Slideshow > Slideshow
        new(LangId.FrmSettings_EnableSlideshowCountdown, "Show slideshow countdown"),
        new(LangId.FrmSettings_EnableFullscreenSlideshow, "Start slideshow in Full Screen mode"),
        new(LangId.FrmSettings_EnableSlideshowRandomInterval, "Use random interval"),
        new(LangId.FrmSettings_SlideshowInterval, "Slideshow interval:"),
        new(LangId.FrmSettings_SlideshowInterval_From, "From"),
        new(LangId.FrmSettings_SlideshowInterval_To, "To"),
        new(LangId.FrmSettings_SlideshowBackgroundColor, "Slideshow background color"),

        // Slideshow > Slideshow notification
        new(LangId.FrmSettings_SlideshowNotification, "Slideshow notification"),
        new(LangId.FrmSettings_SlideshowImagesToNotifySound, "Number of images to trigger a notification sound"),
        #endregion // FrmSettings > Tab Slideshow


        #region FrmSettings > Tab Edit
        // Edit > Edit
        new(LangId.FrmSettings_EnableDeleteConfirmation, "Show confirmation dialog when deleting file"),
        new(LangId.FrmSettings_EnableSaveConfirmation, "Show confirmation dialog when overriding file"),
        new(LangId.FrmSettings_EnablePreserveModifiedDate, "Preserve the image's modified date on save"),
        new(LangId.FrmSettings_EnableOpenSaveAsInCurrentFolder, "Open the Save As dialog in the current image directory"), // v9.1
        new(LangId.FrmSettings_ImageEditQuality, "Image quality"),
        new(LangId.FrmSettings_AfterEditingAction, "After opening editing app"),

        // Edit > Clipboard
        new(LangId.FrmSettings_Clipboard, "Clipboard"),
        new(LangId.FrmSettings_EnableCopyMultipleFiles, "Enable the copying of multiple files at once"),
        new(LangId.FrmSettings_EnableCutMultipleFiles, "Enable the cutting of multiple files at once"),

        // Edit > Image editing apps
        new(LangId.FrmSettings_EditApps, "Image editing apps"),
        new(LangId.FrmSettings_EditApps_AppName, "App name"),
        new(LangId.FrmSettings_EditAppDialog_AddApp, "Add an app for editing"),
        new(LangId.FrmSettings_EditAppDialog_EditApp, "Edit app"),

        #endregion // FrmSettings > Tab Edit


        #region FrmSettings > Tab Layout
        // Layout > Layout
        new(LangId.FrmSettings_Layout_Order, "Order"),
        new(LangId.FrmSettings_Layout_Toolbar, "Toolbar"),
        new(LangId.FrmSettings_Layout_ToolbarContext, "Contextual toolbar"),
        new(LangId.FrmSettings_Layout_Gallery, "Gallery"),
        new(LangId.FrmSettings_Layout_ToolbarPosition, "Toolbar position"),
        new(LangId.FrmSettings_Layout_ToolbarContextPosition, "Contextual toolbar position"),
        new(LangId.FrmSettings_Layout_GalleryPosition, "Gallery position"),
        #endregion // FrmSettings > Tab Layout


        #region FrmSettings > Tab Viewer
        // Viewer > Viewer
        new(LangId.FrmSettings_ShowCheckerboardOnlyImageRegion, "Show checkerboard only within the image region"),
        new(LangId.FrmSettings_EnableNavigationButtons, "Show navigation arrow buttons"),
        new(LangId.FrmSettings_EnableCenterWindowFit, "Automatically center the window in Window Fit mode"),
        new(LangId.FrmSettings_PanSpeed, "Panning speed"),

        // Viewer > Zooming
        new(LangId.FrmSettings_Zooming, "Zooming"),
        new(LangId.FrmSettings_ImageInterpolation, "Image interpolation"),
        new(LangId.FrmSettings_ImageInterpolation_ScaleDown, "When zoom < 100%"),
        new(LangId.FrmSettings_ImageInterpolation_ScaleUp, "When zoom > 100%"),
        new(LangId.FrmSettings_ZoomSpeed, "Zoom speed"),
        new(LangId.FrmSettings_ZoomLevels, "Zoom levels"),
        new(LangId.FrmSettings_UseSmoothZooming, "Use smooth zooming"),
        new(LangId.FrmSettings_LoadDefaultZoomLevels, "Load default zoom levels"),
        #endregion // FrmSettings > Tab Viewer


        #region FrmSettings > Tab Toolbar
        // Toolbar > Toolbar
        new(LangId.FrmSettings_Toolbar_ShowToolbarInFullscreen, "Show toolbar in Full Screen mode"),
        new(LangId.FrmSettings_Toolbar_ToolbarIconHeight, "Toolbar icon size"),

        new(LangId.FrmSettings_Toolbar_AddNewButton, "Add a custom toolbar button"),
        new(LangId.FrmSettings_Toolbar_EditButton, "Edit toolbar button"),
        new(LangId.FrmSettings_Toolbar_ButtonJson, "Button JSON"),


        new(LangId.FrmSettings_Toolbar_ToolbarButtons, "Toolbar buttons"),
        new(LangId.FrmSettings_Toolbar_AddCustomButton, "Add a custom button…"),
        new(LangId.FrmSettings_Toolbar_AvailableButtons, "Available buttons:"),
        new(LangId.FrmSettings_Toolbar_CurrentButtons, "Current buttons:"),
        new(LangId.FrmSettings_Toolbar_Errors_ButtonIdRequired, "Button ID required."),
        new(LangId.FrmSettings_Toolbar_Errors_ButtonIdDuplicated, "A button with the ID '{0}' has already been defined. Please choose a different and unique ID for your button to avoid conflicts."),
        new(LangId.FrmSettings_Toolbar_Errors_ButtonExecutableRequired, "Button executable required."),

        #endregion // FrmSettings > Tab Toolbar


        #region FrmSettings > Tab Gallery
        // Gallery > Gallery
        new(LangId.FrmSettings_ShowGalleryInFullscreen, "Show gallery in Full Screen mode"),
        new(LangId.FrmSettings_ShowGalleryFileName, "Show thumbnail filename"),
        new(LangId.FrmSettings_ThumbnailSize, "Thumbnail size (in pixels)"),
        new(LangId.FrmSettings_GalleryCacheSizeInMb, "Maximum gallery cache size (in megabytes)"),
        new(LangId.FrmSettings_GalleryColumns, "Number of thumbnail columns in vertical gallery layout"),
        #endregion // FrmSettings > Tab Gallery


        #region FrmSettings > Tab Mouse
        // Mouse > Mouse wheel action
        new(LangId.FrmSettings_MouseWheelAction, "Mouse wheel action"),
        #endregion // FrmSettings > Tab Mouse


        #region FrmSettings > Tab Keyboard

        #endregion // FrmSettings > Tab Mouse & Keyboard


        #region FrmSettings > Tab File type associations
        // File type associations > File extension icons
        new(LangId.FrmSettings_FileExtensionIcons, "File extension icons"),
        new(LangId.FrmSettings_FileExtensionIcons_Description, "For customizing file extension icons, download an icon pack, place all .ICO files in the extension icon folder, and click the '{0}' button. This will also set ImageGlass as default photo viewer."),
        new(LangId.FrmSettings_OpenExtensionIconFolder, "Open extension icon folder"),
        new(LangId.FrmSettings_GetExtensionIconPacks, "Get extension icon packs…"),

        // File type associations > Default photo viewer
        new(LangId.FrmSettings_DefaultPhotoViewer, "Default photo viewer"),
        new(LangId.FrmSettings_DefaultPhotoViewer_Description, "Register the supported formats of ImageGlass with Windows. You might need to open the Default apps settings and manually select ImageGlass from the list for it to take effect."),
        new(LangId.FrmSettings_MakeDefault, "Make default"),
        new(LangId.FrmSettings_RemoveDefault, "Remove default"),
        new(LangId.FrmSettings_OpenDefaultAppsSetting, "Open Default apps setting"),

        // File type associations > File formats
        new(LangId.FrmSettings_FileFormats, "File formats"),
        new(LangId.FrmSettings_TotalSupportedFormats, "Total supported formats: {0}"),
        new(LangId.FrmSettings_AddNewFileExtension, "Add new file extension"),

        #endregion // FrmSettings > Tab File type associations


        #region FrmSettings > Tab Tools
        // Tools > Tools
        new(LangId.FrmSettings_Tools_AddNewTool, "Add an external tool"),
        new(LangId.FrmSettings_Tools_EditTool, "Edit external tool"),
        new(LangId.FrmSettings_Tools_Integrated, "Integrated"),
        new(LangId.FrmSettings_Tools_IntegratedWith, "Integrated with {0}"),
        #endregion // FrmSettings > Tab Tools


        #region FrmSettings > Tab Language
        // Language > Language
        new(LangId.FrmSettings_DisplayLanguage, "Display language"),
        new(LangId.FrmSettings_Refresh, "Refresh"),
        new(LangId.FrmSettings_InstallNewLanguagePack, "Install new language packs…"),
        new(LangId.FrmSettings_GetMoreLanguagePacks, "Get more language packs…"),
        new(LangId.FrmSettings_ExportLanguagePack, "Export language pack…"),
        new(LangId.FrmSettings_Contributors, "Contributors"),
        #endregion // FrmSettings > Tab Language


        #region FrmSettings > Tab Appearance
        // Appearance > Appearance
        new(LangId.FrmSettings_WindowBackdrop, "Window backdrop"),
        new(LangId.FrmSettings_BackgroundColor, "Viewer background color"),

        // Appearance > Theme
        new(LangId.FrmSettings_Theme, "Theme"),
        new(LangId.FrmSettings_DarkTheme, "Dark"),
        new(LangId.FrmSettings_LightTheme, "Light"),
        new(LangId.FrmSettings_Author, "Author"),
        new(LangId.FrmSettings_Theme_OpenThemeFolder, "Open theme folder"),
        new(LangId.FrmSettings_Theme_GetMoreThemes, "Get more theme packs…"),
        new(LangId.FrmSettings_Theme_InstallTheme, "Install theme packs"),
        new(LangId.FrmSettings_Theme_UninstallTheme, "Uninstall a theme pack"),

        new(LangId.FrmSettings_UseThemeForDarkMode, "Use this theme for dark mode"),
        new(LangId.FrmSettings_UseThemeForLightMode, "Use this theme for light mode"),
        #endregion // FrmSettings > Tab Appearance

        #endregion // FrmSettings
        

        #region FrmCrop
        new(LangId.FrmCrop_LblAspectRatio, "Aspect ratio"), //v9.0
        new(LangId.FrmCrop_LblLocation, "Location"), //v9.0
        new(LangId.FrmCrop_LblSize, "Size"), //v9.0
        new(LangId.FrmCrop_LblPreset, "Preset"), //v10

        new(LangId.FrmCrop_SelectionAspectRatio_FreeRatio, "Free ratio"), //v9.0
        new(LangId.FrmCrop_SelectionAspectRatio_Custom, "Custom…"), //v9.0
        new(LangId.FrmCrop_SelectionAspectRatio_Original, "Original"), //v9.0

        new(LangId.FrmCrop_BtnReset, "Reset"), //v9.0
        new(LangId.FrmCrop_BtnSave, "Save"), //v9.0
        new(LangId.FrmCrop_BtnSaveAs, "Save as…"), //v9.0
        new(LangId.FrmCrop_BtnCrop, "Crop"), //v9.0
        new(LangId.FrmCrop_BtnCopy, "Copy"), //v9.0

        // Crop settings
        new(LangId.FrmCropSettings_Title, "Crop settings"), //v9.0
        new(LangId.FrmCropSettings_ChkCloseToolAfterSaving, "Close Crop tool after saving"), //v9.0
        new(LangId.FrmCropSettings_LblDefaultSelection, "Default selection"), //v9.0
        new(LangId.FrmCropSettings_ChkAutoCenterSelection, "Auto-center selection"), //v9.0

        new(LangId.FrmCropSettings_DefaultSelectionType_UseTheLastSelection, "Use the last selection"), //v9.0
        new(LangId.FrmCropSettings_DefaultSelectionType_SelectNone, "Select none"), //v9.0
        new(LangId.FrmCropSettings_DefaultSelectionType_SelectX, "Select {0}"), //v9.0
        new(LangId.FrmCropSettings_DefaultSelectionType_SelectAll, "Select all"), //v9.0
        new(LangId.FrmCropSettings_DefaultSelectionType_CustomArea, "Custom area…"), //v9.0

        #endregion // FrmCrop


        #region FrmColorPicker

        new(LangId.FrmColorPicker_BtnSettings_Tooltip, "Open Color picker settings…"), //v9.0

        // Color picker settings
        new(LangId.FrmColorPickerSettings_Title, "Color picker settings"), //v9.0
        new(LangId.FrmColorPickerSettings_ChkShowRgbA, "Use RGB format with alpha value"), //v5.0
        new(LangId.FrmColorPickerSettings_ChkShowHexA, "Use HEX format with alpha value"), //v5.0
        new(LangId.FrmColorPickerSettings_ChkShowHslA, "Use HSL format with alpha value"), //v5.0
        new(LangId.FrmColorPickerSettings_ChkShowHsvA, "Use HSV format with alpha value"), //v8.0
        new(LangId.FrmColorPickerSettings_ChkShowCmykA, "Use CMYK format with alpha value"), //v10.0
        new(LangId.FrmColorPickerSettings_ChkShowCIELabA, "Use CIELAB format with alpha value"), //v9.0

        #endregion // FrmColorPicker


        #region FrmToolNotFound
        new(LangId.FrmToolNotFound_Title, "Tool not found" ), // v9.0
        new(LangId.FrmToolNotFound_BtnSelectExecutable, "Select…" ), // v9.0
        new(LangId.FrmToolNotFound_LblHeading, "'{0}' is not found!" ), // v9.0
        new(LangId.FrmToolNotFound_LblDescription, "ImageGlass was unable to locate the path to the '{0}' executable. To resolve this issue, please update the path to the '{0}' as necessary." ), // v9.0
        new(LangId.FrmToolNotFound_LblDownloadToolText, "You can download more tools for ImageGlass at:" ), // v9.0
        #endregion // FrmToolNotFound


        #region FrmHotkeyPicker
        new(LangId.FrmHotkeyPicker_LblHotkey, "Press hotkeys" ), // v9.0
        #endregion // FrmHotkeyPicker


        #region FrmResize
        new(LangId.FrmResize_RadResizeByPixels, "Pixels" ), // v9.2
        new(LangId.FrmResize_RadResizeByPercentage, "Percentage" ), // v9.2
        new(LangId.FrmResize_ChkKeepRatio, "Keep ratio propotional" ), // v9.2
        new(LangId.FrmResize_LblResample, "Resample:" ), // v9.2
        new(LangId.FrmResize_LblCurrentSize, "Current Size:" ), // v9.2
        new(LangId.FrmResize_LblNewSize, "New Size:" ), // v9.2
        #endregion // FrmResize

        
        #region igcmd.exe

        new(LangId._IgCommandExe_DefaultError_Heading, "Invalid commands" ), //v9.0
        new(LangId._IgCommandExe_DefaultError_Description, "Make sure you pass correct commands!\r\nThis executable file contains command-line functions for ImageGlass software.\r\n\r\nTo explore all command lines, please visit:\r\n{0}" ), //v9.0


        #region FrmSlideshow

        new(LangId.FrmSlideshow_PauseSlideshow, "Slideshow is paused." ), // v9.0
        new(LangId.FrmSlideshow_ResumeSlideshow, "Slideshow is resumed." ), // v9.0
        new(LangId.FrmSlideshow_MnuPauseResumeSlideshow, "Pause/resume slideshow" ), // v9.0
        new(LangId.FrmSlideshow_MnuToggleCountdown, "Show slideshow countdown" ), // v9.0
        new(LangId.FrmSlideshow_MnuExitSlideshow, "Exit slideshow" ), // v9.0

        #endregion // FrmSlideshow


        #region FrmExportFrames
        new(LangId.FrmExportFrames_Title, "Export image frames" ), //v9.0
        new(LangId.FrmExportFrames_FolderPickerTitle, "Select output folder for exporting image frames" ), //v9.0
        new(LangId.FrmExportFrames_Exporting, "Exporting {0}/{1} frames \r\n{2}…" ), //v9.0
        new(LangId.FrmExportFrames_ExportDone, "Exported {0} frames successfully to \r\n{1}" ), //v9.0
        new(LangId.FrmExportFrames_OpenOutputFolder, "Open output folder" ), //v9.0
        #endregion // FrmExportFrames


        #region FrmQuickSetup

        new(LangId.FrmQuickSetup_Text, "ImageGlass Quick Setup" ), //v9.0
        new(LangId.FrmQuickSetup_StepInfo, "Step {0}" ), //v9.0
        new(LangId.FrmQuickSetup_SkipQuickSetup, "Skip this and launch ImageGlass" ), //v9.0

        new(LangId.FrmQuickSetup_SeeWhatNew, "See what's new in this version…" ), // v9.0
        new(LangId.FrmQuickSetup_SelectProfile, "Select a profile" ), //v9.0
        new(LangId.FrmQuickSetup_StandardUser, "Standard user" ), //v9.0
        new(LangId.FrmQuickSetup_ProfessionalUser, "Professional user" ), //v9.0
        new(LangId.FrmQuickSetup_SettingProfileDescription, "To modify these settings, simply access app settings." ), // v9.0

        new(LangId.FrmQuickSetup_SettingsWillBeApplied, "Settings will be applied:" ), //v9.0
        new(LangId.FrmQuickSetup_SetDefaultViewer, "Do you want to set ImageGlass as the default photo viewer?" ), //v9.0
        new(LangId.FrmQuickSetup_SetDefaultViewer_Description, "You can reset it in the app settings > File type associations tab." ), //v9.0

        new(LangId.FrmQuickSetup_ConfirmCloseProcess, "Before applying new settings, it's essential to close all ImageGlass processes. Are you ready to proceed?" ), //v7.5

        #endregion // FrmQuickSetup

        #endregion // igcmd.exe

    ];

}



