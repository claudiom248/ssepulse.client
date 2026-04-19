namespace SsePulse.Client.Common.NamingPolicies;

/// <summary>
/// Controls how SSE event names are derived from handler method names (or from type names)
/// when no explicit name is provided.
/// </summary>
public enum NameCasePolicy
{
    /// <summary>Event names are formatted in PascalCase (e.g. <c>UserCreated</c>).</summary>
    PascalCase,
    /// <summary>Event names are formatted in camelCase (e.g. <c>userCreated</c>).</summary>
    CamelCase,
    /// <summary>Event names are formatted in snake_case (e.g. <c>user_created</c>).</summary>
    SnakeCase,
    /// <summary>Event names are formatted in kebab-case (e.g. <c>user-created</c>).</summary>
    KebabCase
}