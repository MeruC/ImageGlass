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
using ImageGlass.Common.Types;
using ImageGlass.Common.Types.JsonTypeConverters;
using System.Text.Json.Serialization;

namespace ImageGlass.Tools;


[JsonSerializable(typeof(QuickCropConfig))]
public partial class QuickCropConfigJsonContext : JsonSerializerContext { }


/// <summary>
/// Which axis a selection is allowed to move along while dragging; the other axis stays fixed.
/// </summary>
public enum QuickCropAxisLock
{
    None,
    Horizontal,
    Vertical,
}


/// <summary>
/// Provides settings for the Quick Crop tool: a rapid-fire "select, save as a new file, repeat"
/// workflow for cropping the same fixed area out of many images, unrelated to the regular Crop
/// tool's single-image overwrite/save-as flow.
/// </summary>
public class QuickCropConfig() : PhReactive
{
    /// <summary>
    /// Gets, sets the folder crops are saved into. Empty means the default
    /// (<c>Pictures\crops</c> under the user's profile).
    /// </summary>
    public string SaveFolder
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = "";


    /// <summary>
    /// Gets, sets the locked selection width, in source pixels. 0 means no size lock.
    /// </summary>
    public int LockedWidth
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = 0;


    /// <summary>
    /// Gets, sets the locked selection height, in source pixels. 0 means no size lock.
    /// </summary>
    public int LockedHeight
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = 0;


    /// <summary>
    /// Gets, sets which axis a selection can be dragged along.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumSafeConverter<QuickCropAxisLock>))]
    public QuickCropAxisLock AxisLock
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = QuickCropAxisLock.None;


    /// <summary>
    /// Gets, sets the fixed position (in source pixels) of whichever axis <see cref="AxisLock"/>
    /// pins: X while <see cref="QuickCropAxisLock.Vertical"/>, Y while
    /// <see cref="QuickCropAxisLock.Horizontal"/>. -1 means not set yet.
    /// </summary>
    public int LockedAxisPosition
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = -1;

}
