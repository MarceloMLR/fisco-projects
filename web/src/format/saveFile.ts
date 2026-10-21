/** Gives the browser a file made on the screen to save, with its name, as a download link would (ADR-0099). */
export function saveFile(content: Blob, name: string): void {
  const address = URL.createObjectURL(content);
  const link = document.createElement('a');
  link.href = address;
  link.download = name;
  link.style.display = 'none';
  document.body.append(link);
  link.click();
  link.remove();
  // The browser has the file once the click is handled.
  setTimeout(() => {
    URL.revokeObjectURL(address);
  }, 0);
}
