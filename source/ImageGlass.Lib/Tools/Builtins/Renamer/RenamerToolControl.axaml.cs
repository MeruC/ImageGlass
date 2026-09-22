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
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ImageGlass.Common;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.ServiceProviders;
using ImageGlass.UI;
using ImageGlass.UI.Viewer;
using ImageGlass.UI.Windowing;
using ImageGlass.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageGlass.Tools;

/// <summary>
/// Renamer: batch-tags every image in the current folder with a preset name and exports the
/// staged ones as <c>.webp</c> into an "output" subfolder, sweeping anything left untagged into
/// an "unrenamed" subfolder. Built for triaging a big drop of screenshots into a naming scheme,
/// not for editing a single photo like the Crop tools.
/// </summary>
public partial class RenamerToolControl : PhControl, IToolControl
{
    private static readonly string[] DefaultNames =
    [
        "cover", "char", "banner", "poster", "story", "anecdote",
        "garment", "login", "euphoria", "prestorm", "roar", "uttu",
        "crittercrash", "manes", "reveries", "warroom", "wilderness",
        "double", "mart", "shorten", "task", "teaser",
    ];

    private sealed class RenamerItem
    {
        public required string FilePath;
        public required Photo Photo;
        public string? PendingName;
        public Border Cell = null!;
        public Image ImageControl = null!;
        public TextBlock NameLabel = null!;
    }

    private readonly List<RenamerItem> _items = [];
    private readonly List<int> _selection = [];
    private readonly List<List<(int Index, string? OldPendingName)>> _undoStack = [];
    private readonly List<(StackPanel Row, string Name)> _nameRows = [];

    private string _folder = "";
    private TopLevel? _topLevel;
    private int _lastClickedIndex = -1;
    private DispatcherTimer? _zoomReloadTimer;

    private static readonly IBrush NormalBrush = Brushes.Transparent;
    private static readonly IBrush SelectedBrush = new SolidColorBrush(Color.FromArgb(0x50, 0x33, 0x88, 0xFF));
    private static readonly IBrush StagedBrush = new SolidColorBrush(Color.FromArgb(0x45, 0x33, 0xAA, 0x33));
    private static readonly IBrush StagedSelectedBrush = new SolidColorBrush(Color.FromArgb(0x80, 0x33, 0xAA, 0x33));


    public static string TOOL_ID => "Tool_Renamer";
    public string ToolId => TOOL_ID;
    public bool HasSettingsUI => false;
    public bool PrefersFullHost => true;
    public object? Settings { get; private set; } = new RenamerConfig();
    public RenamerConfig Options => (RenamerConfig)Settings!;
    public ViewerControl Viewer { get; set; } = null!;


    public RenamerToolControl()
    {
        InitializeComponent();
    }



    #region Control Events

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        PART_BtnSave.Click += PART_BtnSave_Click;
        PART_BtnUndo.Click += PART_BtnUndo_Click;
        PART_BtnAddName.Click += PART_BtnAddName_Click;
        PART_TxtSearch.TextChanged += PART_TxtSearch_TextChanged;
        PART_ChkPreview.IsCheckedChanged += PART_ChkPreview_IsCheckedChanged;
        PART_SldZoom.ValueChanged += PART_SldZoom_ValueChanged;
        PART_GridScroll.AddHandler(InputElement.PointerWheelChangedEvent, PART_GridScroll_PointerWheelChanged, handledEventsToo: true);

        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, Root_KeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);

        PART_SldZoom.Value = Options.ThumbnailSize;
        PART_LblZoomValue.Text = $"{Options.ThumbnailSize}px";

        RebuildNameRows();
        RebuildGrid();
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        PART_BtnSave.Click -= PART_BtnSave_Click;
        PART_BtnUndo.Click -= PART_BtnUndo_Click;
        PART_BtnAddName.Click -= PART_BtnAddName_Click;
        PART_TxtSearch.TextChanged -= PART_TxtSearch_TextChanged;
        PART_ChkPreview.IsCheckedChanged -= PART_ChkPreview_IsCheckedChanged;
        PART_SldZoom.ValueChanged -= PART_SldZoom_ValueChanged;
        PART_GridScroll.RemoveHandler(InputElement.PointerWheelChangedEvent, PART_GridScroll_PointerWheelChanged);

        _topLevel?.RemoveHandler(KeyDownEvent, Root_KeyDown);
        _topLevel = null;

        _zoomReloadTimer?.Stop();
        _zoomReloadTimer = null;

        base.OnUnloaded(e);
    }


    protected override void OnIgLanguageChanged()
    {
        base.OnIgLanguageChanged();

        PART_BtnSave.Text = Core.Lang[LangId.Tool_Renamer_BtnSave];
        PART_BtnUndo.Text = Core.Lang[LangId.Tool_Renamer_BtnUndo];
        ToolTip.SetTip(PART_BtnAddName, Core.Lang[LangId.Tool_Renamer_BtnAddName]);
        PART_BtnAddName.Text = "+";
        PART_TxtSearch.Watermark = Core.Lang[LangId.Tool_Renamer_TxtSearchPlaceholder];

        UpdateStatus();
    }


    private void Root_KeyDown(object? sender, KeyEventArgs e)
    {
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        if (focused is TextBox) return;

        if (e.Key == Key.A && e.KeyModifiers == KeyModifiers.Control)
        {
            e.Handled = true;
            SelectAll();
        }
        else if (e.Key == Key.Z && e.KeyModifiers == KeyModifiers.Control)
        {
            e.Handled = true;
            UndoStage();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            DeselectAll();
        }
    }


    private async void PART_BtnSave_Click(object? sender, RoutedEventArgs e) => await SaveAllAsync();
    private void PART_BtnUndo_Click(object? sender, RoutedEventArgs e) => UndoStage();


    private async void PART_BtnAddName_Click(object? sender, RoutedEventArgs e)
    {
        var result = await ModalWindow.ShowInputAsync(null, new ModalWindowOptions
        {
            Title = Core.Lang[LangId.Tool_Renamer_BtnAddName],
            Heading = Core.Lang[LangId.Tool_Renamer_BtnAddName],
        });

        if (result.ExitCode != DialogExitCode.OK) return;

        var name = result.InputValue.Trim().ToLowerInvariant().Replace(" ", "");
        if (string.IsNullOrEmpty(name)) return;

        var alreadyExists = GetAllNames().Contains(name);
        var hadSelection = _selection.Count > 0;

        if (!alreadyExists)
        {
            Options.CustomNames.Add(name);
            SaveCustomNames();
            RebuildNameRows();
        }

        if (hadSelection) StageName(name);
    }


    private void PART_TxtSearch_TextChanged(object? sender, TextChangedEventArgs e) => FilterNameRows();


    private void PART_SldZoom_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        SetThumbnailSize((int)e.NewValue);
    }


    /// <summary>
    /// Ctrl+scroll over the grid zooms it, matching image viewers' usual "Ctrl+wheel = zoom"
    /// convention; a plain scroll still scrolls the grid.
    /// </summary>
    private void PART_GridScroll_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;

        e.Handled = true;
        var step = e.Delta.Y > 0 ? 10 : -10;
        SetThumbnailSize((int)PART_SldZoom.Value + step);
    }

    #endregion // Control Events



    #region Control Methods

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void LoadSettings(JsonElement? jsonEl)
    {
        var settings = jsonEl?.Deserialize(RenamerConfigJsonContext.Default.RenamerConfig);
        if (settings is not null)
        {
            Settings = settings;
        }
    }


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public JsonElement? SaveSettings()
    {
        return JsonSerializer.SerializeToElement(Options, RenamerConfigJsonContext.Default.RenamerConfig);
    }


    /// <summary>
    /// Persists <see cref="RenamerConfig.CustomNames"/> immediately: it's edited through its own
    /// dialog, not the usual "close the tool to save" flow the other settings rely on.
    /// </summary>
    private void SaveCustomNames()
    {
        ToolRegistry.SaveToolSettings(this);
    }


    #region Name sidebar

    private IEnumerable<string> GetAllNames() => DefaultNames.Concat(Options.CustomNames);


    private void RebuildNameRows()
    {
        PART_NamesList.Children.Clear();
        _nameRows.Clear();

        foreach (var name in GetAllNames())
        {
            var nameBtn = new PhButton
            {
                Text = name,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            nameBtn.Click += (_, _) => StageName(name);

            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
            row.Children.Add(nameBtn);

            if (Options.CustomNames.Contains(name))
            {
                var delBtn = new PhButton { Text = "✕", MinWidth = 26, HorizontalContentAlignment = HorizontalAlignment.Center };
                delBtn.Click += (_, _) => DeleteName(name);
                row.Children.Add(delBtn);
            }

            PART_NamesList.Children.Add(row);
            _nameRows.Add((row, name));
        }

        FilterNameRows();
    }


    private void FilterNameRows()
    {
        var query = (PART_TxtSearch.Text ?? "").Trim().ToLowerInvariant();
        foreach (var (row, name) in _nameRows)
        {
            row.IsVisible = string.IsNullOrEmpty(query) || name.ToLowerInvariant().Contains(query);
        }
    }


    private void DeleteName(string name)
    {
        if (!Options.CustomNames.Remove(name)) return;

        SaveCustomNames();
        RebuildNameRows();
    }

    #endregion // Name sidebar


    #region Thumbnail grid

    /// <summary>
    /// Applies a new grid thumbnail size: resizes existing cells immediately (no reload, so it
    /// stays smooth while dragging the slider or scrolling), then reloads thumbnails at the new
    /// resolution after a short pause so zooming in doesn't leave them soft.
    /// </summary>
    private void SetThumbnailSize(int size)
    {
        size = Math.Clamp(size, (int)PART_SldZoom.Minimum, (int)PART_SldZoom.Maximum);
        if (size == Options.ThumbnailSize) return;

        Options.ThumbnailSize = size;
        PART_SldZoom.Value = size;
        PART_LblZoomValue.Text = $"{size}px";

        foreach (var item in _items)
        {
            item.ImageControl.Width = size;
            item.ImageControl.Height = size;
            item.NameLabel.MaxWidth = size;
        }

        _zoomReloadTimer?.Stop();
        _zoomReloadTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _zoomReloadTimer.Tick += (_, _) =>
        {
            _zoomReloadTimer!.Stop();
            foreach (var item in _items)
            {
                _ = LoadThumbnailAsync(item);
            }
        };
        _zoomReloadTimer.Start();
    }


    private void RebuildGrid()
    {
        PART_Grid.Children.Clear();
        _items.Clear();
        _selection.Clear();
        _undoStack.Clear();
        _lastClickedIndex = -1;
        UpdatePreview();

        _folder = Path.GetDirectoryName(Core.Photos.CurrentFilePath) ?? "";

        foreach (var photo in Core.Photos.Items)
        {
            var item = new RenamerItem { FilePath = photo.FilePath, Photo = photo };
            _items.Add(item);

            BuildCell(item);
            PART_Grid.Children.Add(item.Cell);

            _ = LoadThumbnailAsync(item);
        }

        UpdateStatus();
    }


    private void BuildCell(RenamerItem item)
    {
        var size = Options.ThumbnailSize;

        var img = new Image { Width = size, Height = size, Stretch = Stretch.Uniform };
        var label = new TextBlock
        {
            Text = Path.GetFileNameWithoutExtension(item.FilePath),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = size,
            FontSize = 11,
            Opacity = 0.75,
        };

        var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
        stack.Children.Add(img);
        stack.Children.Add(label);

        var cell = new Border
        {
            Padding = new Thickness(4),
            Margin = new Thickness(2),
            CornerRadius = new CornerRadius(4),
            Background = NormalBrush,
            Child = stack,
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        cell.PointerPressed += (_, e) => OnCellPointerPressed(item, e);

        item.ImageControl = img;
        item.NameLabel = label;
        item.Cell = cell;
    }


    private async Task LoadThumbnailAsync(RenamerItem item)
    {
        try
        {
            var dpi = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1d;
            await item.Photo.LoadThumbnailAsync(Options.ThumbnailSize * dpi * 2, useCache: true);

            if (item.Photo.GalleryThumbnail is { } bmp)
            {
                item.ImageControl.Source = bmp;
            }
        }
        catch { /* a broken thumbnail shouldn't block the rest of the grid */ }
    }


    private void OnCellPointerPressed(RenamerItem item, PointerPressedEventArgs e)
    {
        var idx = _items.IndexOf(item);
        if (idx < 0) return;

        var props = e.GetCurrentPoint(item.Cell).Properties;
        if (props.IsRightButtonPressed)
        {
            UnstageIndex(idx);
            return;
        }
        if (!props.IsLeftButtonPressed) return;

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        if (shift && _selection.Count > 0)
        {
            var anchor = _selection[^1];
            var lo = Math.Min(idx, anchor);
            var hi = Math.Max(idx, anchor);
            if (!ctrl) _selection.Clear();
            for (var i = lo; i <= hi; i++)
            {
                if (!_selection.Contains(i)) _selection.Add(i);
            }
        }
        else if (ctrl)
        {
            if (!_selection.Remove(idx)) _selection.Add(idx);
        }
        else
        {
            var wasOnlySelected = _selection.Count == 1 && _selection[0] == idx;
            _selection.Clear();
            if (!wasOnlySelected) _selection.Add(idx);
        }

        _lastClickedIndex = idx;
        RefreshHighlight();
        UpdateStatus();
        UpdatePreview();
    }


    private void PART_ChkPreview_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        PART_PreviewPanel.IsVisible = PART_ChkPreview.IsChecked == true;
        UpdatePreview();
    }


    /// <summary>
    /// Shows the last-clicked item's thumbnail in the optional preview panel (when visible).
    /// Reuses the grid's own thumbnail bitmap rather than loading the full image again.
    /// </summary>
    private void UpdatePreview()
    {
        if (PART_ChkPreview.IsChecked != true) return;

        PART_PreviewImage.Source = (_lastClickedIndex >= 0 && _lastClickedIndex < _items.Count)
            ? _items[_lastClickedIndex].Photo.GalleryThumbnail
            : null;
    }


    private void SelectAll()
    {
        _selection.Clear();
        _selection.AddRange(Enumerable.Range(0, _items.Count));
        RefreshHighlight();
        UpdateStatus();
    }


    private void DeselectAll()
    {
        _selection.Clear();
        RefreshHighlight();
        UpdateStatus();
    }


    private void RefreshHighlight()
    {
        var selSet = new HashSet<int>(_selection);

        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var staged = item.PendingName is not null;
            var selected = selSet.Contains(i);

            item.Cell.Background = (staged, selected) switch
            {
                (true, true) => StagedSelectedBrush,
                (true, false) => StagedBrush,
                (false, true) => SelectedBrush,
                _ => NormalBrush,
            };

            item.NameLabel.Text = staged
                ? Path.GetFileNameWithoutExtension(item.PendingName!)
                : Path.GetFileNameWithoutExtension(item.FilePath);
        }
    }

    #endregion // Thumbnail grid


    #region Staging

    private void StageName(string baseName)
    {
        if (_selection.Count == 0)
        {
            SetStatus(Core.Lang[LangId.Tool_Renamer_StatusSelectFirst]);
            return;
        }

        var changed = _selection.Select(i => (i, _items[i].PendingName)).ToList();

        // always numbered, whether it's one image or several, and always continuing from
        // whatever's already staged/saved under this name — so re-clicking "char" after
        // char1/char2 are already staged produces char3, not another bare "char"
        var start = NextAvailable(baseName, _selection);
        for (var i = 0; i < _selection.Count; i++)
        {
            _items[_selection[i]].PendingName = $"{baseName}{start + i}.webp";
        }

        _undoStack.Add(changed);
        _selection.Clear();

        RefreshHighlight();
        UpdateStatus();
    }


    private void UnstageIndex(int idx)
    {
        if (idx < 0 || idx >= _items.Count) return;
        if (_items[idx].PendingName is null) return;

        _undoStack.Add([(idx, _items[idx].PendingName)]);
        _items[idx].PendingName = null;

        RefreshHighlight();
        UpdateStatus();
    }


    private void UndoStage()
    {
        if (_undoStack.Count == 0) return;

        var changed = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);

        foreach (var (idx, oldName) in changed)
        {
            _items[idx].PendingName = oldName;
        }

        RefreshHighlight();
        UpdateStatus();
    }


    /// <summary>
    /// Next free "&lt;base&gt;&lt;n&gt;" number: checks both the output folder's existing files and
    /// whatever's currently staged elsewhere in the grid, so a multi-select stage never collides.
    /// </summary>
    private int NextAvailable(string baseName, IReadOnlyCollection<int> skipIndices)
    {
        var highest = 0;
        var outDir = Path.Combine(_folder, "output");

        if (Directory.Exists(outDir))
        {
            foreach (var f in Directory.EnumerateFiles(outDir))
            {
                if (!Path.GetExtension(f).Equals(".webp", StringComparison.OrdinalIgnoreCase)) continue;

                var stem = Path.GetFileNameWithoutExtension(f);
                if (stem.StartsWith(baseName, StringComparison.Ordinal)
                    && int.TryParse(stem.AsSpan(baseName.Length), out var n))
                {
                    highest = Math.Max(highest, n);
                }
            }
        }

        var skip = new HashSet<int>(skipIndices);
        for (var i = 0; i < _items.Count; i++)
        {
            if (skip.Contains(i)) continue;
            if (_items[i].PendingName is not { } pending) continue;

            var stem = Path.GetFileNameWithoutExtension(pending);
            if (stem.StartsWith(baseName, StringComparison.Ordinal)
                && int.TryParse(stem.AsSpan(baseName.Length), out var n))
            {
                highest = Math.Max(highest, n);
            }
        }

        return highest + 1;
    }

    #endregion // Staging


    #region Saving

    private async Task SaveAllAsync()
    {
        var pending = _items.Where(i => i.PendingName is not null).ToList();
        if (pending.Count == 0) return;

        var outDir = Path.Combine(_folder, "output");
        var unrenamedCount = _items.Count - pending.Count;

        var confirm = await ModalWindow.ShowWarningAsync(null, new ModalWindowOptions
        {
            Title = Core.Lang[LangId.Tool_Renamer_ConfirmSaveTitle],
            Heading = Core.Lang[LangId.Tool_Renamer_ConfirmSaveTitle],
            Description = Core.Lang[LangId.Tool_Renamer_ConfirmSaveDescription, pending.Count, unrenamedCount],
        }, ModalWindowButton.Yes_No);
        if (confirm.ExitCode != DialogExitCode.OK) return;

        // warn once for every file that already exists in output/, instead of per-file
        var collisions = pending
            .Where(i => File.Exists(Path.Combine(outDir, i.PendingName!)))
            .ToList();
        if (collisions.Count > 0)
        {
            var overwrite = await ModalWindow.ShowWarningAsync(null, new ModalWindowOptions
            {
                Title = Core.Lang[LangId.Tool_Renamer_ConfirmOverwriteTitle],
                Heading = Core.Lang[LangId.Tool_Renamer_ConfirmOverwriteTitle],
                Description = Core.Lang[LangId.Tool_Renamer_ConfirmOverwriteDescription, collisions.Count],
            }, ModalWindowButton.Yes_No);
            if (overwrite.ExitCode != DialogExitCode.OK) return;
        }

        Directory.CreateDirectory(outDir);

        var done = 0;
        foreach (var item in pending)
        {
            var destPath = Path.Combine(outDir, item.PendingName!);
            try
            {
                using var photo = new Photo(item.FilePath);
                await photo.LoadAsync(false);
                await photo.SaveAsAsync(destPath, new PhotoTransform(), 100, Core.Config.EnablePreserveModifiedDate);
                done++;
            }
            catch (Exception ex)
            {
                SetStatus(Core.Lang[LangId.Tool_Renamer_StatusSaveError, ex.Message]);
            }
        }

        var moved = MoveUnstaged(pending);

        SetStatus(Core.Lang[LangId.Tool_Renamer_StatusSaved, done, moved > 0 ? $" ({moved} moved to unrenamed/)" : ""]);

        // renamed/moved files can leave the app's own file list (and even the open image)
        // pointing at paths that no longer exist; reload it before re-reading the folder
        if (moved > 0)
        {
            await Core.API!.RunApiAsync(API.IG_ReloadList);
        }

        RebuildGrid();
    }


    /// <summary>
    /// Moves every image that wasn't staged for renaming out of the source folder, so a repeat
    /// pass over the same folder only ever shows what's still left to tag.
    /// </summary>
    private int MoveUnstaged(List<RenamerItem> staged)
    {
        var stagedPaths = new HashSet<string>(staged.Select(i => i.FilePath), StringComparer.OrdinalIgnoreCase);
        var unstaged = _items.Where(i => !stagedPaths.Contains(i.FilePath)).ToList();
        if (unstaged.Count == 0) return 0;

        var leftoverDir = Path.Combine(_folder, "unrenamed");
        Directory.CreateDirectory(leftoverDir);

        var moved = 0;
        foreach (var item in unstaged)
        {
            var dest = Path.Combine(leftoverDir, Path.GetFileName(item.FilePath));
            if (File.Exists(dest))
            {
                var stem = Path.GetFileNameWithoutExtension(item.FilePath);
                var ext = Path.GetExtension(item.FilePath);
                var n = 1;
                while (File.Exists(dest))
                {
                    dest = Path.Combine(leftoverDir, $"{stem}_{n}{ext}");
                    n++;
                }
            }

            try
            {
                File.Move(item.FilePath, dest);
                moved++;
            }
            catch { /* best-effort; a locked file just stays where it was */ }
        }

        return moved;
    }

    #endregion // Saving


    private void UpdateStatus()
    {
        if (_selection.Count > 0)
        {
            SetStatus(Core.Lang[LangId.Tool_Renamer_StatusSelectedCount, _selection.Count, _items.Count]);
        }
        else
        {
            var staged = _items.Count(i => i.PendingName is not null);
            SetStatus(staged > 0
                ? Core.Lang[LangId.Tool_Renamer_StatusStagedCount, staged, _items.Count]
                : Core.Lang[LangId.Tool_Renamer_StatusImageCount, _items.Count]);
        }
    }


    private void SetStatus(string text)
    {
        PART_LblStatus.Text = text;
    }

    #endregion // Control Methods

}
