using Microsoft.Extensions.Logging;
using Oc.BinGrid.Entities;
using Oc.BinGrid.Enums;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Oc.BinGrid.Managers
{
    /// <summary>
    /// EA 订单领域管理器（聚合根生命周期中枢）
    /// </summary>
    public class EaOrderManager : ITransientDependency
    {
        private readonly ILogger<EaOrderManager> _logger;
        private readonly IRepository<EaOrder, long> _orderRepo;

        public EaOrderManager(
            ILogger<EaOrderManager> logger,
            IRepository<EaOrder, long> orderRepo)
        {
            _logger = logger;
            _orderRepo = orderRepo;
        }

        #region ===== 创建订单 =====

        public async Task<EaOrder> CreateAsync(EaOrder order)
        {
            await _orderRepo.InsertAsync(order);

            _logger.LogInformation(
                "[OrderCreated] Strategy={Strategy} Symbol={Symbol} Market={Market} Action={Action} Qty={Qty} Price={Price} OrderId={OrderId}",
                order.StrategyName,
                order.Symbol,
                order.Market,
                order.Action,
                order.ExecQty,
                order.ExecPrice,
                order.Id);

            return order;
        }

        #endregion

        #region ===== 查询 =====

        public Task<EaOrder?> GetAsync(long id) => _orderRepo.FindAsync(id);

        public async Task<List<EaOrder>> GetByStrategyAsync(string strategy)
        {
            var list = await _orderRepo.GetListAsync();
            return list.Where(x => x.StrategyName == strategy).ToList();
        }

        public async Task<List<EaOrder>> GetPendingAsync(string? strategy = null)
        {
            var list = await _orderRepo.GetListAsync();

            return list
                .Where(o => o.Status is OrderState.New
                          or OrderState.PartiallyFilled
                          or OrderState.Submitted)
                .Where(o => strategy == null || o.StrategyName == strategy)
                .OrderBy(o => o.CreationTime)
                .ToList();
        }

        public async Task<bool> HasPendingOrder(string strategy, string symbol)
        {
            var list = await _orderRepo.GetListAsync(x =>
                x.StrategyName == strategy &&
                x.Symbol == symbol &&
                x.Status == OrderState.Submitted);
            return list.Any();
        }

        public async Task<List<EaOrder>> GetAllAsync()
        {
            var list = await _orderRepo.GetListAsync();
            return list.OrderByDescending(o => o.CreationTime).ToList();
        }

        #endregion

        #region ===== 状态推进 =====

        public async Task MarkSubmittedAsync(long id, long exchangeOrderId)
        {
            var order = await RequireAsync(id);
            order.MarkSubmitted(exchangeOrderId);
            await SaveAsync(order, "Submitted");

            _logger.LogInformation(
                "[OrderSubmitted] Strategy={Strategy} Symbol={Symbol} OrderId={OrderId} ExchangeId={ExchangeId}",
                order.StrategyName,
                order.Symbol,
                order.Id,
                exchangeOrderId);
        }

        public async Task<EaOrder> ApplyFillAsync(
            long id,
            decimal fillQty,
            decimal fillPrice,
            decimal fee,
            string feeAsset,
            bool isMaker)
        {
            var order = await RequireAsync(id);
            order.ApplyFill(fillQty, fillPrice, fee, feeAsset, isMaker);
            order = await SaveAsync(order, "Fill");

            _logger.LogInformation(
                "[OrderFill] Strategy={Strategy} Symbol={Symbol} OrderId={OrderId} PositionId={PositionId} FillQty={Qty} Price={Price} Fee={Fee} Maker={Maker}",
                order.StrategyName,
                order.Symbol,
                order.Id,
                order.PositionId,
                fillQty,
                fillPrice,
                fee,
                isMaker);

            return order;
        }

        public async Task CancelAsync(long id, string reason = "")
        {
            var order = await RequireAsync(id);
            order.Cancel(reason);
            await SaveAsync(order, "Cancel");

            _logger.LogWarning(
                "[OrderCancel] Strategy={Strategy} Symbol={Symbol} OrderId={OrderId} Reason={Reason}",
                order.StrategyName,
                order.Symbol,
                order.Id,
                reason);
        }

        public async Task RejectAsync(long id, string reason)
        {
            var order = await RequireAsync(id);
            order.Reject(reason);
            await SaveAsync(order, "Reject");

            _logger.LogError(
                "[OrderReject] Strategy={Strategy} Symbol={Symbol} OrderId={OrderId} Reason={Reason}",
                order.StrategyName,
                order.Symbol,
                order.Id,
                reason);
        }

        #endregion

        #region ===== 批量操作 / 清理 =====

        /// <summary>
        /// 清理已完成订单（Filled / Canceled / Rejected）
        /// </summary>
        public async Task CleanupCompletedAsync()
        {
            var list = await _orderRepo.GetListAsync();
            var completed = list.Where(o => o.Status is OrderState.Filled or OrderState.Canceled or OrderState.Rejected).ToList();

            foreach (var o in completed)
                await _orderRepo.DeleteAsync(o);

            if (completed.Count > 0)
            {
                _logger.LogInformation("[OrderCleanup] Count={Count}", completed.Count);
            }
        }

        /// <summary>
        /// 批量标记挂单超时撤单
        /// </summary>
        public async Task CancelPendingAsync(IEnumerable<long> orderIds, string reason = "Timeout")
        {
            foreach (var id in orderIds)
            {
                var order = await _orderRepo.FindAsync(id);
                if (order != null && order.Status is OrderState.New or OrderState.PartiallyFilled or OrderState.Submitted)
                {
                    order.Cancel(reason);
                    await _orderRepo.UpdateAsync(order, autoSave: true);

                    _logger.LogWarning(
                        "[OrderBatchCancel] Strategy={Strategy} Symbol={Symbol} OrderId={OrderId} Reason={Reason}",
                        order.StrategyName,
                        order.Symbol,
                        order.Id,
                        reason);
                }
            }
        }

        #endregion

        #region ===== 内部工具 =====

        private async Task<EaOrder> RequireAsync(long id)
        {
            var order = await _orderRepo.FindAsync(id);
            if (order == null)
            {
                _logger.LogError("[OrderMissing] Id={Id}", id);
                throw new BusinessException($"Order not found: {id}");
            }
            return order;
        }

        private async Task<EaOrder> SaveAsync(EaOrder order, string action)
        {
            order = await _orderRepo.UpdateAsync(order, autoSave: true);
            _logger.LogInformation("{Action}: {Order}", action, order);

            _logger.LogInformation(
               "[Order{Action}] Strategy={Strategy} Symbol={Symbol} OrderId={OrderId} Status={Status}",
               action,
               order.StrategyName,
               order.Symbol,
               order.Id,
               order.Status);

            return order;
        }

        #endregion
    }
}
