using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwitchEnum
{
    public class SwitchInformation
    {
        public List<string> NotFoundSymbolNames { get; set; } = new List<string>();
        public bool HasDefault { get; set; }
        public bool DefaultIsThrow { get; set; }
        public bool UnreachableDefault => HasDefault && NotFoundSymbolNames.Count == 0 && !DefaultIsThrow;
        public bool NotExhaustiveSwitch => NotFoundSymbolNames.Any() && (HasDefault || DefaultIsThrow);
    }
}
