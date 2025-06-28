using System;

namespace MicroAPI
{
    /// <summary>
    /// Marks a class as a Data Transfer Object (Dto) and specifies the entity type it maps to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class DtoAttribute : Attribute
    {
        /// <summary>
        /// Gets the entity type that this Dto maps to.
        /// </summary>
        public Type EntityType { get; }

        /// <summary>
        /// Gets or sets the properties to ignore when generating the Dto.
        /// </summary>
        public string[]? IgnoredProperties { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DtoAttribute"/> class.
        /// </summary>
        /// <param name="entityType">The entity type that this Dto maps to.</param>
        public DtoAttribute(Type entityType)
        {
            EntityType = entityType;
        }
    }

    /// <summary>
    /// Marks a class as a Data Transfer Object (Dto) and specifies the entity type it maps to.
    /// </summary>
    /// <typeparam name="TEntity">The entity type that this Dto maps to.</typeparam>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class DtoAttribute<TEntity> : DtoAttribute where TEntity : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DtoAttribute{TEntity}"/> class.
        /// </summary>
        public DtoAttribute()
            : base(typeof(TEntity))
        {
        }
    }
}
