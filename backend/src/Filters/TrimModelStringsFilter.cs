using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LogopedicBackend.Filters;

public class TrimModelStringsFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (object? value in context.ActionArguments.Values)
        {
            if (value is null)
            {
                continue;
            }

            IEnumerable<PropertyInfo> stringProperties = value.GetType()
                .GetProperties()
                .Where(p => p.PropertyType == typeof(string) && p is { CanRead: true, CanWrite: true });

            foreach (PropertyInfo stringProperty in stringProperties)
            {
                string? str = (string?)stringProperty.GetValue(value);

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
