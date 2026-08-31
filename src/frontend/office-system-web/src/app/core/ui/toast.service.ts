import { Injectable, signal } from '@angular/core';

export type ToastTone = 'success' | 'error' | 'info';

export interface Toast {
  readonly id: number;
  readonly tone: ToastTone;
  readonly message: string;
}

const DISMISS_AFTER_MS = 4200;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;

  readonly toasts = signal<readonly Toast[]>([]);

  success(message: string): void {
    this.push('success', message);
  }

  error(message: string): void {
    this.push('error', message);
  }

  info(message: string): void {
    this.push('info', message);
  }

  dismiss(id: number): void {
    this.toasts.update((current) => current.filter((toast) => toast.id !== id));
  }

  private push(tone: ToastTone, message: string): void {
    const id = this.nextId++;

    this.toasts.update((current) => [...current, { id, tone, message }]);
    setTimeout(() => this.dismiss(id), DISMISS_AFTER_MS);
  }
}
