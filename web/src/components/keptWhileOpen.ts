import { useState } from 'react';

/**
 * What a window shows, kept while it is closing (ADR-0133): open, the values of now; closed, the last ones it showed,
 * so its way out shows the same content even when the screen already cleared the state the values came from.
 */
export function useKeptWhileOpen<T extends readonly unknown[]>(open: boolean, values: T): T {
  const [kept, setKept] = useState(values);
  if (open && values.some((value, index) => !Object.is(value, kept[index]))) {
    setKept(values);
  }
  return open ? values : kept;
}
