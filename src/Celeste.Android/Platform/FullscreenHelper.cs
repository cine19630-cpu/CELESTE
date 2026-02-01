using Android.App;
using Android.OS;
using Android.Views;

using Celeste.Porting;

namespace celestegame.app;

public static class FullscreenHelper
{
    public static void ApplyImmersive(Activity activity, ILogger log)
    {
        try
        {
            if (activity?.Window == null)
                return;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                var controller = activity.Window.InsetsController;
                if (controller != null)
                {
                    controller.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
                    controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                }
            }
            else
            {
                var decor = activity.Window.DecorView;
                if (decor != null)
                {
                    var flags = (StatusBarVisibility)(
                        SystemUiFlags.LayoutStable
                        | SystemUiFlags.LayoutHideNavigation
                        | SystemUiFlags.LayoutFullscreen
                        | SystemUiFlags.HideNavigation
                        | SystemUiFlags.Fullscreen
                        | SystemUiFlags.ImmersiveSticky);

                    decor.SystemUiVisibility = flags;
                }
            }

            log?.Info("CELESTE/FULLSCREEN", "APPLY_IMMERSIVE_OK");
        }
        catch (System.Exception ex)
        {
            log?.Exception("CELESTE/FULLSCREEN", ex, "APPLY_IMMERSIVE_FAIL");
        }
    }
}
