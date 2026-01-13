using System.Globalization;
using System.Windows.Media;
using System.Windows;

namespace NotepadEx.Util;

public static class AppResourceUtil
{
    public static bool TrySetResource(Application app, string path, object value)
    {
        if(app == null) return false;
        app.Resources[path] = value;
        return true;
    }
}

public static class AppResourceUtil<T>
{
    public static bool TrySetResource<T>(Application app, string path, T value)
    {
        if(app == null) return false;
        app.Resources[path] = value;
        return true;
    }

    public static T TryGetResource(Application app, string path)
    {
        if(app == null) return default;

        var resource = app.Resources[path];
        if(resource is T typedResource)
        {
            return typedResource;
        }
        return default;
    }
}