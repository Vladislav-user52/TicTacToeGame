using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToeGame.Models
{
    public class LazySequence<T> : IEnumerable<T>
    {
        private readonly Func<IEnumerable<T>> _sequenceGenerator;
        
        public LazySequence(Func<IEnumerable<T>> sequenceGenerator)
        {
            _sequenceGenerator = sequenceGenerator ?? throw new ArgumentNullException(nameof(sequenceGenerator));
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in _sequenceGenerator())
            {
                yield return item;
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public LazySequence<TResult> Select<TResult>(Func<T, TResult> selector)
        {
            return new LazySequence<TResult>(() => this.Select(selector));
        }
        
        public LazySequence<T> Where(Func<T, bool> predicate)
        {
            return new LazySequence<T>(() => this.Where(predicate));
        }
        
        public LazySequence<T> Take(int count)
        {
            return new LazySequence<T>(() => this.Take(count));
        }
        
        public LazySequence<T> Skip(int count)
        {
            return new LazySequence<T>(() => this.Skip(count));
        }

        public List<T> ToList() => new List<T>(this);

        public T FirstOrDefault() => this.FirstOrDefault();

        public bool Any() => this.Any();
        public bool Any(Func<T, bool> predicate) => this.Any(predicate);
        
        public int Count() => this.Count();
        
        public override string ToString() => $"LazySequence<{typeof(T).Name}>";
    }

    public static class LazySequenceExtensions
    {
        public static LazySequence<T> ToLazySequence<T>(this IEnumerable<T> source)
        {
            return new LazySequence<T>(() => source);
        }
        
        public static LazySequence<T> ToLazySequence<T>(this Func<IEnumerable<T>> generator)
        {
            return new LazySequence<T>(generator);
        }
    }
}