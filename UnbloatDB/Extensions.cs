using System.Reflection;

namespace UnbloatDB;

public static class Extensions
{
    public static T[] RemoveAt<T>(this T[] source, int index)
    {
        var dest = new T[source.Length - 1];
        
        if (index > 0)
        {
            Array.Copy(source, 0, dest, 0, index);
        }

        if (index < source.Length - 1)
        {
            Array.Copy(source, index + 1, dest, index, source.Length - index - 1);
        }

        
        return dest;
    }
    
    public static async Task<object?> InvokeAsync(this MethodInfo @this, object obj, params object[] parameters)
    {
        var task = (Task) @this.Invoke(obj, parameters)!;
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }
    
}