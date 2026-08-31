import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { errorMessageOf, fieldErrorsOf } from '../../core/api/problem-details';
import { TeamsApi } from '../../core/api/teams.api';
import { TeamStore } from '../../core/teams/team.store';
import { ToastService } from '../../core/ui/toast.service';
import { Icon } from '../../shared/icon/icon';

const JOIN_CODE_LENGTH = 8;

@Component({
  selector: 'app-teams-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Icon],
  templateUrl: './teams.page.html',
  styleUrl: './teams.page.scss',
})
export class TeamsPage {
  private readonly api = inject(TeamsApi);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly teams = inject(TeamStore);

  protected readonly mode = signal<'none' | 'create' | 'join'>('none');
  protected readonly busy = signal(false);
  protected readonly createError = signal<string | null>(null);
  protected readonly joinError = signal<string | null>(null);

  protected readonly createForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(80)]],
    description: ['', [Validators.maxLength(280)]],
  });

  protected readonly joinForm = this.formBuilder.nonNullable.group({
    joinCode: [
      '',
      [Validators.required, Validators.minLength(JOIN_CODE_LENGTH), Validators.maxLength(JOIN_CODE_LENGTH)],
    ],
  });

  constructor() {
    void this.teams.load(true);
  }

  protected show(mode: 'none' | 'create' | 'join'): void {
    this.createError.set(null);
    this.joinError.set(null);
    this.mode.set(mode);
  }

  protected open(teamId: string): void {
    this.teams.select(teamId);
    void this.router.navigate(['/schedule']);
  }

  /** Join codes are stored upper-case; normalising as the user types avoids a needless 400. */
  protected normaliseJoinCode(): void {
    const control = this.joinForm.controls.joinCode;
    const cleaned = control.value.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, JOIN_CODE_LENGTH);

    if (cleaned !== control.value) {
      control.setValue(cleaned, { emitEvent: false });
    }
  }

  protected async create(): Promise<void> {
    this.createError.set(null);

    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const { name, description } = this.createForm.getRawValue();
    this.busy.set(true);

    try {
      const team = await firstValueFrom(this.api.create(name, description.trim() || null));

      this.teams.adopt(team);
      this.toasts.success(`${team.name} is ready. Share code ${team.joinCode} to invite people.`);
      void this.router.navigate(['/schedule']);
    } catch (error) {
      this.createError.set(firstFieldMessage(error) ?? errorMessageOf(error));
    } finally {
      this.busy.set(false);
    }
  }

  protected async join(): Promise<void> {
    this.joinError.set(null);

    if (this.joinForm.invalid) {
      this.joinForm.markAllAsTouched();
      return;
    }

    const { joinCode } = this.joinForm.getRawValue();
    this.busy.set(true);

    try {
      const team = await firstValueFrom(this.api.join(joinCode));

      this.teams.adopt(team);
      this.toasts.success(`You have joined ${team.name}.`);
      void this.router.navigate(['/schedule']);
    } catch (error) {
      this.joinError.set(firstFieldMessage(error) ?? errorMessageOf(error));
    } finally {
      this.busy.set(false);
    }
  }
}

function firstFieldMessage(error: unknown): string | null {
  return Object.values(fieldErrorsOf(error))[0] ?? null;
}
