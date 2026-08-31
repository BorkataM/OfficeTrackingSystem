import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { errorMessageOf } from '../../core/api/problem-details';
import { AuthService } from '../../core/auth/auth.service';
import { TeamStore } from '../../core/teams/team.store';
import { ThemeService } from '../../core/theme/theme.service';
import { ToastService } from '../../core/ui/toast.service';
import { Avatar } from '../../shared/avatar/avatar';
import { Icon } from '../../shared/icon/icon';

@Component({
  selector: 'app-profile-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Avatar, Icon],
  template: `
    <header class="page-head">
      <h1>Your profile</h1>
      <p class="muted">How you appear to everyone in your teams.</p>
    </header>

    @if (user(); as me) {
      <section class="card card-pad section identity">
        <app-avatar [name]="me.displayName" [color]="me.accentColor" size="lg" />
        <div>
          <p class="identity-name">{{ me.displayName }}</p>
          <p class="muted text-sm">{{ me.email }}</p>
        </div>
      </section>

      <section class="card card-pad section">
        <div class="panel-title">
          <div>
            <h2>Display name</h2>
            <p class="muted text-sm">This is the name shown in the schedule grid.</p>
          </div>
        </div>

        <form [formGroup]="form" (ngSubmit)="save()" novalidate>
          <div class="field">
            <label class="label" for="display-name">Name</label>
            <input
              id="display-name"
              class="input"
              type="text"
              formControlName="displayName"
              maxlength="80"
              [class.invalid]="form.controls.displayName.touched && form.controls.displayName.invalid"
            />
          </div>

          <button type="submit" class="btn btn-primary" [disabled]="busy() || !changed()">
            {{ busy() ? 'Saving…' : 'Save name' }}
          </button>
        </form>
      </section>

      <section class="card card-pad section appearance">
        <div>
          <h2>Appearance</h2>
          <p class="muted text-sm">Follows your system setting unless you pick one.</p>
        </div>

        <div class="theme-choices" role="group" aria-label="Theme">
          @for (option of themeOptions; track option.value) {
            <button
              type="button"
              class="theme-choice"
              [class.selected]="theme.preference() === option.value"
              (click)="theme.preference.set(option.value)"
            >
              <app-icon [name]="option.icon" [size]="16" />
              {{ option.label }}
            </button>
          }
        </div>
      </section>

      <section class="card card-pad section memberships">
        <div class="panel-title">
          <h2>Teams you are in</h2>
        </div>

        @if (teams.hasTeams()) {
          <ul class="team-list">
            @for (team of teams.teams(); track team.id) {
              <li>
                <span class="grow truncate">{{ team.name }}</span>
                <span class="badge">{{ team.myRole }}</span>
              </li>
            }
          </ul>
        } @else {
          <p class="muted text-sm">You are not in any team yet.</p>
        }
      </section>
    }
  `,
  styleUrl: './profile.page.scss',
})
export class ProfilePage {
  private readonly auth = inject(AuthService);
  private readonly toasts = inject(ToastService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly teams = inject(TeamStore);
  protected readonly theme = inject(ThemeService);
  protected readonly user = this.auth.user;
  protected readonly busy = signal(false);

  protected readonly themeOptions = [
    { value: 'light' as const, label: 'Light', icon: 'sun' as const },
    { value: 'dark' as const, label: 'Dark', icon: 'moon' as const },
    { value: 'system' as const, label: 'System', icon: 'settings' as const },
  ];

  protected readonly form = this.formBuilder.nonNullable.group({
    displayName: [this.user()?.displayName ?? '', [Validators.required, Validators.maxLength(80)]],
  });

  private readonly draft = signal(this.user()?.displayName ?? '');

  protected readonly changed = computed(() => this.draft().trim() !== (this.user()?.displayName ?? ''));

  constructor() {
    this.form.controls.displayName.valueChanges.subscribe((value) => this.draft.set(value));
    void this.teams.load();
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);

    this.auth.updateDisplayName(this.form.getRawValue().displayName).subscribe({
      next: () => {
        this.busy.set(false);
        this.toasts.success('Your name has been updated.');
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.toasts.error(errorMessageOf(error));
      },
    });
  }
}
