﻿// Copyright (c) 2011-2023 Roland Pheasant. All rights reserved.
// Roland Pheasant licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using DynamicData.Kernel;

namespace DynamicData.Cache.Internal;

internal class GroupOnPropertyWithImmutableState<TObject, TKey, TGroup>
    where TObject : INotifyPropertyChanged
    where TKey : notnull
    where TGroup : notnull
{
    private readonly Func<TObject, TGroup> _groupSelector;

    private readonly Expression<Func<TObject, TGroup>> _propertySelector;

    private readonly IScheduler _scheduler;

    private readonly IObservable<IChangeSet<TObject, TKey>> _source;

    private readonly TimeSpan? _throttle;

    public GroupOnPropertyWithImmutableState(IObservable<IChangeSet<TObject, TKey>> source, Expression<Func<TObject, TGroup>> groupSelectorKey, TimeSpan? throttle = null, IScheduler? scheduler = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _groupSelector = groupSelectorKey.Compile();
        _propertySelector = groupSelectorKey;
        _throttle = throttle;
        _scheduler = scheduler ?? Scheduler.Default;
    }

    public IObservable<IImmutableGroupChangeSet<TObject, TKey, TGroup>> Run()
    {
        return _source.Publish(
            shared =>
            {
                // Monitor explicit property changes
                var regrouper = shared.WhenValueChanged(_propertySelector, false).ToUnit();

                // add a throttle if specified
                if (_throttle is not null)
                {
                    regrouper = regrouper.Throttle(_throttle.Value, _scheduler);
                }

                // Use property changes as a trigger to re-evaluate Grouping
                return shared.GroupWithImmutableState(_groupSelector, regrouper);
            });
    }
}
