using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Managers
{
    public class EaOrderManager : ITransientDependency
    {
        private readonly ILogger<EaOrderManager> _logger;
        private readonly IOrderRepository _orderRepository;

        public EaOrderManager(ILogger<EaOrderManager> logger, IOrderRepository orderRepository)
        {
            _logger = logger;
            _orderRepository = orderRepository;
        }

        public Task SaveAsync(EaOrder order) => _orderRepository.SaveAsync(order);

        public Task<EaOrder?> GetByExchangeIdAsync(long exchangeOrderId) =>
            _orderRepository.GetByExchangeIdAsync(exchangeOrderId);

        public Task<List<EaOrder>> GetActiveOrdersAsync(string strategyName) =>
            _orderRepository.GetActiveOrdersAsync(strategyName);

        public async Task MarkCanceledAsync(long exchangeOrderId, string reason)
        {
            var order = await _orderRepository.GetByExchangeIdAsync(exchangeOrderId);
            if (order is null)
            {
                _logger.LogWarning("Order not found while canceling, exchangeOrderId={ExchangeOrderId}", exchangeOrderId);
                return;
            }

            order.Cancel(reason);
            await _orderRepository.SaveAsync(order);
        }

        public async Task MarkRejectedAsync(long exchangeOrderId, string reason)
        {
            var order = await _orderRepository.GetByExchangeIdAsync(exchangeOrderId);
            if (order is null)
            {
                _logger.LogWarning("Order not found while rejecting, exchangeOrderId={ExchangeOrderId}", exchangeOrderId);
                return;
            }

            order.Reject(reason);
            await _orderRepository.SaveAsync(order);
        }

        public bool IsActive(EaOrder order) =>
            order.Status is OrderState.New or OrderState.Submitted or OrderState.PartiallyFilled;
    }
}
