using System;
using System.Collections.Generic;

namespace PilotMarkdownModule
{
    /// <summary>
    /// Статический агрегатор событий для связи между компонентами расширения
    /// </summary>
    public static class ExtensionEventAggregator
    {
        private static readonly List<object> _subscribers = new List<object>();
        
        /// <summary>
        /// Подписаться на событие
        /// </summary>
        public static void Subscribe<T>(object subscriber) where T : class
        {
            if (!_subscribers.Contains(subscriber))
                _subscribers.Add(subscriber);
        }
        
        /// <summary>
        /// Отписаться от события
        /// </summary>
        public static void Unsubscribe<T>(object subscriber) where T : class
        {
            _subscribers.Remove(subscriber);
        }
        
        /// <summary>
        /// Опубликовать событие
        /// </summary>
        public static void Publish<T>(T message) where T : class
        {
            foreach (var subscriber in _subscribers)
            {
                if (subscriber is IHandle<T> handler)
                {
                    try
                    {
                        handler.Handle(message);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error handling event: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Интерфейс обработчика событий
    /// </summary>
    public interface IHandle<T>
    {
        void Handle(T message);
    }
}
