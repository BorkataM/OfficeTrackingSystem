import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { initialsOf } from '../attendance';

@Component({
  selector: 'app-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="avatar"
      [class.avatar-sm]="size() === 'sm'"
      [class.avatar-lg]="size() === 'lg'"
      [style.background]="color()"
      [attr.title]="name()"
      [attr.aria-label]="name()"
    >{{ initials() }}</span>
  `,
  styles: `
    .avatar {
      display: grid;
      place-items: center;
      flex: none;
      width: 32px;
      height: 32px;
      border-radius: var(--radius-pill);
      color: #fff;
      font-size: var(--text-xs);
      font-weight: 650;
      letter-spacing: 0.02em;
      /* A hairline ring keeps light avatars legible on white surfaces. */
      box-shadow: inset 0 0 0 1px rgb(255 255 255 / 22%);
      user-select: none;
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
  `,
})
export class Avatar {
  readonly name = input.required<string>();
  readonly color = input<string>('#6366f1');
  readonly size = input<'sm' | 'md' | 'lg'>('md');

  readonly initials = computed(() => initialsOf(this.name()));
}
