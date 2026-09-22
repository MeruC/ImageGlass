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
namespace ImageGlass.Common.Localization;

public enum LangId
{
    #region General
    _OK,
    _Cancel,
    _Apply,
    _Close,
    _Yes,
    _No,
    _LearnMore,
    _Continue,
    _Quit,
    _Back,
    _Next,
    _Save,
    _Error,
    _Warning,
    _Copy,
    _Browse,
    _Reset,
    _ResetToDefault,
    _CheckForUpdate,
    _Download,
    _Update,
    _Website,
    _Email,
    _Install,
    _Refresh,
    _Delete,
    _Add,
    _Edit,
    _ID,
    _Name,
    _Hotkeys,
    _AddHotkey,
    _Executable,
    _Argument,
    _CommandPreview,
    _FileExtension,
    _Empty,
    _MoveUp,
    _MoveDown,
    _Separator,
    _Icon,
    _Description,
    _GetHelp,
    _Start,

    _UnhandledException,
    _UnhandledException_Description,
    _DoNotShowThisMessageAgain,
    _CreatingFile,
    _CreatingFileError,
    _NotSupported,

    _InvalidAction,
    _InvalidAction_Transformation,

    _UserAction_MenuNotFound,
    _UserAction_MethodNotFound,
    _UserAction_MethodArgumentNotSupported,
    _UserAction_Win32ExeError,

    // Gallery tooltip
    _Metadata_FileSize,
    _Metadata_FileCreationTime,
    _Metadata_FileLastAccessTime,
    _Metadata_FileLastWriteTime,
    _Metadata_FrameCount,
    _Metadata_ExifRatingPercent,
    _Metadata_ColorSpace,
    _Metadata_ColorProfile,
    _Metadata_ExifDateTime,
    _Metadata_ExifDateTimeOriginal,

    // image info
    _ImageInfo_ListCount,
    _ImageInfo_FrameCount,

    // layout position
    _Position_Left,
    _Position_Right,
    _Position_Top,
    _Position_Bottom,

    _Validation_Required,
    _Validation_RegexPattern,
    _Validation_IntValueOnly,
    _Validation_UnsignedIntValueOnly,
    _Validation_FloatValueOnly,
    _Validation_UnsignedFloatValueOnly,
    _Validation_FileNameValueOnly,

    #endregion // General


    #region Enums

    ImageOrderBy_Name,
    ImageOrderBy_Random,
    ImageOrderBy_FileSize,
    ImageOrderBy_Extension,
    ImageOrderBy_DateCreated,
    ImageOrderBy_DateAccessed,
    ImageOrderBy_DateModified,
    ImageOrderBy_ExifDateTaken,
    ImageOrderBy_ExifRating,

    ImageOrderType_Asc,
    ImageOrderType_Desc,

    AfterEditAppAction_Nothing,
    AfterEditAppAction_Minimize,
    AfterEditAppAction_Close,

    ColorProfileOption_None,
    ColorProfileOption_CurrentMonitorProfile,
    ColorProfileOption_Custom,

    BackdropStyle_None,

    MouseWheelEvent_Scroll,
    MouseWheelEvent_CtrlAndScroll,
    MouseWheelEvent_ShiftAndScroll,
    MouseWheelEvent_AltAndScroll,

    MouseWheelAction_DoNothing,
    MouseWheelAction_Zoom,
    MouseWheelAction_PanVertically,
    MouseWheelAction_PanHorizontally,
    MouseWheelAction_BrowseImages,

    ImageInterpolation_NearestNeighbor,
    ImageInterpolation_Linear,
    ImageInterpolation_Cubic,
    ImageInterpolation_MultiSampleLinear,
    ImageInterpolation_Antisotropic,
    ImageInterpolation_HighQualityBicubic,

    #endregion // Enums


    #region Main Window

    #region Main Window > General
    FrmMain_MnuMain,
    FrmMain_MnuToolbarOverflow,
    FrmMain_PicMain_ErrorText,

    FrmMain_OpenFileDialog,
    FrmMain_Loading,
    FrmMain_OpenWith,
    FrmMain_ReachedFirstImage,
    FrmMain_ReachedLastImage,
    FrmMain_ClipboardImage,
    FrmMain_FolderAccessPrompt,
    #endregion // Main Window > General


    #region Main Window > Main Menu

    #region Main Menu > File
    FrmMain_MnuFile,
    FrmMain_MnuOpenFile,
    FrmMain_MnuNewWindow,
    FrmMain_MnuNewWindow_Error,

    FrmMain_MnuSave,
    FrmMain_MnuSave_Confirm,
    FrmMain_MnuSave_ConfirmDescription,
    FrmMain_MnuSave_Saving,
    FrmMain_MnuSave_Success,
    FrmMain_MnuSave_Error,
    FrmMain_MnuSaveAs,
    FrmMain_MnuExportFrames,

    FrmMain_MnuOpenWith,
    FrmMain_MnuEdit,
    FrmMain_MnuEdit_AppNotFound,
    FrmMain_MnuPrint,
    FrmMain_MnuPrint_Error,
    FrmMain_MnuShare,
    FrmMain_MnuShare_Error,
    FrmMain_MnuOpenLocation,

    FrmMain_MnuRename,
    FrmMain_MnuRename_Description,
    FrmMain_MnuMoveToRecycleBin,
    FrmMain_MnuMoveToRecycleBin_Description,
    FrmMain_MnuDeleteFromHardDisk,
    FrmMain_MnuDeleteFromHardDisk_Description,

    #endregion // Main Menu > File


    #region Main Menu > Navigation
    FrmMain_MnuNavigation,
    FrmMain_MnuViewNext,
    FrmMain_MnuViewPrevious,

    FrmMain_MnuGoTo,
    FrmMain_MnuGoTo_Description,
    FrmMain_MnuGoToFirst,
    FrmMain_MnuGoToLast,

    FrmMain_MnuViewNextFrame,
    FrmMain_MnuViewPreviousFrame,
    FrmMain_MnuViewFirstFrame,
    FrmMain_MnuViewLastFrame,
    #endregion // Main Menu > Navigation


    #region Main Menu > Zoom
    FrmMain_MnuZoom,
    FrmMain_MnuZoomIn,
    FrmMain_MnuZoomOut,
    FrmMain_MnuCustomZoom,
    FrmMain_MnuCustomZoom_Description,
    FrmMain_MnuScaleToFit,
    FrmMain_MnuScaleToFill,
    FrmMain_MnuActualSize,
    FrmMain_MnuLockZoom,
    FrmMain_MnuAutoZoom,
    FrmMain_MnuScaleToWidth,
    FrmMain_MnuScaleToHeight,
    #endregion // Main Menu > Zoom


    #region Main Menu > Panning
    FrmMain_MnuPanning,

    FrmMain_MnuPanLeft,
    FrmMain_MnuPanRight,
    FrmMain_MnuPanUp,
    FrmMain_MnuPanDown,

    FrmMain_MnuPanToLeftSide,
    FrmMain_MnuPanToRightSide,
    FrmMain_MnuPanToTop,
    FrmMain_MnuPanToBottom,
    #endregion // Main Menu > Panning


    #region Main Menu > Image
    FrmMain_MnuImage,

    FrmMain_MnuRefresh,
    FrmMain_MnuReload,
    FrmMain_MnuReloadImageList,
    FrmMain_MnuUnload,

    FrmMain_MnuViewChannels,
    FrmMain_MnuLoadingOrders,

    FrmMain_MnuInvertColors,
    FrmMain_MnuToggleImageAnimation,

    FrmMain_MnuRotateLeft,
    FrmMain_MnuRotateRight,
    FrmMain_MnuFlipHorizontal,
    FrmMain_MnuFlipVertical,

    FrmMain_MnuSetDesktopBackground,
    FrmMain_MnuSetDesktopBackground_Error,
    FrmMain_MnuSetDesktopBackground_Success,
    FrmMain_MnuSetLockScreen,
    FrmMain_MnuSetLockScreen_Error,
    FrmMain_MnuSetLockScreen_Success,

    FrmMain_MnuImageProperties,
    #endregion // Main Menu > Image


    #region Main Menu > Clipboard
    FrmMain_MnuClipboard,
    FrmMain_MnuCopyFile,
    FrmMain_MnuCopyFile_Success,
    FrmMain_MnuCopyImagePixels,
    FrmMain_MnuCopyImagePixels_Copying,
    FrmMain_MnuCopyImagePixels_Success,
    FrmMain_MnuCutFile,
    FrmMain_MnuCutFile_Success,
    FrmMain_MnuCopyPath,
    FrmMain_MnuCopyPath_Success,
    FrmMain_MnuPasteImage,
    FrmMain_MnuPasteImage_Error,
    FrmMain_MnuClearClipboard,
    FrmMain_MnuClearClipboard_Success,
    #endregion // Main Menu > Clipboard


    FrmMain_MnuWindowFit,
    FrmMain_MnuFullScreen,
    FrmMain_MnuFrameless,
    FrmMain_MnuFrameless_EnableDescription,
    FrmMain_MnuSlideshow,


    #region Main Menu > Layout
    FrmMain_MnuLayout,
    FrmMain_MnuToggleToolbar,
    FrmMain_MnuToggleGallery,
    FrmMain_MnuToggleCheckerboard,
    FrmMain_MnuToggleTopMost,
    FrmMain_MnuToggleTopMost_Enable,
    FrmMain_MnuToggleTopMost_Disable,
    FrmMain_MnuChangeBackgroundColor,
    #endregion // Main Menu > Layout

    #region Main Menu > Tools
    FrmMain_MnuTools,
    FrmMain_MnuColorPicker,
    FrmMain_MnuCropTool,
    FrmMain_MnuResizeTool,
    FrmMain_MnuFrameNav,
    FrmMain_MnuGetMoreTools,

    FrmMain_MnuLosslessCompression,
    FrmMain_MnuLosslessCompression_Confirm,
    FrmMain_MnuLosslessCompression_Description,
    FrmMain_MnuLosslessCompression_Compressing,
    FrmMain_MnuLosslessCompression_Done,
    #endregion // Main Menu > Tools

    FrmMain_MnuSettings,

    #region Main Menu > Help
    FrmMain_MnuHelp,
    FrmMain_MnuAbout,
    FrmMain_MnuQuickSetup,
    FrmMain_MnuReportIssue,
    FrmMain_MnuCheckForUpdate_NewVersion,
    FrmMain_MnuCheckForUpdate_NoUpdate,
    FrmMain_MnuCheckForUpdate_Checking,
    FrmMain_MnuCheckForUpdate_Failed,
    FrmMain_MnuCheckForUpdate_SkipVersion,
    FrmMain_MnuCheckForUpdate_CurrentVersion,
    FrmMain_MnuCheckForUpdate_LatestVersion,
    FrmMain_MnuCheckForUpdate_PublishedDate,

    FrmMain_MnuSetDefaultPhotoViewer,
    FrmMain_MnuSetDefaultPhotoViewer_Success,
    FrmMain_MnuSetDefaultPhotoViewer_Error,

    FrmMain_MnuRemoveDefaultPhotoViewer,
    FrmMain_MnuRemoveDefaultPhotoViewer_Success,
    FrmMain_MnuRemoveDefaultPhotoViewer_Error,
    #endregion // Main Menu > Help

    FrmMain_MnuExit,


    #endregion // Main Window > Main Menu

    #endregion // Main Window


    #region FrmAbout
    FrmAbout_Slogan,
    FrmAbout_Version,
    FrmAbout_License,
    FrmAbout_Privacy,
    FrmAbout_Thanks,
    FrmAbout_LogoDesigner,
    FrmAbout_Collaborator,
    FrmAbout_Contact,
    FrmAbout_Homepage,
    FrmAbout_Email,
    FrmAbout_Credits,
    FrmAbout_Donate,
    #endregion // FrmAbout


    #region FrmSettings

    FrmSettings_ResetSettings,
    FrmSettings_UnmanagedSettingReminder,

    #region FrmSettings > Navbar
    FrmSettings_Nav_General,
    FrmSettings_Nav_Image,
    FrmSettings_Nav_Slideshow,
    FrmSettings_Nav_Edit,
    FrmSettings_Nav_Viewer,
    FrmSettings_Nav_Toolbar,
    FrmSettings_Nav_Gallery,
    FrmSettings_Nav_Layout,
    FrmSettings_Nav_Mouse,
    FrmSettings_Nav_Keyboard,
    FrmSettings_Nav_FileTypeAssociations,
    FrmSettings_Nav_Tools,
    FrmSettings_Nav_Language,
    FrmSettings_Nav_Appearance,
    #endregion // FrmSettings > Navbar


    #region FrmSettings > Tab General
    // General > General
    FrmSettings_StartupDir,
    FrmSettings_ConfigDir,
    FrmSettings_UserConfigFile,

    // General > Startup
    FrmSettings_Startup,
    FrmSettings_EnableWelcomeImage,
    FrmSettings_EnableLastSeenImage,

    FrmSettings_StartupBoost,
    FrmSettings_StartupBoost_Description,
    FrmSettings_StartupBoost_Enabled,
    FrmSettings_StartupBoost_Disabled,
    FrmSettings_StartupBoost_Error,
    FrmSettings_EnableStartupBoost,
    FrmSettings_DisableStartupBoost,
    FrmSettings_OpenStartupAppsSetting,

    // General > Real-time update
    FrmSettings_RealTimeFileUpdate,
    FrmSettings_EnableFileWatcher,
    FrmSettings_EnableAutoOpenNewAddedImage,

    // General > Others
    FrmSettings_Others,
    FrmSettings_AutoUpdate,
    FrmSettings_EnableMultiInstances,
    FrmSettings_ShowAppIcon,
    FrmSettings_InAppMessageDuration,
    FrmSettings_ImageInfoTags,
    FrmSettings_AvailableImageInfoTags,
    #endregion // FrmSettings > Tab General


    #region FrmSettings > Tab Image
    // Image > Image loading
    FrmSettings_ImageLoading,
    FrmSettings_ImageLoadingOrder,
    FrmSettings_EnableExplorerSortOrder,
    FrmSettings_EnableSubfoldersLoading,
    FrmSettings_EnableImageFolderGrouping,
    FrmSettings_EnableHiddenImagesLoading,
    FrmSettings_EnableLoopBackNavigation,
    FrmSettings_EnableImagePreview,

    FrmSettings_EmbeddedThumbnail,
    FrmSettings_EnableOnlyLoadRawPreview,
    FrmSettings_EnableOnlyLoadNonRawPreview,
    FrmSettings_MinEmbeddedThumbnailSize,
    FrmSettings_MinEmbeddedThumbnailSize_Width,
    FrmSettings_MinEmbeddedThumbnailSize_Height,

    // Image > Image Booster
    FrmSettings_ImageBooster,
    FrmSettings_ImageBoosterCacheCount,
    FrmSettings_ImageBoosterCacheMaxDimension,
    FrmSettings_ImageBoosterCacheMaxFileSizeInMb,

    // Image > Color management
    FrmSettings_ColorManagement,
    FrmSettings_EnableAlwaysApplyColorProfile,
    FrmSettings_ColorProfile,
    FrmSettings_CurrentMonitorProfile_Description,
    #endregion // FrmSettings > Tab Image


    #region FrmSettings > Tab Slideshow
    // Slideshow > Slideshow
    FrmSettings_EnableSlideshowCountdown,
    FrmSettings_EnableFullscreenSlideshow,
    FrmSettings_EnableSlideshowRandomInterval,
    FrmSettings_SlideshowInterval,
    FrmSettings_SlideshowInterval_From,
    FrmSettings_SlideshowInterval_To,
    FrmSettings_SlideshowBackgroundColor,

    // Slideshow > Slideshow notification
    FrmSettings_SlideshowNotification,
    FrmSettings_SlideshowImagesToNotifySound,
    #endregion // FrmSettings > Tab Slideshow


    #region FrmSettings > Tab Edit
    // Edit > Edit
    FrmSettings_EnableDeleteConfirmation,
    FrmSettings_EnableSaveConfirmation,
    FrmSettings_EnablePreserveModifiedDate,
    FrmSettings_EnableOpenSaveAsInCurrentFolder,
    FrmSettings_ImageEditQuality,
    FrmSettings_AfterEditingAction,

    // Edit > Clipboard
    FrmSettings_Clipboard,
    FrmSettings_EnableCopyMultipleFiles,
    FrmSettings_EnableCutMultipleFiles,

    // Edit > Image editing apps
    FrmSettings_EditApps,
    FrmSettings_EditApps_AppName,
    FrmSettings_EditAppDialog_AddApp,
    FrmSettings_EditAppDialog_EditApp,
    #endregion // FrmSettings > Tab Edit


    #region FrmSettings > Tab Layout
    // Layout > Layout
    FrmSettings_Layout_Order,
    FrmSettings_Layout_Toolbar,
    FrmSettings_Layout_ToolbarContext,
    FrmSettings_Layout_Gallery,
    FrmSettings_Layout_ToolbarPosition,
    FrmSettings_Layout_ToolbarContextPosition,
    FrmSettings_Layout_GalleryPosition,
    #endregion // FrmSettings > Tab Layout


    #region FrmSettings > Tab Viewer
    // Viewer > Viewer
    FrmSettings_ShowCheckerboardOnlyImageRegion,
    FrmSettings_EnableNavigationButtons,
    FrmSettings_EnableCenterWindowFit,
    FrmSettings_PanSpeed,

    // Viewer > Zooming
    FrmSettings_Zooming,
    FrmSettings_ImageInterpolation,
    FrmSettings_ImageInterpolation_ScaleDown,
    FrmSettings_ImageInterpolation_ScaleUp,
    FrmSettings_ZoomSpeed,
    FrmSettings_ZoomLevels,
    FrmSettings_UseSmoothZooming,
    FrmSettings_LoadDefaultZoomLevels,
    #endregion // FrmSettings > Tab Viewer


    #region FrmSettings > Tab Toolbar
    // Toolbar > Toolbar
    FrmSettings_Toolbar_ShowToolbarInFullscreen,
    FrmSettings_Toolbar_ToolbarIconHeight,

    FrmSettings_Toolbar_AddNewButton,
    FrmSettings_Toolbar_EditButton,
    FrmSettings_Toolbar_ButtonJson,

    FrmSettings_Toolbar_ToolbarButtons,
    FrmSettings_Toolbar_AddCustomButton,
    FrmSettings_Toolbar_AvailableButtons,
    FrmSettings_Toolbar_CurrentButtons,
    FrmSettings_Toolbar_Errors_ButtonIdRequired,
    FrmSettings_Toolbar_Errors_ButtonIdDuplicated,
    FrmSettings_Toolbar_Errors_ButtonExecutableRequired,
    #endregion // FrmSettings > Tab Toolbar


    #region FrmSettings > Tab Gallery
    // Gallery > Gallery
    FrmSettings_ShowGalleryInFullscreen,
    FrmSettings_ShowGalleryFileName,
    FrmSettings_ThumbnailSize,
    FrmSettings_GalleryCacheSizeInMb,
    FrmSettings_GalleryColumns,
    #endregion // FrmSettings > Tab Gallery


    #region FrmSettings > Tab Mouse
    // Mouse > Mouse wheel action
    FrmSettings_MouseWheelAction,
    #endregion // FrmSettings > Tab Mouse


    #region FrmSettings > Tab Keyboard

    #endregion // FrmSettings > Tab Mouse & Keyboard


    #region FrmSettings > Tab File type associations
    // File type associations > File extension icons
    FrmSettings_FileExtensionIcons,
    FrmSettings_FileExtensionIcons_Description,
    FrmSettings_OpenExtensionIconFolder,
    FrmSettings_GetExtensionIconPacks,

    // File type associations > Default photo viewer
    FrmSettings_DefaultPhotoViewer,
    FrmSettings_DefaultPhotoViewer_Description,
    FrmSettings_MakeDefault,
    FrmSettings_RemoveDefault,
    FrmSettings_OpenDefaultAppsSetting,

    // File type associations > File formats
    FrmSettings_FileFormats,
    FrmSettings_TotalSupportedFormats,
    FrmSettings_AddNewFileExtension,

    #endregion // FrmSettings > Tab File type associations


    #region FrmSettings > Tab Tools
    // Tools > Tools
    FrmSettings_Tools_AddNewTool,
    FrmSettings_Tools_EditTool,
    FrmSettings_Tools_Integrated,
    FrmSettings_Tools_IntegratedWith,
    #endregion // FrmSettings > Tab Tools


    #region FrmSettings > Tab Language
    // Language > Language
    FrmSettings_DisplayLanguage,
    FrmSettings_Refresh,
    FrmSettings_InstallNewLanguagePack,
    FrmSettings_GetMoreLanguagePacks,
    FrmSettings_ExportLanguagePack,
    FrmSettings_Contributors,
    #endregion // FrmSettings > Tab Language


    #region FrmSettings > Tab Appearance
    // Appearance > Appearance
    FrmSettings_WindowBackdrop,
    FrmSettings_BackgroundColor,

    // Appearance > Theme
    FrmSettings_Theme,
    FrmSettings_DarkTheme,
    FrmSettings_LightTheme,
    FrmSettings_Author,
    FrmSettings_Theme_OpenThemeFolder,
    FrmSettings_Theme_GetMoreThemes,
    FrmSettings_Theme_InstallTheme,
    FrmSettings_Theme_UninstallTheme,

    FrmSettings_UseThemeForDarkMode,
    FrmSettings_UseThemeForLightMode,
    #endregion // FrmSettings > Tab Appearance

    #endregion // FrmSettings


    #region FrmCrop
    FrmCrop_LblAspectRatio,
    FrmCrop_LblLocation,
    FrmCrop_LblSize,
    FrmCrop_LblPreset,

    FrmCrop_SelectionAspectRatio_FreeRatio,
    FrmCrop_SelectionAspectRatio_Custom,
    FrmCrop_SelectionAspectRatio_Original,

    FrmCrop_BtnReset,
    FrmCrop_BtnSave,
    FrmCrop_BtnSaveAs,
    FrmCrop_BtnCrop,
    FrmCrop_BtnCopy,


    // Crop settings
    FrmCropSettings_Title,
    FrmCropSettings_ChkCloseToolAfterSaving,
    FrmCropSettings_LblDefaultSelection,
    FrmCropSettings_ChkAutoCenterSelection,

    FrmCropSettings_DefaultSelectionType_UseTheLastSelection,
    FrmCropSettings_DefaultSelectionType_SelectNone,
    FrmCropSettings_DefaultSelectionType_SelectX,
    FrmCropSettings_DefaultSelectionType_SelectAll,
    FrmCropSettings_DefaultSelectionType_CustomArea,
    #endregion // FrmCrop


    #region FrmColorPicker
    FrmColorPicker_BtnSettings_Tooltip,

    // Color picker settings
    FrmColorPickerSettings_Title,
    FrmColorPickerSettings_ChkShowRgbA,
    FrmColorPickerSettings_ChkShowHexA,
    FrmColorPickerSettings_ChkShowHslA,
    FrmColorPickerSettings_ChkShowHsvA,
    FrmColorPickerSettings_ChkShowCmykA,
    FrmColorPickerSettings_ChkShowCIELabA,
    #endregion // FrmColorPicker


    #region FrmToolNotFound
    FrmToolNotFound_Title,
    FrmToolNotFound_BtnSelectExecutable,
    FrmToolNotFound_LblHeading,
    FrmToolNotFound_LblDescription,
    FrmToolNotFound_LblDownloadToolText,
    #endregion // FrmToolNotFound


    #region FrmHotkeyPicker
    FrmHotkeyPicker_LblHotkey,
    #endregion // FrmHotkeyPicker


    #region FrmResize
    FrmResize_RadResizeByPixels,
    FrmResize_RadResizeByPercentage,
    FrmResize_ChkKeepRatio,
    FrmResize_LblResample,
    FrmResize_LblCurrentSize,
    FrmResize_LblNewSize,
    #endregion // FrmResize


    #region igcmd.exe

    _IgCommandExe_DefaultError_Heading,
    _IgCommandExe_DefaultError_Description,

    #region FrmSlideshow
    FrmSlideshow_PauseSlideshow,
    FrmSlideshow_ResumeSlideshow,
    FrmSlideshow_MnuPauseResumeSlideshow,
    FrmSlideshow_MnuToggleCountdown,
    FrmSlideshow_MnuExitSlideshow,
    #endregion // FrmSlideshow


    #region FrmExportFrames
    FrmExportFrames_Title,
    FrmExportFrames_FolderPickerTitle,
    FrmExportFrames_Exporting,
    FrmExportFrames_ExportDone,
    FrmExportFrames_OpenOutputFolder,
    #endregion // FrmExportFrames


    #region FrmQuickSetup
    FrmQuickSetup_Text,
    FrmQuickSetup_StepInfo,
    FrmQuickSetup_SkipQuickSetup,

    FrmQuickSetup_SeeWhatNew,
    FrmQuickSetup_SelectProfile,
    FrmQuickSetup_StandardUser,
    FrmQuickSetup_ProfessionalUser,
    FrmQuickSetup_SettingProfileDescription,

    FrmQuickSetup_SettingsWillBeApplied,
    FrmQuickSetup_SetDefaultViewer,
    FrmQuickSetup_SetDefaultViewer_Description,

    FrmQuickSetup_ConfirmCloseProcess,
    #endregion // FrmQuickSetup

    #endregion // igcmd.exe

}

