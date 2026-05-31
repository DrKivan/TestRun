using EvaluaT.Domain.Events;

namespace EvaluaT.Application.Abstractions;

public interface IDomainEventObserver
{
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
