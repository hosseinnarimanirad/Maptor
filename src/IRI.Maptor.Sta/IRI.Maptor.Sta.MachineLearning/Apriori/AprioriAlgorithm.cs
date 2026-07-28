using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Sta.MachineLearning
{
    public class AprioriAlgorithm
    {
        public static (List<TBasket> outBaskets, List<Itemset> outItemsets) DoStep<TBasket>(
            List<TBasket> baskets,
            List<Itemset> frequentItemsets,
            Func<TBasket, IEnumerable<string>> func,
            Action<TBasket, int> removeItemAtFunc,
            int n,
            int minSupport)
        {
            var distinctItemsets = new HashSet<string>(frequentItemsets.SelectMany(s => s.SortedTuple));

            foreach (var basket in baskets)
            {
                var items = func(basket).ToList();

                for (int i = items.Count - 1; i >= 0; i--)
                {
                    if (!distinctItemsets.Contains(items[i]))
                    {
                        //article.author.RemoveAt(i);
                        removeItemAtFunc(basket, i);
                    }
                }
            }

            var nonEmptyBaskets = baskets.Where(a => func(a).Any()).ToList();

            return CreateFrequentItemSet(nonEmptyBaskets, n, func, minSupport);
        }

        public static (List<TBasket> articles, List<Itemset> itemsets) CreateFrequentItemSet<TBasket>(List<TBasket> articles, int n, Func<TBasket, IEnumerable<string>> func, int minSupport)
        {
            //var frequentAuthors = frequentItemsets.SelectMany(f => f.SortedTuple).Distinct().ToList();

            //var frequentArticles = articles.Where(a => frequentItemsets.Any(itemset => itemset.CoveredBy(a.author))).ToList();

            //var t1 = articles
            //            ?.SelectMany(f => GetExtendedItemsets(f, frequentItemsets)).ToList();

            //var t2 = t1.Where(i => i != null)
            //            ?.GroupBy(g => g.SortedTuple).ToList();

            //var t3 = t2?.Select(g => new Itemset(g.Key, g.Sum(y => y.Frequency))).ToList();

            //var n = frequentItemsets.First().SortedTuple.Count;

            List<TBasket> newArticles = new List<TBasket>(articles.Count);
            List<Itemset> newItemsets = new List<Itemset>(10000);

            for (int i = 0; i < articles.Count; i++)
            {
                if (func(articles[i]).Count() <= n - 1)
                {
                    continue;
                }

                //var temp = GetExtendedItemsets(articles[i], frequentItemsets, frequentAuthors);

                var temp = GetExtendedItemsets(articles[i], func, n);

                if (temp.Count > 0)
                {
                    newArticles.Add(articles[i]);
                    newItemsets.AddRange(temp);
                }
            }

            var itemsets = newItemsets
                       ?.GroupBy(g => g.Signature)
                       ?.Select(g => new Itemset(g.First().SortedTuple, g.Sum(y => y.Frequency)))
                       ?.Where(g => g.Frequency > minSupport)
                       ?.ToList();

            //var itemsets = newItemsets
            //         ?.GroupBy(g => g.Signature)
            //         ?.Select(g => new Itemset(g.First().SortedTuple, g.Sum(y => y.Frequency)))
            //         ?.OrderByDescending(g => g.Frequency).ThenBy(g => g.Signature)
            //         ?.ToList();

            return (articles: newArticles, itemsets: itemsets);

            //var frequentAuthorArticles = articles?.Select(f => (article: f, itemsets: GetExtendedItemsets(f, frequentItemsets, frequentAuthors))).Where(i => i.itemsets.Count > 0).ToList();

            //var itemsets = frequentAuthorArticles
            //            .SelectMany(i => i.itemsets)
            //            ?.GroupBy(g => g.SortedTuple)
            //            ?.Select(g => new Itemset(g.Key, g.Sum(y => y.Frequency)))
            //            ?.Where(g => g.Frequency > min_sup)
            //            ?.ToList();

            //return (frequentAuthorArticles.Select(f => f.article).ToList(), itemsets);
        }

        public static List<Itemset> GetExtendedItemsets<TBasket>(TBasket article, Func<TBasket, IEnumerable<string>> func, int n)
        {
            var result = new List<Itemset>();

            var items = func(article);

            if (items.Count() <= n)
            {
                return result;
            }

            //var currentFrequent = frequentItemsets.Where(itemset => itemset.CoveredBy(article.author))?.ToList();

            //var authors = article.author.Where(a => frequentAuthors.Contains(a));

            //if (currentFrequent?.Any() == true && authors?.Any() == true)
            //{
            //    var temp = currentFrequent.SelectMany(current => authors.Where(a => !current.Contains(a))?.Select(a => current.Extend(a))).ToList();

            //    result = temp.Distinct().ToList();
            //}

            result = Choice(items, n)?.Select(i => new Itemset(i.ToList(), 1)).Distinct().ToList();

            return result;

        }

        public static IEnumerable<IEnumerable<T>> Choice<T>(IEnumerable<T> list, int count)
        {
            if (count == 0)
            {
                yield return new T[0];
            }
            else
            {
                int startingElementIndex = 0;

                for (int i = 0; i < list.Count(); i++)
                {
                    IEnumerable<T> remainingItems = list.Skip(i + 1);

                    foreach (IEnumerable<T> permutationOfRemainder in Choice(remainingItems, count - 1))
                    {
                        yield return Concat<T>(
                            new T[] { list.ElementAt(i) },
                            permutationOfRemainder);
                    }
                    startingElementIndex += 1;
                }
            }
        }

        public static IEnumerable<IEnumerable<T>> Permute<T>(IEnumerable<T> list, int count)
        {
            if (count == 0)
            {
                yield return new T[0];
            }
            else
            {
                int startingElementIndex = 0;
                foreach (T startingElement in list)
                {
                    IEnumerable<T> remainingItems = AllExcept(list, startingElementIndex);

                    foreach (IEnumerable<T> permutationOfRemainder in Permute(remainingItems, count - 1))
                    {
                        yield return Concat<T>(
                            new T[] { startingElement },
                            permutationOfRemainder);
                    }
                    startingElementIndex += 1;
                }
            }
        }

        public static IEnumerable<T> Concat<T>(IEnumerable<T> a, IEnumerable<T> b)
        {
            foreach (T item in a) { yield return item; }
            foreach (T item in b) { yield return item; }
        }

        // Enumerates over all items in the input, skipping over the item
        // with the specified offset.
        public static IEnumerable<T> AllExcept<T>(IEnumerable<T> input, int indexToSkip)
        {
            int index = 0;
            foreach (T item in input)
            {
                if (index != indexToSkip) yield return item;
                index += 1;
            }
        }

    }
}
