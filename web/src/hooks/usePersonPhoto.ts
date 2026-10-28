import { useQuery } from '@tanstack/react-query';
import { useCallback } from 'react';
import { signInOptionsQuery } from '../api/queries';

/**
 * The address of the photo of a person in Microsoft 365 (ADR-0124), for the avatar: only when the installation signs
 * in through Entra ID, since the local sign-in and the demonstration have no photos and their avatars ask for none.
 */
export function usePersonPhoto(): (personId: string) => string | undefined {
  const { data } = useQuery(signInOptionsQuery());
  const photos = data?.entraSignIn === true;
  return useCallback(
    (personId: string) => (photos ? `/api/people/${personId}/photo` : undefined),
    [photos],
  );
}
