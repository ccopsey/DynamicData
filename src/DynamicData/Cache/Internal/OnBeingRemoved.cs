// Copyright (c) 2011-2023 Roland Pheasant. All rights reserved.
// Roland Pheasant licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;

using DynamicData.Kernel;

namespace DynamicData.Cache.Internal;

internal sealed class OnBeingRemoved<TObject, TKey>
    where TObject : notnull
    where TKey : notnull
{
    private readonly Action<TObject> _removeAction;
    private readonly bool _invokeOnUnsubscribe;

    private readonly IObservable<IChangeSet<TObject, TKey>> _source;

    public OnBeingRemoved(IObservable<IChangeSet<TObject, TKey>> source, Action<TObject> removeAction, bool invokeOnUnsubscribe)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _removeAction = removeAction ?? throw new ArgumentNullException(nameof(removeAction));
        _invokeOnUnsubscribe = invokeOnUnsubscribe;
    }

    public IObservable<IChangeSet<TObject, TKey>> Run()
    {
        return Observable.Create<IChangeSet<TObject, TKey>>(
            observer =>
            {
                var locker = new object();
                var cache = new Cache<TObject, TKey>();
                var subscriber = _source.Synchronize(locker).Do(changes => RegisterForRemoval(changes, cache), observer.OnError).SubscribeSafe(observer);

                return Disposable.Create(
                    () =>
                    {
                        subscriber.Dispose();

                        lock (locker)
                        {
                            if (_invokeOnUnsubscribe)
                            {
                                cache.Items.ForEach(t => _removeAction(t));
                            }

                            cache.Clear();
                        }
                    });
            });
    }

    private void RegisterForRemoval(IChangeSet<TObject, TKey> changes, Cache<TObject, TKey> cache)
    {
        changes.ForEach(
            change =>
            {
                switch (change.Reason)
                {
                    case ChangeReason.Remove:
                        // ReSharper disable once InconsistentlySynchronizedField
                        _removeAction(change.Current);
                        break;
                }
            });
        cache.Clone(changes);
    }
}
