// The last e-mail used to sign in, kept in the browser (ADR-0009). Nothing of the session goes here.
// The storage may be missing or blocked (private window, blocked site data), so every access is guarded.
const key = 'fiscoProjects.signInEmail';

export function rememberedEmail(): string {
  try {
    return window.localStorage.getItem(key) ?? '';
  } catch {
    return '';
  }
}

export function rememberEmail(email: string): void {
  try {
    window.localStorage.setItem(key, email);
  } catch {
    // Without storage the field just starts empty next time.
  }
}
