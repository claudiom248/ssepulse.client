namespace SsePulse.Common;

internal interface INamingCasePolicy
{
    string Apply(string pascalCaseName);
}