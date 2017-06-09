
namespace CBCTQC.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// The base class for <see cref="INotifyPropertyChanged"/> interface.
    /// </summary>
    public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        #region Property access utilities
        /// <summary>
        /// Record all property values
        /// </summary>
        protected Dictionary<string, object> propertyValues = new Dictionary<string, object>();

        /// <summary>
        /// Set property value
        /// </summary>
        /// <typeparam name="T">The class type of the property</typeparam>
        /// <param name="value">property value</param>
        /// <param name="name">property name</param>
        protected void Set<T>(T value, [CallerMemberName] string name = "")
        {
            if (propertyValues.ContainsKey(name))
            {
                var oldValue = propertyValues[name];
                if ((value == null && oldValue != null) || !value.Equals(oldValue))
                {
                    propertyValues[name] = value;
                    OnPropertyChanged(name);
                }
            }
            else
            {
                propertyValues.Add(name, value);
                OnPropertyChanged(name);
            }
        }

        /// <summary>
        /// Retrieve property value
        /// </summary>
        /// <typeparam name="T">The class type of the property</typeparam>
        /// <param name="name">property name</param>
        /// <returns>property value</returns>
        protected T Get<T>([CallerMemberName] string name = "")
        {
            if (propertyValues.ContainsKey(name))
            {
                return (T)propertyValues[name];
            }
            else
            {
                return default(T);
            }
        }
        #endregion

        #region [ Events ]

        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region [ Public Methods ]

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="property">The property.</param>
        public virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        /// <summary>
        /// Copy the value table
        /// </summary>
        /// <returns></returns>
        public void CopyTo(NotifyPropertyChangedBase to)
        {
            foreach (string prop in propertyValues.Keys)
            {
                if (this.propertyValues.ContainsKey(prop))
                {
                    if (to.propertyValues.ContainsKey(prop))
                    {
                        to.propertyValues[prop] = this.propertyValues[prop];
                    }
                    else
                    {
                        to.propertyValues.Add(prop, this.propertyValues[prop]);
                    }
                }
            }
        }
        #endregion
    }
}
