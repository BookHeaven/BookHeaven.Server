using BookHeaven.Domain.Shared;
using MediatR;

namespace BookHeaven.Server.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}