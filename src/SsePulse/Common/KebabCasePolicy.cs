namespace SsePulse.Common;

public class KebabCasePolicy : INamingCasePolicy
{
    public string Apply(string pascalCaseName) => System.Text.RegularExpressions.Regex.Replace(pascalCaseName, @"(?<!^)([A-Z])", "-$1").ToLower();
}