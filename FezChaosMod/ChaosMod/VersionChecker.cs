using FezEngine.Components;
using FezEngine.Tools;
using ImGuiNET;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using NVector2 = System.Numerics.Vector2;

namespace FezGame
{
    internal class VersionChecker
    {
        public static string RemoteUrl { get; private set; } = null;
        public static Version localChaosModVersion { get; private set; } = null;
        public static Version remoteChaosModVersion { get; private set; } = null;

        private static bool openVersionCheckWindow = false;
        private static bool updateAvailable = false;
        private static string statusText = string.Empty;
        public static void CheckVersions()
        {
            _ = Waiters.Wait(() => ServiceHelper.FirstLoadDone, static async () =>
            {
                static Version GetLocalVersion(string ModName)
                {
                    try
                    {
                        var hatType = typeof(Fez).Assembly.GetType("HatModLoader.Source.Hat");
                        static object GetMemberValue(Type t, string memberName, object parent)
                        {
                            return t.GetProperty(memberName)?.GetValue(parent) ?? t.GetField(memberName)?.GetValue(parent);
                        }
                        object hat = GetMemberValue(hatType, "Instance", null);
                        object ModsList = GetMemberValue(hatType, "Mods", hat);
                        if (ModsList is System.Collections.IList list)
                        {
                            Type modContainerType = ModsList.GetType().GetGenericArguments()[0];
                            // 2. Iterate or find the entry
                            for (int i = 0; i < list.Count; i++)
                            {
                                object entry = list[i];
                                var metadata = GetMemberValue(modContainerType, "Metadata", entry);
                                var name = GetMemberValue(metadata.GetType(), "Name", metadata);
                                var version = GetMemberValue(metadata.GetType(), "Version", metadata);
                                if (ModName.Equals(name))
                                {
                                    return version as Version;
                                }
                            }
                        }
                    }
                    catch { }
                    return null;
                }
                localChaosModVersion = GetLocalVersion("FezChaosMod");
                static async System.Threading.Tasks.Task<Version> GetRemoteVersion(string GithubRepoPathPart)
                {
                    try
                    {
                        using HttpClientHandler handler = new()
                        {
                            AllowAutoRedirect = false
                        };
                        using (HttpClient client = new(handler))
                        {
                            const string versionRegex = @"(\d+\.\d+(?:\.\d+)*)";
                            handler.AllowAutoRedirect = false;
                            RemoteUrl = $"https://github.com/{GithubRepoPathPart}/releases/latest";
                            var response = await client.GetAsync(RemoteUrl, HttpCompletionOption.ResponseHeadersRead);
                            string location = response?.Headers?.Location?.AbsoluteUri;
                            Match match = null;
                            if (location != null)
                            {
                                match = Regex.Match(location, versionRegex);
                            }
                            if (match == null || !match.Success)
                            {
                                handler.AllowAutoRedirect = true;
                                string content = await client.GetStringAsync($"https://api.github.com/repos/{GithubRepoPathPart}/releases/latest");
                                match = Regex.Match(content, versionRegex);
                            }
                            if (match != null && match.Success)
                            {
                                return new Version(match.Groups[1].Value);
                            }
                        }
                    }
                    catch (Exception) { }
                    return null;
                }
                remoteChaosModVersion = await GetRemoteVersion("Jenna1337/FezChaosMod");
                if (localChaosModVersion != null && remoteChaosModVersion != null)
                {
                    if (localChaosModVersion < remoteChaosModVersion)
                    {
                        ChaosModWindow.LogLine(statusText = "New version update available at " + RemoteUrl);
                        openVersionCheckWindow = true;
                        updateAvailable = true;
                    }
                    else
                    {
                        ChaosModWindow.LogLine(statusText = $"Version is up to date (v{localChaosModVersion})");
                        updateAvailable = false;
                    }
                }
                else
                {
                    ChaosModWindow.LogLine(statusText = "Failed to check for updated version");
                    updateAvailable = false;
                }
            });
        }
        public static void OpenPopup()
        {
            openVersionCheckWindow = true;
        }
        public static void Draw()
        {
            const string VersionCheckerWindowName = "FezChaosMod Version Checker";
            if (openVersionCheckWindow)
            {
                ImGui.OpenPopup(VersionCheckerWindowName);
                openVersionCheckWindow = false; // Reset so it doesn't re-trigger every frame
            }

            ImGuiViewportPtr viewport = ImGui.GetMainViewport();
            NVector2 center = viewport.GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new NVector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new NVector2(350, 150), ImGuiCond.Appearing);
            if (ImGui.BeginPopup(VersionCheckerWindowName, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (updateAvailable)
                {
                    ImGui.Text("A new version of FezChaosMod is available!");
                    ImGui.Text($"Current: v{localChaosModVersion}  ->  Latest: v{remoteChaosModVersion}");
                    ImGui.Separator();
                    ImGui.Spacing();

                    // 3. Action Buttons
                    if (ImGui.Button("Download Now", new NVector2(120, 0)))
                    {
                        // Example: Open the download link in the user's default browser
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = RemoteUrl,
                            UseShellExecute = true
                        });
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Remind Me Later", new NVector2(120, 0)))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                }
                else
                {
                    ImGui.Text(statusText);
                    if (ImGui.Button("Close", new NVector2(120, 0)))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.EndPopup(); // Required if BeginPopupModal returns true
            }
        }
    }
}
