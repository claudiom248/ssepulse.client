namespace SsePulse.Common;

public class CamelCasePolicy : INamingCasePolicy
{
    public string Apply(string pascalCaseName) => 
        string.IsNullOrEmpty(pascalCaseName) ? pascalCaseName : char.ToLowerInvariant(pascalCaseName[0]) + pascalCaseName.Substring(1);
}