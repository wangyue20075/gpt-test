using Oc.BinGrid.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Oc.BinGrid.Domain.Interfaces
{
    /// <summary>
    /// 策略核心契约，定义了外部组件（如监控服务）如何与策略交互
    /// </summary>
    public interface IStrategy
    {
        /// <summary>
        /// 策略唯一标识
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 策略名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 交易对
        /// </summary>
        string Symbol { get; }

        /// <summary>
        /// 订单状态更新回调
        /// 当交易所推送成交、或监控服务执行撤单后，通过此接口撞回策略逻辑
        /// </summary>
        /// <param name="order">标准化的订单响应对象</param>
        Task OnOrderUpdateAsync(OrderResponse order);

        /// <summary>
        /// 恢复逻辑：用于系统重启后重新绑定活跃订单
        /// </summary>
        void RestoreActiveOrder(string orderId);
    }
}
