﻿// -------------------------------------------------------------------------------------------------
// img_viewer.cs 0.2
//
// Simple KSP plugin to take img_viewer ingame.
// Copyright (C) 2014 Iván Atienza
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/.
//
// Email: mecagoenbush at gmail dot com
// Freenode: hashashin
//
// -------------------------------------------------------------------------------------------------

using KSP.IO;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using File = KSP.IO.File;

namespace img_viewer
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class ImgViewer : MonoBehaviour
    {
        private Rect _windowRect;
        private Rect _windowRect2 = new Rect(Screen.width / 2 - 150f, Screen.height / 2 - 75f, 260f, 390f);

        private string _keybind;

        private bool _visible;

        private IButton _button;
        private const string _tooltipOn = "Hide Image Viewer";
        private const string _tooltipOff = "Show Image Viewer";
        private const string _btextureOn = "img_viewer/Textures/icon_on";
        private const string _btextureOff = "img_viewer/Textures/icon_off";

        private string _version;
        private string _versionlastrun;
        private Texture2D _image;

        private WWW _imagetex;
        private string _imagefile;

        private readonly string _imageurl = "file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") +
                                   "/GameData/img_viewer/Plugins/PluginData/Images/";

        private readonly string _imagedir = KSPUtil.ApplicationRootPath.Replace("\\", "/") +
                                   "/GameData/img_viewer/Plugins/PluginData/Images/";

        private List<string> _imageList;
        private Vector2 _scrollViewVector = Vector2.zero;
        private int _selectionGridInt;
        private bool _showList;
        private bool _useKSPskin;
        private int _lastimg = -1;

        private void Awake()
        {
            LoadVersion();
            VersionCheck();
            LoadSettings();
        }

        private void Start()
        {
            //populate the list of images
            if (_imageList == null)
            {
                GetImages();
            }
            // toolbar stuff
            if (!ToolbarManager.ToolbarAvailable) return;
            _button = ToolbarManager.Instance.add("img_viewer", "toggle");
            _button.TexturePath = _btextureOff;
            _button.ToolTip = _tooltipOff;
            _button.OnClick += (e => Toggle());
            _button.OnMouseEnter += (e => TogglePopupMenu(_button));
        }

        private void OnGUI()
        {
            // Saves the current Gui.skin for later restore
            GUISkin _defGuiSkin = GUI.skin;
            if (_visible)
            {
                GUI.skin = _useKSPskin ? HighLogic.Skin : _defGuiSkin;
                _windowRect = GUI.Window(GUIUtility.GetControlID(0, FocusType.Passive), _windowRect, IvWindow,
                    "Image viewer");
            }
            if (_showList)
            {
                GUI.skin = _useKSPskin ? HighLogic.Skin : _defGuiSkin;
                _windowRect2 = GUI.Window(GUIUtility.GetControlID(FocusType.Passive), _windowRect2, ListWindow,
                    "Image list");
            }
            //Restore the skin
            GUI.skin = _defGuiSkin;
        }

        private void IvWindow(int windowID)
        {
            _windowRect = new Rect(_windowRect.xMin, _windowRect.yMin, _image.width, _image.height + 20f);
            GUI.DrawTexture(new Rect(0f, 20f, _image.width, _image.height), _image, ScaleMode.ScaleToFit, true, 0f);

            if (GUI.Button(new Rect(2f, 2f, 13f, 13f), "X"))
            {
                Toggle();
            }
            GUI.DragWindow();
        }

        private void ListWindow(int windowId)
        {
            // Notes list gui.
            _scrollViewVector = GUI.BeginScrollView(new Rect(3f, 25f, 255f, 300f), _scrollViewVector,
                new Rect(0f, 0f, 0f, 25f * (_imageList.Count + 5)));
            var _options = new[] { GUILayout.Width(225f), GUILayout.ExpandWidth(false) };
            _selectionGridInt = GUILayout.SelectionGrid(_selectionGridInt, _imageList.ToArray(), 1, _options);
            GUI.EndScrollView();
            // Refresh images list.
            if (GUI.Button(new Rect(5f, 330f, 100f, 30f), "Refresh list"))
            {
                GetImages();
            }
            // Close the list window.
            if (GUI.Button(new Rect(2f, 2f, 13f, 13f), "X"))
            {
                _showList = !_showList;
            }
            // Makes the window dragable.
            GUI.DragWindow();
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(_keybind))
            {
                Toggle();
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(_keybind))
            {
                _showList = !_showList;
            }
            if (_lastimg == _selectionGridInt) return;
            Destroy(_image);
            _imagefile = _imageList[_selectionGridInt];
            _imagetex = new WWW(_imageurl + _imagefile);
            _image = _imagetex.texture;
            _imagetex.Dispose();
            _lastimg = _selectionGridInt;
        }

        private void GetImages()
        {
            _imageList = new List<string>(Directory.GetFiles(_imagedir, "*"));
            for (int i = 0; i < _imageList.Count; i++)
            {
                _imageList[i] = Path.GetFileName(_imageList[i]);
            }
        }

        private void OnDestroy()
        {
            SaveSettings();
            if (_button != null)
            {
                _button.Destroy();
            }
        }

        private void LoadSettings()
        {
            print("[img_viewer.dll] Loading Config...");
            PluginConfiguration _configfile = PluginConfiguration.CreateForType<ImgViewer>();
            _configfile.load();

            _windowRect = _configfile.GetValue<Rect>("windowpos");
            _windowRect2 = _configfile.GetValue("windowpos2",
                new Rect(Screen.width / 2 - 150f, Screen.height / 2 - 75f, 260f, 390f));
            _keybind = _configfile.GetValue("keybind", "i");
            _versionlastrun = _configfile.GetValue<string>("version");
            _useKSPskin = _configfile.GetValue("kspskin", false);
            _visible = _configfile.GetValue("visible", false);
            _selectionGridInt = _configfile.GetValue("lastimage", 0);

            print("[img_viewer.dll] Config Loaded Successfully");
        }

        private void SaveSettings()
        {
            print("[img_viewer.dll] Saving Config...");
            PluginConfiguration _configfile = PluginConfiguration.CreateForType<ImgViewer>();

            _configfile.SetValue("windowpos", _windowRect);
            _configfile.SetValue("windowpos2", _windowRect2);
            _configfile.SetValue("keybind", _keybind);
            _configfile.SetValue("version", _version);
            _configfile.SetValue("kspskin", _useKSPskin);
            _configfile.SetValue("visible", _visible);
            _configfile.SetValue("lastimage", _selectionGridInt);

            _configfile.save();
            print("[img_viewer.dll] Config Saved ");
        }

        private void Toggle()
        {
            if (_visible)
            {
                _visible = false;
                _button.TexturePath = _btextureOff;
                _button.ToolTip = _tooltipOff;
                if (_showList)
                {
                    _showList = false;
                }
            }
            else
            {
                _visible = true;
                _button.TexturePath = _btextureOn;
                _button.ToolTip = _tooltipOn;
            }
        }

        private void VersionCheck()
        {
            _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            print("img_viewer.dll version: " + _version);
            if ((_version != _versionlastrun) && (File.Exists<ImgViewer>("config.xml")))
            {
                File.Delete<ImgViewer>("config.xml");
            }
#if DEBUG
            File.Delete<ImgViewer>("config.xml");
#endif
        }

        private void LoadVersion()
        {
            PluginConfiguration _configfile = PluginConfiguration.CreateForType<ImgViewer>();
            _configfile.load();
            _versionlastrun = _configfile.GetValue<string>("version");
        }

        private void TogglePopupMenu(IButton button)
        {
            if (button.Drawable == null)
            {
                createPopupMenu(button);
            }
            else
            {
                destroyPopupMenu(button);
            }
        }

        private void createPopupMenu(IButton button)
        {
            // create menu drawable
            PopupMenuDrawable _menu = new PopupMenuDrawable();

            // create menu options
            IButton _option1 = _menu.AddOption("Show/hide image list");
            _option1.OnClick += e => _showList = !_showList;
            IButton _option2 = _menu.AddOption("Change skin");
            _option2.OnClick += e => _useKSPskin = !_useKSPskin;
            IButton _option3 = _menu.AddOption("Next image");
            _option3.OnClick += e => ImageNext();
            IButton _option4 = _menu.AddOption("Prev image");
            _option4.OnClick += e => ImagePrev();
            // auto-close popup menu when any option is clicked
            _menu.OnAnyOptionClicked += () => destroyPopupMenu(button);

            // hook drawable to button
            button.Drawable = _menu;
        }

        private void ImagePrev()
        {
            if (_selectionGridInt == 0) return;
            _selectionGridInt--;
        }

        private void ImageNext()
        {
            if (_selectionGridInt == _imageList.Count - 1) return;
            _selectionGridInt++;
        }

        private void destroyPopupMenu(IButton button)
        {
            // PopupMenuDrawable must be destroyed explicitly
            ((PopupMenuDrawable)button.Drawable).Destroy();

            // unhook drawable
            button.Drawable = null;
        }
    }
}