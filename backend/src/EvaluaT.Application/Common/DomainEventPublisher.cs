using EvaluaT.Application.Abstractions;
using EvaluaT.Domain.Events;

namespace EvaluaT.Application.Common;

public sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IEnumerable<IDomainEventObserver> _observers;

    public DomainEventPublisher(IEnumerable<IDomainEventObserver> observers)
    {
        _observers = observers;
    }

    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            foreach (var observer in _observers)
            {
                await observer.HandleAsync(domainEvent, cancellationToken);
            }
        }
    }
}
