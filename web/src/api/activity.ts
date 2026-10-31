let started = 0;

/** Counts a call to the API, made by the client of the API (client.ts). */
export function noteRequest() {
  started += 1;
}

/**
 * How many calls to the API the page has made: an action that made none did not wait for the server, as a form refused
 * by its own checks or a copy, so its button does not stay busy after it (ADR-0132).
 */
export function requestsStarted() {
  return started;
}
