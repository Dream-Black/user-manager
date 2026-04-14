using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectHub.Api.Filters;

/// <summary>
/// 忽略循环引用，避免 Swagger 生成失败
/// </summary>
public class IgnoreCyclicReferenceSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null)
            return;

        foreach (var property in schema.Properties)
        {
            // 跳过可能导致循环引用的属性
            if (property.Value.Type == "object" && property.Value.Reference != null)
            {
                // 检查是否是导航属性类型
                var typeName = property.Value.Reference.Id;
                if (IsNavigationProperty(context.Type, property.Key))
                {
                    // 替换为简单类型
                    property.Value.Type = "object";
                    property.Value.Reference = null;
                    property.Value.Properties = null;
                }
            }
        }
    }

    private bool IsNavigationProperty(Type modelType, string propertyName)
    {
        var properties = modelType.GetProperties()
            .Where(p => p.PropertyType.IsClass && p.PropertyType.Namespace != null 
                     && p.PropertyType.Namespace.StartsWith("ProjectHub"))
            .Select(p => p.Name.ToLower())
            .ToHashSet();
        
        return properties.Contains(propertyName.ToLower());
    }
}

/// <summary>
/// 自定义 JSON 序列化选项，用于处理循环引用
/// </summary>
public static class JsonOptions
{
    public static JsonSerializerOptions GetApiJsonOptions()
    {
        return new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}
