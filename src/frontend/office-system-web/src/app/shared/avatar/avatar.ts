import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { initialsOf } from '../attendance';
import { avatarImage } from './avatar-styles';

@Component({
  selector: 'app-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="avatar"
      [class.avatar-sm]="size() === 'sm'"
      [class.avatar-lg]="size() === 'lg'"
      [class.avatar-xl]="size() === 'xl'"
      [style.--avatar-color]="color()"
      [attr.title]="name()"
      [attr.aria-label]="name()"
      [class.avatar-image]="image()"
    >
      @if (image(); as src) {
        <img [src]="src" alt="" />
      } @else {
        {{ initials() }}
      }
    </span>
  `,
  styles: `
    .avatar {
      display: grid;
      place-items: center;
      flex: none;
      width: 32px;
      height: 32px;
      border-radius: var(--radius-pill);
      background: color-mix(in oklab, var(--avatar-color) 82%, #18181b);
      color: #fff;
      font-size: var(--text-xs);
      font-weight: 500;
      letter-spacing: 0.02em;
      user-select: none;
    }

    .avatar-image {
      overflow: hidden;
      background: var(--surface-sunken);
      box-shadow: inset 0 0 0 1px var(--border);
    }

    .avatar-image img {
      width: 100%;
      height: 100%;
    }

    .avatar-sm {
      width: 26px;
      height: 26px;
      font-size: 0.6875rem;
    }

    .avatar-lg {
      width: 44px;
      height: 44px;
      font-size: var(--text-base);
    }

    .avatar-xl {
      width: 72px;
      height: 72px;
      font-size: var(--text-xl);
    }
  `,
})
export class Avatar {
  readonly name = input.required<string>();
  readonly color = input<string>('#6366f1');
  readonly avatar = input<string | null | undefined>(null);
  readonly size = input<'sm' | 'md' | 'lg' | 'xl'>('md');

  readonly image = computed(() => avatarImage(this.avatar()));

  readonly initials = computed(() => initialsOf(this.name()));
}
