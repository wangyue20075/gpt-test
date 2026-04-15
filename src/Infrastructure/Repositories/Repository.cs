using Oc.BinGrid.Core.Common;
using Oc.BinGrid.Domain.Interfaces;
using SqlSugar;
using System.Linq.Expressions;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Infrastructure.Repositories
{
    public class Repository<T, TKey> : IRepository<T, TKey>, ITransientDependency where T : class, new()
    {
        protected readonly ISqlSugarClient _db;

        public Repository(ISqlSugarClient db)
        {
            _db = db;
        }

        #region 查询逻辑

        public virtual async Task<T?> GetByIdAsync(TKey id) =>
            await _db.Queryable<T>().InSingleAsync(id);

        public virtual async Task<T?> GetFirstAsync(Expression<Func<T, bool>> predicate) =>
            await _db.Queryable<T>().FirstAsync(predicate);

        public virtual async Task<List<T>> GetListAsync() =>
            await _db.Queryable<T>().ToListAsync();

        public virtual async Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate) =>
            await _db.Queryable<T>().Where(predicate).ToListAsync();

        public virtual async Task<List<T>> GetPageListAsync(Expression<Func<T, bool>> predicate, PageRequest page)
        {
            RefAsync<int> totalCount = 0;
            var list = await _db.Queryable<T>()
                .Where(predicate)
                .ToPageListAsync(page.PageIndex, page.PageSize, totalCount);
            page.TotalCount = totalCount;
            return list;
        }

        #endregion

        #region 插入逻辑

        public virtual async Task<bool> InsertAsync(T entity) =>
            await _db.Insertable(entity).ExecuteCommandAsync() > 0;

        public virtual async Task<long> InsertReturnIdentityAsync(T entity) =>
            await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();

        public virtual async Task<bool> InsertRangeAsync(List<T> entities) =>
            await _db.Insertable(entities).ExecuteCommandAsync() > 0;

        #endregion

        #region 更新逻辑

        public virtual async Task<bool> UpdateAsync(T entity) =>
            await _db.Updateable(entity).ExecuteCommandAsync() > 0;

        public virtual async Task<bool> UpdateRangeAsync(List<T> entities) =>
            await _db.Updateable(entities).ExecuteCommandAsync() > 0;

        public virtual async Task<bool> UpdateAsync(Expression<Func<T, T>> columns, Expression<Func<T, bool>> predicate) =>
            await _db.Updateable<T>().SetColumns(columns).Where(predicate).ExecuteCommandAsync() > 0;

        #endregion

        #region 删除逻辑

        public virtual async Task<bool> DeleteAsync(T entity) =>
            await _db.Deleteable(entity).ExecuteCommandAsync() > 0;

        public virtual async Task<bool> DeleteByIdAsync(TKey id) =>
            await _db.Deleteable<T>().In(id).ExecuteCommandAsync() > 0;

        public virtual async Task<bool> DeleteAsync(Expression<Func<T, bool>> predicate) =>
            await _db.Deleteable<T>().Where(predicate).ExecuteCommandAsync() > 0;

        #endregion

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) =>
            await _db.Queryable<T>().AnyAsync(predicate);

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate) =>
            await _db.Queryable<T>().CountAsync(predicate);
    }
}
