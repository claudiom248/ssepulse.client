namespace SsePulse.Client.Common.NamingPolicies;

internal class SnakeCasePolicy : INamingCasePolicy
{
    public string Apply(string pascalCaseName) => System.Text.RegularExpressions.Regex.Replace(pascalCaseName, @"(?<!^)([A-Z])", "_$1").ToLower();
}