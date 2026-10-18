import { GridLoadingOverlay, type GridLoadingOverlayProps } from '@mui/x-data-grid';
import { useEffect, useState } from 'react';

/** How long a table waits before showing that it is loading: a shorter wait shows nothing, so the screen does not blink. */
const loadingDelay = 300;

/**
 * Loading indicator of the standard table: nothing in the first moment, then the Data Grid's own indicator, a bar
 * over the rows on screen or, without rows, the skeleton of the rows. While it waits, a table without rows shows
 * only the header, never the message of an empty list. The Data Grid mounts it when the wait starts and removes it
 * when the rows arrive, so the moment counts from the start of each wait.
 */
export function DelayedLoadingOverlay(props: GridLoadingOverlayProps) {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setVisible(true);
    }, loadingDelay);
    return () => {
      window.clearTimeout(timer);
    };
  }, []);

  if (!visible) {
    return null;
  }
  // Transparent, so the bar does not hide the rows of the list being replaced.
  return <GridLoadingOverlay {...props} style={{ ...props.style, backgroundColor: 'transparent' }} />;
}
