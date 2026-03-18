using SsePulse.Common;

namespace SsePulse.Utils;

public class NamingCasePolicyRegistry
{
    private static readonly Dictionary<NameCasePolicy, INamingCasePolicy> Policies = new()
    {
        { NameCasePolicy.PascalCase, new PascalCasePolicy() },
        { NameCasePolicy.CamelCase, new CamelCasePolicy() },
        { NameCasePolicy.SnakeCase, new SnakeCasePolicy() }
    };
    
    public static INamingCasePolicy GetPolicy(NameCasePolicy policy) => Policies[policy];
}