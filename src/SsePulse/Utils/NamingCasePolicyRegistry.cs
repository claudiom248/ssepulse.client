using SsePulse.Common;

namespace SsePulse.Utils;

internal class NamingCasePolicyRegistry
{
    private static readonly Dictionary<NameCasePolicy, INamingCasePolicy> Policies = new()
    {
        { NameCasePolicy.PascalCase, new PascalCasePolicy() },
        { NameCasePolicy.CamelCase, new CamelCasePolicy() },
        { NameCasePolicy.SnakeCase, new SnakeCasePolicy() },
        { NameCasePolicy.KebabCase, new KebabCasePolicy() }
    };
    
    public static INamingCasePolicy GetPolicy(NameCasePolicy policy) => Policies[policy];
}