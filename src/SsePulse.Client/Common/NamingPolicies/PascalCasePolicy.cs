namespace SsePulse.Client.Common.NamingPolicies;

internal class PascalCasePolicy : INamingCasePolicy
{
    public string Apply(string pascalCaseName) => pascalCaseName;
}