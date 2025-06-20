using System;

namespace MicroAPI
{
    /// <summary>
    /// Marks a class or interface for generating a controller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    public class HttpFacadeAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the controller to generate.
        /// </summary>
        public string? ControllerName { get; }

        /// <summary>
        /// Gets or sets the service type that the facade implements.
        /// This is only needed when applying the attribute to a class that doesn't implement the service interface directly.
        /// </summary>
        public Type? Service { get; set; }

        /// <summary>
        /// Gets or sets the namespace for the generated controller.
        /// If not specified, the controller will be generated in the same namespace as the facade with ".Controllers" appended.
        /// </summary>
        public string? ControllerNamespace { get; set; }

        /// <summary>
        /// Gets or sets the namespace for the generated DTOs.
        /// If not specified, DTOs will be generated in the same namespace as the controller.
        /// </summary>
        public string? DtoNamespace { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpFacadeAttribute"/> class.
        /// </summary>
        public HttpFacadeAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpFacadeAttribute"/> class.
        /// </summary>
        /// <param name="controllerName">The name of the controller to generate.</param>
        public HttpFacadeAttribute(string controllerName)
        {
            ControllerName = controllerName;
        }
    }


}
