using Microsoft.Extensions.Logging;
using Oc.BinGrid.Entities;
using Oc.BinGrid.Enums;
using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Oc.BinGrid.Managers
{
    /// <summary>
    /// 仓位领域管理器
    /// </summary>
    public class EaPositionManager : ITransientDependency
    {
        private readonly ILogger<EaPositionManager> _logger;
        private readonly IRepository<EaPosition, long> _positionRepo;
        private readonly IRepository<EaOrder, long> _orderRepo;

        /// <summary>
        /// Long Stack
        /// </summary>
        private readonly ConcurrentDictionary<string, Stack<EaPosition>> _longStacks = new();

        /// <summary>
        /// Short Stack
        /// </summary>
        private readonly ConcurrentDictionary<string, Stack<EaPosition>> _shortStacks = new();

        public EaPositionManager(
            ILogger<EaPositionManager> logger,
            IRepository<EaPosition, long> positionRepo,
            IRepository<EaOrder, long> orderRepo)
        {
            _logger = logger;
            _positionRepo = positionRepo;
            _orderRepo = orderRepo;
        }

        /// <summary>
        /// 订单成交后更新仓位
        /// </summary>
        public async Task HandleFillAsync(EaOrder order)
        {
            // 1. 只有成交或部分成交的订单才触发
            if (order.Status != OrderState.Filled &&
                order.Status != OrderState.PartiallyFilled)
                return;

            _logger.LogInformation(
                "[Fill] Strategy={Strategy} Symbol={Symbol} Action={Action} " +
                "OrderId={OrderId} ExecQty={Qty} ExecPrice={Price} Fee={Fee}",
                order.StrategyName,
                order.Symbol,
                order.Action,
                order.Id,
                order.ExecQty,
                order.ExecPrice,
                order.Fee);

            var side = order.Action switch
            {
                OrderAction.OpenLong => PositionSide.Long,
                OrderAction.CloseLong => PositionSide.Long,
                OrderAction.OpenShort => PositionSide.Short,
                OrderAction.CloseShort => PositionSide.Short,
                _ => throw new InvalidOperationException("未知方向")
            };

            var isOpen = order.Action == OrderAction.OpenLong ||
                         order.Action == OrderAction.OpenShort;

            if (isOpen)
            {
                // 加仓
                await HandleOpenAsync(order, side);
            }
            else
            {
                // 减仓
                await HandleCloseAsync(order, side);
            }
        }

        /// <summary>
        /// 获取今天开仓的数量，用于控制每天开仓次数限制
        /// </summary>
        public async Task<int> GetTodayCountAsync(string strategy, string symbol)
        {
            var todayStart = DateTime.UtcNow.Date;

            var list = await _positionRepo.GetListAsync(
                p => p.StrategyName == strategy &&
                p.OpenTime >= todayStart);

            return list.Count;
        }

        public async Task<List<EaPosition>> GetAllPositionsAsync(string strategy, string symbol)
        {
            var list = await _positionRepo.GetListAsync(p =>
                p.StrategyName == strategy &&
                p.Symbol == symbol &&
                p.Status != PositionStatusType.Closed);

            return list;
        }

        public async Task<List<EaPosition>> GetAllPositionsAsync()
        {
            var list = await _positionRepo.GetListAsync(p => p.Status != PositionStatusType.Closed);
            return list;
        }

        public async Task<EaPosition?> GetActiveAsync(string strategy, string symbol)
        {
            var list = await _positionRepo.GetListAsync(p =>
                p.StrategyName == strategy &&
                p.Symbol == symbol &&
                p.Status != PositionStatusType.Closed);

            var position = list.OrderByDescending(p => p.OpenTime).FirstOrDefault();

            return position;
        }

        public async Task UpdatePositionStatusAsync(EaPosition position)
        {
            await _positionRepo.UpdateAsync(position, autoSave: true);
        }

        private async Task HandleOpenAsync(EaOrder order, PositionSide side)
        {
            // 如果订单已经绑定仓位（加仓）
            if (order.PositionId.HasValue)
            {
                var pos = await _positionRepo.GetAsync(order.PositionId.Value);
                var oldQty = pos.Qty;
                var oldAvg = pos.EntryPrice;

                pos.AddPosition(order.ExecQty, order.ExecPrice, order.Fee);

                await _positionRepo.UpdateAsync(pos, autoSave: true);

                _logger.LogInformation(
                    "[AddPosition] {Symbol} PosId={PosId} Side={Side} " +
                    "OldQty={OldQty} -> NewQty={NewQty} " +
                    "OldAvg={OldAvg} -> NewAvg={NewAvg}",
                    order.Symbol,
                    pos.Id,
                    side,
                    oldQty,
                    pos.Qty,
                    oldAvg,
                    pos.EntryPrice);

                return;
            }

            // 新仓
            var posNew = new EaPosition(
                order.StrategyName,
                order.Symbol,
                order.Market,
                side);

            posNew.AddPosition(order.ExecQty, order.ExecPrice, order.Fee);

            await _positionRepo.InsertAsync(posNew, autoSave: true);

            order.SetPositionId(posNew.Id);
            await _orderRepo.UpdateAsync(order, autoSave: true);

            _logger.LogWarning(
                "[NewPosition] Strategy={Strategy} Symbol={Symbol} " +
                "PosId={PosId} Side={Side} Qty={Qty} AvgPrice={Avg}",
                order.StrategyName,
                order.Symbol,
                posNew.Id,
                side,
                posNew.Qty,
                posNew.EntryPrice);
        }

        private async Task HandleCloseAsync(EaOrder order, PositionSide side)
        {
            if (order.PositionId.HasValue)
            {
                // ⭐ 指定仓平仓（推荐）
                var pos = await _positionRepo.GetAsync(order.PositionId.Value);
                var oldQty = pos.Qty;

                pos.ReducePosition(order.ExecQty, order.ExecPrice, order.Fee);

                await _positionRepo.UpdateAsync(pos, autoSave: true);

                _logger.LogInformation(
                    "[ClosePosition] Symbol={Symbol} PosId={PosId} Side={Side} " +
                    "CloseQty={CloseQty} Remaining={RemainQty} PnL={Realized}",
                    order.Symbol,
                    pos.Id,
                    side,
                    order.ExecQty,
                    pos.Qty,
                    pos.RealizedPnl);

                if (pos.Status == PositionStatusType.Closed)
                {
                    _logger.LogWarning(
                        "[PositionClosed] Symbol={Symbol} PosId={PosId} " +
                        "FinalPnL={PnL} TotalFee={Fee}",
                        order.Symbol,
                        pos.Id,
                        pos.RealizedPnl,
                        pos.Fee);
                }

                return;
            }

            // ===== 未指定仓 -> FIFO =====
            var positions = await _positionRepo.GetListAsync(p =>
                p.StrategyName == order.StrategyName &&
                p.Symbol == order.Symbol &&
                p.Side == side &&
                p.Status != PositionStatusType.Closed);

            var remain = order.ExecQty;

            foreach (var pos in positions.OrderBy(p => p.OpenTime))
            {
                if (remain <= 0) break;

                var closeQty = Math.Min(pos.Qty, remain);

                pos.ReducePosition(closeQty, order.ExecPrice, order.Fee);

                remain -= closeQty;

                await _positionRepo.UpdateAsync(pos, autoSave: true);

                _logger.LogInformation(
                    "[FIFO-Close] Symbol={Symbol} PosId={PosId} CloseQty={Qty} Remaining={Remain}",
                    order.Symbol,
                    pos.Id,
                    closeQty,
                    pos.Qty);
            }
        }
    }
}
