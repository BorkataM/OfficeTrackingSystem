import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../../core/ui/toast.service';
import { Icon } from '../icon/icon';

@Component({
  selector: 'app-toast-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  template: `
    <div class="host" aria-live="polite" aria-atomic="true">
      @for (toast of toasts.toasts(); track toast.id) {
        <div class="toast" [class]="'toast-' + toast.tone" role="status">
          <app-icon
            [name]="toast.tone === 'success' ? 'check' : toast.tone === 'error' ? 'alert' : 'info'"
            [size]="16"
          />
          <span class="grow">{{ toast.message }}</span>
          <button type="button" class="dismiss" (click)="toasts.dismiss(toast.id)" aria-label="Dismiss">
            <app-icon name="close" [size]="14" />
          </button>
        </div>
      }
    </div>
  `,
  styles: `
    .host {
      position: fixed;
      right: var(--space-5);
      bottom: var(--space-5);
      z-index: 100;
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      max-width: min(380px, calc(100vw - 2 * var(--space-5)));
      pointer-events: none;
    }

    .toast {
      display: flex;
      align-items: flex-start;
      gap: var(--space-3);
      padding: var(--space-3) var(--space-3) var(--space-3) var(--space-4);
      background: var(--surface-raised);
      border: 1px solid var(--border);
      border-left: 3px solid var(--text-subtle);
      border-radius: var(--radius-md);
      box-shadow: var(--shadow-lg);
      font-size: var(--text-base);
      pointer-events: auto;
      animation: slide-in var(--duration-slow) var(--ease);
    }

    .toast app-icon {
      margin-top: 2px;
      flex: none;
    }

    .toast-success {
      border-left-color: var(--success);
    }

    .toast-success > app-icon {
      color: var(--success);
    }

    .toast-error {
      border-left-color: var(--danger);
    }

    .toast-error > app-icon {
      color: var(--danger);
    }

    .toast-info {
      border-left-color: var(--brand-500);
    }

    .toast-info > app-icon {
      color: var(--brand-500);
    }

    .dismiss {
      display: grid;
      place-items: center;
      flex: none;
      width: 22px;
      height: 22px;
      margin-top: 1px;
      border-radius: var(--radius-xs);
      color: var(--text-subtle);
    }

    .dismiss:hover {
      background: var(--surface-hover);
      color: var(--text);
    }

    @keyframes slide-in {
      from {
        opacity: 0;
        transform: translateY(10px) scale(0.97);
      }

      to {
        opacity: 1;
        transform: none;
      }
    }
  `,
})
export class ToastHost {
  protected readonly toasts = inject(ToastService);
}
