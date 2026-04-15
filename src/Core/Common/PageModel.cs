namespace Oc.BinGrid.Core.Common
{
    public class PageRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // 总记录数（通常由仓储层在执行完查询后回填）
        public int TotalCount { get; set; }

        // 总页数（可选）
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
