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
using Avalonia.Layout;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Types;
using ImageGlass.SDK.Plugins;
using ImageGlass.UI;
using ImageGlass.UI.Windowing;
using System;
using System.Collections.Generic;

namespace ImageGlass.Common.Windows;

/// <summary>
/// The action a <see cref="PluginInfoWindow"/> offers for a plugin, decided from its trust state.
/// </summary>
internal enum PluginInfoWindowMode
{
    /// <summary>
    /// Read-only metadata view ([OK]).
    /// </summary>
    View,

    /// <summary>
    /// Trust-and-enable consent prompt ([Trust and Enable] / [Cancel]).
    /// </summary>
    Enable,

    /// <summary>
    /// Format picker for an already-trusted plugin ([Save] / [Cancel]).
    /// </summary>
    Configure,
}


/// <summary>
/// Modal window that displays a native plugin's manifest metadata and, depending on
/// <see cref="PluginInfoWindowMode"/>, offers to enable it or edit its formats.
/// </summary>
internal sealed class PluginInfoWindow : DialogWindow
{
    private readonly PluginInfoWindowView _view;
    private readonly PluginInfoWindowMode _mode;
    private readonly PhButton _deleteButton;
    private readonly string _pluginName;


    // fixed dialog width so it doesn't grow/shrink with the metadata text
    protected override int MIN_WIDTH => 500;
    protected override int MAX_WIDTH => 500;
    protected override Thickness ContentPadding => new(0);


    /// <summary>
    /// Whether the user clicked the footer "Delete" link (the caller runs the delete flow).
    /// </summary>
    public bool DeleteRequested { get; private set; }


    /// <summary>
    /// Extensions the user switched off for decoding.
    /// </summary>
    public IReadOnlyCollection<string> DisabledDecodeExtensions => _view.DisabledDecodeExtensions;


    /// <summary>
    /// Extensions the user switched off for encoding.
    /// </summary>
    public IReadOnlyCollection<string> DisabledEncodeExtensions => _view.DisabledEncodeExtensions;


    /// <summary>
    /// Whether the format choices differ from what the window opened with.
    /// </summary>
    public bool ChoicesChanged => _view.ChoicesChanged;


    /// <summary>
    /// Opens the window on <paramref name="manifest"/> (folder <paramref name="pluginDir"/>), titled
    /// with the plugin's name. <paramref name="mode"/> picks the footer and whether the format picker
    /// is editable: <see cref="PluginInfoWindowMode.Enable"/> is the consent prompt
    /// (<paramref name="hashChanged"/> adds a stronger warning),
    /// <see cref="PluginInfoWindowMode.Configure"/> allows edits, and
    /// <see cref="PluginInfoWindowMode.View"/> is read-only.
    /// </summary>
    public PluginInfoWindow(PluginManifest manifest, string pluginDir,
        PluginInfoWindowMode mode = PluginInfoWindowMode.View, bool hashChanged = false)
    {
        _mode = mode;
        _pluginName = string.IsNullOrWhiteSpace(manifest.Name) ? manifest.Id : manifest.Name;

        if (mode is PluginInfoWindowMode.Enable or PluginInfoWindowMode.Configure)
        {
            IsButton1Visible = true;
            IsButton2Visible = true;
            IsButton3Visible = false;

            // Enable is a consent prompt, so Cancel stays the accented default; once the plugin is
            // trusted this is just a format editor, where Save is the expected action.
            var cta = mode == PluginInfoWindowMode.Configure ? DialogButton.Button1 : DialogButton.Button2;
            DefaultButton = cta;
            DefaultFocus = cta == DialogButton.Button1 ? DialogFocus.Button1 : DialogFocus.Button2;
        }
        else
        {
            IsButton1Visible = true;
            IsButton2Visible = false;
            IsButton3Visible = false;
            DefaultButton = DialogButton.Button1;
            DefaultFocus = DialogFocus.Button1;
        }

        _view = new PluginInfoWindowView();
        _view.LoadData(manifest, pluginDir,
            allowEdit: mode == PluginInfoWindowMode.Configure,
            // file formats are a codec concept; a future non-codec kind gets the info tab only
            showFormats: manifest.Kind == IGPluginKind.Codec);
        if (mode == PluginInfoWindowMode.Enable)
        {
            // the badge glyph is an outline, so it has to be stroked instead of filled
            Button1Icon = Resx.GetIcon(ResxIconId.IconVerify);
            _btn1.IconStrokeThickness = 1.5;
            _view.ShowConsentWarning(manifest, hashChanged);
        }
        DialogContent = _view;

        // footer-left link; closes the window signalling the caller to run the delete flow
        _deleteButton = NewFooterLink(() => DeleteRequested = true);
        DialogFooterLeftContent = _deleteButton;
    }


    /// <summary>
    /// Creates a footer link that flags the requested action and closes the window.
    /// </summary>
    private PhButton NewFooterLink(Action flag)
    {
        var button = new PhButton
        {
            Variant = PhButtonVariant.Link,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        button.Click += (_, _) =>
        {
            flag();
            OnDialogCancelled(new DialogEventArgs(DialogAction.Cancel));
        };
        return button;
    }


    protected override void OnIgLanguageChanged()
    {
        base.OnIgLanguageChanged();

        // the plugin's own name titles the window; the enable prompt's heading lives in the banner,
        // and only the footer buttons differ per mode.
        Title = _pluginName;
        _deleteButton.Text = Core.Lang[LangId._Delete];

        switch (_mode)
        {
            case PluginInfoWindowMode.Enable:
                Button1Text = Core.Lang[LangId.Settings_Plugins_TrustAndEnable];
                Button2Text = Core.Lang[LangId._Cancel];
                break;

            case PluginInfoWindowMode.Configure:
                Button1Text = Core.Lang[LangId._Save];
                Button2Text = Core.Lang[LangId._Cancel];
                break;

            default:
                Button1Text = Core.Lang[LangId._OK];
                break;
        }
    }

}
