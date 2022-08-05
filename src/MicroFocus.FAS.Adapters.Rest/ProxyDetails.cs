using System.Diagnostics.CodeAnalysis;

namespace MicroFocus.FAS.Adapters.Rest
{
    [SuppressMessage("ReSharper",
                     "ClassNeverInstantiated.Global",
                     Justification = "This is a configuration class instantiated by the configuration system")]
    public class ProxyDetails
    {
        public string Address { get; set; }

        public bool BypassOnLocal { get; set; }

        public IEnumerable<string>? BypassList { get; set; }

        public string? UserName { get; set; }

        public string? Password { get; set; }
    }
}
