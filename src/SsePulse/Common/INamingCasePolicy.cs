namespace SsePulse.Common;

public interface INamingCasePolicy
{
    string Apply(string pascalCaseName);
}