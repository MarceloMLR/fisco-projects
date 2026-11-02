import type { ChangeEvent } from 'react';
import { api, installationProblems } from '../api/client';

/** Up to 1 MB, PNG or SVG, as the API checks (LogoFile in the Administration module). */
const logoMaxBytes = 1024 * 1024;
const logoTypes = ['image/png', 'image/svg+xml'];

export type LogoError = 'logoFormat' | 'logoTooLarge' | 'logoUnsafe';

/** A logo in the form: the one saved, a new file with its picture, or removed. */
export type LogoChoice =
  { kind: 'saved' } | { kind: 'new'; file: File; picture: string } | { kind: 'removed' };

/** What the form shows for a logo: the new picture, nothing once removed, or the one saved. */
export function shownLogo(choice: LogoChoice, saved: string | null): string | null {
  if (choice.kind === 'new') {
    return choice.picture;
  }
  return choice.kind === 'removed' ? null : saved;
}

function isLogoFile(file: File): boolean {
  const name = file.name.toLowerCase();
  return logoTypes.includes(file.type) || name.endsWith('.png') || name.endsWith('.svg');
}

function readPicture(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      resolve(typeof reader.result === 'string' ? reader.result : '');
    };
    reader.onerror = () => {
      reject(reader.error ?? new Error('The file could not be read.'));
    };
    reader.readAsDataURL(file);
  });
}

/**
 * Reads the file chosen for a logo: a file of another format or above 1 MB is refused at once, as the API would; the
 * same file can be chosen again after a refusal.
 */
export function logoChooser(
  choose: (choice: LogoChoice) => void,
  refuse: (error: LogoError | undefined) => void,
) {
  return async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (file === undefined) {
      return;
    }
    if (!isLogoFile(file)) {
      refuse('logoFormat');
      return;
    }
    if (file.size > logoMaxBytes) {
      refuse('logoTooLarge');
      return;
    }
    refuse(undefined);
    choose({ kind: 'new', file, picture: await readPicture(file) });
  };
}

/** Sends or removes the logo of a theme, if it changed; the error of a refused file, or nothing. */
export async function saveLogo(choice: LogoChoice, dark: boolean): Promise<LogoError | undefined> {
  if (choice.kind === 'new') {
    const body = new FormData();
    body.append('file', choice.file, choice.file.name);
    const { error, response } = dark
      ? await api.PUT('/api/installation/logo/dark', { body: {}, bodySerializer: () => body })
      : await api.PUT('/api/installation/logo', { body: {}, bodySerializer: () => body });
    if (response.status === 400 || response.status === 413) {
      return response.status === 413 ? 'logoTooLarge' : logoErrorOf(error?.type);
    }
    if (!response.ok) {
      throw new Error(`The API answered ${String(response.status)}.`);
    }
  } else if (choice.kind === 'removed') {
    const { response } = dark
      ? await api.DELETE('/api/installation/logo/dark')
      : await api.DELETE('/api/installation/logo');
    if (!response.ok) {
      throw new Error(`The API answered ${String(response.status)}.`);
    }
  }
  return undefined;
}

function logoErrorOf(type: unknown): LogoError {
  if (type === installationProblems.logoTooLarge) {
    return 'logoTooLarge';
  }
  return type === installationProblems.logoUnsafe ? 'logoUnsafe' : 'logoFormat';
}
