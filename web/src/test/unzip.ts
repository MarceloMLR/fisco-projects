async function inflate(bytes: Uint8Array<ArrayBuffer>): Promise<Uint8Array> {
  const stream = new DecompressionStream('deflate-raw');
  const writer = stream.writable.getWriter();
  void writer.write(bytes).then(() => writer.close());
  return new Uint8Array(await new Response(stream.readable).arrayBuffer());
}

/** The files of a .zip, as text, for the tests of the files the screens make (stored or compressed with deflate). */
export async function unzip(bytes: Uint8Array<ArrayBuffer>): Promise<Map<string, string>> {
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  const decoder = new TextDecoder();
  const files = new Map<string, string>();
  let offset = 0;
  while (view.getUint32(offset, true) === 0x04034b50) {
    const method = view.getUint16(offset + 8, true);
    const size = view.getUint32(offset + 18, true);
    const nameLength = view.getUint16(offset + 26, true);
    const extraLength = view.getUint16(offset + 28, true);
    const name = decoder.decode(bytes.slice(offset + 30, offset + 30 + nameLength));
    const start = offset + 30 + nameLength + extraLength;
    const data = bytes.slice(start, start + size);
    const content = method === 8 ? await inflate(data) : data;
    files.set(name, decoder.decode(content));
    offset = start + size;
  }
  return files;
}

/** The bytes of a file the screen saved, to open with unzip. */
export function bytesOf(content: Blob): Promise<Uint8Array<ArrayBuffer>> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      resolve(new Uint8Array(reader.result as ArrayBuffer));
    };
    reader.onerror = () => {
      reject(new Error('The file could not be read.'));
    };
    reader.readAsArrayBuffer(content);
  });
}
