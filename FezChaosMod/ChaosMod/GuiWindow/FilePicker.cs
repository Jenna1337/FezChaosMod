using ImGuiNET;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public string CurrentFolder;
        public string SelectedFile;
        private string[] AllowedExtensions;
        public bool OnlyAllowFolders;
        public readonly string Id;

        public static FilePicker GetFolderPicker(string id, string startingPath)
            => GetFilePicker(id, startingPath, onlyAllowFolders: true);

        public static FilePicker GetFilePicker(string id, string startingPath, bool onlyAllowFolders = false)
        {
            FilePicker picker;
            if (!FilePickers.TryGetValue(id, out picker))
            {
                picker = new FilePicker(id, startingPath, onlyAllowFolders);
                FilePickers.Add(id, picker);
            }
            return picker;
        }

        private static readonly Dictionary<string, FilePicker> FilePickers = new Dictionary<string, FilePicker>();

        private FilePicker(string id, string startingPath, bool onlyAllowFolders = false)
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

            CurrentFolder = Path.GetFullPath(startingPath);
            OnlyAllowFolders = onlyAllowFolders;
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

        private int currentFilterItem = 0;
        private const string AllFiles = "All Files (*.*)";
        private OrderedDictionary<string, string[]> FilterList = [];
        private string[] FilterListKeys = [];
        public void AddFileExtensionOption(string name, string[] exts)
        {
            if (AllowedExtensions == null)
            {
                AllowedExtensions = exts;
            }
            FilterList.Add(name, exts);
            FilterListKeys = FilterList.Keys.Append(AllFiles).ToArray();
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

                var style = ImGui.GetStyle();
                float rightIndent = style.WindowPadding.X;
                float framePaddingX = style.FramePadding.X;
                float itemSpacingX = style.ItemSpacing.X;
                float itemSpacingY = style.ItemSpacing.Y;
                float frameHeight = ImGui.GetFrameHeightWithSpacing();

                if (ImGui.Button("Up"))
                {
                    CurrentFolder = Directory.GetParent(CurrentFolder).FullName;
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1.0f);
                string path = CurrentFolder;
                if (ImGui.InputText($"##{Id}--PathInput", ref path, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (File.Exists(path))
                    {
                        CurrentFolder = Path.GetFullPath(Path.GetDirectoryName(path));
                    }
                    else if (Directory.Exists(path))
                    {
                        CurrentFolder = Path.GetFullPath(path);
                    }
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Current Folder");
                }
                if (ImGui.IsItemHovered() && ImGui.IsItemFocused())
                {
                    ImGui.SetTooltip("Press enter to navigate to the entered directory");
                }

                ImGui.Separator();

                const int footerLines = 2;

                // Total height needed for the bottom elements
                float totalFooterHeight = frameHeight * footerLines + itemSpacingY * (footerLines - 1);

                // Wrap file browsing inside a child frame so buttons remain locked to the bottom
                if (ImGui.BeginChild("FileBrowserList", new System.Numerics.Vector2(0, -totalFooterHeight), ImGuiChildFlags.Border))
                {
                    var di = new DirectoryInfo(CurrentFolder);
                    if (di.Exists)
                    {
                        if (di.Parent != null)
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
                ImGui.Text("File name: ");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1.0f);
                string input = Path.GetFileName(SelectedFile ?? "");
                if (ImGui.InputText($"##{Id}--FileName", ref input, 128))
                {
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("File name");
                }

                //new line

                const string CancelButtonText = "Cancel";
                const string OpenButtonText = "Open";
                float widthBtn1 = ImGui.CalcTextSize(CancelButtonText).X + (framePaddingX * 2.0f);
                float widthBtn2 = ImGui.CalcTextSize(OpenButtonText).X + (framePaddingX * 2.0f);
                float totalWidthNeeded = widthBtn1 + itemSpacingX + widthBtn2;
                float targetX = ImGui.GetContentRegionAvail().X - totalWidthNeeded;

                ImGui.SetNextItemWidth(targetX - itemSpacingX - ImGui.GetCursorPosX());
                if (ImGui.Combo($"##{Id}--FilterCombo", ref currentFilterItem, FilterListKeys.ToArray(), FilterListKeys.Length))
                {
                    string value = FilterListKeys[currentFilterItem];
                    if (value == AllFiles)
                    {
                        AllowedExtensions = null;
                    }
                    else
                    {
                        AllowedExtensions = FilterList[value];
                    }
                }
                ImGui.SameLine();


                //right align buttons
                //ImGui.SetCursorPosX(targetX);
                // Keep buttons strictly INSIDE BeginPopupModal's execution bracket
                if (ImGui.Button(CancelButtonText))
                {
                    result = false;
                    IsOpen = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                ImGui.BeginDisabled(OnlyAllowFolders || SelectedFile != null);
                if (ImGui.Button(OpenButtonText))
                {
                    result = true;
                    if (OnlyAllowFolders)
                    {
                        SelectedFile = CurrentFolder;
                    }
                    IsOpen = false;
                    ImGui.CloseCurrentPopup();

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