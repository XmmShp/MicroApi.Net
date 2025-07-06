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
        /// Gets or sets the service type that the facade implements.
        /// This is only needed when applying the attribute to a class that doesn't implement the service interface directly.
        /// </summary>
        public Type? Service { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpFacadeAttribute"/> class.
        /// </summary>
        public HttpFacadeAttribute()
        {
        }
    }

    /// <summary>
    /// Marks a class or interface for generating a controller.
    /// </summary>
    /// <typeparam name="TService">The service type that the facade implements.</typeparam>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    public class HttpFacadeAttribute<TService> : HttpFacadeAttribute where TService : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpFacadeAttribute{TService}"/> class.
        /// </summary>
        public HttpFacadeAttribute()
        {
            Service = typeof(TService);
        }
    }
}
