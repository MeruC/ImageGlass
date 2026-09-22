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
using ImageGlass.Common.Types;
using ImageGlass.Common.Types.JsonTypeConverters;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Linq;

namespace ImageGlass.Tools;


[JsonSerializable(typeof(CropImageConfig))]
[JsonSerializable(typeof(List<string>))]
public partial class CropImageConfigJsonContext : JsonSerializerContext { }


/// <summary>
/// A parsed crop preset: a fixed size, and an optional fixed location.
/// </summary>
public readonly record struct CropPresetSize(int W, int H, int? X, int? Y);


/// <summary>
/// Provides settings for Color Picker tool.
/// </summary>
public class CropImageConfig() : PhReactive
{

    /// <summary>
    /// Gets, sets the option to close the Crop tool after the selected area is saved.
    /// </summary>
    public bool CloseAfterSaved
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = false;


    /// <summary>
    /// Gets, sets whether saving backs up the original file into an "originals" subfolder
    /// next to it before it's overwritten.
    /// </summary>
    public bool BackupOriginalOnSave
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = false;


    /// <summary>
    /// Gets, sets the option to center the <see cref="InitSelectedArea"/>.
    /// </summary>
    public bool AutoCenterSelection
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = true;


    /// <summary>
    /// Gets, sets the aspect ratio type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumSafeConverter<SelectionAspectRatio>))]
    public SelectionAspectRatio AspectRatio
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = SelectionAspectRatio.FreeRatio;


    /// <summary>
    /// Gets, sets the aspect ratio values.
    /// </summary>
    [JsonConverter(typeof(JsonArrayToIntConverter))]
    public int[] AspectRatioValues
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = [0, 0];


    /// <summary>
    /// Gets, sets the default selection type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumSafeConverter<DefaultSelectionType>))]
    public DefaultSelectionType InitSelectionType
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = DefaultSelectionType.SelectAll;


    /// <summary>
    /// Gets, sets the custom selection area is used for <see cref="DefaultSelectionType.CustomArea"/>.
    /// </summary>
    [JsonConverter(typeof(JsonArrayToRectConverter))]
    public Rect InitSelectedArea
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = new();





    /// <summary>
    /// Gets, sets user-defined preset crop sizes as "WxH" strings (e.g. "1660x1080"), optionally
    /// followed by a fixed location as "WxH@X,Y" (e.g. "1920x816@0,132"). A preset without a
    /// location keeps whatever position the selection currently has when applied.
    /// </summary>
    public List<string> PresetSizes
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = ["1660x1080", "1920x816@0,132"];


    /// <summary>
    /// Parses <see cref="PresetSizes"/> into <see cref="CropPresetSize"/> values, skipping invalid entries.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<CropPresetSize> ParsedPresetSizes =>
        PresetSizes
            .Select(ParsePresetSize)
            .Where(p => p is not null)
            .Select(p => p!.Value);


    /// <summary>
    /// Parses a single "WxH" or "WxH@X,Y" preset string.
    /// </summary>
    private static CropPresetSize? ParsePresetSize(string s)
    {
        var parts = s.Split('@', 2);

        var sizeTokens = parts[0].Split(['x', 'X', '×'], 2);
        if (sizeTokens.Length != 2
            || !int.TryParse(sizeTokens[0].Trim(), out var w)
            || !int.TryParse(sizeTokens[1].Trim(), out var h))
        {
            return null;
        }

        int? x = null, y = null;
        if (parts.Length > 1)
        {
            var locationTokens = parts[1].Split(',', 2);
            if (locationTokens.Length == 2
                && int.TryParse(locationTokens[0].Trim(), out var px)
                && int.TryParse(locationTokens[1].Trim(), out var py))
            {
                x = px;
                y = py;
            }
        }

        return new CropPresetSize(w, h, x, y);
    }


    /// <summary>
    /// Gets the aspect ratio value.
    /// </summary>
    public static Dictionary<SelectionAspectRatio, int[]> AspectRatioValue => new(9)
    {
        { SelectionAspectRatio.Ratio1_1,    [1, 1] },
        { SelectionAspectRatio.Ratio1_2,    [1, 2] },
        { SelectionAspectRatio.Ratio2_1,    [2, 1] },
        { SelectionAspectRatio.Ratio2_3,    [2, 3] },
        { SelectionAspectRatio.Ratio3_2,    [3, 2] },
        { SelectionAspectRatio.Ratio3_4,    [3, 4] },
        { SelectionAspectRatio.Ratio4_3,    [4, 3] },
        { SelectionAspectRatio.Ratio9_16,   [9, 16] },
        { SelectionAspectRatio.Ratio16_9,   [16, 9] },
    };

}
