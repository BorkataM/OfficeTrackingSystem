import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';
import { TeamStore } from '../core/teams/team.store';
import { ThemeService } from '../core/theme/theme.service';
import { canManageTeam } from '../shared/attendance';
import { Avatar } from '../shared/avatar/avatar';
import { Icon } from '../shared/icon/icon';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Avatar, Icon],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly teams = inject(TeamStore);
  protected readonly theme = inject(ThemeService);

  protected readonly user = this.auth.user;
  protected readonly navOpen = signal(false);
  protected readonly switcherOpen = signal(false);
  protected readonly userMenuOpen = signal(false);

  protected readonly selectedTeam = this.teams.selected;
  protected readonly canManage = computed(() => {
    const team = this.selectedTeam();

    return team !== null && canManageTeam(team.myRole);
  });

  protected toggleSwitcher(): void {
    this.switcherOpen.update((open) => !open);
    this.userMenuOpen.set(false);
  }

  protected toggleUserMenu(): void {
    this.userMenuOpen.update((open) => !open);
    this.switcherOpen.set(false);
  }

  protected closeMenus(): void {
    this.switcherOpen.set(false);
    this.userMenuOpen.set(false);
  }

  protected closeNav(): void {
    this.navOpen.set(false);
    this.closeMenus();
  }

  protected chooseTeam(teamId: string): void {
    this.teams.select(teamId);
    this.closeMenus();
    this.navOpen.set(false);
    void this.router.navigate(['/schedule']);
  }

  protected signOut(): void {
    this.closeMenus();
    this.teams.reset();
    this.auth.signOut();
  }
}
