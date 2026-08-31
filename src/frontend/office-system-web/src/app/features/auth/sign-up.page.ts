import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { errorMessageOf, fieldErrorsOf } from '../../core/api/problem-details';
import { AuthService } from '../../core/auth/auth.service';
import { Icon } from '../../shared/icon/icon';
import { AuthShowcase } from './auth-showcase/auth-showcase';

const MIN_PASSWORD_LENGTH = 8;

@Component({
  selector: 'app-sign-up-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, Icon, AuthShowcase],
  template: `
    <app-auth-showcase />

    <section class="form-side">
      <div class="form-wrap">
        <div class="mobile-brand">
          <span class="brand-mark"><app-icon name="calendar" [size]="20" /></span>
          Office Days
        </div>

        <header class="form-head">
          <h2>Create your account</h2>
          <p>Then create a team, or join one with an invite code.</p>
        </header>

        @if (formError()) {
          <p class="form-error" role="alert">
            <app-icon name="alert" [size]="16" />
            <span>{{ formError() }}</span>
          </p>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label class="label" for="displayName">Your name</label>
            <input
              id="displayName"
              class="input"
              type="text"
              formControlName="displayName"
              autocomplete="name"
              placeholder="Ada Lovelace"
              [class.invalid]="isInvalid('displayName')"
            />
            @if (isInvalid('displayName')) {
              <span class="field-error">{{ messageFor('displayName', 'Tell us your name.') }}</span>
            }
          </div>

          <div class="field">
            <label class="label" for="email">Work email</label>
            <input
              id="email"
              class="input"
              type="email"
              formControlName="email"
              autocomplete="email"
              placeholder="you@company.com"
              [class.invalid]="isInvalid('email')"
            />
            @if (isInvalid('email')) {
              <span class="field-error">{{ messageFor('email', 'Enter a valid email address.') }}</span>
            }
          </div>

          <div class="field">
            <label class="label" for="password">Password</label>
            <input
              id="password"
              class="input"
              type="password"
              formControlName="password"
              autocomplete="new-password"
              placeholder="At least 8 characters"
              [class.invalid]="isInvalid('password')"
            />

            <div class="strength" aria-hidden="true">
              @for (bar of [0, 1, 2]; track bar) {
                <span class="strength-bar" [class]="barClass(bar)"></span>
              }
            </div>

            @if (isInvalid('password')) {
              <span class="field-error">{{ messageFor('password', passwordHint()) }}</span>
            } @else {
              <span class="field-hint">{{ passwordHint() }}</span>
            }
          </div>

          <button type="submit" class="btn btn-primary btn-lg btn-block" [disabled]="busy()">
            {{ busy() ? 'Creating your account…' : 'Create account' }}
          </button>
        </form>

        <p class="form-foot">
          Already have an account? <a routerLink="/sign-in">Sign in</a>
        </p>
      </div>
    </section>
  `,
  styleUrl: './auth-layout.scss',
})
export class SignUpPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly busy = signal(false);
  protected readonly formError = signal<string | null>(null);

  private readonly serverFieldErrors = signal<Record<string, string>>({});

  protected readonly form = this.formBuilder.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(80)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(MIN_PASSWORD_LENGTH), varietyValidator]],
  });

  private readonly password = toSignal(this.form.controls.password.valueChanges, { initialValue: '' });

  /** 0 = empty, 1 = too short, 2 = long enough, 3 = long enough and mixed. */
  protected readonly strength = computed(() => {
    const value = this.password();

    if (!value) {
      return 0;
    }

    if (value.length < MIN_PASSWORD_LENGTH) {
      return 1;
    }

    return hasVariety(value) ? 3 : 2;
  });

  protected readonly passwordHint = computed(() => {
    switch (this.strength()) {
      case 0:
        return `At least ${MIN_PASSWORD_LENGTH} characters, mixing letters with a number or symbol.`;
      case 1:
        return `${MIN_PASSWORD_LENGTH - this.password().length} more character(s) needed.`;
      case 2:
        return 'Add a number or a symbol.';
      default:
        return 'Looks good.';
    }
  });

  protected barClass(index: number): string {
    const strength = this.strength();

    if (strength === 0 || index >= strength) {
      return '';
    }

    switch (strength) {
      case 1:
        return 'on-weak';
      case 2:
        return 'on-fair';
      default:
        return 'on-strong';
    }
  }

  protected isInvalid(control: 'displayName' | 'email' | 'password'): boolean {
    const field = this.form.controls[control];

    return (field.touched && field.invalid) || control in this.serverFieldErrors();
  }

  protected messageFor(control: 'displayName' | 'email' | 'password', fallback: string): string {
    return this.serverFieldErrors()[control] ?? fallback;
  }

  protected submit(): void {
    this.formError.set(null);
    this.serverFieldErrors.set({});

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, displayName, password } = this.form.getRawValue();
    this.busy.set(true);

    this.auth.register(email, displayName, password).subscribe({
      next: () => {
        this.busy.set(false);
        void this.router.navigate(['/teams']);
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.serverFieldErrors.set(fieldErrorsOf(error));
        this.formError.set(errorMessageOf(error));
      },
    });
  }
}

function hasVariety(value: string): boolean {
  return /[a-z]/i.test(value) && /[^a-z]/i.test(value);
}

/** Mirrors the server's rule, so the message arrives before the round trip. */
function varietyValidator(control: { value: string }): Record<string, true> | null {
  const value = control.value ?? '';

  return !value || hasVariety(value) ? null : { variety: true };
}
