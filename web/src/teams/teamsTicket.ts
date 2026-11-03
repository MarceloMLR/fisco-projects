import { useEffect, useState } from 'react';
import { api, type TeamsTicket } from '../api/client';
import { connectToTeams, type TeamsHost, type TeamsScheme } from './teamsHost';

/** The number of the ticket of a path of the application, only "/tickets/{number}"; anything else is no ticket. */
export function ticketNumberOf(path: string): number | null {
  const match = /^\/tickets\/(\d{1,9})$/.exec(path);
  return match === null ? null : Number(match[1]);
}

/**
 * What the page in Teams shows (ADR-0152): the page of always (the brand and "Abrir no FiscoProjects"), with the
 * sentence that the ticket could not be shown when its reading failed; the ticket while it is read; the ticket; or that
 * the person does not see it.
 */
export type TeamsTicketState =
  | { kind: 'brand'; failed: boolean }
  | { kind: 'loading' }
  | { kind: 'ticket'; ticket: TeamsTicket }
  | { kind: 'noAccess' };

const brand: TeamsTicketState = { kind: 'brand', failed: false };

/**
 * Reads the ticket as the person of the token of Teams, only in the header of the reading (ADR-0152). A token refused
 * (401) is asked again once, as Teams renews it; refused again, the page of always.
 */
export async function readTeamsTicket(host: TeamsHost, number: number): Promise<TeamsTicketState> {
  for (let attempt = 0; attempt < 2; attempt += 1) {
    let token: string;
    try {
      token = await host.token();
    } catch {
      return brand;
    }
    try {
      const { data, response } = await api.GET('/api/teams/tickets/{number}', {
        params: { path: { number } },
        headers: { Authorization: `Bearer ${token}` },
      });
      if (data !== undefined) {
        return { kind: 'ticket', ticket: data };
      }
      if (response.status === 404) {
        return { kind: 'noAccess' };
      }
      if (response.status !== 401) {
        return { kind: 'brand', failed: true };
      }
    } catch {
      return { kind: 'brand', failed: true };
    }
  }
  return brand;
}

/**
 * The ticket of the notice, read by the page in Teams (ADR-0152). It lives only in the state of the page, never in the
 * cache of the queries: the token belongs to the page, not to a session, and the end of a session clears that cache.
 * The theme of Teams goes to <code>onScheme</code>, now and when it changes.
 */
export function useTeamsTicket(
  number: number | null,
  onScheme: (scheme: TeamsScheme) => void,
): TeamsTicketState {
  const [state, setState] = useState<TeamsTicketState>(
    number === null ? brand : { kind: 'loading' },
  );
  useEffect(() => {
    if (number === null) {
      return undefined;
    }
    // A function, so the checks after each wait read the value of now.
    let active = true;
    const open = () => active;
    void (async () => {
      const host = await connectToTeams();
      if (!open()) {
        return;
      }
      if (host === null) {
        setState(brand);
        return;
      }
      if (host.scheme !== undefined) {
        onScheme(host.scheme);
      }
      host.onScheme((scheme) => {
        if (open()) {
          onScheme(scheme);
        }
      });
      const read = await readTeamsTicket(host, number);
      if (open()) {
        setState(read);
      }
    })();
    return () => {
      active = false;
    };
  }, [number, onScheme]);
  return state;
}
