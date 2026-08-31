using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Authentication.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Application.Features.Authentication.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<AuthenticatedUserResponse>;

internal sealed class GetCurrentUserQueryHandler(IUserRepository users, ICurrentUser currentUser)
    : IQueryHandler<GetCurrentUserQuery, AuthenticatedUserResponse>
{
    public async Task<Result<AuthenticatedUserResponse>> HandleAsync(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        User? user = await users.FindByIdAsync(currentUser.RequireUserId(), cancellationToken).ConfigureAwait(false);

        return user is null
            ? Result.Failure<AuthenticatedUserResponse>(UserErrors.NotFound)
            : AuthenticationSessionFactory.Describe(user);
    }
}
