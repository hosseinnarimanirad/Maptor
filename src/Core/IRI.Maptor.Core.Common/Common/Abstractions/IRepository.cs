using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Common.Abstractions;

public interface IRepository<T> where T : class
{
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);

    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);

    T Get(int id);
    IEnumerable<T> GetAll();
    //we can use lambda expression to filter objects
    IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
}
