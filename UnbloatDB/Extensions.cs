using System.Reflection;

namespace UnbloatDB;

public static class Extensions
{
    /// <summary>
    /// Zero allocation array remove function
    /// </summary>
    public static T[] RemoveAt<T>(this T[] source, int index)
    {
        if (index > 0)
        {
            Array.Copy(source, 0, source, 0, index);
        }

        if (index < source.Length - 1)
        {
            Array.Copy(source, index + 1, source, index, source.Length - index - 1);
        }
    
        Array.Resize(ref source, source.Length - 1);
        
        return source;
    }
    
    public static async Task<object?> InvokeAsync(this MethodInfo @this, object instance, params object[] parameters)
    {
        var task = (Task) @this.Invoke(instance, parameters)!;
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }
}