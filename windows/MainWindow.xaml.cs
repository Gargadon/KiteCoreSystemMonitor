using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;

namespace KiteCoreWindowsMonitor
{
    public class AppSettings
    {
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;
        public bool StartWithWindows { get; set; } = false;
        public string ActiveMascot { get; set; } = "tetsu";
        public string GlowColor { get; set; } = "#00F0FF";
        public bool ShowLabels { get; set; } = true;
        public bool PinnedToDesktop { get; set; } = true;
        public string Language { get; set; } = "es";
        public double Opacity { get; set; } = 0.6;
    }

    public sealed partial class MainWindow : Window
    {
        // Win32 API Imports for Dragging & TopMost
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 2;

        // P/Invoke for pinning to desktop (WorkerW/Progman)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        // Native Performance Metrics struct & imports for CPU/RAM
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        // Core variables
        private DispatcherTimer _timer;
        private string _activeMascot = "tetsu"; // "tetsu" or "hakka"
        private bool _showLabels = true;
        private SolidColorBrush? _activeCoreColor;
        private string _glowColorHex = "#00F0FF";
        private bool _startWithWindows = false;
        private int _savedX = -1;
        private int _savedY = -1;
        private double _bgOpacity = 0.6;

        // CPU tracking
        private ulong _prevIdleTime = 0;
        private ulong _prevKernelTime = 0;
        private ulong _prevUserTime = 0;

        public MainWindow()
        {
            this.InitializeComponent();

            // Load settings first
            LoadSettings();

            // Set custom core glow color from settings
            SetCoreColorByHex(_glowColorHex);

            // Set window title and customize titlebar
            this.Title = "Kite Core System Monitor";
            
            // Adjust Window layout to borderless widget style
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                appWindow.Resize(new Windows.Graphics.SizeInt32(250, 270));

                // Restore position if valid
                if (_savedX != -1 && _savedY != -1)
                {
                    appWindow.Move(new Windows.Graphics.PointInt32(_savedX, _savedY));
                }
                
                // Set custom icon
                string iconPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }
                
                var presenter = appWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsAlwaysOnTop = true;
                    presenter.IsResizable = false;
                    presenter.SetBorderAndTitleBar(false, false);
                }
            }

            // Start smooth core spinning animation
            StartRotationAnimation();

            // Setup polling timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1000);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // First run tick
            UpdateSystemStats();

            // Apply saved language
            ApplyLanguage(_currentLanguage);

            // Apply saved desktop mode
            SetDesktopMode(_isPinnedToDesktop);

            // Set labels visibility
            ToggleLabelsItem.IsChecked = _showLabels;
            ExtraInfoRow.Visibility = _showLabels ? Visibility.Visible : Visibility.Collapsed;

            // Set mascot check states
            MascotTetsu.IsChecked = _activeMascot == "tetsu";
            MascotHakka.IsChecked = _activeMascot == "hakka";

            // Set start with Windows check state
            StartWithWindowsItem.IsChecked = _startWithWindows;

            // Set background opacity
            SetOpacity(_bgOpacity);

            // Subscribe to Closed event to save settings
            this.Closed += MainWindow_Closed;
        }

        private void StartRotationAnimation()
        {
            var anim = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(8)),
                RepeatBehavior = RepeatBehavior.Forever
            };
            var sb = new Storyboard();
            sb.Children.Add(anim);
            Storyboard.SetTarget(anim, CoreRingRotation);
            Storyboard.SetTargetProperty(anim, "Angle");
            sb.Begin();
        }

        private void Timer_Tick(object? sender, object e)
        {
            UpdateSystemStats();
        }

        private void UpdateSystemStats()
        {
            double cpuUsage = GetCpuUsage();
            double ramUsage = GetRamUsage();

            // Update texts
            CpuText.Text = $"{Math.Round(cpuUsage * 100)}% CPU";
            RamText.Text = $"{Math.Round(ramUsage * 100)}% RAM";

            // Update text colors based on threshold
            CpuText.Foreground = cpuUsage > 0.85 
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 239, 68, 68)) // Red
                : new SolidColorBrush(Colors.White);

            // Update Arc Gauges
            UpdateArcGeometry(CpuPathFigure, CpuArcSegment, cpuUsage, 76, 135, 162);
            UpdateArcGeometry(RamPathFigure, RamArcSegment, ramUsage, 76, 315, 162);

            // Update Mascot State
            string state = "idle";
            if (cpuUsage > 0.85)
            {
                state = "panic";
            }
            else if (cpuUsage > 0.50)
            {
                state = "focused";
            }

            string uriStr = $"ms-appx:///Assets/{_activeMascot}_{state}.webp";
            MascotSprite.Source = new BitmapImage(new Uri(uriStr));
        }

        private void UpdateArcGeometry(PathFigure figure, ArcSegment arc, double percentage, double radius, double startAngleDegrees, double sweepAngleMaxDegrees)
        {
            // Center is (100, 100) inside our 200x200 coordinate space
            double centerX = 100;
            double centerY = 100;

            double angleDegrees = startAngleDegrees + (percentage * sweepAngleMaxDegrees);
            double angleRadians = Math.PI * angleDegrees / 180.0;
            double startRadians = Math.PI * startAngleDegrees / 180.0;

            double startX = centerX + radius * Math.Cos(startRadians);
            double startY = centerY + radius * Math.Sin(startRadians);

            double endX = centerX + radius * Math.Cos(angleRadians);
            double endY = centerY + radius * Math.Sin(angleRadians);

            figure.StartPoint = new Point(startX, startY);
            arc.Point = new Point(endX, endY);
            arc.Size = new Size(radius, radius);
            arc.IsLargeArc = (percentage * sweepAngleMaxDegrees) > 180.0;
        }

        private double GetCpuUsage()
        {
            if (!GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime))
            {
                return 0.0;
            }

            ulong currentIdle = ((ulong)idleTime.dwHighDateTime << 32) | idleTime.dwLowDateTime;
            ulong currentKernel = ((ulong)kernelTime.dwHighDateTime << 32) | kernelTime.dwLowDateTime;
            ulong currentUser = ((ulong)userTime.dwHighDateTime << 32) | userTime.dwLowDateTime;

            if (_prevIdleTime == 0 && _prevKernelTime == 0 && _prevUserTime == 0)
            {
                _prevIdleTime = currentIdle;
                _prevKernelTime = currentKernel;
                _prevUserTime = currentUser;
                return 0.0;
            }

            ulong idleDiff = currentIdle - _prevIdleTime;
            ulong kernelDiff = currentKernel - _prevKernelTime;
            ulong userDiff = currentUser - _prevUserTime;

            _prevIdleTime = currentIdle;
            _prevKernelTime = currentKernel;
            _prevUserTime = currentUser;

            ulong totalDiff = kernelDiff + userDiff;
            if (totalDiff == 0) return 0.0;

            // CPU Usage = (Total - Idle) / Total
            double usage = (double)(totalDiff - idleDiff) / totalDiff;
            return Math.Clamp(usage, 0.0, 1.0);
        }

        private double GetRamUsage()
        {
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                double total = memStatus.ullTotalPhys;
                double avail = memStatus.ullAvailPhys;
                return (total - avail) / total;
            }
            return 0.0;
        }

        private void SetCoreColor(Windows.UI.Color color)
        {
            _activeCoreColor = new SolidColorBrush(color);
            
            // Apply color to CPU Arc and highlight ring
            if (CpuPath != null) CpuPath.Stroke = _activeCoreColor;
            if (OuterHighlightRing != null) OuterHighlightRing.Stroke = _activeCoreColor;
            if (StatusText != null) StatusText.Foreground = _activeCoreColor;

            // Apply RAM color (Emerald Green)
            var ramColor = ColorHelper.FromArgb(255, 16, 185, 129);
            if (RamPath != null) RamPath.Stroke = new SolidColorBrush(ramColor);
        }

        // Drag window implementation
        private void DragWindow_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint(sender as UIElement).Properties;
            if (properties.IsLeftButtonPressed)
            {
                var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
                ReleaseCapture();
                SendMessage(windowHandle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            }
        }

        // Context menu Handlers
        private void MascotTetsu_Click(object sender, RoutedEventArgs e)
        {
            _activeMascot = "tetsu";
            MascotTetsu.IsChecked = true;
            MascotHakka.IsChecked = false;
            UpdateSystemStats();
        }

        private void MascotHakka_Click(object sender, RoutedEventArgs e)
        {
            _activeMascot = "hakka";
            MascotTetsu.IsChecked = false;
            MascotHakka.IsChecked = true;
            UpdateSystemStats();
        }

        private void ColorCyan_Click(object sender, RoutedEventArgs e) => SetCoreColorByHex("#00F0FF");
        private void ColorPurple_Click(object sender, RoutedEventArgs e) => SetCoreColorByHex("#A855F7");
        private void ColorEmerald_Click(object sender, RoutedEventArgs e) => SetCoreColorByHex("#10B981");
        private void ColorAmber_Click(object sender, RoutedEventArgs e) => SetCoreColorByHex("#F59E0B");
        private void ColorRose_Click(object sender, RoutedEventArgs e) => SetCoreColorByHex("#F43F5E");

        private void ToggleLabels_Click(object sender, RoutedEventArgs e)
        {
            _showLabels = !_showLabels;
            ToggleLabelsItem.IsChecked = _showLabels;
            ExtraInfoRow.Visibility = _showLabels ? Visibility.Visible : Visibility.Collapsed;
        }

        private string _currentLanguage = "es";
        private bool _isPinnedToDesktop = false;

        private void ApplyLanguage(string lang)
        {
            _currentLanguage = lang;

            // Translation tables
            string mascotText = "Mascot";
            string tetsuName = "Tetsuya \"Tetsu\" Kurobane";
            string hakkaName = "Souta \"Hakka\" Aoyama";
            string colorText = "Core Glow Color";
            string cyanText = "Cyan (Default)";
            string purpleText = "Purple";
            string emeraldText = "Emerald";
            string amberText = "Amber";
            string roseText = "Rose";
            string showLabelsText = "Show Extra Labels";
            string pinDesktopText = "Pin to Desktop";
            string startWithWindowsText = "Start with Windows";
            string opacityMenuText = "Widget Opacity";
            string aboutText = "About Widget";
            string exitText = "Exit Widget";
            string languageText = "Language";
            string coreStatusText = "CORE STATUS: ONLINE";
            string systemText = "SYSTEM: ACTIVE";

            switch (lang)
            {
                case "es":
                    mascotText = "Mascota";
                    colorText = "Color del Brillo";
                    cyanText = "Cian (Por defecto)";
                    purpleText = "Púrpura";
                    emeraldText = "Esmeralda";
                    amberText = "Ámbar";
                    roseText = "Rosa";
                    showLabelsText = "Mostrar etiquetas";
                    pinDesktopText = "Fijar al escritorio";
                    startWithWindowsText = "Arrancar con Windows";
                    opacityMenuText = "Opacidad del widget";
                    aboutText = "Acerca del widget";
                    exitText = "Salir";
                    languageText = "Idioma";
                    coreStatusText = "ESTADO DEL NÚCLEO: EN LÍNEA";
                    systemText = "SISTEMA: ACTIVO";
                    break;
                case "fr":
                    mascotText = "Mascotte";
                    colorText = "Couleur du noyau";
                    cyanText = "Cyan (Par défaut)";
                    purpleText = "Violet";
                    emeraldText = "Émeraude";
                    amberText = "Ambre";
                    roseText = "Rosa";
                    showLabelsText = "Afficher les étiquettes";
                    pinDesktopText = "Épingler au bureau";
                    startWithWindowsText = "Démarrer avec Windows";
                    opacityMenuText = "Opacité du widget";
                    aboutText = "À propos du widget";
                    exitText = "Quitter le widget";
                    languageText = "Langue";
                    coreStatusText = "STATUT DU NOYAU: EN LIGNE";
                    systemText = "SYSTÈME: ACTIF";
                    break;
                case "it":
                    mascotText = "Mascotte";
                    colorText = "Colore del bagliore";
                    cyanText = "Ciano (Predefinito)";
                    purpleText = "Viola";
                    emeraldText = "Smeraldo";
                    amberText = "Ambra";
                    roseText = "Rosa";
                    showLabelsText = "Mostra etichette extra";
                    pinDesktopText = "Aggiungi al desktop";
                    startWithWindowsText = "Avvia con Windows";
                    opacityMenuText = "Opacità del widget";
                    aboutText = "Informazioni sul widget";
                    exitText = "Esci dal widget";
                    languageText = "Lingua";
                    coreStatusText = "STATO DEL NUCLEO: IN LINEA";
                    systemText = "SISTEMA: ATTIVO";
                    break;
                case "de":
                    mascotText = "Maskottchen";
                    colorText = "Kern-Glühfarbe";
                    cyanText = "Cyan (Standard)";
                    purpleText = "Lila";
                    emeraldText = "Smaragd";
                    amberText = "Bernstein";
                    roseText = "Rosa";
                    showLabelsText = "Zusatzlabels anzeigen";
                    pinDesktopText = "An Desktop anheften";
                    startWithWindowsText = "Mit Windows starten";
                    opacityMenuText = "Widget-Deckkraft";
                    aboutText = "Über das Widget";
                    exitText = "Widget beenden";
                    languageText = "Sprache";
                    coreStatusText = "KERN-STATUS: ONLINE";
                    systemText = "SYSTEM: AKTIV";
                    break;
                case "pt":
                    mascotText = "Mascote";
                    colorText = "Cor do brilho";
                    cyanText = "Ciano (Padrão)";
                    purpleText = "Roxo";
                    emeraldText = "Esmeralda";
                    amberText = "Âmbar";
                    roseText = "Rosa";
                    showLabelsText = "Mostrar etiquetas";
                    pinDesktopText = "Fixar na área de trabalho";
                    startWithWindowsText = "Iniciar con o Windows";
                    opacityMenuText = "Opacidade do widget";
                    aboutText = "Sobre o widget";
                    exitText = "Sair do widget";
                    languageText = "Idioma";
                    coreStatusText = "ESTADO DO NÚCLEO: ONLINE";
                    systemText = "SISTEMA: ATIVO";
                    break;
                case "zh":
                    mascotText = "吉祥物";
                    colorText = "核心发光颜色";
                    cyanText = "青色 (默认)";
                    purpleText = "紫色";
                    emeraldText = "翡翠绿";
                    amberText = "琥珀色";
                    roseText = "玫瑰红";
                    showLabelsText = "显示额外标签";
                    pinDesktopText = "固定到桌面";
                    startWithWindowsText = "随 Windows 启动";
                    opacityMenuText = "小部件不透明度";
                    aboutText = "关于小部件";
                    exitText = "退出小部件";
                    languageText = "语言";
                    coreStatusText = "核心状态: 在线";
                    systemText = "系统: 运行中";
                    tetsuName = "黑羽铁矢（铁）";
                    hakkaName = "青山聪太（黑客）";
                    break;
                case "ja":
                    mascotText = "マスコット";
                    colorText = "コアの発光色";
                    cyanText = "シアン (デフォルト)";
                    purpleText = "パープル";
                    emeraldText = "エメラルド";
                    amberText = "アンバー";
                    roseText = "ローズ";
                    showLabelsText = "追加のラベルを表示";
                    pinDesktopText = "デスクトップにピン留め";
                    startWithWindowsText = "Windows 起動時に実行";
                    opacityMenuText = "不透明度";
                    aboutText = "バージョン情報";
                    exitText = "終了";
                    languageText = "言語";
                    coreStatusText = "コアステータス: オンライン";
                    systemText = "システム: アクティブ";
                    tetsuName = "黒羽鉄矢（鉄）";
                    hakkaName = "青山聡太 （ハッカー）";
                    break;
                case "ko":
                    mascotText = "마스코트";
                    colorText = "코어 글로우 색상";
                    cyanText = "시안 (기본값)";
                    purpleText = "보라색";
                    emeraldText = "에메랄드";
                    amberText = "호박색";
                    roseText = "장미색";
                    showLabelsText = "추가 레이블 표시";
                    pinDesktopText = "바탕화면에 고정";
                    startWithWindowsText = "Windows 시작 시 실행";
                    opacityMenuText = "위젯 불투명도";
                    aboutText = "위젯 정보";
                    exitText = "위젯 종료";
                    languageText = "언어";
                    coreStatusText = "코어 상태: 온라인";
                    systemText = "시스템: 활성";
                    tetsuName = "쿠로바네 테츠야 (테츠)";
                    hakkaName = "아오야마 소우타 (해커)";
                    break;
            }

            // Update UI elements
            if (MascotMenu != null) MascotMenu.Text = mascotText;
            if (MascotTetsu != null) MascotTetsu.Text = tetsuName;
            if (MascotHakka != null) MascotHakka.Text = hakkaName;
            if (ColorMenu != null) ColorMenu.Text = colorText;
            if (ColorCyanItem != null) ColorCyanItem.Text = cyanText;
            if (ColorPurpleItem != null) ColorPurpleItem.Text = purpleText;
            if (ColorEmeraldItem != null) ColorEmeraldItem.Text = emeraldText;
            if (ColorAmberItem != null) ColorAmberItem.Text = amberText;
            if (ColorRoseItem != null) ColorRoseItem.Text = roseText;
            if (ToggleLabelsItem != null) ToggleLabelsItem.Text = showLabelsText;
            if (PinToDesktopItem != null) PinToDesktopItem.Text = pinDesktopText;
            if (StartWithWindowsItem != null) StartWithWindowsItem.Text = startWithWindowsText;
            if (OpacityMenu != null) OpacityMenu.Text = opacityMenuText;
            if (AboutItem != null) AboutItem.Text = aboutText;
            if (ExitItem != null) ExitItem.Text = exitText;
            if (LanguageMenu != null) LanguageMenu.Text = languageText;

            if (StatusText != null) StatusText.Text = coreStatusText;
            if (SubstatusText != null) SubstatusText.Text = systemText;

            // Update check states
            if (LangEnglish != null) LangEnglish.IsChecked = lang == "en";
            if (LangSpanish != null) LangSpanish.IsChecked = lang == "es";
            if (LangFrench != null) LangFrench.IsChecked = lang == "fr";
            if (LangItalian != null) LangItalian.IsChecked = lang == "it";
            if (LangGerman != null) LangGerman.IsChecked = lang == "de";
            if (LangPortuguese != null) LangPortuguese.IsChecked = lang == "pt";
            if (LangChinese != null) LangChinese.IsChecked = lang == "zh";
            if (LangJapanese != null) LangJapanese.IsChecked = lang == "ja";
            if (LangKorean != null) LangKorean.IsChecked = lang == "ko";
        }

        private void SetDesktopMode(bool pin)
        {
            _isPinnedToDesktop = pin;
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow == null) return;

            var presenter = appWindow.Presenter as OverlappedPresenter;

            if (pin)
            {
                // Disable AlwaysOnTop so it doesn't float over other apps
                if (presenter != null)
                {
                    presenter.IsAlwaysOnTop = false;
                }

                // Hide from taskbar and Alt+Tab switcher
                appWindow.IsShownInSwitchers = false;

                // Find the WorkerW or Progman window and set as parent
                IntPtr progman = FindWindow("Progman", null);
                IntPtr result = IntPtr.Zero;
                // Send 0x052C to Progman to spawn WorkerW
                SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, out result);

                IntPtr workerW = IntPtr.Zero;
                EnumWindows((toplevelHandle, lParam) =>
                {
                    IntPtr shell = FindWindowEx(toplevelHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (shell != IntPtr.Zero)
                    {
                        // The WorkerW window is the one immediately following the current toplevel
                        workerW = FindWindowEx(IntPtr.Zero, toplevelHandle, "WorkerW", null);
                    }
                    return true;
                }, IntPtr.Zero);

                // Fallback to progman if WorkerW is not found
                IntPtr parentHwnd = workerW != IntPtr.Zero ? workerW : progman;

                if (parentHwnd != IntPtr.Zero)
                {
                    SetParent(windowHandle, parentHwnd);
                }
            }
            else
            {
                // Restore top-level window
                SetParent(windowHandle, IntPtr.Zero);

                // Show in taskbar and Alt+Tab switcher
                appWindow.IsShownInSwitchers = true;

                // Restore floating always-on-top presenter
                if (presenter != null)
                {
                    presenter.IsAlwaysOnTop = true;
                }
            }

            if (PinToDesktopItem != null)
            {
                PinToDesktopItem.IsChecked = pin;
            }
        }

        private void PinToDesktop_Click(object sender, RoutedEventArgs e)
        {
            SetDesktopMode(!_isPinnedToDesktop);
        }

        private void LangEnglish_Click(object sender, RoutedEventArgs e) => ApplyLanguage("en");
        private void LangSpanish_Click(object sender, RoutedEventArgs e) => ApplyLanguage("es");
        private void LangFrench_Click(object sender, RoutedEventArgs e) => ApplyLanguage("fr");
        private void LangItalian_Click(object sender, RoutedEventArgs e) => ApplyLanguage("it");
        private void LangGerman_Click(object sender, RoutedEventArgs e) => ApplyLanguage("de");
        private void LangPortuguese_Click(object sender, RoutedEventArgs e) => ApplyLanguage("pt");
        private void LangChinese_Click(object sender, RoutedEventArgs e) => ApplyLanguage("zh");
        private void LangJapanese_Click(object sender, RoutedEventArgs e) => ApplyLanguage("ja");
        private void LangKorean_Click(object sender, RoutedEventArgs e) => ApplyLanguage("ko");

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            string title = "Kite Core System Monitor";
            string text;
            
            if (_currentLanguage == "es")
            {
                text = "Kite Core System Monitor v1.0.0\n" +
                       "Desarrollado por David Kantún\n\n" +
                       "Un monitor de recursos del sistema en tiempo real con Tetsu y Hakka.\n" +
                       "Repositorio: https://github.com/Gargadon/KiteCoreSystemMonitor";
            }
            else
            {
                text = "Kite Core System Monitor v1.0.0\n" +
                       "Developed by David Kantún\n\n" +
                       "A real-time system resource monitor widget featuring Tetsu and Hakka.\n" +
                       "Repository: https://github.com/Gargadon/KiteCoreSystemMonitor";
            }
            
            MessageBox(handle, text, title, 0x00000040); // MB_OK | MB_ICONINFORMATION
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            this.Close();
        }

        private void LoadSettings()
        {
            try
            {
                string appDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KiteCoreSystemMonitor");
                string settingsPath = System.IO.Path.Combine(appDataFolder, "settings.json");
                if (System.IO.File.Exists(settingsPath))
                {
                    string json = System.IO.File.ReadAllText(settingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        _savedX = settings.X;
                        _savedY = settings.Y;
                        _startWithWindows = settings.StartWithWindows;
                        _activeMascot = settings.ActiveMascot;
                        _glowColorHex = settings.GlowColor;
                        _showLabels = settings.ShowLabels;
                        _isPinnedToDesktop = settings.PinnedToDesktop;
                        _currentLanguage = settings.Language;
                        _bgOpacity = settings.Opacity;
                    }
                }
                
                _startWithWindows = GetStartWithWindows();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                string appDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KiteCoreSystemMonitor");
                if (!System.IO.Directory.Exists(appDataFolder))
                {
                    System.IO.Directory.CreateDirectory(appDataFolder);
                }
                string settingsPath = System.IO.Path.Combine(appDataFolder, "settings.json");

                int x = -1, y = -1;
                var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                if (appWindow != null)
                {
                    x = appWindow.Position.X;
                    y = appWindow.Position.Y;
                }

                var settings = new AppSettings
                {
                    X = x,
                    Y = y,
                    StartWithWindows = _startWithWindows,
                    ActiveMascot = _activeMascot,
                    GlowColor = _glowColorHex,
                    ShowLabels = _showLabels,
                    PinnedToDesktop = _isPinnedToDesktop,
                    Language = _currentLanguage,
                    Opacity = _bgOpacity
                };

                string json = System.Text.Json.JsonSerializer.Serialize(settings);
                System.IO.File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private void SetOpacity(double opacity)
        {
            _bgOpacity = opacity;
            if (MainBorder != null)
            {
                byte alpha = (byte)(opacity * 255);
                MainBorder.Background = new SolidColorBrush(ColorHelper.FromArgb(alpha, 18, 18, 18));
            }
            UpdateOpacityMenuChecks();
        }

        private void UpdateOpacityMenuChecks()
        {
            if (Opacity100 != null) Opacity100.IsChecked = Math.Abs(_bgOpacity - 1.0) < 0.05;
            if (Opacity80 != null) Opacity80.IsChecked = Math.Abs(_bgOpacity - 0.8) < 0.05;
            if (Opacity60 != null) Opacity60.IsChecked = Math.Abs(_bgOpacity - 0.6) < 0.05;
            if (Opacity40 != null) Opacity40.IsChecked = Math.Abs(_bgOpacity - 0.4) < 0.05;
            if (Opacity20 != null) Opacity20.IsChecked = Math.Abs(_bgOpacity - 0.2) < 0.05;
        }

        private void Opacity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleMenuFlyoutItem item)
            {
                if (item == Opacity100) SetOpacity(1.0);
                else if (item == Opacity80) SetOpacity(0.8);
                else if (item == Opacity60) SetOpacity(0.6);
                else if (item == Opacity40) SetOpacity(0.4);
                else if (item == Opacity20) SetOpacity(0.2);
            }
        }

        private void SetStartWithWindows(bool enable)
        {
            _startWithWindows = enable;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)!)
                {
                    if (enable)
                    {
                        key.SetValue("KiteCoreSystemMonitor", $"\"{Environment.ProcessPath}\"");
                    }
                    else
                    {
                        key.DeleteValue("KiteCoreSystemMonitor", false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set registry key: {ex.Message}");
            }
        }

        private bool GetStartWithWindows()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false)!)
                {
                    return key?.GetValue("KiteCoreSystemMonitor") != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private void StartWithWindows_Click(object sender, RoutedEventArgs e)
        {
            bool enable = StartWithWindowsItem.IsChecked;
            SetStartWithWindows(enable);
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            SaveSettings();
        }

        private void SetCoreColorByHex(string hex)
        {
            _glowColorHex = hex;
            switch (hex)
            {
                case "#A855F7":
                    SetCoreColor(ColorHelper.FromArgb(255, 168, 85, 247));
                    break;
                case "#10B981":
                    SetCoreColor(ColorHelper.FromArgb(255, 16, 185, 129));
                    break;
                case "#F59E0B":
                    SetCoreColor(ColorHelper.FromArgb(255, 245, 158, 11));
                    break;
                case "#F43F5E":
                    SetCoreColor(ColorHelper.FromArgb(255, 244, 63, 94));
                    break;
                default:
                    SetCoreColor(ColorHelper.FromArgb(255, 0, 240, 255));
                    break;
            }
        }
    }
}
