namespace SsePulse.Client.Internal;

internal class PascalCasePolicy : INamingCasePolicy
{
    public string Apply(string pascalCaseName) => pascalCaseName;
}