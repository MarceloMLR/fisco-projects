// That the person chose "Sair" in this browser, kept until the next sign-in (ADR-0153), so the link of Teams shows the
// sign-in page instead of going straight to Microsoft. Only the mark, as the last e-mail (ADR-0009); never a token
// (ADR-0054). The storage may be missing or blocked (private window, blocked site data), so every access is guarded.
const key = 'fiscoProjects.signedOut';

export function signedOutHere(): boolean {
  try {
    return window.localStorage.getItem(key) !== null;
  } catch {
    return false;
  }
}

export function rememberSignOut(): void {
  try {
    window.localStorage.setItem(key, '1');
  } catch {
    // Without storage the link of Teams goes to Microsoft, which asks for the account the browser does not have.
  }
}

export function forgetSignOut(): void {
  try {
    window.localStorage.removeItem(key);
  } catch {
    // Nothing kept, nothing to forget.
  }
}
