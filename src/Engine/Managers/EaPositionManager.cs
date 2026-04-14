using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Managers
{
    public class EaPositionManager : ITransientDependency
    {
        private readonly ILogger<EaPositionManager> _logger;
        private readonly IPositionRepository _positionRepository;

        public EaPositionManager(ILogger<EaPositionManager> logger, IPositionRepository positionRepository)
        {
            _logger = logger;
            _positionRepository = positionRepository;
        }

        public Task<EaPosition?> GetPositionAsync(string strategyName, string symbol, PositionSide side)
        {
            return _positionRepository.GetPositionAsync(strategyName, symbol, side);
        }

        public async Task ApplyFillAsync(EaOrder order)
        {
            var side = order.Action is OrderAction.OpenLong or OrderAction.CloseLong
                ? PositionSide.Long
                : PositionSide.Short;

            var position = await _positionRepository.GetPositionAsync(order.StrategyName, order.Symbol, side)
                ?? new EaPosition(order.StrategyName, order.Symbol, order.Market, side);

            if (order.Action is OrderAction.OpenLong or OrderAction.OpenShort)
            {
                position.AddPosition(order.ExecQty, order.ExecPrice, order.Fee);
            }
            else
            {
                var reduceQty = Math.Min(position.Qty, order.ExecQty);
                if (reduceQty > 0)
                {
                    position.ReducePosition(reduceQty, order.ExecPrice, order.Fee);
                }
            }

            await _positionRepository.UpdateAsync(position);

            _logger.LogInformation(
                "Position updated Strategy={Strategy} Symbol={Symbol} Side={Side} Qty={Qty} Entry={EntryPrice}",
                position.StrategyName,
                position.Symbol,
                position.Side,
                position.Qty,
                position.EntryPrice);
        }
    }
}
