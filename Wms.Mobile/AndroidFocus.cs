namespace Wms.Mobile;

internal static class AndroidFocus
{
    public static void Suppress(VisualElement element)
    {
#if ANDROID
        if (element.Handler?.PlatformView is Android.Views.View view)
        {
            view.Focusable = false;
            view.FocusableInTouchMode = false;
        }
#endif
    }
}
