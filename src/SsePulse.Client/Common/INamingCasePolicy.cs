namespace SsePulse.Client.Common;

internal interface INamingCasePolicy
{
    string Apply(string pascalCaseName);
}