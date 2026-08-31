import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TeamDetail, TeamMember, TeamRole } from '../../core/api/api.models';
import { errorMessageOf } from '../../core/api/problem-details';
import { TeamsApi } from '../../core/api/teams.api';
import { SessionStore } from '../../core/auth/session.store';
import { LatestRequest } from '../../core/latest-request';
import { TeamStore } from '../../core/teams/team.store';
import { ToastService } from '../../core/ui/toast.service';
import { canManageTeam } from '../../shared/attendance';
import { Avatar } from '../../shared/avatar/avatar';
import { Icon } from '../../shared/icon/icon';

type Confirmation =
  | { readonly kind: 'remove'; readonly member: TeamMember }
  | { readonly kind: 'transfer'; readonly member: TeamMember }
  | { readonly kind: 'leave' }
  | { readonly kind: 'delete' };

@Component({
  selector: 'app-team-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Avatar, Icon],
  templateUrl: './team-detail.page.html',
  styleUrl: './team-detail.page.scss',
})
export class TeamDetailPage {
  private readonly api = inject(TeamsApi);
  private readonly session = inject(SessionStore);
  private readonly toasts = inject(ToastService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly teams = inject(TeamStore);

  protected readonly team = signal<TeamDetail | null>(null);
  protected readonly loading = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly editingDetails = signal(false);
  protected readonly confirmation = signal<Confirmation | null>(null);
  protected readonly codeCopied = signal(false);

  private readonly teamRequest = new LatestRequest();

  protected readonly myUserId = computed(() => this.session.user()?.id ?? null);
  protected readonly canManage = computed(() => {
    const team = this.team();

    return team !== null && canManageTeam(team.myRole);
  });
  protected readonly isOwner = computed(() => this.team()?.myRole === 'Owner');

  protected readonly detailsForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(80)]],
    description: ['', [Validators.maxLength(280)]],
  });

  constructor() {
    effect(() => {
      if (this.teams.loaded() && !this.teams.selectedId()) {
        void this.router.navigate(['/teams']);
      }
    });

    effect(() => {
      // See SchedulePage: only load once the remembered selection is reconciled.
      if (!this.teams.loaded()) {
        return;
      }

      const teamId = this.teams.selectedId();

      if (teamId) {
        void this.reload(teamId);
      }
    });

    void this.teams.load();
  }

  protected startEditingDetails(): void {
    const team = this.team();

    if (!team) {
      return;
    }

    this.detailsForm.setValue({ name: team.name, description: team.description ?? '' });
    this.editingDetails.set(true);
  }

  protected async saveDetails(): Promise<void> {
    const team = this.team();

    if (!team || this.detailsForm.invalid) {
      this.detailsForm.markAllAsTouched();
      return;
    }

    const { name, description } = this.detailsForm.getRawValue();
    this.busy.set(true);

    try {
      const updated = await firstValueFrom(this.api.update(team.id, name, description.trim() || null));

      this.team.set(updated);
      this.teams.adopt(updated);
      this.editingDetails.set(false);
      this.toasts.success('Team details saved.');
    } catch (error) {
      this.toasts.error(errorMessageOf(error));
    } finally {
      this.busy.set(false);
    }
  }

  protected async copyJoinCode(): Promise<void> {
    const code = this.team()?.joinCode;

    if (!code) {
      return;
    }

    try {
      await navigator.clipboard.writeText(code);
      this.codeCopied.set(true);
      setTimeout(() => this.codeCopied.set(false), 2000);
    } catch {
      // Clipboard access can be denied; the code is on screen either way.
      this.toasts.info(`Copy it manually: ${code}`);
    }
  }

  protected async regenerateJoinCode(): Promise<void> {
    const team = this.team();

    if (!team) {
      return;
    }

    this.busy.set(true);

    try {
      await firstValueFrom(this.api.regenerateJoinCode(team.id));
      await this.reload(team.id);
      this.toasts.success('A new invite code has been issued. The old one no longer works.');
    } catch (error) {
      this.toasts.error(errorMessageOf(error));
    } finally {
      this.busy.set(false);
    }
  }

  protected async changeRole(member: TeamMember, role: TeamRole): Promise<void> {
    const team = this.team();

    if (!team) {
      return;
    }

    this.busy.set(true);

    try {
      await firstValueFrom(this.api.changeRole(team.id, member.userId, role));
      await this.reload(team.id);
      this.toasts.success(`${member.displayName} is now ${role === 'Admin' ? 'an admin' : 'a member'}.`);
    } catch (error) {
      this.toasts.error(errorMessageOf(error));
    } finally {
      this.busy.set(false);
    }
  }

  protected ask(confirmation: Confirmation): void {
    this.confirmation.set(confirmation);
  }

  protected async confirm(): Promise<void> {
    const pending = this.confirmation();
    const team = this.team();

    if (!pending || !team) {
      return;
    }

    this.busy.set(true);

    try {
      switch (pending.kind) {
        case 'remove':
          await firstValueFrom(this.api.removeMember(team.id, pending.member.userId));
          await this.reload(team.id);
          this.toasts.success(`${pending.member.displayName} has been removed.`);
          break;

        case 'transfer':
          await firstValueFrom(this.api.transferOwnership(team.id, pending.member.userId));
          await this.reload(team.id);
          this.toasts.success(`${pending.member.displayName} now owns this team.`);
          break;

        case 'leave':
          await firstValueFrom(this.api.leave(team.id));
          this.teams.forget(team.id);
          this.toasts.success(`You have left ${team.name}.`);
          void this.router.navigate(['/teams']);
          break;

        case 'delete':
          await firstValueFrom(this.api.delete(team.id));
          this.teams.forget(team.id);
          this.toasts.success(`${team.name} has been deleted.`);
          void this.router.navigate(['/teams']);
          break;
      }

      this.confirmation.set(null);
    } catch (error) {
      this.toasts.error(errorMessageOf(error));
    } finally {
      this.busy.set(false);
    }
  }

  protected confirmTitle(pending: Confirmation): string {
    switch (pending.kind) {
      case 'remove':
        return `Remove ${pending.member.displayName}?`;
      case 'transfer':
        return `Make ${pending.member.displayName} the owner?`;
      case 'leave':
        return `Leave ${this.team()?.name}?`;
      case 'delete':
        return `Delete ${this.team()?.name}?`;
    }
  }

  protected confirmBody(pending: Confirmation): string {
    switch (pending.kind) {
      case 'remove':
        return 'Their attendance in this team is deleted too. They can rejoin with the invite code.';
      case 'transfer':
        return 'They gain full control of the team. You stay on as an admin.';
      case 'leave':
        return 'Your attendance in this team is deleted. You can rejoin with the invite code.';
      case 'delete':
        return 'The team, its members and everyone’s attendance are permanently deleted. This cannot be undone.';
    }
  }

  protected confirmAction(pending: Confirmation): string {
    switch (pending.kind) {
      case 'remove':
        return 'Remove';
      case 'transfer':
        return 'Transfer ownership';
      case 'leave':
        return 'Leave team';
      case 'delete':
        return 'Delete team';
    }
  }

  protected isDestructive(pending: Confirmation): boolean {
    return pending.kind !== 'transfer';
  }

  private async reload(teamId: string): Promise<void> {
    const token = this.teamRequest.begin();

    this.loading.set(true);
    this.error.set(null);

    try {
      const team = await firstValueFrom(this.api.get(teamId));

      // Switching teams mid-request must not adopt the team we just left.
      if (this.teamRequest.isCurrent(token)) {
        this.team.set(team);
        this.teams.adopt(team);
      }
    } catch (error) {
      if (this.teamRequest.isCurrent(token)) {
        this.team.set(null);
        this.error.set(errorMessageOf(error));
      }
    } finally {
      if (this.teamRequest.isCurrent(token)) {
        this.loading.set(false);
      }
    }
  }
}
