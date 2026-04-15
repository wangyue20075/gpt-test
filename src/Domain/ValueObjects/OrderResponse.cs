namespace Oc.BinGrid.Domain.ValueObjects
{
    public record OrderResponse(
        string OrderId,
        string Symbol,
        string Side,
        string Status,
        decimal Price,
        decimal ExecutedQty,
        DateTime CreateTime
    );
}
