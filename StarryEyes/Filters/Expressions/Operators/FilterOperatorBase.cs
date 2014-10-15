﻿using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Filters.Expressions.Operators
{
    // ReSharper disable MemberCanBeProtected.Global
    public abstract class FilterOperatorBase : FilterExpressionBase
    {
        protected abstract string OperatorString { get; }

        public abstract IEnumerable<FilterExpressionType> SupportedTypes { get; }

        public virtual Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            this.ThrowMismatchType(FilterExpressionType.Boolean);
            return null;//unreachable code
        }

        public virtual Func<TwitterStatus, long> GetNumericValueProvider()
        {
            this.ThrowMismatchType(FilterExpressionType.Numeric);
            return null;//unreachable code
        }

        public virtual Func<TwitterStatus, string> GetStringValueProvider()
        {
            this.ThrowMismatchType(FilterExpressionType.String);
            return null;//unreachable code
        }

        public virtual Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            this.ThrowMismatchType(FilterExpressionType.Set);
            return null;//unreachable code
        }

        public virtual string GetBooleanSqlQuery()
        {
            this.ThrowMismatchType(FilterExpressionType.Boolean);
            return null;//unreachable code
        }

        public virtual string GetNumericSqlQuery()
        {
            this.ThrowMismatchType(FilterExpressionType.Numeric);
            return null;//unreachable code
        }

        public virtual string GetStringSqlQuery()
        {
            this.ThrowMismatchType(FilterExpressionType.String);
            return null;//unreachable code
        }

        public virtual string GetSetSqlQuery()
        {
            this.ThrowMismatchType(FilterExpressionType.Set);
            return null;//unreachable code
        }

        public abstract StringComparison GetStringComparison();

        protected void ThrowMismatchType(FilterExpressionType type)
        {
            throw FilterQueryException.CreateUnsupportedType(this.OperatorString, type, this.ToQuery());
        }

        protected void ThrowMismatchType(params FilterExpressionType[] types)
        {
            throw FilterQueryException.CreateUnsupportedType(this.OperatorString, types, this.ToQuery());
        }
    }

    public abstract class FilterSingleValueOperator : FilterOperatorBase
    {
        private FilterOperatorBase _value;
        public FilterOperatorBase Value
        {
            get { return _value; }
            set
            {
                if (_value != null)
                    _value.InvalidateRequested -= this.RaiseInvalidateFilter;
                _value = value;
                if (_value != null)
                    _value.InvalidateRequested += this.RaiseInvalidateFilter;
            }
        }

        public override void BeginLifecycle()
        {
            if (Value != null)
            {
                Value.BeginLifecycle();
            }
        }

        public override void EndLifecycle()
        {
            if (Value != null)
            {
                Value.EndLifecycle();
            }
        }

        public override StringComparison GetStringComparison()
        {
            return Value.GetStringComparison();
        }
    }

    public abstract class FilterTwoValueOperator : FilterOperatorBase
    {
        private FilterOperatorBase _leftValue;
        private FilterOperatorBase _rightValue;

        public FilterOperatorBase LeftValue
        {
            get { return _leftValue; }
            set
            {
                if (_leftValue != null)
                    _leftValue.InvalidateRequested -= this.RaiseInvalidateFilter;
                _leftValue = value;
                if (_leftValue != null)
                    _leftValue.InvalidateRequested += this.RaiseInvalidateFilter;
            }
        }

        public FilterOperatorBase RightValue
        {
            get { return _rightValue; }
            set
            {
                if (_rightValue != null)
                    _rightValue.InvalidateRequested -= this.RaiseInvalidateFilter;
                _rightValue = value;
                if (_rightValue != null)
                    _rightValue.InvalidateRequested += this.RaiseInvalidateFilter;
            }
        }

        public override string ToQuery()
        {
            return LeftValue.ToQuery() + " " + OperatorString + " " + RightValue.ToQuery();
        }

        public override void BeginLifecycle()
        {
            LeftValue.BeginLifecycle();
            RightValue.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            LeftValue.EndLifecycle();
            RightValue.EndLifecycle();
        }

        public override StringComparison GetStringComparison()
        {
            return LeftValue.GetStringComparison() == StringComparison.CurrentCulture
                || RightValue.GetStringComparison() == StringComparison.CurrentCulture
                ? StringComparison.CurrentCulture
                : StringComparison.CurrentCultureIgnoreCase;
        }
    }
    // ReSharper restore MemberCanBeProtected.Global
}
