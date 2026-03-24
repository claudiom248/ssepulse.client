namespace SsePulse.Client.Common.NamingPolicies;

internal interface INamingCasePolicy
{
    string Apply(string pascalCaseName);
}