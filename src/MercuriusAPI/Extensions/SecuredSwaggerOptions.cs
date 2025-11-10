namespace MercuriusAPI.Extensions;

public class SecuredSwaggerOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to include XML comments in the Swagger documentation.
    /// </summary>
    public bool IncludeXMLComments { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use enum schema filter in the Swagger documentation.
    /// </summary>
    public bool UseEnumSchemaFilter { get; set; }
}
