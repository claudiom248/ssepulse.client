namespace SsePulse.Client.Internal;

internal interface INamingCasePolicy
{
    string Apply(string pascalCaseName);
}