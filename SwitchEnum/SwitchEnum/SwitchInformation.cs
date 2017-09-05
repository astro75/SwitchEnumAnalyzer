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
        public bool DefaultThrows { get; }

        public bool UnreachableDefault => HasDefault && NotFoundSymbolNames.Any() == false && !DefaultThrows;
        public bool NotExhaustiveSwitch => NotFoundSymbolNames.Any() && (!HasDefault || DefaultThrows);

        public SwitchInformation(ImmutableArray<string> notFoundSymbolNames, bool hasDefault, bool defaultThrows)
        {
            NotFoundSymbolNames = notFoundSymbolNames;
            HasDefault = hasDefault;
            DefaultThrows = defaultThrows;
        }
    }
}
