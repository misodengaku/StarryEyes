﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;
using System.Windows.Threading;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Views.Utils;

namespace StarryEyes.Views.Behaviors
{
    public class TimelineScrollLockerBehavior : Behavior<ScrollViewer>
    {
        private const double TopLockThreshold = 0.9;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        /// <summary>
        /// Flag of scroll lock is enabled or disabled.
        /// </summary>
        public bool IsScrollLockEnabled
        {
            get { return (bool)GetValue(IsScrollLockEnabledProperty); }
            set
            {
                SetValue(IsScrollLockEnabledProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for flag of scroll lock is enabled or disabled.
        /// </summary>
        public static readonly DependencyProperty IsScrollLockEnabledProperty =
            DependencyProperty.Register("IsScrollLockEnabled", typeof(bool),
                                        typeof(TimelineScrollLockerBehavior), new PropertyMetadata(false));

        /// <summary>
        /// Flag of scroll lock is enabled only scroll position is not zero. <para />
        /// This property will be appled only IsScrollLockEnabled = true.
        /// </summary>
        public bool IsScrollLockOnlyScrolled
        {
            get { return (bool)GetValue(IsScrollLockOnlyScrolledProperty); }
            set { SetValue(IsScrollLockOnlyScrolledProperty, value); }
        }

        /// <summary>
        /// Dependency property for flag of scroll lock is enable only scroll position is not zero.
        /// </summary>
        public static readonly DependencyProperty IsScrollLockOnlyScrolledProperty =
            DependencyProperty.Register("IsScrollLockOnlyScrolled", typeof(bool), typeof(TimelineScrollLockerBehavior),
                new PropertyMetadata(true));

        /// <summary>
        /// Displaying collection
        /// </summary>
        public IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        /// <summary>
        /// Dependency property for displaying collection
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IList), typeof(TimelineScrollLockerBehavior),
                                        new PropertyMetadata(null, ItemsSourceChanged));

        private static void ItemsSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var behavior = dependencyObject as TimelineScrollLockerBehavior;
            // sender is ScollLockerBehavior and that is attached.
            if (behavior != null && behavior.AssociatedObject != null)
            {
                // call ItemsSourceChanged on the instance.
                behavior.ItemsSourceChanged();
            }
        }

        /// <summary>
        /// Flag of scroll animation is enabled or disabled
        /// </summary>
        public bool IsAnimationEnabled
        {
            get { return (bool)GetValue(IsAnimationEnabledProperty); }
            set { SetValue(IsAnimationEnabledProperty, value); }
        }

        /// <summary>
        /// Dependency property for flag of scroll animation is enabled or disabled.
        /// </summary>
        public static readonly DependencyProperty IsAnimationEnabledProperty =
            DependencyProperty.Register("IsAnimationEnabled", typeof(bool), typeof(TimelineScrollLockerBehavior), new PropertyMetadata(true));

        // internal caches
        private readonly Queue<double> _scrollOffsetQueue = new Queue<double>();
        private readonly Timer _scrollTimer;
        private double _lastScrollOffset;
        private int _previousItemCount;
        private IDisposable _itemSourceCollectionChangeListener;
        // ✨ the magic ✨
        private volatile bool _magicIgnoreUserScrollOnce;
        private volatile bool _isMagicIgnoreHolded;
        // wait for dispatcher
        private volatile bool _isDispatcherAffected;

        public TimelineScrollLockerBehavior()
        {
            if (DesignTimeUtil.IsInDesignMode) return;
            _scrollTimer = new Timer(
                _ => TimerCallback(), null,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);
            _isDispatcherAffected = true;
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            _disposables.Add(Disposable.Create(() =>
            {
                var disposable = Interlocked.Exchange(ref _itemSourceCollectionChangeListener, null);
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }));

            _disposables.Add(
                Observable.FromEventPattern<ScrollChangedEventHandler, ScrollChangedEventArgs>(
                    h => this.AssociatedObject.ScrollChanged += h,
                    h => this.AssociatedObject.ScrollChanged -= h)
                          .Select(p => p.EventArgs)
                          .Subscribe(this.ScrollChanged));
        }

        protected override void OnDetaching()
        {
            _disposables.Dispose();
            lock (_scrollOffsetQueue)
            {
                _scrollOffsetQueue.Clear();
            }
            this.StopScrollTimer();
        }

        /// <summary>
        /// Handle scroll changed
        /// </summary>
        /// <param name="e">scrollchanged event arguments</param>
        private void ScrollChanged(ScrollChangedEventArgs e)
        {
            // scroll events are caused by:
            // + priority high
            // * item addition -> hold position or execute animation if needed
            // * user scroll -> stop animation and reflect that immediately.
            // * scroll lock(animation) -> do nothing.
            // - priority low

            // get source
            var source = ItemsSource;
            if (source == null) return;

            var itemCount = source.Count;
            var verticalOffset = e.VerticalOffset;

            // ***** check item is added or not *****

            // check and update items count latch
            if (_previousItemCount != itemCount)
            {

                // caused by item addition?
                _previousItemCount = itemCount;

                // if scroll extent is not changed or shrinked, nothing to do.
                if (e.ExtentHeightChange <= 0)
                {
                    _lastScrollOffset = verticalOffset;
                    return;
                }

                // calculate position should scroll to.
                // e.VerticalOffset indicates illegal offset when timeline is invisible.
                // var prevPosition = e.VerticalOffset + e.ExtentHeightChange;
                var prevPosition = _lastScrollOffset + e.ExtentHeightChange;
                if (prevPosition > e.ExtentHeight)
                {
                    // too large to scroll -> use value from e.VerticalOffset
                    prevPosition = verticalOffset + e.ExtentHeightChange;
                }

                // scroll back to previous position

                // ScrollLock AND !ScrollLockOnlyScrolled -> Lock
                // ScrollLock AND ScrollLockOnlyScrolled AND _lastScrollOffset is not Zero -> Lock
                if (IsScrollLockEnabled && (!IsScrollLockOnlyScrolled || _lastScrollOffset > TopLockThreshold))
                {
                    if (IsScrollLockOnlyScrolled)
                    {
                        // when running animation, re-invoke animation from current position. 
                        lock (_scrollOffsetQueue)
                        {
                            if (_scrollOffsetQueue.Count > 0)
                            {
                                System.Diagnostics.Debug.WriteLine("* Currently scrolling -> update animation.");
                                // start animation with a ✨ magic ✨
                                this.RunAnimation(prevPosition, true);
                                return;
                            }
                        }
                    }
                    // simply scrolled to previous position
                    System.Diagnostics.Debug.WriteLine("* Lock executed. offset: " + prevPosition + " / vertical extent size: " + e.ExtentHeight);
                    ScrollToVerticalOffset(prevPosition);
                }
                else if (IsAnimationEnabled)
                {
                    System.Diagnostics.Debug.WriteLine("* Run animation! offset: " + prevPosition);
                    // animate to new position
                    this.RunAnimation(prevPosition);
                }
                else
                {
                    // simply update last offset.
                    _lastScrollOffset = verticalOffset;
                }
                return;
            }

            // ***** check is user scrolled or not ****

            // _lastScrollOffset != e.VerticalOffset
            if (Math.Abs(_lastScrollOffset - verticalOffset) > TopLockThreshold)
            {
                if (_magicIgnoreUserScrollOnce)
                {
                    // magic ignore once
                    if (!_isMagicIgnoreHolded)
                    {
                        _magicIgnoreUserScrollOnce = false;
                        System.Diagnostics.Debug.WriteLine(
                            "✨ MAGICAL IGNORE [RELEASE] ✨" +
                            " LSO:" + _lastScrollOffset + " / VO: " + verticalOffset);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "✨ MAGICAL IGNORE [HOLD] ✨" +
                            " LSO:" + _lastScrollOffset + " / VO: " + verticalOffset);
                    }
                    _lastScrollOffset = verticalOffset;
                    return;
                }
                System.Diagnostics.Debug.WriteLine(
                    "* User scroll detected." +
                    " LSO: " + _lastScrollOffset + " / VO: " + verticalOffset);
                // scrolled by user?
                // -> abort animation, exit immediately
                lock (_scrollOffsetQueue)
                {
                    if (_scrollOffsetQueue.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine(" -> Scroll stopped by user-interaction.");
                        _scrollOffsetQueue.Clear();
                    }
                }
                // write back last scroll offset and item count
                _lastScrollOffset = verticalOffset;
                _previousItemCount = itemCount;
                return;
            }

            // ***** or maybe caused by scroll animation *****
            // -> do nothing.

            return;
        }

        /// <summary>
        /// Reflect changing items source
        /// </summary>
        private void ItemsSourceChanged()
        {
            // capture current value
            var itemsSource = ItemsSource;

            // validate interface
            var nc = itemsSource as INotifyCollectionChanged;
            if (nc == null) return;

            // initialize count
            this._previousItemCount = itemsSource.Count;

            // create listener of timeline changes
            var listener = ListenCollectionChange(itemsSource);

            // swap old listener
            var disposable = Interlocked.Exchange(
                ref this._itemSourceCollectionChangeListener, listener);

            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Create listener for specified collection.
        /// </summary>
        /// <param name="source">notifiable collection(must be implemented INotifyCollectionChanged)</param>
        /// <returns>listener disposable</returns>
        private IDisposable ListenCollectionChange(IList source)
        {
            return ((INotifyCollectionChanged)source).ListenCollectionChanged(ev =>
            {
                if (this.AssociatedObject == null) return;
                if (ev.Action == NotifyCollectionChangedAction.Add)
                {
                    // get virtualizing stack panel
                    var vsp = this.AssociatedObject.FindVisualChild<VirtualizingStackPanel>();
                    if (vsp != null)
                    {
                        var index = vsp.ItemContainerGenerator
                                       .IndexFromGeneratorPosition(new GeneratorPosition(0, 0));
                        // check new index is newer than the bottom item of current viewport items.
                        if (ev.NewStartingIndex <= index)
                        {
                            return;
                        }
                    }
                }
                // we should not scroll -> update items count.
                this._previousItemCount = source.Count;
            });
        }

        /// <summary>
        /// Run scroll animation (to position 0).
        /// </summary>
        /// <param name="offset">beginning scroll offset</param>
        /// <param name="setMagicalIgnore">set magical ignore flag</param>
        private void RunAnimation(double offset, bool setMagicalIgnore = false)
        {
            // clear old animation queue
            lock (_scrollOffsetQueue)
            {
                _scrollOffsetQueue.Clear();
            }

            System.Diagnostics.Debug.WriteLine("# New animation started. VO: " + offset);

            if (setMagicalIgnore)
            {
                this._isMagicIgnoreHolded = true;
                _magicIgnoreUserScrollOnce = true;
            }

            // scroll to initial position (enforced)
            ScrollToVerticalOffset(offset);
            // above method should not call via dispatcher. It will causes flickers in timeline.


            System.Diagnostics.Debug.WriteLine("# Registering new animation. (magic ignore?" + setMagicalIgnore + ")");
            // create animation
            // callback method is called every 10 milliseconds.
            // scroll is should be completed in 60 msec.
            var scrollPerTick = offset / 6;
            lock (_scrollOffsetQueue)
            {
                for (; offset > 0; offset -= scrollPerTick)
                {
                    _scrollOffsetQueue.Enqueue(offset);
                }
                // ensure scroll to zero.
                _scrollOffsetQueue.Enqueue(0);
                RunScrollTimer();
            }
            this._isMagicIgnoreHolded = false;
        }

        private void RunScrollTimer()
        {
            _scrollTimer.Change(
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(10));
        }

        private void StopScrollTimer()
        {
            _scrollTimer.Change(
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Callback method.
        /// </summary>
        private void TimerCallback()
        {
            // run scroll animation

            double dequeuedOffset;
            var lastItemOfQueue = false;
            lock (_scrollOffsetQueue)
            {
                // dequeue next one
                if (_scrollOffsetQueue.Count == 0)
                {
                    StopScrollTimer();
                    System.Diagnostics.Debug.WriteLine("# Scroll completed. [magic ignore hold?" + _isMagicIgnoreHolded + "]");
                    // disable magical ignore
                    if (!this._isMagicIgnoreHolded)
                    {
                        this._magicIgnoreUserScrollOnce = false;
                    }

                    return;
                }
                dequeuedOffset = this._scrollOffsetQueue.Dequeue();
                if (_scrollOffsetQueue.Count == 0)
                {
                    lastItemOfQueue = true;
                }
            }

            // skip invocation if dispatcher is not responding 
            // (unless next target is not the final destination)
            if (!_isDispatcherAffected && !lastItemOfQueue)
            {
                return;
            }
            _isDispatcherAffected = false;

            // set scroll offset
            this.Dispatcher.InvokeAsync(() =>
            {
                ScrollToVerticalOffset(dequeuedOffset);
                _isDispatcherAffected = true;
            }, DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Scroll associated list to vertical offset.
        /// </summary>
        /// <param name="offset">target offset</param>
        /// <param name="caller">caller member name</param>
        private void ScrollToVerticalOffset(double offset, [CallerMemberName]string caller = "[not provided]")
        {
            System.Diagnostics.Debug.WriteLine("# Scroll to: " + offset + " by " + caller);
            this._lastScrollOffset = offset;
            if (offset < 1)
            {
                this.AssociatedObject.ScrollToTop();
            }
            else
            {
                this.AssociatedObject.ScrollToVerticalOffset(offset);
            }
        }
    }
}