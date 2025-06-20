using System;

namespace MicroAPI;

/// <summary>
/// Base class for HTTP method attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public abstract class HttpMethodAttributeBase : Attribute
{
    /// <summary>
    /// Gets or sets the name of the controller method to generate.
    /// If not specified, the original method name will be used.
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Gets or sets the route template for the endpoint.
    /// If not specified, the original method name will be used as the route.
    /// </summary>
    public string? Route { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMethodAttributeBase"/> class.
    /// </summary>
    protected HttpMethodAttributeBase()
    {
    }
}

/// <summary>
/// Marks a method to be exposed as an HTTP GET endpoint.
/// </summary>
public class GetAttribute : HttpMethodAttributeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAttribute"/> class.
    /// </summary>
    public GetAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAttribute"/> class with a custom route.
    /// </summary>
    /// <param name="route">The route template for the endpoint.</param>
    public GetAttribute(string route)
    {
        Route = route;
    }
}

/// <summary>
/// Marks a method to be exposed as an HTTP POST endpoint.
/// </summary>
public class PostAttribute : HttpMethodAttributeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostAttribute"/> class.
    /// </summary>
    public PostAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostAttribute"/> class with a custom route.
    /// </summary>
    /// <param name="route">The route template for the endpoint.</param>
    public PostAttribute(string route)
    {
        Route = route;
    }
}

/// <summary>
/// Marks a method to be exposed as an HTTP PUT endpoint.
/// </summary>
public class PutAttribute : HttpMethodAttributeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PutAttribute"/> class.
    /// </summary>
    public PutAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PutAttribute"/> class with a custom route.
    /// </summary>
    /// <param name="route">The route template for the endpoint.</param>
    public PutAttribute(string route)
    {
        Route = route;
    }
}

/// <summary>
/// Marks a method to be exposed as an HTTP DELETE endpoint.
/// </summary>
public class DeleteAttribute : HttpMethodAttributeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAttribute"/> class.
    /// </summary>
    public DeleteAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAttribute"/> class with a custom route.
    /// </summary>
    /// <param name="route">The route template for the endpoint.</param>
    public DeleteAttribute(string route)
    {
        Route = route;
    }
}

/// <summary>
/// Marks a method to be exposed as an HTTP PATCH endpoint.
/// </summary>
public class PatchAttribute : HttpMethodAttributeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PatchAttribute"/> class.
    /// </summary>
    public PatchAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PatchAttribute"/> class with a custom route.
    /// </summary>
    /// <param name="route">The route template for the endpoint.</param>
    public PatchAttribute(string route)
    {
        Route = route;
    }
}
