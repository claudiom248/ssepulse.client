using SsePulse.Utils;

namespace SsePulse.Common.Extensions;

public static class StringExtensions
{
    extension(string @this)
    {
        public string ToCamelCase()
        {
            return NamingCasePolicyRegistry.GetPolicy(NameCasePolicy.CamelCase).Apply(@this);
        }

        public string ToSnakeCase()
        {
            return NamingCasePolicyRegistry.GetPolicy(NameCasePolicy.SnakeCase).Apply(@this);
        }

        public string ApplyNamingCasePolicy(NameCasePolicy policy)
        {
            return NamingCasePolicyRegistry.GetPolicy(policy).Apply(@this);       
        }
    }
}