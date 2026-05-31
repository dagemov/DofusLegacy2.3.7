using System;

namespace Sunshine.Protocol.Utils
{
    public sealed class ObjectValidator<T>
    {
        private readonly object m_sync = new object();
        private bool m_isValid;
        private T m_instance;
        private readonly Func<T> m_creator;

        public event Action<ObjectValidator<T>> ObjectInvalidated;

        private void NotifyObjectInvalidated()
        {
            Action<ObjectValidator<T>> objectInvalidated = this.ObjectInvalidated;
            if (objectInvalidated != null)
            {
                objectInvalidated(this);
            }
        }

        public ObjectValidator(Func<T> creator)
        {
            this.m_creator = creator;
        }

        public void Invalidate()
        {
            this.m_isValid = false;
            this.NotifyObjectInvalidated();
        }

        public static implicit operator T(ObjectValidator<T> validator)
        {
            T instance;
            if (validator.m_isValid)
            {
                instance = validator.m_instance;
            }
            else
            {
                lock (validator.m_sync)
                {
                    if (validator.m_isValid)
                    {
                        instance = validator.m_instance;
                        return instance;
                    }
                    validator.m_instance = validator.m_creator();
                    validator.m_isValid = true;
                }
                instance = validator.m_instance;
            }
            return instance;
        }
    }

    public class ObjectValidator<T, TContext>
    {
        private readonly object m_sync = new object();
        private bool m_isValid;
        private T m_instance;
        private readonly TContext m_context;
        private readonly Func<TContext, T> m_creator;

        public event Action<ObjectValidator<T, TContext>> ObjectInvalidated;

        private void NotifyObjectInvalidated()
        {
            Action<ObjectValidator<T, TContext>> objectInvalidated = this.ObjectInvalidated;
            if (objectInvalidated != null)
            {
                objectInvalidated(this);
            }
        }

        public ObjectValidator(TContext context, Func<TContext, T> creator)
        {
            this.m_context = context;
            this.m_creator = creator;
        }

        public void Invalidate()
        {
            this.m_isValid = false;
            this.NotifyObjectInvalidated();
        }

        public static implicit operator T(ObjectValidator<T, TContext> validator)
        {
            T instance;
            if (validator.m_isValid)
            {
                instance = validator.m_instance;
            }
            else
            {
                lock (validator.m_sync)
                {
                    if (validator.m_isValid)
                    {
                        instance = validator.m_instance;
                        return instance;
                    }
                    validator.m_instance = validator.m_creator(validator.m_context);
                    validator.m_isValid = true;
                }
                instance = validator.m_instance;
            }
            return instance;
        }
    }
}