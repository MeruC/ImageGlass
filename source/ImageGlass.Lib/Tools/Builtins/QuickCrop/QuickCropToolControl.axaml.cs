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
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ImageGlass.Common;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Photoing;
using ImageGlass.UI;
using ImageGlass.UI.Viewer;
using ImageGlass.Windows;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageGlass.Tools;

/// <summary>
/// Quick Crop: a rapid-fire "select an area, save it as a new file, repeat" tool for cropping the
/// same fixed area out of many images in a row (e.g. batch-cropping screenshots). Unlike the
/// regular Crop tool, it never overwrites or prompts — every save writes a new, uniquely named
/// file next to (or in a chosen folder near) the source images.
/// </summary>
public partial class QuickCropToolControl : PhControl, IToolControl
{
    private static readonly string[] AllowedExts = [".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tiff"];

    // guards against SelectionChanged re-entrancy when we override the rect ourselves
    private bool _isApplyingConstraint;

    // guards against feedback when PART_NumAxisValue and the selection update each other
    private bool _isUpdatingAxisValue;

    // remembers where an axis-locked drag started, so the fixed axis doesn't drift mid-drag
    private double? _axisAnchorX;
    private double? _axisAnchorY;

    // the last non-empty selection, restored (clamped to the new image) when the photo changes,
    // so a locked size and/or axis position survive browsing to the next image
    private Rect _lastSelection;

    // the window Root_KeyDown is attached to, so it can be detached on unload
    private TopLevel? _topLevel;


    public static string TOOL_ID => "Tool_QuickCrop";
    public string ToolId => TOOL_ID;
    public bool HasSettingsUI => false;
    public object? Settings { get; private set; } = new QuickCropConfig();
    public QuickCropConfig Options => (QuickCropConfig)Settings!;
    public ViewerControl Viewer { get; set; } = null!;


    public QuickCropToolControl()
    {
        InitializeComponent();
    }



    #region Control Events

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        Viewer.EnableSelection = true;
        Viewer.SelectionAspectRatio = new Size();
        Viewer.SelectionChanged += Viewer_SelectionChanged;
        Viewer.PhotoLoading += Viewer_PhotoLoading;

        PART_BtnLockSize.Click += PART_BtnLockSize_Click;
        PART_BtnAxisHorizontal.Click += PART_BtnAxisHorizontal_Click;
        PART_BtnAxisVertical.Click += PART_BtnAxisVertical_Click;
        PART_BtnBrowse.Click += PART_BtnBrowse_Click;
        PART_BtnSave.Click += PART_BtnSave_Click;
        PART_NumAxisValue.ValueChanged += PART_NumAxisValue_ValueChanged;

        // tunnel on the window, not just this control's subtree: focus is usually on the Viewer
        // (or nowhere in particular) while cropping, not inside this panel
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, Root_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);

        // restore the locked-axis position from last session, before anything reads the anchors
        if (Options.LockedAxisPosition >= 0)
        {
            if (Options.AxisLock == QuickCropAxisLock.Vertical) _axisAnchorX = Options.LockedAxisPosition;
            else if (Options.AxisLock == QuickCropAxisLock.Horizontal) _axisAnchorY = Options.LockedAxisPosition;
        }

        PART_TxtSaveFolder.Text = ResolveSaveFolder();
        UpdateLockUI();
        UpdateAxisUI();
        SyncAxisValueInput();

        // show the remembered crop box immediately, instead of waiting for the user to interact
        ApplyRememberedSelection();

        SetStatus(Core.Lang[LangId.Tool_QuickCrop_StatusHint]);
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        Viewer.SelectionChanged -= Viewer_SelectionChanged;
        Viewer.PhotoLoading -= Viewer_PhotoLoading;

        PART_BtnLockSize.Click -= PART_BtnLockSize_Click;
        PART_BtnAxisHorizontal.Click -= PART_BtnAxisHorizontal_Click;
        PART_BtnAxisVertical.Click -= PART_BtnAxisVertical_Click;
        PART_BtnBrowse.Click -= PART_BtnBrowse_Click;
        PART_BtnSave.Click -= PART_BtnSave_Click;
        PART_NumAxisValue.ValueChanged -= PART_NumAxisValue_ValueChanged;

        _topLevel?.RemoveHandler(KeyDownEvent, Root_KeyDown);
        _topLevel = null;

        Viewer.SourceSelection = default;
        Viewer.EnableSelection = false;
        Viewer.EnableSelectionResize = true; // restore the default for whichever tool opens next

        base.OnUnloaded(e);
    }


    protected override void OnIgLanguageChanged()
    {
        base.OnIgLanguageChanged();

        PART_BtnAxisHorizontal.Text = "↔"; // left-right arrow
        PART_BtnAxisVertical.Text = "↕";   // up-down arrow
        ToolTip.SetTip(PART_BtnAxisHorizontal, Core.Lang[LangId.Tool_QuickCrop_BtnAxisHorizontal]);
        ToolTip.SetTip(PART_BtnAxisVertical, Core.Lang[LangId.Tool_QuickCrop_BtnAxisVertical]);

        PART_BtnBrowse.Text = Core.Lang[LangId.Tool_QuickCrop_BtnBrowse];
        PART_BtnSave.Text = Core.Lang[LangId.Tool_QuickCrop_BtnSave];

        UpdateLockUI();
    }


    /// <summary>
    /// Enter saves a crop while this tool is open, unless a text box in the panel has focus
    /// (e.g. while editing the save folder path), matching the rest of the app's Enter-to-submit.
    /// </summary>
    private void Root_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        if (focused is TextBox) return;

        e.Handled = true;
        _ = SaveCropAsync();
    }


    private void Viewer_SelectionChanged(ViewerControl sender, ViewerSelectionChangedEventArgs e)
    {
        if (_isApplyingConstraint) return;

        var rect = e.SourceSelection;
        var constrained = rect;

        // size lock: keep the box at the locked size, centered on wherever the user just dragged to
        if (Options.LockedWidth > 0 && Options.LockedHeight > 0)
        {
            constrained = CenterFixedSize(constrained, Options.LockedWidth, Options.LockedHeight);
        }

        // axis lock: the locked coordinate always stays pinned to the anchor, no matter whether
        // this change came from moving the existing box or drawing/resizing a fresh one (clicking
        // outside the current selection starts a new draw, which must not bypass the lock)
        if (Options.AxisLock == QuickCropAxisLock.Horizontal)
        {
            if (_axisAnchorY is double ay)
            {
                constrained = constrained.WithY(ay);
            }
            else
            {
                SetAxisAnchor(_axisAnchorX, constrained.Y);
            }
        }
        else if (Options.AxisLock == QuickCropAxisLock.Vertical)
        {
            if (_axisAnchorX is double ax)
            {
                constrained = constrained.WithX(ax);
            }
            else
            {
                SetAxisAnchor(constrained.X, _axisAnchorY);
            }
        }

        if (constrained != rect)
        {
            _isApplyingConstraint = true;
            Viewer.SetSourceSelection(constrained, false);
            _isApplyingConstraint = false;
        }

        if (!IsEmptySelection(constrained))
        {
            _lastSelection = constrained;
        }

        UpdateSelectionStatus();
        SyncAxisValueInput();
    }


    /// <summary>
    /// Restores the last selection (size and, for a locked axis, position) on the new photo, so
    /// browsing to the next image doesn't lose a locked size or a locked axis's position.
    /// </summary>
    private void Viewer_PhotoLoading(ViewerControl sender, PhotoLoadingEventArgs e)
    {
        if (e.State != PhotoState.Loaded) return;

        ApplyRememberedSelection();
    }


    /// <summary>
    /// Shows a selection built from what's remembered, without requiring the user to draw first:
    /// the locked size (if any) or the last-drawn size, positioned at the locked axis's saved
    /// coordinate (if any) or the last-drawn position, centered as a last resort. Used both right
    /// after the tool opens and whenever the photo changes.
    /// </summary>
    private void ApplyRememberedSelection()
    {
        var srcW = Viewer.BitmapSize.Width;
        var srcH = Viewer.BitmapSize.Height;
        if (srcW <= 0 || srcH <= 0) return;

        double w, h;
        if (Options.LockedWidth > 0 && Options.LockedHeight > 0)
        {
            w = Options.LockedWidth;
            h = Options.LockedHeight;
        }
        else if (!IsEmptySelection(_lastSelection))
        {
            w = _lastSelection.Width;
            h = _lastSelection.Height;
        }
        else
        {
            return; // nothing remembered yet to show
        }

        w = Math.Min(w, srcW);
        h = Math.Min(h, srcH);

        var hasLastSelection = !IsEmptySelection(_lastSelection);
        var x = _axisAnchorX ?? (hasLastSelection ? _lastSelection.X : (srcW - w) / 2.0);
        var y = _axisAnchorY ?? (hasLastSelection ? _lastSelection.Y : (srcH - h) / 2.0);

        x = Math.Clamp(x, 0, Math.Max(0, srcW - w));
        y = Math.Clamp(y, 0, Math.Max(0, srcH - h));

        var rect = new Rect(x, y, w, h);

        _isApplyingConstraint = true;
        Viewer.SetSourceSelection(rect, false);
        _isApplyingConstraint = false;

        _lastSelection = rect;
        UpdateSelectionStatus();
        SyncAxisValueInput();
    }


    private void PART_BtnLockSize_Click(object? sender, RoutedEventArgs e)
    {
        if (Options.LockedWidth > 0 && Options.LockedHeight > 0)
        {
            Options.LockedWidth = 0;
            Options.LockedHeight = 0;
        }
        else
        {
            if (IsEmptySelection(Viewer.SourceSelection))
            {
                SetStatus(Core.Lang[LangId.Tool_QuickCrop_StatusLockFirst]);
                return;
            }

            Options.LockedWidth = (int)Viewer.SourceSelection.Width;
            Options.LockedHeight = (int)Viewer.SourceSelection.Height;
        }

        UpdateLockUI();
    }


    private void PART_BtnAxisHorizontal_Click(object? sender, RoutedEventArgs e) => ToggleAxis(QuickCropAxisLock.Horizontal);
    private void PART_BtnAxisVertical_Click(object? sender, RoutedEventArgs e) => ToggleAxis(QuickCropAxisLock.Vertical);


    private void ToggleAxis(QuickCropAxisLock axis)
    {
        Options.AxisLock = Options.AxisLock == axis ? QuickCropAxisLock.None : axis;

        if (Options.AxisLock != QuickCropAxisLock.None && !IsEmptySelection(Viewer.SourceSelection))
        {
            SetAxisAnchor(Viewer.SourceSelection.X, Viewer.SourceSelection.Y);
        }
        else
        {
            // no live selection to capture from right now; keep whatever anchor is already
            // in memory (e.g. restored from Options.LockedAxisPosition on open) and persist it
            PersistAxisAnchor();
        }

        UpdateAxisUI();
        SyncAxisValueInput();
    }


    /// <summary>
    /// Sets the in-memory anchor and persists whichever coordinate <see cref="QuickCropConfig.AxisLock"/>
    /// currently pins, so it survives closing and reopening the tool.
    /// </summary>
    private void SetAxisAnchor(double? x, double? y)
    {
        _axisAnchorX = x;
        _axisAnchorY = y;
        PersistAxisAnchor();
    }


    private void PersistAxisAnchor()
    {
        var value = Options.AxisLock switch
        {
            QuickCropAxisLock.Vertical => _axisAnchorX,
            QuickCropAxisLock.Horizontal => _axisAnchorY,
            _ => (double?)null,
        };

        Options.LockedAxisPosition = value is double v ? (int)Math.Round(v) : -1;
    }


    /// <summary>
    /// Typing an exact value for the locked axis: moves the current selection there immediately
    /// and updates the anchor so the next drag holds at the new value instead of the old one.
    /// </summary>
    private void PART_NumAxisValue_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_isUpdatingAxisValue) return;
        if (Options.AxisLock == QuickCropAxisLock.None) return;

        var value = (double)(PART_NumAxisValue.Value ?? 0);

        if (Options.AxisLock == QuickCropAxisLock.Vertical)
        {
            SetAxisAnchor(value, _axisAnchorY);
        }
        else
        {
            SetAxisAnchor(_axisAnchorX, value);
        }

        if (IsEmptySelection(Viewer.SourceSelection)) return;

        var srcW = Viewer.BitmapSize.Width;
        var srcH = Viewer.BitmapSize.Height;
        var rect = Viewer.SourceSelection;

        var newRect = Options.AxisLock == QuickCropAxisLock.Vertical
            ? rect.WithX(Math.Clamp(value, 0, Math.Max(0, srcW - rect.Width)))
            : rect.WithY(Math.Clamp(value, 0, Math.Max(0, srcH - rect.Height)));

        _isApplyingConstraint = true;
        Viewer.SetSourceSelection(newRect, false);
        _isApplyingConstraint = false;

        _lastSelection = newRect;
        UpdateSelectionStatus();
    }


    /// <summary>
    /// Shows/hides and fills the locked-axis coordinate input from the current anchor.
    /// </summary>
    private void SyncAxisValueInput()
    {
        _isUpdatingAxisValue = true;

        if (Options.AxisLock == QuickCropAxisLock.Vertical)
        {
            PART_PanelAxisValue.IsVisible = true;
            PART_LblAxisValue.Text = "X:";
            PART_NumAxisValue.Value = (decimal)(_axisAnchorX ?? Viewer.SourceSelection.X);
        }
        else if (Options.AxisLock == QuickCropAxisLock.Horizontal)
        {
            PART_PanelAxisValue.IsVisible = true;
            PART_LblAxisValue.Text = "Y:";
            PART_NumAxisValue.Value = (decimal)(_axisAnchorY ?? Viewer.SourceSelection.Y);
        }
        else
        {
            PART_PanelAxisValue.IsVisible = false;
        }

        _isUpdatingAxisValue = false;
    }


    private async void PART_BtnBrowse_Click(object? sender, RoutedEventArgs e)
    {
        var dirs = await App.MainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
        var dir = dirs?.ElementAtOrDefault(0)?.TryGetLocalPath();
        if (string.IsNullOrEmpty(dir)) return;

        Options.SaveFolder = dir;
        PART_TxtSaveFolder.Text = dir;
    }


    private async void PART_BtnSave_Click(object? sender, RoutedEventArgs e) => await SaveCropAsync();

    #endregion // Control Events



    #region Control Methods

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void LoadSettings(JsonElement? jsonEl)
    {
        var settings = jsonEl?.Deserialize(QuickCropConfigJsonContext.Default.QuickCropConfig);
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
        return JsonSerializer.SerializeToElement(Options, QuickCropConfigJsonContext.Default.QuickCropConfig);
    }


    /// <summary>
    /// Returns a rect of exactly <paramref name="w"/>x<paramref name="h"/>, centered on
    /// <paramref name="rect"/>'s center and clamped within the image bounds.
    /// </summary>
    private Rect CenterFixedSize(Rect rect, int w, int h)
    {
        var srcW = Viewer.BitmapSize.Width;
        var srcH = Viewer.BitmapSize.Height;

        var cx = rect.X + rect.Width / 2;
        var cy = rect.Y + rect.Height / 2;

        var clampedW = Math.Min(w, srcW);
        var clampedH = Math.Min(h, srcH);

        var x = Math.Clamp(cx - clampedW / 2.0, 0, Math.Max(0, srcW - clampedW));
        var y = Math.Clamp(cy - clampedH / 2.0, 0, Math.Max(0, srcH - clampedH));

        return new Rect(x, y, clampedW, clampedH);
    }


    private void UpdateLockUI()
    {
        var locked = Options.LockedWidth > 0 && Options.LockedHeight > 0;

        PART_BtnLockSize.Variant = locked ? PhButtonVariant.Accent : PhButtonVariant.Default;
        PART_BtnLockSize.Text = locked
            ? $"{Options.LockedWidth}×{Options.LockedHeight}"
            : Core.Lang[LangId.Tool_QuickCrop_BtnLockSize];

        // a locked size only snaps back after a resize anyway, so hide the handles that invite one
        Viewer.EnableSelectionResize = !locked;
        Viewer.Refresh(false);
    }


    private void UpdateAxisUI()
    {
        PART_BtnAxisHorizontal.Variant = Options.AxisLock == QuickCropAxisLock.Horizontal
            ? PhButtonVariant.Accent : PhButtonVariant.Default;
        PART_BtnAxisVertical.Variant = Options.AxisLock == QuickCropAxisLock.Vertical
            ? PhButtonVariant.Accent : PhButtonVariant.Default;
    }


    private static bool IsEmptySelection(Rect r) => r.Width <= 0 || r.Height <= 0;


    private void UpdateSelectionStatus()
    {
        if (IsEmptySelection(Viewer.SourceSelection)) return;

        var w = (int)Viewer.SourceSelection.Width;
        var h = (int)Viewer.SourceSelection.Height;
        SetStatus($"{w}×{h} px");
    }


    private void SetStatus(string text)
    {
        PART_LblStatus.Text = text;
    }


    private string ResolveSaveFolder()
    {
        return string.IsNullOrWhiteSpace(Options.SaveFolder) ? DefaultSaveFolder : Options.SaveFolder;
    }


    private static string DefaultSaveFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "crops");


    /// <summary>
    /// Crops the current selection out of the full-resolution source image and saves it as a new,
    /// uniquely named file in the save folder. Never overwrites, so there's nothing to confirm.
    /// </summary>
    private async Task SaveCropAsync()
    {
        if (IsEmptySelection(Viewer.SourceSelection))
        {
            SetStatus(Core.Lang[LangId.Tool_QuickCrop_StatusNoSelection]);
            return;
        }

        var srcFilePath = Core.Photos.CurrentFilePath;
        if (string.IsNullOrEmpty(srcFilePath))
        {
            SetStatus(Core.Lang[LangId.Tool_QuickCrop_StatusNoImage]);
            return;
        }

        var saveDir = ResolveSaveFolder();
        try
        {
            Directory.CreateDirectory(saveDir);
        }
        catch
        {
            SetStatus(Core.Lang[LangId.Tool_QuickCrop_StatusFolderError]);
            return;
        }

        var baseName = Path.GetFileNameWithoutExtension(srcFilePath);
        var srcExt = Path.GetExtension(srcFilePath).ToLowerInvariant();
        var ext = Array.IndexOf(AllowedExts, srcExt) >= 0 ? srcExt : ".png";

        // never overwrites: keeps counting up until an unused name is found
        string destPath;
        var n = 1;
        do
        {
            destPath = Path.Combine(saveDir, $"{baseName}_crop_{n}{ext}");
            n++;
        } while (File.Exists(destPath));

        using var bitmap = Viewer.GetRenderedBitmap(true);
        if (bitmap is null)
        {
            SetStatus(Core.Lang[LangId.Tool_QuickCrop_StatusSaveError, "no image data"]);
            return;
        }

        try
        {
            using var photo = new Photo(bitmap);
            await photo.SaveAsAsync(destPath, new PhotoTransform(),
                Core.Config.ImageEditQuality, Core.Config.EnablePreserveModifiedDate);

            SetStatus(Core.Lang[LangId.Tool_QuickCrop_StatusSaved,
                Path.GetFileName(destPath), (int)Viewer.SourceSelection.Width, (int)Viewer.SourceSelection.Height]);
        }
        catch (Exception ex)
        {
            SetStatus(Core.Lang[LangId.Tool_QuickCrop_StatusSaveError, ex.Message]);
        }
    }

    #endregion // Control Methods

}
