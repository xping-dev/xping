// SPDX-License-Identifier: MIT
// Copyright (c) Xping

using System.Text.RegularExpressions;
using Xping.Core.Rules;

namespace Xping.Core.Tests.Rules;

public sealed class RuleIdsTests
{
    [Fact]
    public void RuleIdsAreUnique() => Assert.Equal(RuleIds.All.Count, RuleIds.All.Distinct(StringComparer.Ordinal).Count());

    [Fact]
    public void RuleIdsLookLikeXpFollowedByThreeDigits() =>
        Assert.All(RuleIds.All, id => Assert.Matches(new Regex("^XP[0-9]{3}$"), id));
}
