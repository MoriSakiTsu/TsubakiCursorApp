using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace TsubakiCursorApp
{
    public partial class MainWindow : System.Windows.Window
    {
        // ========== Win32 API ==========
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType,
            int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadCursorFromFile(string fileName);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x00000010;
        private const uint LR_DEFAULTSIZE = 0x00000040;
        private const uint SPI_SETCURSORS = 0x0057;
        private const uint SPIF_SENDCHANGE = 0x0002;

        // ========== 多源 Manifest URL（Gitee 优先，GitHub 回退） ==========
        private static readonly string[] DEFAULT_MANIFEST_URLS = new[]
        {
            "https://gitee.com/MoriSakiTsu/Tsubaki-Cursor-Themes/raw/main/manifest.json",
            "https://raw.githubusercontent.com/MoriSakiTsu/Tsubaki-Cursor-Themes/main/manifest.json"
        };

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // ========== 别名映射 ==========
        private static readonly Dictionary<string, string> CursorAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // 标准注册表名
            {"Arrow", "Arrow"}, {"Help", "Help"}, {"AppStarting", "AppStarting"},
            {"Wait", "Wait"}, {"Crosshair", "Crosshair"}, {"IBeam", "IBeam"},
            {"NWPen", "NWPen"}, {"No", "No"}, {"SizeNS", "SizeNS"},
            {"SizeWE", "SizeWE"}, {"SizeNWSE", "SizeNWSE"}, {"SizeNESW", "SizeNESW"},
            {"SizeAll", "SizeAll"}, {"UpArrow", "UpArrow"}, {"Hand", "Hand"},
            {"Pin", "Pin"}, {"Person", "Person"},
            
            // 可不 / 蓝情
            {"Normal", "Arrow"},
            {"Busy", "Wait"},
            {"Working", "AppStarting"},
            {"Link", "Hand"},
            {"Text", "IBeam"},
            {"Precision", "Crosshair"},
            {"Unavailable", "No"},
            {"Horizontal", "SizeWE"},
            {"Vertical", "SizeNS"},
            {"Diagonal1", "SizeNESW"},
            {"Diagonal2", "SizeNWSE"},
            {"Move", "SizeAll"},
            {"Alternate", "UpArrow"},
            {"Handwriting", "NWPen"},
            
            // 花谱v2 / 黄情
            {"Pointer", "Arrow"},      // 鼠标指针
            {"Cross", "Crosshair"},    // 十字准星
            {"Horz", "SizeWE"},        // 水平缩写
            {"Vert", "SizeNS"},        // 垂直缩写
            {"Dgn1", "SizeNESW"},      // 对角线1
            {"Dgn2", "SizeNWSE"},      // 对角线2
            {"Loc", "Pin"},            // 定位/图钉 (Location)
        };

        // ========== 标准光标角色顺序（用于注册表和 Schemes） ==========
        private static readonly string[] CursorNames = new string[]
        {
            "Arrow", "Help", "AppStarting", "Wait", "Crosshair",
            "IBeam", "NWPen", "No", "SizeNS", "SizeWE",
            "SizeNWSE", "SizeNESW", "SizeAll", "UpArrow",
            "Hand", "Pin", "Person"
        };

        private List<ThemeInfo> _allThemes = new List<ThemeInfo>();
        private string _activePage = "Using";
        private int _currentNavIndex = 0;
        private string _currentAppliedThemeId = null;

        public MainWindow()
        {
            InitializeComponent();
            LoadCurrentCursors();
            Canvas.SetTop(NavHighlight, 0);
            AnimateNavTo(0, instant: true);
            ScanLocalThemes();
            _ = LoadRemoteThemesAsync();
        }

        // ========== 流体变形色块动画 ==========
        private void AnimateNavTo(int newIndex, bool instant = false)
        {
            if (_currentNavIndex == newIndex)
                return;

            _currentNavIndex = newIndex;
            double targetY = newIndex * 44;

            var activeFg = new SolidColorBrush(Color.FromRgb(248, 244, 236));
            var inactiveFg = new SolidColorBrush(Color.FromRgb(192, 180, 166));
            NavUsingText.Foreground = (newIndex == 0) ? activeFg : inactiveFg;
            NavListText.Foreground = (newIndex == 1) ? activeFg : inactiveFg;
            NavAboutText.Foreground = (newIndex == 2) ? activeFg : inactiveFg;

            if (instant)
            {
                Canvas.SetTop(NavHighlight, targetY);
                NavHighlight.Height = 40;
                NavHighlight.CornerRadius = new CornerRadius(8);
                return;
            }

            var sb = new Storyboard();

            // Canvas.Top：SineEase 平滑
            var yAnim = new DoubleAnimation(targetY, TimeSpan.FromMilliseconds(450))
            {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(yAnim, NavHighlight);
            Storyboard.SetTargetProperty(yAnim, new PropertyPath("(Canvas.Top)"));
            sb.Children.Add(yAnim);

            // Height：拉伸 → 平滑恢复（无压扁）
            var hAnim = new DoubleAnimationUsingKeyFrames();
            hAnim.KeyFrames.Add(new SplineDoubleKeyFrame(40, TimeSpan.FromMilliseconds(0)));
            hAnim.KeyFrames.Add(new SplineDoubleKeyFrame(56, TimeSpan.FromMilliseconds(200),
                new KeySpline(0.3, 0.0, 1.0, 1.0)));
            hAnim.KeyFrames.Add(new SplineDoubleKeyFrame(40, TimeSpan.FromMilliseconds(450),
                new KeySpline(0.2, 0.0, 0.5, 1.0)));
            Storyboard.SetTarget(hAnim, NavHighlight);
            Storyboard.SetTargetProperty(hAnim, new PropertyPath("Height"));
            sb.Children.Add(hAnim);

            sb.Begin();
        }

        private string DetectCurrentAppliedTheme()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors"))
                {
                    if (key == null) return null;
                    string arrowPath = key.GetValue("Arrow") as string;
                    if (string.IsNullOrEmpty(arrowPath)) return null;

                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string cursorsBase = Path.Combine(appData, "TsubakiCursor", "Cursors");
                    if (!arrowPath.StartsWith(cursorsBase, StringComparison.OrdinalIgnoreCase))
                        return null;

                    string relative = arrowPath.Substring(cursorsBase.Length).TrimStart('\\', '/');
                    string themeId = relative.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    return themeId;
                }
            }
            catch { return null; }
        }

        private void LoadCurrentCursors()
        {
            var cursors = new List<CursorInfo>();

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors"))
            {
                if (key != null)
                {
                    foreach (string name in CursorNames)
                    {
                        string path = key.GetValue(name) as string;
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            BitmapSource bitmap = LoadCursorImage(path);
                            cursors.Add(new CursorInfo
                            {
                                Name = name,
                                FilePath = path,
                                FileName = Path.GetFileName(path),
                                Image = bitmap
                            });
                        }
                    }
                }
            }

            CursorsList.ItemsSource = cursors;
        }

        private BitmapSource LoadCursorImage(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            string ext = Path.GetExtension(path).ToLower();

            if (ext == ".cur")
            {
                try
                {
                    IntPtr hIcon = LoadImage(IntPtr.Zero, path, IMAGE_ICON, 32, 32,
                        LR_LOADFROMFILE | LR_DEFAULTSIZE);
                    if (hIcon != IntPtr.Zero)
                    {
                        var bitmap = Imaging.CreateBitmapSourceFromHIcon(
                            hIcon,
                            System.Windows.Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        DestroyIcon(hIcon);
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
                catch { }
            }

            if (ext == ".ani")
            {
                try
                {
                    IntPtr hCursor = LoadCursorFromFile(path);
                    if (hCursor != IntPtr.Zero)
                    {
                        IntPtr hIcon = CopyIcon(hCursor);
                        if (hIcon != IntPtr.Zero)
                        {
                            var bitmap = Imaging.CreateBitmapSourceFromHIcon(
                                hIcon,
                                System.Windows.Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());
                            DestroyIcon(hIcon);
                            bitmap.Freeze();
                            return bitmap;
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        private void ScanLocalThemes()
        {
            _currentAppliedThemeId = DetectCurrentAppliedTheme();

            List<string> searchPaths = new List<string>
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Themes")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Themes"))
            };

            string themesDir = searchPaths.FirstOrDefault(Directory.Exists);
            if (themesDir == null)
                return;

            _allThemes.Clear();

            foreach (string dir in Directory.GetDirectories(themesDir))
            {
                ThemeInfo theme = LoadThemeFromFolder(dir);
                if (theme != null)
                {
                    theme.IsApplied = (theme.Id == _currentAppliedThemeId);
                    _allThemes.Add(theme);
                }
            }

            ThemesList.ItemsSource = null;
            ThemesList.ItemsSource = _allThemes;
        }

        private ThemeInfo LoadThemeFromFolder(string folderPath)
        {
            var cursorFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string ext in new[] { "*.cur", "*.ani" })
            {
                foreach (string file in Directory.GetFiles(folderPath, ext))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string standardName = GetStandardCursorName(fileName);
                    if (standardName != null)
                        cursorFiles[standardName] = file;
                }
            }

            if (cursorFiles.Count == 0)
                return null;

            string jsonPath = Path.Combine(folderPath, "theme.json");
            string themeName = Path.GetFileName(folderPath);
            string themeAuthor = "未知作者";
            string themeVersion = "1.0";

            if (File.Exists(jsonPath))
            {
                try
                {
                    string jsonText = File.ReadAllText(jsonPath);
                    using JsonDocument doc = JsonDocument.Parse(jsonText);
                    if (doc.RootElement.TryGetProperty("name", out var nameProp))
                        themeName = nameProp.GetString() ?? themeName;
                    if (doc.RootElement.TryGetProperty("author", out var authorProp))
                        themeAuthor = authorProp.GetString() ?? themeAuthor;
                    if (doc.RootElement.TryGetProperty("version", out var verProp))
                        themeVersion = verProp.GetString() ?? themeVersion;
                }
                catch { }
            }

            BitmapSource preview = null;
            string[] previewPriority = new[]
            {
                "Arrow", "Hand", "Person", "Pin", "Wait", "AppStarting",
                "Help", "Crosshair", "IBeam", "NWPen", "No",
                "SizeNS", "SizeWE", "SizeNWSE", "SizeNESW", "SizeAll", "UpArrow"
            };

            foreach (string key in previewPriority)
            {
                if (cursorFiles.ContainsKey(key))
                {
                    preview = LoadCursorImage(cursorFiles[key]);
                    if (preview != null)
                        break;
                }
            }

            return new ThemeInfo
            {
                Id = Path.GetFileName(folderPath),
                Name = themeName,
                Author = themeAuthor,
                Version = themeVersion,
                FolderPath = folderPath,
                CursorFiles = cursorFiles,
                PreviewImage = preview
            };
        }

        private string GetStandardCursorName(string fileNameWithoutExt)
        {
            if (CursorAliases.TryGetValue(fileNameWithoutExt, out string standardName))
                return standardName;
            return null;
        }

        private void ApplyTheme(ThemeInfo theme)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appDir = Path.Combine(appData, "TsubakiCursor");
                string cursorsDir = Path.Combine(appDir, "Cursors", theme.Id);
                string backupDir = Path.Combine(appDir, "Backup");

                Directory.CreateDirectory(cursorsDir);
                Directory.CreateDirectory(backupDir);

                var backup = new Dictionary<string, string>();
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors"))
                {
                    if (key != null)
                    {
                        foreach (string name in CursorNames)
                        {
                            var val = key.GetValue(name) as string;
                            backup[name] = val ?? "";
                        }
                    }
                }

                string backupFile = Path.Combine(backupDir, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.WriteAllText(backupFile, JsonSerializer.Serialize(backup));

                var appliedFiles = new Dictionary<string, string>();
                foreach (var kvp in theme.CursorFiles)
                {
                    string source = kvp.Value;
                    string dest = Path.Combine(cursorsDir, Path.GetFileName(source));
                    File.Copy(source, dest, true);

                    IntPtr hTest = LoadCursorFromFile(dest);
                    if (hTest == IntPtr.Zero)
                        throw new Exception($"文件验证失败: {Path.GetFileName(dest)}");
                    
                    if (Path.GetExtension(dest).ToLower() == ".cur")
                        DestroyIcon(hTest);

                    appliedFiles[kvp.Key] = dest;
                }

                var newValues = new Dictionary<string, string>(backup);
                foreach (var kvp in appliedFiles)
                {
                    newValues[kvp.Key] = kvp.Value;
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", true))
                {
                    if (key == null)
                        throw new Exception("无法打开注册表键");

                    foreach (var kvp in newValues)
                    {
                        key.SetValue(kvp.Key, kvp.Value);
                    }
                }

                string schemeValue = BuildSchemeValue(newValues);
                using (RegistryKey schemesKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors\Schemes", true))
                {
                    if (schemesKey != null)
                    {
                        schemesKey.SetValue(theme.Name, schemeValue);
                    }
                }

                SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDCHANGE);

                _currentAppliedThemeId = theme.Id;
                ScanLocalThemes();
                LoadCurrentCursors();
                _activePage = "Using";
                AnimateNavTo(0);
                SwitchPage(PageUsing);

                MessageBox.Show(
                    $"主题「{theme.Name}」应用成功！\n已备份原设置到：\n{backupFile}",
                    "成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"应用失败：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string BuildSchemeValue(Dictionary<string, string> values)
        {
            var parts = new List<string>();
            foreach (string name in CursorNames)
            {
                if (values.TryGetValue(name, out string path))
                    parts.Add(path ?? "");
                else
                    parts.Add("");
            }
            return string.Join(",", parts);
        }

        private async System.Threading.Tasks.Task LoadRemoteThemesAsync()
        {
            string manifestJson = null;

            foreach (string url in DEFAULT_MANIFEST_URLS)
            {
                try
                {
                    manifestJson = await _httpClient.GetStringAsync(url);
                    break;
                }
                catch { }
            }

            if (string.IsNullOrEmpty(manifestJson))
                return;

            try
            {
                using JsonDocument doc = JsonDocument.Parse(manifestJson);
                if (!doc.RootElement.TryGetProperty("themes", out var themesArray))
                    return;

                foreach (JsonElement themeElem in themesArray.EnumerateArray())
                {
                    string id = themeElem.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    string name = themeElem.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                    string author = themeElem.TryGetProperty("author", out var authorProp) ? authorProp.GetString() : null;
                    string downloadUrl = themeElem.TryGetProperty("downloadUrl", out var urlProp) ? urlProp.GetString() : null;
                    string sha256 = themeElem.TryGetProperty("sha256", out var shaProp) ? shaProp.GetString() : "";
                    string version = themeElem.TryGetProperty("version", out var verProp) ? verProp.GetString() : "";

                    if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(downloadUrl))
                        continue;

                    var existing = _allThemes.FirstOrDefault(t => t.Id == id);
                    if (existing != null)
                    {
                        existing.IsRemote = true;
                        existing.IsDownloaded = true;
                        existing.DownloadUrl = downloadUrl;
                        existing.Sha256 = sha256;
                        existing.RemoteVersion = version;
                        existing.HasUpdate = !string.IsNullOrEmpty(version) && version != existing.Version;
                    }
                    else
                    {
                        _allThemes.Add(new ThemeInfo
                        {
                            Id = id,
                            Name = name ?? id,
                            Author = author ?? "未知作者",
                            Version = "",
                            RemoteVersion = version,
                            IsRemote = true,
                            IsDownloaded = false,
                            DownloadUrl = downloadUrl,
                            Sha256 = sha256,
                            FolderPath = null,
                            CursorFiles = new Dictionary<string, string>(),
                            PreviewImage = null
                        });
                    }
                }

                ThemesList.ItemsSource = null;
                ThemesList.ItemsSource = _allThemes;
            }
            catch { }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectoryRecursive(subDir, destSubDir);
            }
        }

        private async System.Threading.Tasks.Task DownloadThemeAsync(ThemeInfo theme)
        {
            try
            {
                theme.IsDownloading = true;

                string tempZip = Path.Combine(Path.GetTempPath(), $"{theme.Id}_{Guid.NewGuid():N}.zip");
                byte[] data = await _httpClient.GetByteArrayAsync(theme.DownloadUrl);
                await File.WriteAllBytesAsync(tempZip, data);

                if (!string.IsNullOrEmpty(theme.Sha256))
                {
                    using var sha = SHA256.Create();
                    byte[] hash = sha.ComputeHash(data);
                    string hashStr = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    if (hashStr != theme.Sha256.ToLowerInvariant())
                    {
                        File.Delete(tempZip);
                        throw new Exception("SHA256 校验失败，文件可能已被篡改");
                    }
                }

                string themesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes");
                string extractDir = Path.Combine(themesDir, theme.Id);

                string tempExtractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempExtractDir);

                ZipFile.ExtractToDirectory(tempZip, tempExtractDir);

                var extractedItems = Directory.GetFileSystemEntries(tempExtractDir);
                if (extractedItems.Length == 1 && Directory.Exists(extractedItems[0]))
                {
                    string innerFolder = extractedItems[0];
                    if (Directory.Exists(extractDir))
                        Directory.Delete(extractDir, true);
                    CopyDirectoryRecursive(innerFolder, extractDir);
                    Directory.Delete(innerFolder, true);
                }
                else
                {
                    if (Directory.Exists(extractDir))
                        Directory.Delete(extractDir, true);
                    CopyDirectoryRecursive(tempExtractDir, extractDir);
                    Directory.Delete(tempExtractDir, true);
                }

                if (File.Exists(tempZip))
                    File.Delete(tempZip);
                if (Directory.Exists(tempExtractDir))
                    Directory.Delete(tempExtractDir, true);

                string downloadedFolder = Path.Combine(themesDir, theme.Id);
                if (Directory.Exists(downloadedFolder))
                {
                    var loaded = LoadThemeFromFolder(downloadedFolder);
                    if (loaded != null)
                    {
                        theme.FolderPath = loaded.FolderPath;
                        theme.CursorFiles = loaded.CursorFiles;
                        theme.PreviewImage = loaded.PreviewImage;
                        theme.Version = loaded.Version;
                        theme.IsDownloaded = true;
                        theme.HasUpdate = false;
                    }
                }

                MessageBox.Show(
                    $"主题「{theme.Name}」下载成功！",
                    "成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"下载失败：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                theme.IsDownloading = false;
            }
        }

        private void SwitchPage(Grid targetPage)
        {
            Grid currentPage = null;
            if (PageUsing.Visibility == Visibility.Visible) currentPage = PageUsing;
            else if (PageList.Visibility == Visibility.Visible) currentPage = PageList;
            else if (PageAbout.Visibility == Visibility.Visible) currentPage = PageAbout;

            if (currentPage == targetPage) return;

            if (currentPage != null)
            {
                var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180));
                fadeOut.Completed += (s, e) =>
                {
                    currentPage.Visibility = Visibility.Collapsed;
                    targetPage.Visibility = Visibility.Visible;
                    targetPage.Opacity = 0;
                    var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(220))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    targetPage.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                };
                fadeOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
                currentPage.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
            else
            {
                targetPage.Visibility = Visibility.Visible;
            }
        }

        private void NavUsing_Click(object sender, MouseButtonEventArgs e)
        {
            if (_activePage == "Using") return;
            _activePage = "Using";
            AnimateNavTo(0);
            SwitchPage(PageUsing);
        }

        private void NavList_Click(object sender, MouseButtonEventArgs e)
        {
            if (_activePage == "List") return;
            _activePage = "List";
            AnimateNavTo(1);
            SwitchPage(PageList);
        }

        private void NavAbout_Click(object sender, MouseButtonEventArgs e)
        {
            if (_activePage == "About") return;
            _activePage = "About";
            AnimateNavTo(2);
            SwitchPage(PageAbout);
        }

        private async void BtnRefreshRemote_Click(object sender, RoutedEventArgs e)
        {
            LoadingBar.Visibility = Visibility.Visible;
            try
            {
                await LoadRemoteThemesAsync();
            }
            finally
            {
                LoadingBar.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnRestoreDefault_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "确定要恢复系统默认鼠标指针吗？\n这将清除当前使用的 TsubakiCursor 主题设置，并删除 AppData 中的数据。",
                "确认恢复默认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var backup = new Dictionary<string, string>();
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors"))
                {
                    if (key != null)
                    {
                        foreach (string name in CursorNames)
                        {
                            var val = key.GetValue(name) as string;
                            backup[name] = val ?? "";
                        }
                    }
                }

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appDir = Path.Combine(appData, "TsubakiCursor");
                string backupDir = Path.Combine(appDir, "Backup");
                Directory.CreateDirectory(backupDir);
                string backupFile = Path.Combine(backupDir, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.WriteAllText(backupFile, JsonSerializer.Serialize(backup));

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", true))
                {
                    if (key != null)
                    {
                        foreach (string name in CursorNames)
                        {
                            key.SetValue(name, "");
                        }
                    }
                }

                using (RegistryKey schemesKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors\Schemes", true))
                {
                    if (schemesKey != null)
                    {
                        var schemeNames = schemesKey.GetValueNames();
                        foreach (string schemeName in schemeNames)
                        {
                            string schemeValue = schemesKey.GetValue(schemeName) as string;
                            if (!string.IsNullOrEmpty(schemeValue) &&
                                schemeValue.IndexOf(@"\TsubakiCursor\", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                schemesKey.DeleteValue(schemeName, false);
                            }
                        }
                    }
                }

                SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDCHANGE);

                if (Directory.Exists(appDir))
                {
                    try { Directory.Delete(appDir, true); } catch { }
                }

                _currentAppliedThemeId = null;
                ScanLocalThemes();
                LoadCurrentCursors();
                _activePage = "Using";
                AnimateNavTo(0);
                SwitchPage(PageUsing);

                MessageBox.Show(
                    "已恢复系统默认鼠标指针。\n最后一次备份仍保留在：\n" + backupFile,
                    "恢复成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"恢复失败：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void BtnApplyTheme_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ThemeInfo theme = button?.Tag as ThemeInfo;
            if (theme == null || theme.IsApplied) return;

            if (theme.IsRemote && !theme.IsDownloaded)
            {
                await DownloadThemeAsync(theme);
                return;
            }

            if (theme.HasUpdate)
            {
                var result = MessageBox.Show(
                    $"主题「{theme.Name}」有新版本（当前：{theme.Version}，最新：{theme.RemoteVersion}），是否更新？",
                    "发现更新",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    await DownloadThemeAsync(theme);
                }
                return;
            }

            var applyResult = MessageBox.Show(
                $"确定要应用主题「{theme.Name}」吗？\n作者：{theme.Author}\n包含 {theme.CursorFiles?.Count ?? 0} 个光标文件",
                "确认应用",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (applyResult == MessageBoxResult.Yes)
            {
                ApplyTheme(theme);
            }
        }
    }

    public class BoolToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b && b)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CursorInfo
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public BitmapSource Image { get; set; }
    }

    public class ThemeInfo : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isApplied;
        private bool _isRemote;
        private bool _isDownloaded;
        private bool _isDownloading;
        private bool _hasUpdate;

        public string Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string FolderPath { get; set; }
        public Dictionary<string, string> CursorFiles { get; set; }
        public BitmapSource PreviewImage { get; set; }

        public string Version { get; set; }
        public string RemoteVersion { get; set; }
        public string DownloadUrl { get; set; }
        public string Sha256 { get; set; }

        public bool IsApplied
        {
            get => _isApplied;
            set { _isApplied = value; OnPropertyChanged(nameof(ButtonText)); OnPropertyChanged(nameof(IsButtonEnabled)); }
        }

        public bool IsRemote
        {
            get => _isRemote;
            set { _isRemote = value; OnPropertyChanged(nameof(ButtonText)); OnPropertyChanged(nameof(IsButtonEnabled)); }
        }

        public bool IsDownloaded
        {
            get => _isDownloaded;
            set { _isDownloaded = value; OnPropertyChanged(nameof(ButtonText)); OnPropertyChanged(nameof(IsButtonEnabled)); }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set { _isDownloading = value; OnPropertyChanged(nameof(ButtonText)); OnPropertyChanged(nameof(IsButtonEnabled)); }
        }

        public bool HasUpdate
        {
            get => _hasUpdate;
            set { _hasUpdate = value; OnPropertyChanged(nameof(ButtonText)); OnPropertyChanged(nameof(IsButtonEnabled)); }
        }

        public string ButtonText
        {
            get
            {
                if (IsApplied) return "使用中";
                if (IsDownloading) return "下载中";
                if (IsRemote && !IsDownloaded) return "下载";
                if (HasUpdate) return "更新";
                return "应用";
            }
        }

        public bool IsButtonEnabled => !IsApplied && !IsDownloading;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}