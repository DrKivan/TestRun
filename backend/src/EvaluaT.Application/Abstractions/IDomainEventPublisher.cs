using EvaluaT.Domain.Events;

namespace EvaluaT.Application.Abstractions;

public interface IDomainEventPublisher
{
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}
