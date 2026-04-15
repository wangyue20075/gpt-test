using Oc.BinGrid.Core.Common;
using System.Linq.Expressions;

namespace Oc.BinGrid.Domain.Interfaces
{
    public interface IRepository<T, TKey> where T : class, new()
    {
        // --- 查询 ---
        /// <summary>
        /// 根据强类型主键获取实体
        /// </summary>
        Task<T?> GetByIdAsync(TKey id);

        Task<T?> GetFirstAsync(Expression<Func<T, bool>> predicate);
        Task<List<T>> GetListAsync();
        Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate);
        Task<List<T>> GetPageListAsync(Expression<Func<T, bool>> predicate, PageRequest page);

        // --- 插入 ---
        Task<bool> InsertAsync(T entity);
        Task<long> InsertReturnIdentityAsync(T entity);
        Task<bool> InsertRangeAsync(List<T> entities);

        // --- 更新 ---
        Task<bool> UpdateAsync(T entity);
        Task<bool> UpdateRangeAsync(List<T> entities);
        Task<bool> UpdateAsync(Expression<Func<T, T>> columns, Expression<Func<T, bool>> predicate);

        // --- 删除 ---
        Task<bool> DeleteAsync(T entity);

        /// <summary>
        /// 根据强类型主键删除
        /// </summary>
        Task<bool> DeleteByIdAsync(TKey id);

        Task<bool> DeleteAsync(Expression<Func<T, bool>> predicate);

        // --- 判断 ---
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    }
}
