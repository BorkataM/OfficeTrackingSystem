import { createAvatar } from '@dicebear/core';
import { glass, lorelei, notionists, openPeeps, shapes, thumbs } from '@dicebear/collection';

export interface AvatarStyle {
  readonly id: string;
  readonly label: string;
}

const RENDERERS = {
  notionists,
  lorelei,
  'open-peeps': openPeeps,
  thumbs,
  shapes,
  glass,
} as const;

type AvatarStyleId = keyof typeof RENDERERS;

export const AVATAR_STYLES: readonly AvatarStyle[] = [
  { id: 'notionists', label: 'Sketch' },
  { id: 'lorelei', label: 'Portrait' },
  { id: 'open-peeps', label: 'Doodle' },
  { id: 'thumbs', label: 'Thumbs' },
  { id: 'shapes', label: 'Shapes' },
  { id: 'glass', label: 'Glass' },
];

const cache = new Map<string, string>();

/** "style:seed" → an SVG data URI, or null for anything this build cannot draw. */
export function avatarImage(avatar: string | null | undefined): string | null {
  if (!avatar) {
    return null;
  }

  const cached = cache.get(avatar);

  if (cached) {
    return cached;
  }

  const [style, seed] = avatar.split(':');
  const renderer = RENDERERS[style as AvatarStyleId];

  if (!renderer || !seed) {
    return null;
  }

  const uri = createAvatar(renderer as never, { seed, backgroundColor: ['transparent'] }).toDataUri();
  cache.set(avatar, uri);

  return uri;
}

export function randomSeed(): string {
  return Math.random().toString(36).slice(2, 10);
}
