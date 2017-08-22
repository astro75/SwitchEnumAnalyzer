using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwitchEnum
{
    public sealed class SwitchInformation
    {
        public ImmutableArray<string> NotFoundSymbolNames { get; }
        public bool HasDefault { get; }
        public bool DefaultIsThrow { get; }

        public bool UnreachableDefault => HasDefault && NotFoundSymbolNames.Any() == false && !DefaultIsThrow;
        public bool NotExhaustiveSwitch => NotFoundSymbolNames.Any() && (!HasDefault || DefaultIsThrow);

        public SwitchInformation(ImmutableArray<string> notFoundSymbolNames, bool hasDefault, bool defaultIsThrow)
        {
            NotFoundSymbolNames = notFoundSymbolNames;
            HasDefault = hasDefault;
            DefaultIsThrow = defaultIsThrow;
        }
    }
}
