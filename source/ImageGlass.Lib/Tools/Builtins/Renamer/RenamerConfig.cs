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
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ImageGlass.Tools;


[JsonSerializable(typeof(RenamerConfig))]
public partial class RenamerConfigJsonContext : JsonSerializerContext { }


/// <summary>
/// Provides settings for the Renamer tool: batch-tag images in the current folder and export them
/// as <c>.webp</c> under a preset name, unrelated to the Crop/Quick Crop tools' selection editing.
/// </summary>
public class RenamerConfig() : PhReactive
{
    /// <summary>
    /// Gets, sets the user-added preset tag names, shown after the built-in list.
    /// </summary>
    public List<string> CustomNames
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = [];


    /// <summary>
    /// Gets, sets the grid thumbnail size, in pixels. Independent of the Gallery's own
    /// <c>ThumbnailSize</c> setting, so zooming the Renamer grid doesn't affect the filmstrip.
    /// </summary>
    public int ThumbnailSize
    {
        get; set
        {
            if (field == value) return;
            field = value;
            _ = OnPropertyChanged();
        }
    } = 110;

}
