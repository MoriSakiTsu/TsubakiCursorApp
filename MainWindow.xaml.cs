using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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

        private Border _activeNav;
        private string _currentAppliedThemeId = null;

        public MainWindow()
        {
            InitializeComponent();
            LoadCurrentCursors();
            _activeNav = NavUsing;
            ScanLocalThemes();
        }

        // ========== 检测是否使用某个本地主题 ==========
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

        // ========== 读取当前系统指针（使用中页面） ==========
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

        // ========== 加载 .cur / .ani 为图片 ==========
        private BitmapSource LoadCursorImage(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            string ext = Path.GetExtension(path).ToLower();

            // .cur：Win32 LoadImage
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

            // .ani：LoadCursorFromFile + CopyIcon 提取第一帧
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

        // ========== 扫描本地主题 ==========
        private void ScanLocalThemes()
        {
            // 启动时检测当前是否正在使用某个本地主题
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

            var themes = new List<ThemeInfo>();

            foreach (string dir in Directory.GetDirectories(themesDir))
            {
                ThemeInfo theme = LoadThemeFromFolder(dir);
                if (theme != null)
                {
                    theme.IsApplied = (theme.Id == _currentAppliedThemeId);
                    themes.Add(theme);
                }
            }

            ThemesList.ItemsSource = themes;
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

            // 读取 theme.json
            string jsonPath = Path.Combine(folderPath, "theme.json");
            string themeName = Path.GetFileName(folderPath);
            string themeAuthor = "未知作者";

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
                }
                catch { }
            }

            // 取预览图（循环回退直到成功）
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

        // ========== 应用主题（核心逻辑） ==========
        private void ApplyTheme(ThemeInfo theme)
        {
            try
            {
                // 1. 准备目录
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appDir = Path.Combine(appData, "TsubakiCursor");
                string cursorsDir = Path.Combine(appDir, "Cursors", theme.Id);
                string backupDir = Path.Combine(appDir, "Backup");

                Directory.CreateDirectory(cursorsDir);
                Directory.CreateDirectory(backupDir);

                // 2. 备份当前注册表
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

                // 3. 复制文件到应用目录并验证有效性
                var appliedFiles = new Dictionary<string, string>();
                foreach (var kvp in theme.CursorFiles)
                {
                    string source = kvp.Value;
                    string dest = Path.Combine(cursorsDir, Path.GetFileName(source));
                    File.Copy(source, dest, true);

                    // 验证：尝试加载
                    IntPtr hTest = LoadCursorFromFile(dest);
                    if (hTest == IntPtr.Zero)
                        throw new Exception($"文件验证失败: {Path.GetFileName(dest)}");
                    
                    // .cur 需要释放句柄，.ani 是系统共享句柄不释放
                    if (Path.GetExtension(dest).ToLower() == ".cur")
                        DestroyIcon(hTest);

                    appliedFiles[kvp.Key] = dest;
                }

                // 4. 构建新注册表值（只替换主题包含的，未包含的保持原样）
                var newValues = new Dictionary<string, string>(backup);
                foreach (var kvp in appliedFiles)
                {
                    newValues[kvp.Key] = kvp.Value;
                }

                // 5. 写入注册表
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", true))
                {
                    if (key == null)
                        throw new Exception("无法打开注册表键");

                    foreach (var kvp in newValues)
                    {
                        key.SetValue(kvp.Key, kvp.Value);
                    }
                }

                // 6. 注册到 Schemes
                string schemeValue = BuildSchemeValue(newValues);
                using (RegistryKey schemesKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors\Schemes", true))
                {
                    if (schemesKey != null)
                    {
                        schemesKey.SetValue(theme.Name, schemeValue);
                    }
                }

                // 7. 广播生效
                SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDCHANGE);

                // 8. 刷新状态与界面
                _currentAppliedThemeId = theme.Id;
                ScanLocalThemes();
                LoadCurrentCursors();
                SetActiveNav(NavUsing);
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

        // 按 Windows Schemes 格式构建逗号分隔的路径字符串
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

        // ========== 导航切换 ==========
        private void SetActiveNav(Border nav)
        {
            _activeNav.Background = Brushes.Transparent;
            ((TextBlock)_activeNav.Child).Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204));

            _activeNav = nav;
            _activeNav.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            ((TextBlock)_activeNav.Child).Foreground = Brushes.White;
        }

        private void SwitchPage(Grid page)
        {
            PageUsing.Visibility = Visibility.Collapsed;
            PageList.Visibility = Visibility.Collapsed;
            PageAbout.Visibility = Visibility.Collapsed;
            page.Visibility = Visibility.Visible;
        }

        private void NavUsing_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveNav(NavUsing);
            SwitchPage(PageUsing);
        }

        private void NavList_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveNav(NavList);
            SwitchPage(PageList);
        }

        private void NavAbout_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveNav(NavAbout);
            SwitchPage(PageAbout);
        }

        // ========== 点击"应用"按钮 ==========
        private void BtnApplyTheme_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ThemeInfo theme = button?.Tag as ThemeInfo;
            if (theme == null || theme.IsApplied) return;

            var result = MessageBox.Show(
                $"确定要应用主题「{theme.Name}」吗？\n作者：{theme.Author}\n包含 {theme.CursorFiles.Count} 个光标文件",
                "确认应用",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ApplyTheme(theme);
            }
        }
    }

    // ========== 数据类 ==========
    public class CursorInfo
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public BitmapSource Image { get; set; }
    }

    public class ThemeInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string FolderPath { get; set; }
        public Dictionary<string, string> CursorFiles { get; set; }
        public BitmapSource PreviewImage { get; set; }
        public bool IsApplied { get; set; }
        public string ButtonText => IsApplied ? "使用中" : "应用";
        public bool IsButtonEnabled => !IsApplied;
    }
}