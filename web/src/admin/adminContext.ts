import { useOutletContext } from 'react-router';

/** What the Administration area gives its sections. */
export interface AdminContext {
  /** People of the installation, without filters; null while unknown. */
  usersTotal: number | null;
  /** Clients of the installation, without filters; null while unknown. */
  clientsTotal: number | null;
}

export function useAdminContext(): AdminContext {
  return useOutletContext<AdminContext>();
}
