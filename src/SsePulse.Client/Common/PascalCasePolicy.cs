namespace SsePulse.Client.Common;

internal class PascalCasePolicy : INamingCasePolicy
{
    public string Apply(string pascalCaseName) => pascalCaseName;
}