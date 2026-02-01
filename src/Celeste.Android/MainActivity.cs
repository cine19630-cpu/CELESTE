using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Runtime;
using Android.Content.PM;
using Android.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Android;

using Celeste.Porting;
using CelesteGame = Celeste.Celeste;

namespace celestegame.app;

[Activity(
    Label = "CELESTE",
    MainLauncher = true,
    Icon = "@mipmap/ic_launcher",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.UiMode,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    LaunchMode = LaunchMode.SingleTask
)]
public class MainActivity : AndroidGameActivity
{
    private const string Tag = "CELESTE/ANDROID";
    private CelesteGame game;

    protected override void OnCreate(Bundle bundle)
    {
        // Logger precisa nascer antes de qualquer coisa que possa falhar.
        var paths = new AndroidPathsProvider(this);

        Directory.CreateDirectory(paths.BaseDataPath);
        Directory.CreateDirectory(paths.ContentPath);
        Directory.CreateDirectory(paths.LogsPath);
        Directory.CreateDirectory(paths.SavePath);

        var logger = new AndroidLogger(paths);
        PortServices.Paths = paths;
        PortServices.Logger = logger;
        PortServices.FileSystem = new RootRedirectFileSystem(paths, logger);

        // Boot manager (validação de Content + tela de erro/diagnóstico).
        PortServices.Boot = new BootManagerMonoGame(requireAudio: true);

        HookGlobalExceptions(logger);

        logger.Info(Tag, "SESSION_START");
        logger.Info(Tag, $"DEVICE={Build.Manufacturer} {Build.Model} API={Build.VERSION.SdkInt}");
        logger.Info(Tag, $"ABI={Android.OS.Build.SupportedAbis?[0]}");
        logger.Info(Tag, $"PATHS Base={paths.BaseDataPath}");
        logger.Info(Tag, $"PATHS Content={paths.ContentPath}");
        logger.Info(Tag, $"PATHS Logs={paths.LogsPath}");
        logger.Info(Tag, $"PATHS Save={paths.SavePath}");

        // Redireciona Console para o logger (útil para libs legadas).
        Console.SetOut(new LoggerTextWriter(logger, "CELESTE/CONSOLE", isError: false));
        Console.SetError(new LoggerTextWriter(logger, "CELESTE/CONSOLE", isError: true));

        base.OnCreate(bundle);

        // Tela cheia antes de apresentar view (evita "flash" de barras).
        FullscreenHelper.ApplyImmersive(this, logger);

        game = new CelesteGame();

        try { Microsoft.Xna.Framework.Input.Touch.TouchPanel.EnabledGestures = Microsoft.Xna.Framework.Input.Touch.GestureType.None; } catch { }

        var view = (View)game.Services.GetService(typeof(View));
        // Consome touch: sem overlay touch e sem mapeamento de toque.
        view?.SetOnTouchListener(new ConsumeAllTouchListener());

        SetContentView(view);
        game.Run();
    }

    protected override void OnResume()
    {
        base.OnResume();
        PortServices.Info(Tag, "LIFECYCLE OnResume");
        FullscreenHelper.ApplyImmersive(this, PortServices.Logger);
    }

    protected override void OnPause()
    {
        base.OnPause();
        PortServices.Info(Tag, "LIFECYCLE OnPause");
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        PortServices.Info(Tag, $"LIFECYCLE OnWindowFocusChanged focus={hasFocus}");
        if (hasFocus)
            FullscreenHelper.ApplyImmersive(this, PortServices.Logger);
    }

    private static void HookGlobalExceptions(AndroidLogger logger)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                logger.Exception(Tag, ex ?? new Exception("UnhandledException (non-Exception object)"), "AppDomain.UnhandledException");
                logger.Flush();
            }
            catch { }
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            try
            {
                logger.Exception(Tag, e.Exception, "TaskScheduler.UnobservedTaskException");
                logger.Flush();
            }
            catch { }
        };

        AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
        {
            try
            {
                logger.Exception(Tag, e.Exception, "AndroidEnvironment.UnhandledExceptionRaiser");
                logger.Flush();
            }
            catch { }
        };
    }
}

internal sealed class ConsumeAllTouchListener : Java.Lang.Object, View.IOnTouchListener
{
    public bool OnTouch(View v, MotionEvent e) => true; // consome tudo
}
