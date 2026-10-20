/**
 * A one-page PDF with lines of text, for the documents of the demonstration (ADR-0089): Helvetica in the encoding of
 * Windows, which has the letters with accents of Portuguese, on an A4 page.
 */
export function pdfOf(lines: readonly string[]): Uint8Array<ArrayBuffer> {
  const escape = (line: string) => line.replace(/[\\()]/g, (character) => `\\${character}`);
  const text = lines.map((line) => `(${escape(line)}) Tj T*`).join(' ');
  const stream = `BT /F1 12 Tf 16 TL 56 780 Td ${text} ET`;
  const objects = [
    '<< /Type /Catalog /Pages 2 0 R >>',
    '<< /Type /Pages /Kids [3 0 R] /Count 1 >>',
    '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>',
    `<< /Length ${String(stream.length)} >>\nstream\n${stream}\nendstream`,
    '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>',
  ];
  let body = '%PDF-1.4\n';
  const offsets: number[] = [];
  objects.forEach((object, index) => {
    offsets.push(body.length);
    body += `${String(index + 1)} 0 obj\n${object}\nendobj\n`;
  });
  const xref = body.length;
  body += `xref\n0 ${String(objects.length + 1)}\n0000000000 65535 f \n`;
  body += offsets.map((offset) => `${String(offset).padStart(10, '0')} 00000 n \n`).join('');
  body += `trailer\n<< /Size ${String(objects.length + 1)} /Root 1 0 R >>\nstartxref\n${String(xref)}\n%%EOF\n`;
  // One byte per character: the letters of Portuguese are all in Latin-1, as the encoding of Windows has them.
  return Uint8Array.from(body, (character) => character.charCodeAt(0) & 0xff);
}
