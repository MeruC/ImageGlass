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
using ImageGlass.Common.Actions;
using ImageGlass.Common.AppThemes;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.ServiceProviders;
using ImageGlass.Common.Types;
using ImageGlass.Tools;
using ImageGlass.UI;
using ImageGlass.UI.Viewer;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace ImageGlass.Common;

public partial class Config
{
    #region Public static properties

    /// <summary>
    /// App setting specs version, to check for compatibility.
    /// </summary>
    [JsonIgnore]
    public static float SPEC_VERSION => 10f;


    /// <summary>
    /// Gets the user config file name.
    /// </summary>
    [JsonIgnore]
    public static string CONFIG_USER => "igconfig.json";


    /// <summary>
    /// Gets the default config file located.
    /// </summary>
    [JsonIgnore]
    public static string CONFIG_DEFAULT => "igconfig.default.json";


    /// <summary>
    /// Gets the admin config file name.
    /// </summary>
    [JsonIgnore]
    public static string CONFIG_ADMIN => "igconfig.admin.json";


    /// <summary>
    /// Gets the exception while loading app settings.
    /// </summary>
    [JsonIgnore]
    public static Exception? LoadingException { get; private set; } = null;


    /// <summary>
    /// Gets the default image formats.
    /// </summary>
    [JsonIgnore]
    public static ReadOnlyCollection<string> DefaultFileFormats { get; } = new(
        Const.IMAGE_FORMATS.Split(';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        )
    );


    /// <summary>
    /// Gets the default image info tags.
    /// </summary>
    [JsonIgnore]
    public static ReadOnlyCollection<string> DefaultImageInfoTags { get; } = new([
        "Name",
        "ListCount",
        "FrameCount",
        "Zoom",
        "Dimension",
        "FileSize",
        "ColorSpace",
        "HdrInfo",
        "ExifRating",
        "DateTimeAuto",
        "AppName",
    ]);


    /// <summary>
    /// Gets the default mouse wheel actions.
    /// </summary>
    [JsonIgnore]
    public static Dictionary<MouseWheelEvent, MouseWheelAction> DefaultMouseWheelActions { get; } = new()
    {
        [MouseWheelEvent.Scroll] = MouseWheelAction.Zoom,
        [MouseWheelEvent.CtrlAndScroll] = MouseWheelAction.PanVertically,
        [MouseWheelEvent.ShiftAndScroll] = MouseWheelAction.PanHorizontally,
        [MouseWheelEvent.AltAndScroll] = MouseWheelAction.BrowseImages,
    };


    /// <summary>
    /// Gets the default mouse click actions.
    /// </summary>
    [JsonIgnore]
    public static Dictionary<MouseClickEvent, SingleAction> DefaultMouseClickActions { get; } = new()
    {
        [MouseClickEvent.LeftDoubleClick] = new SingleAction(API.IG_SetZoomForMouseClick),
        [MouseClickEvent.RightClick] = new SingleAction(API.IG_OpenContextMenu),
        [MouseClickEvent.WheelClick] = new SingleAction(API.IG_Refresh),
        [MouseClickEvent.XButton1Click] = new SingleAction(API.IG_ViewPrevious),
        [MouseClickEvent.XButton2Click] = new SingleAction(API.IG_ViewNext),
    };


    /// <summary>
    /// Gets the default toolbar items.
    /// </summary>
    [JsonIgnore]
    public static ReadOnlyCollection<ToolbarItemModel> DefaultToolbarItems =>
    [
        // open file
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.OpenFile) }",
            Image = nameof(IgThemeIcon.OpenFile),
            Text = Lang.KeysMap[LangId.FrmMain_MnuOpenFile],
            Alignment = ToolbarItemAlignment.Right,
            OnClick = new(LangId.FrmMain_MnuOpenFile, API.IG_OpenFile),
        },



        // view previous
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ViewPreviousImage)}",
            Image = nameof(IgThemeIcon.ViewPreviousImage),
            Text = Lang.KeysMap[LangId.FrmMain_MnuViewPrevious],
            OnClick = new(LangId.FrmMain_MnuViewPrevious, API.IG_ViewPrevious),
        },
        // view next
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ViewNextImage)}",
            Image = nameof(IgThemeIcon.ViewNextImage),
            Text = Lang.KeysMap[LangId.FrmMain_MnuViewNext],
            OnClick = new(LangId.FrmMain_MnuViewNext, API.IG_ViewNext),
        },
        ToolbarItemModel.Separator,


        // rotate left
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.RotateLeft)}",
            Image = nameof(IgThemeIcon.RotateLeft),
            Text = Lang.KeysMap[LangId.FrmMain_MnuRotateLeft],
            OnClick = new(LangId.FrmMain_MnuRotateLeft, API.IG_Rotate, nameof(RotateOption.Left)),
        },
        // rotate right
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.RotateRight)}",
            Image = nameof(IgThemeIcon.RotateRight),
            Text = Lang.KeysMap[LangId.FrmMain_MnuRotateRight],
            OnClick = new(LangId.FrmMain_MnuRotateRight, API.IG_Rotate, nameof(RotateOption.Right)),
        },
        // flip horizontal
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.FlipHorz)}",
            Image = nameof(IgThemeIcon.FlipHorz),
            Text = Lang.KeysMap[LangId.FrmMain_MnuFlipHorizontal],
            OnClick = new(LangId.FrmMain_MnuFlipHorizontal, API.IG_FlipImage, nameof(FlipOptions.Horizontal)),
        },
        // flip vertical
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.FlipVert)}",
            Image = nameof(IgThemeIcon.FlipVert),
            Text = Lang.KeysMap[LangId.FrmMain_MnuFlipVertical],
            OnClick = new(LangId.FrmMain_MnuFlipVertical, API.IG_FlipImage, nameof(FlipOptions.Vertical)),
        },
        // crop tool
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Crop)}",
            Image = nameof(IgThemeIcon.Crop),
            Text = Lang.KeysMap[LangId.FrmMain_MnuCropTool],
            OnClick = new(LangId.FrmMain_MnuCropTool, API.IG_ToggleTool, CropImageToolControl.TOOL_ID),
        },
        ToolbarItemModel.Separator,


        // auto zoom
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.AutoZoom)}",
            Image = nameof(IgThemeIcon.AutoZoom),
            Text = Lang.KeysMap[LangId.FrmMain_MnuAutoZoom],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.AutoZoom.ToString(),
            OnClick = new(LangId.FrmMain_MnuAutoZoom, API.IG_SetZoomMode, nameof(ZoomMode.AutoZoom)),
        },
        // lock zoom
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.LockZoom)}",
            Image = nameof(IgThemeIcon.LockZoom),
            Text = Lang.KeysMap[LangId.FrmMain_MnuLockZoom],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.LockZoom.ToString(),
            OnClick = new(LangId.FrmMain_MnuLockZoom, API.IG_SetZoomMode, nameof(ZoomMode.LockZoom)),
        },
        // scale to width
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToWidth)}",
            Image = nameof(IgThemeIcon.ScaleToWidth),
            Text = Lang.KeysMap[LangId.FrmMain_MnuScaleToWidth],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToWidth.ToString(),
            OnClick = new(LangId.FrmMain_MnuScaleToWidth, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToWidth)),
        },
        // scale to height
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToHeight)}",
            Image = nameof(IgThemeIcon.ScaleToHeight),
            Text = Lang.KeysMap[LangId.FrmMain_MnuScaleToHeight],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToHeight.ToString(),
            OnClick = new(LangId.FrmMain_MnuScaleToHeight, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToHeight)),
        },
        // scale to fit
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToFit)}",
            Image = nameof(IgThemeIcon.ScaleToFit),
            Text = Lang.KeysMap[LangId.FrmMain_MnuScaleToFit],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToFit.ToString(),
            OnClick = new(LangId.FrmMain_MnuScaleToFit, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToFit)),
        },
        // scale to fill
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToFill)}",
            Image = nameof(IgThemeIcon.ScaleToFill),
            Text = Lang.KeysMap[LangId.FrmMain_MnuScaleToFill],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToFill.ToString(),
            OnClick = new(LangId.FrmMain_MnuScaleToFill, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToFill)),
        },
        ToolbarItemModel.Separator,


        // refresh
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Refresh)}",
            Image = nameof(IgThemeIcon.Refresh),
            Text = Lang.KeysMap[LangId.FrmMain_MnuRefresh],
            OnClick = new(LangId.FrmMain_MnuRefresh, API.IG_Refresh),
        },
        // toggle gallery
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Gallery)}",
            Image = nameof(IgThemeIcon.Gallery),
            Text = Lang.KeysMap[LangId.FrmMain_MnuToggleGallery],
            ConfigBinding = nameof(Config.ShowGallery),
            ConfigBindingValue = "True",
            OnClick = new(LangId.FrmMain_MnuToggleGallery, API.IG_ToggleGallery),
        },
        // toggle checkerboard
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Checkerboard)}",
            Image = nameof(IgThemeIcon.Checkerboard),
            Text = Lang.KeysMap[LangId.FrmMain_MnuToggleCheckerboard],
            ConfigBinding = nameof(Config.CheckerboardMode),
            ConfigBindingValue = $"!{nameof(CheckerboardType.None)}",
            OnClick = new(LangId.FrmMain_MnuToggleCheckerboard, API.IG_ToggleCheckerboard),
        },
        // toggle fullscreen
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.FullScreen)}",
            Image = nameof(IgThemeIcon.FullScreen),
            Text = Lang.KeysMap[LangId.FrmMain_MnuFullScreen],
            ConfigBinding = nameof(Config.EnableFullScreen),
            ConfigBindingValue = "True",
            OnClick = new(LangId.FrmMain_MnuFullScreen, API.IG_ToggleFullScreen),
        },
        ToolbarItemModel.Separator,


        // delete
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Delete)}",
            Image = nameof(IgThemeIcon.Delete),
            Text = Lang.KeysMap[LangId.FrmMain_MnuMoveToRecycleBin],
            OnClick = new(LangId.FrmMain_MnuMoveToRecycleBin, API.IG_Delete),
        }
    ];

    #endregion // Public static properties



    // Public static methods
    #region Public static methods

    /// <summary>
    /// Loads and merges configs from multiple sources.
    /// Priority (lowest -> highest):
    /// developer defaults -> igconfig.default.json -> igconfig.json -> CLI args -> igconfig.admin.json.
    /// </summary>
    public static Config Load(string configFileName, string[]? cliArgs = null)
    {
        Config? appConfig = null;

        try
        {
            var jsonOptions = BHelper.CreateJsonOptions();
            var jsonContext = new ConfigJsonContext(jsonOptions);

            if (Const.ENABLE_CONFIG_MERGE)
            {
                // 1. read igconfig.default.json (Startup Dir, then Config Dir fallback)
                using var defaultDoc = ReadConfigJsonDocument(
                    BHelper.BaseDir(CONFIG_DEFAULT),
                    BHelper.ConfigDir(CONFIG_DEFAULT));

                // 2. read igconfig.json (Config Dir only)
                var userConfigPath = BHelper.ConfigDir(configFileName);
                using var userDoc = BHelper.ReadJsonDocFromFile(userConfigPath);

                // 3. parse CLI -p: args
                var cliOverrides = ParseCliConfigArgs(cliArgs);

                // 4. read igconfig.admin.json (Startup Dir, then Config Dir fallback)
                using var adminDoc = ReadConfigJsonDocument(
                    BHelper.BaseDir(CONFIG_ADMIN),
                    BHelper.ConfigDir(CONFIG_ADMIN));

                // 5. merge all layers into a single JSON byte array
                var mergedJson = MergeJsonLayers(defaultDoc, userDoc, cliOverrides, adminDoc);

                // 6. deserialize the merged JSON into Config
                var config = JsonSerializer.Deserialize(mergedJson, jsonContext.Config)
                    ?? throw new FileLoadException("IGE: Could not parse merged config.");

                // 7. migrate if config version changed
                appConfig = MigrateUserConfigFile(config);
            }
            else
            {
                // simple single-file load (no merge)
                var configPath = BHelper.ConfigDir(configFileName);

                if (File.Exists(configPath))
                {
                    var config = BHelper.ReadJsonFromFile(configPath, jsonContext.Config)
                        ?? throw new FileLoadException($"IGE: Could not parse settings from file: {configPath}");

                    appConfig = MigrateUserConfigFile(config);
                }
            }
        }
        catch (Exception ex)
        {
            LoadingException = ex;
        }

        appConfig ??= new();
        return appConfig;
    }


    /// <summary>
    /// Applies CLI config overrides (<c>-p:Key=Value</c>) to the current config instance.
    /// Used when the first instance receives forwarded args from a second instance.
    /// </summary>
    public static void ApplyCliOverrides(Config config, string[]? cliArgs)
    {
        if (!Const.ENABLE_CONFIG_MERGE) return;

        var overrides = ParseCliConfigArgs(cliArgs);
        if (overrides.Count == 0) return;

        try
        {
            var jsonOptions = BHelper.CreateJsonOptions();
            var jsonContext = new ConfigJsonContext(jsonOptions);

            // serialize current config to JSON
            var currentJson = JsonSerializer.SerializeToUtf8Bytes(config, jsonContext.Config);
            using var currentDoc = JsonDocument.Parse(currentJson);

            // merge CLI overrides on top
            var mergedJson = MergeJsonLayers(null, currentDoc, overrides, null);

            // deserialize back into a new Config
            var updated = JsonSerializer.Deserialize(mergedJson, jsonContext.Config);
            if (updated == null) return;

            // copy all values from the updated config
            foreach (var kvp in updated._values)
            {
                config.Set(kvp.Key, kvp.Value);
            }
        }
        catch { }
    }


    /// <summary>
    /// Migrates user config file.
    /// </summary>
    private static Config MigrateUserConfigFile(Config config)
    {
        var configVersion = config._Metadata.Version;

        // update config version
        config._Metadata.Version = SPEC_VERSION;

        // no change
        if (SPEC_VERSION <= configVersion) return config;


        // Migration v9 to v10
        if (configVersion < 10)
        {
            // ShowCheckerboard + ShowCheckerboardOnlyImageRegion: merged into CheckerboardMode
            // ZoomLevels: change type: number[] to string
        }

        return config;
    }


    #endregion // Public static methods



    // Public methods
    #region Public methods


    /// <summary>
    /// Writes configs to file.
    /// </summary>
    public async Task SaveAsync()
    {
        var jsonFilePath = BHelper.ConfigDir(CONFIG_USER);
        var jsonOptions = BHelper.CreateJsonOptions();
        var jsonContext = new ConfigJsonContext(jsonOptions);

        await BHelper.WriteJsonToFileAsync(jsonFilePath, this, jsonContext.Config);
    }


    /// <summary>
    /// Loads app language <see cref="Config.Lang"/>.
    /// </summary>
    public async Task LoadCurrentLanguageAsync()
    {
        var langPath = BHelper.BaseDir(Dir.Language, Language);
        var lang = new Lang(langPath);

        // load language pack
        await lang.LoadAsync();

        // set app language
        Core.Lang = lang;
    }


    /// <summary>
    /// Loads theme pack <see cref="Config.Theme"/>.
    /// </summary>
    /// <param name="darkMode">
    /// Determine which theme should be loaded: <see cref="DarkTheme"/> or <see cref="LightTheme"/>.
    /// </param>
    /// <param name="useFallBackTheme">
    /// If theme pack is invalid, should load the default theme pack <see cref="Const.DEFAULT_THEME"/>.
    /// </param>
    /// <param name="throwIfThemeInvalid">
    /// If theme pack is invalid, should throw exception.
    /// </param>
    /// <param name="forceUpdateBackground">Force updating background according to theme value</param>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> LoadCurrentThemeAsync(bool darkMode,
        bool useFallBackTheme, bool throwIfThemeInvalid, bool forceUpdateBackground)
    {
        // 1. get the theme folder name
        var themeFolderName = darkMode ? DarkTheme : LightTheme;
        if (string.IsNullOrEmpty(themeFolderName))
        {
            themeFolderName = Const.DEFAULT_THEME;
        }

        // 2. check if theme pack is already loaded
        if (themeFolderName.Equals(Core.Theme.FolderName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 3. load theme pack
        var th = await FindAndLoadThemePackAsync(themeFolderName, useFallBackTheme, throwIfThemeInvalid);

        // 4. update the name of dark/light theme
        if (darkMode) DarkTheme = th.FolderName;
        else LightTheme = th.FolderName;


        // 5. load background color
        if (BackgroundColor == Core.Theme.Colors.BgColor || forceUpdateBackground)
        {
            BackgroundColor = th.Colors.BgColor;
        }


        // 6. set to the current theme
        var success = Core.SetTheme(th);
        return success;
    }


    /// <summary>
    /// Finds the correct location of theme name and loads it.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    private static async Task<IgTheme> FindAndLoadThemePackAsync(string themeFolderName,
        bool useFallBackTheme, bool throwIfThemeInvalid)
    {
        // 1. look for theme pack in the Config dir
        var themeConfigPath = BHelper.ConfigDir(Dir.Themes, themeFolderName);
        var th = await new IgTheme().LoadAsync(themeConfigPath);

        if (!th.IsValid)
        {
            // 2. look for theme pack in the base dir
            var baseThemeConfigPath = BHelper.BaseDir(Dir.Themes, themeFolderName);
            th = await new IgTheme().LoadAsync(baseThemeConfigPath);

            // 3. cannot find theme, use fall back theme
            if (!th.IsValid && useFallBackTheme)
            {
                // 4. load default theme
                baseThemeConfigPath = BHelper.BaseDir(Dir.Themes, Const.DEFAULT_THEME);
                th = await new IgTheme().LoadAsync(baseThemeConfigPath);
            }
        }

        // 5. throw error if theme is invalid
        if (!th.IsValid && throwIfThemeInvalid)
        {
            throw new ArgumentException($"IGE: Unable to load '{themeFolderName}' theme pack. " +
                $"Please make sure '{themeConfigPath}' file is valid.", nameof(themeFolderName));
        }

        return th;
    }


    /// <summary>
    /// Gets control layout position.
    /// </summary>
    public static LayoutPosition GetControlLayout(LayoutControl control)
    {
        var defaultPos = control == LayoutControl.Toolbar
            ? LayoutPosition.Top
            : LayoutPosition.Bottom;


        // 1. read control's layouts from setting
        var pos = Core.Config.Layout.GetValueOrDefault(control, defaultPos);


        // 2. standardize toolbar position
        if (control == LayoutControl.Toolbar)
        {
            if (pos is LayoutPosition.Left or LayoutPosition.Right)
            {
                pos = LayoutPosition.Top;
            }
        }

        return pos;
    }


    #endregion // Public methods



    // Private static methods (config merge)
    #region Private static methods (config merge)

    /// <summary>
    /// Reads a JSON config file using dual-path fallback (primary first, then fallback).
    /// Returns <c>null</c> if neither path exists.
    /// </summary>
    private static JsonDocument? ReadConfigJsonDocument(string primaryPath, string fallbackPath)
    {
        var path = File.Exists(primaryPath) ? primaryPath
            : File.Exists(fallbackPath) ? fallbackPath
            : null;

        if (string.IsNullOrEmpty(path)) return null;

        return BHelper.ReadJsonDocFromFile(path);
    }


    /// <summary>
    /// Parses CLI arguments with <see cref="Const.CONFIG_CMD_PREFIX"/> prefix
    /// into a dictionary of property-name -> raw-JSON-value pairs.
    /// Example: <c>-p:ShowGallery=true</c> -> <c>{ "ShowGallery": "true" }</c>.
    /// </summary>
    private static Dictionary<string, string> ParseCliConfigArgs(string[]? args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (args == null) return result;

        foreach (var arg in args)
        {
            if (!arg.StartsWith(Const.CONFIG_CMD_PREFIX, StringComparison.OrdinalIgnoreCase))
                continue;

            // strip prefix, e.g. "-p:ShowGallery=true" -> "ShowGallery=true"
            var kvPart = arg[Const.CONFIG_CMD_PREFIX.Length..];
            var eqIdx = kvPart.IndexOf('=');
            if (eqIdx <= 0) continue;

            var key = kvPart[..eqIdx].Trim();
            var value = kvPart[(eqIdx + 1)..].Trim();
            if (key.Length == 0) continue;

            result[key] = value;
        }

        return result;
    }


    /// <summary>
    /// Merges multiple JSON config layers into a single UTF-8 byte array.
    /// Later layers override earlier ones at the top-level property level (shallow merge).
    /// CLI overrides are written as raw JSON values.
    /// </summary>
    private static byte[] MergeJsonLayers(
        JsonDocument? defaultDoc,
        JsonDocument? userDoc,
        Dictionary<string, string> cliOverrides,
        JsonDocument? adminDoc)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = false,
        });

        writer.WriteStartObject();

        // collect all property names across all layers (preserves override order)
        var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectKeys(defaultDoc, allKeys);
        CollectKeys(userDoc, allKeys);
        foreach (var k in cliOverrides.Keys) allKeys.Add(k);
        CollectKeys(adminDoc, allKeys);

        foreach (var key in allKeys)
        {
            // admin > CLI > user > default (last wins)
            if (TryGetProperty(adminDoc, key, out var adminVal))
            {
                writer.WritePropertyName(key);
                adminVal.WriteTo(writer);
            }
            else if (cliOverrides.TryGetValue(key, out var cliRaw))
            {
                WriteCliValue(writer, key, cliRaw);
            }
            else if (TryGetProperty(userDoc, key, out var userVal))
            {
                writer.WritePropertyName(key);
                userVal.WriteTo(writer);
            }
            else if (TryGetProperty(defaultDoc, key, out var defVal))
            {
                writer.WritePropertyName(key);
                defVal.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }


    /// <summary>
    /// Collects top-level property names from a <see cref="JsonDocument"/>.
    /// </summary>
    private static void CollectKeys(JsonDocument? doc, HashSet<string> keys)
    {
        if (doc == null) return;
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            keys.Add(prop.Name);
        }
    }


    /// <summary>
    /// Tries to get a top-level property from a <see cref="JsonDocument"/> (case-insensitive).
    /// </summary>
    private static bool TryGetProperty(JsonDocument? doc, string name, out JsonElement value)
    {
        value = default;
        if (doc == null) return false;

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Writes a CLI override value as a JSON property.
    /// Attempts to parse the value as JSON first; falls back to writing as a string.
    /// </summary>
    private static void WriteCliValue(Utf8JsonWriter writer, string key, string rawValue)
    {
        writer.WritePropertyName(key);

        // try parsing as valid JSON (handles true, false, null, numbers, arrays, objects)
        try
        {
            using var doc = JsonDocument.Parse(rawValue);
            doc.RootElement.WriteTo(writer);
        }
        catch (JsonException)
        {
            // not valid JSON, write as a quoted string
            writer.WriteStringValue(rawValue);
        }
    }

    #endregion // Private static methods (config merge)


}
