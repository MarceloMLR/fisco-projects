import { queryOptions } from '@tanstack/react-query';
import { api } from './client';
import type { operations } from './schema';

/** Filters of the list of users, as the contract has them. */
export type UsersParams = NonNullable<operations['ListUsers']['parameters']['query']>;
/** Filters of the audit, as the contract has them. */
export type AuditParams = NonNullable<operations['ListAuditEntries']['parameters']['query']>;

/** An answer of the API without data, with its status and the Problem Details (RFC 9457) that came with it. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly problem: unknown,
  ) {
    super(`The API answered ${String(status)}.`);
    this.name = 'ApiError';
  }
}

/** The data of a call of the typed client; an answer without data becomes an ApiError. */
async function dataOf<Data>(call: Promise<{ data?: Data; error?: unknown; response: Response }>) {
  const { data, error, response } = await call;
  if (data === undefined) {
    throw new ApiError(response.status, error);
  }
  return data;
}

/**
 * The start of the keys of each resource, to ask again for everything that shows it after a change (ADR-0080).
 * Every key of a resource starts with its entry here.
 */
export const queryKeys = {
  users: ['users'],
  audit: ['audit'],
} as const;

/** A page of the users, with the filters of the Users screen; with pageSize 1, only the count. */
export function usersQuery(params: UsersParams) {
  return queryOptions({
    queryKey: [...queryKeys.users, 'list', params] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/users', { params: { query: params }, signal })),
  });
}

/** A page of the audit, with the filters of the Audit screen. */
export function auditQuery(params: AuditParams) {
  return queryOptions({
    queryKey: [...queryKeys.audit, 'list', params] as const,
    queryFn: ({ signal }) => dataOf(api.GET('/api/audit', { params: { query: params }, signal })),
  });
}
