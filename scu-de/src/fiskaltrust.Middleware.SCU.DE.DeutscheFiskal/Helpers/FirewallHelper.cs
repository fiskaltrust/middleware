using System.Linq;
using WindowsFirewallHelper;
using WindowsFirewallHelper.FirewallAPIv2.Rules;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers
{
    public class FirewallHelper
    {
        internal bool RuleExists(string ruleName, string executablePath)
            => FirewallManager.Instance.Rules.OfType<StandardRule>().Any(x => x.Name == ruleName && x.ApplicationName== NormalizePath(executablePath));

        internal void Allow(string ruleName, string javaPath)
        {
            var rule = FirewallManager.Instance.CreateApplicationRule(FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public, 
                ruleName, FirewallAction.Allow, NormalizePath(javaPath), FirewallProtocol.Any);
            rule.Direction = FirewallDirection.Inbound;
            FirewallManager.Instance.Rules.Add(rule);
        }

        private string NormalizePath(string path) => path.Replace("/", "\\");
    }
}
