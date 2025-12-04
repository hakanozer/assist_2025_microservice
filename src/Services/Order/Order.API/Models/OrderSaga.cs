namespace Order.API.Models
{   
// 1. SagaState Enum
public enum SagaState
{
    Started,
    OrderCreated,
    PaymentProcessing,
    PaymentCompleted,
    PaymentFailed,
    OrderCancelled,
    Completed
}

public class OrderSaga
{
    public Guid Id { get; set; }
    public int OrderId { get; set; }
    public SagaState State { get; set; }
    public string Data { get; set; } = string.Empty; // JSON
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}

}