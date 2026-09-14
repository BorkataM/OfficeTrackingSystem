import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { errorMessageOf, fieldErrorsOf } from '../../core/api/problem-details';
import { AuthService } from '../../core/auth/auth.service';
import { Icon } from '../../shared/icon/icon';
import { AuthShowcase } from './auth-showcase/auth-showcase';

@Component({
  selector: 'app-sign-in-page',
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
          <h2>Welcome back</h2>
          <p>Sign in to see your team's week.</p>
        </header>

        @if (formError()) {
          <p class="form-error" role="alert">
            <app-icon name="alert" [size]="16" />
            <span>{{ formError() }}</span>
          </p>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label class="label" for="email">Email</label>
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
              <span class="field-error">{{ messageFor('email', 'Enter your email address.') }}</span>
            }
          </div>

          <div class="field">
            <label class="label" for="password">Password</label>
            <input
              id="password"
              class="input"
              type="password"
              formControlName="password"
              autocomplete="current-password"
              placeholder="••••••••"
              [class.invalid]="isInvalid('password')"
            />
            @if (isInvalid('password')) {
              <span class="field-error">{{ messageFor('password', 'Enter your password.') }}</span>
            }
          </div>

          <button type="submit" class="btn btn-primary btn-lg btn-block" [disabled]="busy()">
            {{ busy() ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>

        <p class="form-foot">
          New here? <a routerLink="/sign-up">Create an account</a>
        </p>
      </div>
    </section>
  `,
  styleUrl: './auth-layout.scss',
})
export class SignInPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly busy = signal(false);
  protected readonly formError = signal<string | null>(null);

  private readonly serverFieldErrors = signal<Record<string, string>>({});

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  protected isInvalid(control: 'email' | 'password'): boolean {
    const field = this.form.controls[control];

    return (field.touched && field.invalid) || control in this.serverFieldErrors();
  }

  protected messageFor(control: 'email' | 'password', fallback: string): string {
    return this.serverFieldErrors()[control] ?? fallback;
  }

  protected submit(): void {
    this.formError.set(null);
    this.serverFieldErrors.set({});

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    this.busy.set(true);

    this.auth.login(email, password).subscribe({
      next: () => {
        this.busy.set(false);
        void this.router.navigateByUrl(this.returnUrl());
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.serverFieldErrors.set(fieldErrorsOf(error));
        this.formError.set(errorMessageOf(error));
      },
    });
  }

  /** Honours ?returnUrl so a deep link survives the sign-in detour. */
  private returnUrl(): string {
    const requested = new URLSearchParams(window.location.search).get('returnUrl');

    // Only same-app paths are followed, never an absolute URL from the query string.
    return requested?.startsWith('/') && !requested.startsWith('//') ? requested : '/';
  }
}
