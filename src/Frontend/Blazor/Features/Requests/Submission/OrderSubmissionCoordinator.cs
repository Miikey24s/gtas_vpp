using gtas_vpp_fe.Features.Requests.Api;

namespace gtas_vpp_fe.Features.Requests.Submission;

public abstract record OrderSubmissionOperation(OrderSubmissionSnapshot Submission)
{
    public sealed record Create(
        OrderSubmissionSnapshot Submission,
        int Year,
        int Month,
        bool IsAdditionalOrder,
        bool IncludeAdditionalContext,
        Guid? BaseRequestId)
        : OrderSubmissionOperation(Submission);

    public sealed record Update(
        OrderSubmissionSnapshot Submission,
        Guid OrderId,
        bool IsAdditionalOrder,
        byte[]? RowVersion)
        : OrderSubmissionOperation(Submission);

    public sealed record Recreate(
        OrderSubmissionSnapshot Submission,
        Guid OrderId,
        bool IsAdditionalOrder,
        byte[]? RowVersion)
        : OrderSubmissionOperation(Submission);
}

public enum OrderSubmissionOutcome
{
    Created,
    Updated,
    Recreated
}

public sealed class OrderSubmissionCoordinator(RequestsCommandClient commands)
{
    public async Task<OrderSubmissionOutcome> SubmitAsync(OrderSubmissionOperation operation)
    {
        switch (operation)
        {
            case OrderSubmissionOperation.Create create:
                await commands.CreateAsync(OrderSubmissionRequestFactory.BuildCreateRequest(
                    create.Submission,
                    create.Year,
                    create.Month,
                    create.IsAdditionalOrder,
                    create.IncludeAdditionalContext,
                    create.BaseRequestId));
                return OrderSubmissionOutcome.Created;

            case OrderSubmissionOperation.Update update:
                await commands.UpdateAsync(
                    update.OrderId,
                    OrderSubmissionRequestFactory.BuildUpdateRequest(
                        update.Submission,
                        update.OrderId,
                        update.IsAdditionalOrder,
                        update.RowVersion));
                return OrderSubmissionOutcome.Updated;

            case OrderSubmissionOperation.Recreate recreate:
                await commands.RecreateAsync(
                    recreate.OrderId,
                    OrderSubmissionRequestFactory.BuildRecreateRequest(
                        recreate.Submission,
                        recreate.IsAdditionalOrder,
                        recreate.RowVersion));
                return OrderSubmissionOutcome.Recreated;

            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }
}
