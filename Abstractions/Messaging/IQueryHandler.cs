using BookHeaven.Domain.Shared;
using MediatR;

namespace BookHeaven.Server.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse>
{
    
}