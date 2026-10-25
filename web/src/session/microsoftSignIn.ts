/**
 * Leaves the application for the sign-in page of Microsoft (ADR-0117): the API answers the address, and the browser
 * goes there for the password and the MFA. An object of its own, so the tests can watch where the page would go.
 */
export const microsoftSignIn = {
  go(location: string): void {
    window.location.assign(location);
  },
};
