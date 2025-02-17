// Copyright (c) 2011-2023 Roland Pheasant. All rights reserved.
// Roland Pheasant licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace DynamicData;

/// <summary>
/// Specifies which filter strategy should be used when the filter predicate is changed.
/// </summary>
public enum ListFilterPolicy
{
    /// <summary>
    /// Clear all items and replace with matches - optimised for large data sets.
    ///
    /// This option preserves order.
    /// </summary>
    ClearAndReplace,

    /// <summary>
    /// Calculate diff set - optimised for general filtering.
    ///
    /// This option does not preserve order.
    /// </summary>
    CalculateDiff
}
