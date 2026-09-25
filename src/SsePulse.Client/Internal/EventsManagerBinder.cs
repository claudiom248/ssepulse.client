using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SsePulse.Client.Internal;

internal static class EventsManagerBinder
{
    private const string RequiresUnreferencedCodeMessage =
        "Binding an events manager discovers its handler methods by reflection, which the trimmer cannot see.";

    private const string RequiresDynamicCodeMessage =
        "Binding an events manager creates generic handler types at run time, which is not supported with native AOT.";

    public static bool IsHandlerName(string methodName)
    {
        return methodName.Length > 2
               && methodName.StartsWith("On", StringComparison.Ordinal)
               && char.IsUpper(methodName[2]);
    }

    public static void Validate(Type managerType, MethodInfo method)
    {
        string name = $"'{managerType.Name}.{method.Name}'";
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length is < 1 or > 2 || parameters.Length == 2 && parameters[1].ParameterType != typeof(CancellationToken))
        {
            throw new InvalidOperationException(
                $"{name} looks like an event handler because its name starts with 'On', " +
                $"but it has {parameters.Length} parameters. Event handlers must take the event data and, optionally, a {nameof(CancellationToken)}.");
        }

        if (method.ReturnType == typeof(void))
        {
            if (method.GetCustomAttribute<AsyncStateMachineAttribute>() is not null)
            {
                throw new InvalidOperationException(
                    $"{name} is an 'async void' event handler, whose completion and exceptions cannot be observed. Return a Task or a ValueTask instead.");
            }

            if (parameters.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{name} is a synchronous event handler and cannot take a {nameof(CancellationToken)}. Return a Task or a ValueTask instead.");
            }

            return;
        }

        if (method.ReturnType != typeof(Task) && method.ReturnType != typeof(ValueTask))
        {
            throw new InvalidOperationException(
                $"{name} looks like an event handler, but it returns '{method.ReturnType.Name}'. Event handlers must return void, Task or ValueTask.");
        }
    }

    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static void Register(SseHandlersDictionary handlers, string eventName, MethodInfo method, object manager)
    {
        Type dataType = method.GetParameters()[0].ParameterType;
        object handler = typeof(EventsManagerBinder)
            .GetMethod(nameof(CreateHandler), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(dataType)
            .Invoke(null, [method, manager])!;

        MethodInfo add = dataType == typeof(string)
            ? typeof(SseHandlersDictionary).GetMethod(nameof(SseHandlersDictionary.AddAsyncDataHandler))!
            : typeof(SseHandlersDictionary)
                .GetMethod(nameof(SseHandlersDictionary.AddAsyncStronglyTypedDataHandler))!
                .MakeGenericMethod(dataType);
        add.Invoke(handlers, [eventName, handler]);
    }

    private static Func<TData, CancellationToken, ValueTask> CreateHandler<TData>(MethodInfo method, object manager)
    {
        bool hasToken = method.GetParameters().Length == 2;
        if (method.ReturnType == typeof(void))
        {
            return HandlerAdapter.ToAsync(method.CreateDelegate<Action<TData>>(manager));
        }

        if (method.ReturnType == typeof(Task))
        {
            if (hasToken)
            {
                Func<TData, CancellationToken, Task> withToken = method.CreateDelegate<Func<TData, CancellationToken, Task>>(manager);
                return (data, token) => new ValueTask(withToken.Invoke(data, token));
            }

            Func<TData, Task> withoutToken = method.CreateDelegate<Func<TData, Task>>(manager);
            return (data, _) => new ValueTask(withoutToken.Invoke(data));
        }

        return hasToken
            ? method.CreateDelegate<Func<TData, CancellationToken, ValueTask>>(manager)
            : HandlerAdapter.ToAsync(method.CreateDelegate<Func<TData, ValueTask>>(manager));
    }
}
