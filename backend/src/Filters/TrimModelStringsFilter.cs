using Microsoft.AspNetCore.Mvc.Filters;

namespace LogopedicBackend.Filters;

public class TrimModelStringsFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var value in context.ActionArguments.Values)
        {
            if (value is null)
            {
                continue;
            }

            var stringProperties = value.GetType()
                .GetProperties()
                .Where(p => p.PropertyType == typeof(string) && p is { CanRead: true, CanWrite: true });

            foreach (var stringProperty in stringProperties)
            {
                var str = (string?)stringProperty.GetValue(value);

                if (str is not null)
                {
                    stringProperty.SetValue(value, str.Trim());
                }
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}