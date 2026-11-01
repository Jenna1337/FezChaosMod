using ImGuiNET;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nez.ImGuiTools
{
    /// <summary>
    /// Simple file and folder picker for ImGui.Net
    /// </summary>
    /// <remarks>
    /// Adapted from <a href="https://gist.github.com/prime31/91d1582624eb2635395417393018016e">prime31/FilePicker.cs</a>
    /// <br />
    /// Original Author: <a href="https://gist.github.com/prime31">@prime31 on github</a>
    /// </remarks>
    public class FilePicker
    {
        public string RootFolder;
        public string CurrentFolder;
        public string SelectedFile;
        public List<string> AllowedExtensions;
        public bool OnlyAllowFolders;
        public readonly string Id;

        public static FilePicker GetFolderPicker(string id, string startingPath)
            => GetFilePicker(id, startingPath, onlyAllowFolders: true);

        public static FilePicker GetFilePicker(string id, string startingPath, string[] allowedExtensions = null, bool onlyAllowFolders = false)
        {
            FilePicker picker;
            if (!FilePickers.TryGetValue(id, out picker))
            {
                picker = new FilePicker(id, startingPath, allowedExtensions, onlyAllowFolders);
                FilePickers.Add(id, picker);
            }
            return picker;
        }

        private static readonly Dictionary<string, FilePicker> FilePickers = new Dictionary<string, FilePicker>();

        private FilePicker(string id, string startingPath, string[] allowedExtensions = null, bool onlyAllowFolders = false)
        {
            Id = id;
            if (File.Exists(startingPath))
            {
                startingPath = new FileInfo(startingPath).DirectoryName;
            }
            else if (string.IsNullOrEmpty(startingPath) || !Directory.Exists(startingPath))
            {
                startingPath = Environment.CurrentDirectory;
                if (string.IsNullOrEmpty(startingPath))
                    startingPath = AppContext.BaseDirectory;
            }

            RootFolder = startingPath;
            CurrentFolder = startingPath;
            OnlyAllowFolders = onlyAllowFolders;

            if (AllowedExtensions != null)
            {
                if (AllowedExtensions != null)
                    AllowedExtensions.Clear();
                else
                    AllowedExtensions = new List<string>();

                AllowedExtensions.AddRange(allowedExtensions);
            }
        }

        private bool isOpen = false;
        private bool shouldOpenPopupTrigger = false; // Internal deferred activation flag

        public bool IsOpen
        {
            get => isOpen; set
            {
                isOpen = value;

                if (isOpen)
                {
                    // Do NOT call ImGui.OpenPopup here to avoid nested ID scope mismatches
                    shouldOpenPopupTrigger = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if/when a file is selected</returns>
        public bool Draw()
        {
            bool result = false;

            if (!IsOpen)
            {
                return false;
            }

            // Force OpenPopup to execute inside Draw's current ID stack context
            if (shouldOpenPopupTrigger)
            {
                ImGui.OpenPopup(Id);
                shouldOpenPopupTrigger = false;
            }

            // Set size boundaries before opening the window
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 400), ImGuiCond.FirstUseEver);

            bool isOpenLocal = IsOpen;
            // Checked for titlebar flag context if using custom layouts
            if (ImGui.BeginPopupModal(Id, ref isOpenLocal, ImGuiWindowFlags.None))
            {
                IsOpen = isOpenLocal;

                ImGui.Text("Current Folder: " + Path.GetFileName(RootFolder) + CurrentFolder.Replace(RootFolder, ""));
                ImGui.Separator();

                // Wrap file browsing inside a child frame so buttons remain locked to the bottom
                if (ImGui.BeginChild("FileBrowserList", new System.Numerics.Vector2(0, -35), ImGuiChildFlags.Border))
                {
                    var di = new DirectoryInfo(CurrentFolder);
                    if (di.Exists)
                    {
                        if (di.Parent != null && CurrentFolder != RootFolder)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, Color.Yellow.PackedValue);
                            if (ImGui.Selectable("../", false, ImGuiSelectableFlags.DontClosePopups))
                                CurrentFolder = di.Parent.FullName;

                            ImGui.PopStyleColor();
                        }

                        var fileSystemEntries = GetFileSystemEntries(di.FullName);
                        foreach (var fse in fileSystemEntries)
                        {
                            if (Directory.Exists(fse))
                            {
                                var name = Path.GetFileName(fse);
                                ImGui.PushStyleColor(ImGuiCol.Text, Color.Yellow.PackedValue);
                                if (ImGui.Selectable(name + "/", false, ImGuiSelectableFlags.DontClosePopups))
                                    CurrentFolder = fse;
                                ImGui.PopStyleColor();
                            }
                            else
                            {
                                var name = Path.GetFileName(fse);
                                bool isSelected = SelectedFile == fse;
                                if (ImGui.Selectable(name, isSelected, ImGuiSelectableFlags.DontClosePopups))
                                    SelectedFile = fse;

                                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && isSelected)
                                {
                                    result = true;
                                    IsOpen = false;
                                    ImGui.CloseCurrentPopup();
                                }
                            }
                        }
                    }
                    ImGui.EndChild();
                }

                // Keep buttons strictly INSIDE BeginPopupModal's execution bracket
                if (ImGui.Button("Cancel"))
                {
                    result = false;
                    IsOpen = false;
                    ImGui.CloseCurrentPopup();
                }

                if (OnlyAllowFolders)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Open"))
                    {
                        result = true;
                        SelectedFile = CurrentFolder;
                        IsOpen = false;
                        ImGui.CloseCurrentPopup();
                    }
                }
                else if (SelectedFile != null)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Open"))
                    {
                        result = true;
                        IsOpen = false;
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.EndPopup();
            }
            else
            {
                // In case the user clicks the title bar 'X' button
                IsOpen = false;
            }

            return result;
        }


        bool TryGetFileInfo(string fileName, out FileInfo realFile)
        {
            try
            {
                realFile = new FileInfo(fileName);
                return true;
            }
            catch
            {
                realFile = null;
                return false;
            }
        }

        List<string> GetFileSystemEntries(string fullName)
        {
            var files = new List<string>();
            var dirs = new List<string>();

            foreach (var fse in Directory.GetFileSystemEntries(fullName, ""))
            {
                if (Directory.Exists(fse))
                {
                    dirs.Add(fse);
                }
                else if (!OnlyAllowFolders)
                {
                    if (AllowedExtensions != null)
                    {
                        var ext = Path.GetExtension(fse);
                        if (AllowedExtensions.Contains(ext))
                            files.Add(fse);
                    }
                    else
                    {
                        files.Add(fse);
                    }
                }
            }

            var ret = new List<string>(dirs);
            ret.AddRange(files);

            return ret;
        }

    }
}