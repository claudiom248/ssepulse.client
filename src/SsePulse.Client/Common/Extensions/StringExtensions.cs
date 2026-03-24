using SsePulse.Client.Common.NamingPolicies;
using SsePulse.Client.Utils;

namespace SsePulse.Client.Common.Extensions;

internal static class StringExtensions
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
        
        public string ToKebabCase()
        {
            return NamingCasePolicyRegistry.GetPolicy(NameCasePolicy.KebabCase).Apply(@this);
        }


        public string ApplyNamingCasePolicy(NameCasePolicy policy)
        {
            return NamingCasePolicyRegistry.GetPolicy(policy).Apply(@this);       
        }
    }
}