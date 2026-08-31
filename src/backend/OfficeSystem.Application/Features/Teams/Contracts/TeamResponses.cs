using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Teams.Contracts;

public sealed record TeamListItemResponse(
    Guid Id,
    string Name,
    string? Description,
    TeamRole MyRole,
    int MemberCount,
    DateTimeOffset CreatedAtUtc);

public sealed record TeamMemberResponse(
    Guid UserId,
    string DisplayName,
    string Email,
    string AccentColor,
    TeamRole Role,
    DateTimeOffset JoinedAtUtc);

public sealed record TeamDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    TeamRole MyRole,
    // Only populated for admins and owners: the join code is an invite secret.
    string? JoinCode,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<TeamMemberResponse> Members);
