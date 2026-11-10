using System.Text.Json;
using System.Text.Json.Nodes;
using Scriban;
using Scriban.Runtime;

namespace EurocatMenusPlugin;

/// <summary>
/// A text template from the configuration, in the Scriban syntax (for example, <c>{{ mode | string.slice 0 1 }}</c>).
/// The template gets the properties of a JSON object as variables.
/// </summary>
public class TextTemplate
{
    readonly Template _template;

    /// <exception cref="InvalidOperationException">The template is not correct.</exception>
    public TextTemplate(string text)
    {
        _template = Template.Parse(text);
        if (_template.HasErrors)
            throw new InvalidOperationException($"The template {text} is not correct: {string.Join("; ", _template.Messages)}");
    }

    /// <param name="data">The data for the template. If it is a JSON object, its properties are variables.</param>
    public string Render(JsonNode? data)
    {
        var variables = data is JsonObject jsonObject ? ToScriptObject(jsonObject) : new ScriptObject();

        // The context has no template loader. Thus, a template cannot include files.
        var context = new TemplateContext { LoopLimit = 1000 };
        context.PushGlobal(variables);
        return _template.Render(context);
    }

    static ScriptObject ToScriptObject(JsonObject jsonObject)
    {
        var scriptObject = new ScriptObject();
        foreach (var property in jsonObject)
            scriptObject[property.Key] = ToScriptValue(property.Value);

        return scriptObject;
    }

    static object? ToScriptValue(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return null;

            case JsonObject jsonObject:
                return ToScriptObject(jsonObject);

            case JsonArray jsonArray:
                var scriptArray = new ScriptArray();
                foreach (var item in jsonArray)
                    scriptArray.Add(ToScriptValue(item));
                return scriptArray;

            case JsonValue value:
                switch (value.GetValueKind())
                {
                    case JsonValueKind.String:
                        return value.GetValue<string>();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Number:
                        return value.TryGetValue<long>(out var integer) ? integer : value.GetValue<double>();
                    default:
                        return null;
                }

            default:
                return null;
        }
    }
}
