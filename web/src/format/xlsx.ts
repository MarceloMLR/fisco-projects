// A small writer of spreadsheets in the format of Excel (Office Open XML, .xlsx), for the reports of the screens
// (ADR-0099): sheets with text, numbers and dates, a header with the filter of Excel and the first row fixed, the widths
// of the columns and text that breaks into lines. The file is a .zip of XML parts, compressed by the browser.

/**
 * A cell: text, a label in bold, an indented text, a number, a percentage (0 to 1), a date and time as the spreadsheet
 * keeps it (toSpreadsheetDateTime) or a day without time (spreadsheetDay).
 */
export type XlsxCell =
  | string
  | number
  | { label: string }
  | { indented: string }
  | { percent: number }
  | { dateTime: number }
  | { day: number };

export interface XlsxSheet {
  /** The name of the tab, up to 31 characters. */
  name: string;
  /** The width of each column, in characters. */
  widths: readonly number[];
  rows: readonly (readonly XlsxCell[])[];
  /** The first row is the header: in bold over a fill, fixed when scrolling and with the filter of Excel. */
  header: boolean;
}

/** The colors of the header and of the lines, as RRGGBB, taken from the tokens of the design system. */
export interface XlsxColors {
  headerFill: string;
  headerText: string;
  border: string;
}

const encoder = new TextEncoder();

const mainNamespace = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main';
const relationshipsNamespace =
  'http://schemas.openxmlformats.org/officeDocument/2006/relationships';
const packageRelationships = 'http://schemas.openxmlformats.org/package/2006/relationships';

// Characters XML does not take, even escaped: the controls but the tab and the line breaks.
// eslint-disable-next-line no-control-regex
const notInXml = /[\u0000-\u0008\u000B\u000C\u000E-\u001F￾￿]/g;

/** Text inside XML, with the special characters escaped and the ones XML does not take removed. */
export function xmlText(text: string): string {
  return text
    .replace(notInXml, '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

const dayMilliseconds = 24 * 60 * 60 * 1000;

/** A day as the spreadsheet keeps it: the days since 30/12/1899, from a day without time zone (yyyy-mm-dd). */
export function spreadsheetDay(day: string): number {
  const [year = 0, month = 1, date = 1] = day.split('-').map(Number);
  return (Date.UTC(year, month - 1, date) - Date.UTC(1899, 11, 30)) / dayMilliseconds;
}

/** The name of a column: A, B, ..., Z, AA, AB... */
export function columnName(index: number): string {
  let name = '';
  for (let rest = index + 1; rest > 0; rest = Math.floor((rest - 1) / 26)) {
    name = String.fromCharCode(65 + ((rest - 1) % 26)) + name;
  }
  return name;
}

// The styles, by their place in cellXfs.
const style = {
  plain: 0,
  header: 1,
  text: 2,
  dateTime: 3,
  number: 4,
  label: 5,
  day: 6,
  percent: 7,
  indented: 8,
} as const;

function stylesXml(colors: XlsxColors): string {
  const argb = (rgb: string) => `FF${rgb.toUpperCase()}`;
  const side = (name: string) =>
    `<${name} style="thin"><color rgb="${argb(colors.border)}"/></${name}>`;
  return [
    `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`,
    `<styleSheet xmlns="${mainNamespace}">`,
    `<numFmts count="2"><numFmt numFmtId="164" formatCode="dd/mm/yyyy hh:mm"/><numFmt numFmtId="165" formatCode="dd/mm/yyyy"/></numFmts>`,
    `<fonts count="2">`,
    `<font><sz val="11"/><name val="Calibri"/><family val="2"/></font>`,
    `<font><b/><sz val="11"/><color rgb="${argb(colors.headerText)}"/><name val="Calibri"/><family val="2"/></font>`,
    `</fonts>`,
    `<fills count="3">`,
    `<fill><patternFill patternType="none"/></fill>`,
    `<fill><patternFill patternType="gray125"/></fill>`,
    `<fill><patternFill patternType="solid"><fgColor rgb="${argb(colors.headerFill)}"/><bgColor indexed="64"/></patternFill></fill>`,
    `</fills>`,
    `<borders count="2">`,
    `<border><left/><right/><top/><bottom/><diagonal/></border>`,
    `<border>${side('left')}${side('right')}${side('top')}${side('bottom')}<diagonal/></border>`,
    `</borders>`,
    `<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>`,
    `<cellXfs count="9">`,
    `<xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>`,
    `<xf numFmtId="0" fontId="1" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>`,
    `<xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyAlignment="1"><alignment vertical="top" wrapText="1"/></xf>`,
    `<xf numFmtId="164" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyBorder="1" applyAlignment="1"><alignment horizontal="left" vertical="top"/></xf>`,
    `<xf numFmtId="3" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyBorder="1" applyAlignment="1"><alignment horizontal="left" vertical="top"/></xf>`,
    `<xf numFmtId="0" fontId="1" fillId="0" borderId="1" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment vertical="top" wrapText="1"/></xf>`,
    `<xf numFmtId="165" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyBorder="1" applyAlignment="1"><alignment horizontal="left" vertical="top"/></xf>`,
    `<xf numFmtId="9" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyBorder="1" applyAlignment="1"><alignment horizontal="left" vertical="top"/></xf>`,
    `<xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyAlignment="1"><alignment vertical="top" indent="1" wrapText="1"/></xf>`,
    `</cellXfs>`,
    `<cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>`,
    `</styleSheet>`,
  ].join('');
}

function cellXml(cell: XlsxCell, reference: string, header: boolean): string {
  if (typeof cell === 'number') {
    return `<c r="${reference}" s="${String(style.number)}"><v>${String(cell)}</v></c>`;
  }
  if (typeof cell === 'object' && 'dateTime' in cell) {
    return `<c r="${reference}" s="${String(style.dateTime)}"><v>${String(cell.dateTime)}</v></c>`;
  }
  if (typeof cell === 'object' && 'day' in cell) {
    return `<c r="${reference}" s="${String(style.day)}"><v>${String(cell.day)}</v></c>`;
  }
  if (typeof cell === 'object' && 'percent' in cell) {
    return `<c r="${reference}" s="${String(style.percent)}"><v>${String(cell.percent)}</v></c>`;
  }
  // An empty text keeps only the lines of the cell.
  if (cell === '' && !header) {
    return `<c r="${reference}" s="${String(style.text)}"/>`;
  }
  const [text, cellStyle] =
    typeof cell === 'string'
      ? [cell, header ? style.header : style.text]
      : 'indented' in cell
        ? [cell.indented, header ? style.header : style.indented]
        : [cell.label, header ? style.header : style.label];
  return `<c r="${reference}" t="inlineStr" s="${String(cellStyle)}"><is><t xml:space="preserve">${xmlText(text)}</t></is></c>`;
}

/** The range the sheet fills, such as A1:F120. */
function rangeOf(sheet: XlsxSheet): string {
  const columns = Math.max(1, sheet.widths.length, ...sheet.rows.map((row) => row.length));
  return `A1:${columnName(columns - 1)}${String(Math.max(1, sheet.rows.length))}`;
}

function sheetXml(sheet: XlsxSheet): string {
  const rows = sheet.rows
    .map((row, rowIndex) => {
      const number = String(rowIndex + 1);
      const header = sheet.header && rowIndex === 0;
      const cells = row
        .map((cell, columnIndex) => cellXml(cell, `${columnName(columnIndex)}${number}`, header))
        .join('');
      return `<row r="${number}">${cells}</row>`;
    })
    .join('');
  const columns = sheet.widths
    .map((width, index) => {
      const column = String(index + 1);
      return `<col min="${column}" max="${column}" width="${String(width)}" customWidth="1"/>`;
    })
    .join('');
  const frozen = sheet.header
    ? `<pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/><selection pane="bottomLeft" activeCell="A2" sqref="A2"/>`
    : '';
  return [
    `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`,
    `<worksheet xmlns="${mainNamespace}" xmlns:r="${relationshipsNamespace}">`,
    `<dimension ref="${rangeOf(sheet)}"/>`,
    `<sheetViews><sheetView workbookViewId="0">${frozen}</sheetView></sheetViews>`,
    `<sheetFormatPr defaultRowHeight="15"/>`,
    columns === '' ? '' : `<cols>${columns}</cols>`,
    `<sheetData>${rows}</sheetData>`,
    sheet.header && sheet.rows.length > 0 ? `<autoFilter ref="${rangeOf(sheet)}"/>` : '',
    `<pageMargins left="0.5" right="0.5" top="0.75" bottom="0.75" header="0.3" footer="0.3"/>`,
    `</worksheet>`,
  ].join('');
}

/** The name of a tab as Excel takes it: up to 31 characters, without : \ / ? * [ ]. */
function tabName(name: string): string {
  return name.replace(/[:\\/?*[\]]/g, ' ').slice(0, 31);
}

function workbookXml(sheets: readonly XlsxSheet[]): string {
  const tabs = sheets
    .map(
      (sheet, index) =>
        `<sheet name="${xmlText(tabName(sheet.name))}" sheetId="${String(index + 1)}" r:id="rId${String(index + 1)}"/>`,
    )
    .join('');
  const filters = sheets
    .map((sheet, index) =>
      sheet.header && sheet.rows.length > 0
        ? `<definedName name="_xlnm._FilterDatabase" localSheetId="${String(index)}" hidden="1">'${xmlText(tabName(sheet.name).replace(/'/g, "''"))}'!${rangeOf(sheet).replace(/([A-Z]+)(\d+)/g, '$$$1$$$2')}</definedName>`
        : '',
    )
    .join('');
  return [
    `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`,
    `<workbook xmlns="${mainNamespace}" xmlns:r="${relationshipsNamespace}">`,
    `<bookViews><workbookView/></bookViews>`,
    `<sheets>${tabs}</sheets>`,
    filters === '' ? '' : `<definedNames>${filters}</definedNames>`,
    `</workbook>`,
  ].join('');
}

function partsOf(sheets: readonly XlsxSheet[], colors: XlsxColors): [string, string][] {
  const sheetTypes = sheets
    .map(
      (_, index) =>
        `<Override PartName="/xl/worksheets/sheet${String(index + 1)}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>`,
    )
    .join('');
  const sheetRelationships = sheets
    .map(
      (_, index) =>
        `<Relationship Id="rId${String(index + 1)}" Type="${relationshipsNamespace}/worksheet" Target="worksheets/sheet${String(index + 1)}.xml"/>`,
    )
    .join('');
  return [
    [
      '[Content_Types].xml',
      `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>${sheetTypes}</Types>`,
    ],
    [
      '_rels/.rels',
      `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="${packageRelationships}"><Relationship Id="rId1" Type="${relationshipsNamespace}/officeDocument" Target="xl/workbook.xml"/></Relationships>`,
    ],
    ['xl/workbook.xml', workbookXml(sheets)],
    [
      'xl/_rels/workbook.xml.rels',
      `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="${packageRelationships}">${sheetRelationships}<Relationship Id="rId${String(sheets.length + 1)}" Type="${relationshipsNamespace}/styles" Target="styles.xml"/></Relationships>`,
    ],
    ['xl/styles.xml', stylesXml(colors)],
    ...sheets.map((sheet, index): [string, string] => [
      `xl/worksheets/sheet${String(index + 1)}.xml`,
      sheetXml(sheet),
    ]),
  ];
}

const crcTable = Array.from({ length: 256 }, (_, index) => {
  let value = index;
  for (let bit = 0; bit < 8; bit++) {
    value = value & 1 ? 0xedb88320 ^ (value >>> 1) : value >>> 1;
  }
  return value >>> 0;
});

/** The CRC-32 of the bytes, as the .zip keeps it. */
export function crc32(bytes: Uint8Array): number {
  let crc = 0xffffffff;
  for (const byte of bytes) {
    crc = (crcTable[(crc ^ byte) & 0xff] ?? 0) ^ (crc >>> 8);
  }
  return (crc ^ 0xffffffff) >>> 0;
}

/** The bytes compressed with deflate, when the browser can; otherwise none, and the file goes stored. */
async function deflate(bytes: Uint8Array<ArrayBuffer>): Promise<Uint8Array | undefined> {
  if (typeof CompressionStream === 'undefined') {
    return undefined;
  }
  const stream = new CompressionStream('deflate-raw');
  const writer = stream.writable.getWriter();
  // Written while read, so a large part does not wait for room.
  void writer.write(bytes).then(() => writer.close());
  return new Uint8Array(await new Response(stream.readable).arrayBuffer());
}

/** A .zip with the files, compressed when it makes them smaller. */
export async function zip(
  files: readonly [string, Uint8Array<ArrayBuffer>][],
): Promise<Uint8Array<ArrayBuffer>> {
  const chunks: Uint8Array[] = [];
  const directory: Uint8Array[] = [];
  let offset = 0;
  for (const [name, content] of files) {
    const nameBytes = encoder.encode(name);
    const compressed = await deflate(content);
    const deflated = compressed !== undefined && compressed.length < content.length;
    const data = deflated ? compressed : content;
    const crc = crc32(content);
    const local = new DataView(new ArrayBuffer(30));
    local.setUint32(0, 0x04034b50, true);
    local.setUint16(4, 20, true);
    // Bit 11: the names are UTF-8.
    local.setUint16(6, 0x0800, true);
    local.setUint16(8, deflated ? 8 : 0, true);
    local.setUint16(10, 0, true);
    // 01/01/1980, the first day a .zip can say.
    local.setUint16(12, 0x21, true);
    local.setUint32(14, crc, true);
    local.setUint32(18, data.length, true);
    local.setUint32(22, content.length, true);
    local.setUint16(26, nameBytes.length, true);
    local.setUint16(28, 0, true);
    const central = new DataView(new ArrayBuffer(46));
    central.setUint32(0, 0x02014b50, true);
    central.setUint16(4, 20, true);
    central.setUint16(6, 20, true);
    central.setUint16(8, 0x0800, true);
    central.setUint16(10, deflated ? 8 : 0, true);
    central.setUint16(12, 0, true);
    central.setUint16(14, 0x21, true);
    central.setUint32(16, crc, true);
    central.setUint32(20, data.length, true);
    central.setUint32(24, content.length, true);
    central.setUint16(28, nameBytes.length, true);
    central.setUint32(42, offset, true);
    chunks.push(new Uint8Array(local.buffer), nameBytes, data);
    directory.push(new Uint8Array(central.buffer), nameBytes);
    offset += 30 + nameBytes.length + data.length;
  }
  const directorySize = directory.reduce((size, chunk) => size + chunk.length, 0);
  const end = new DataView(new ArrayBuffer(22));
  end.setUint32(0, 0x06054b50, true);
  end.setUint16(8, files.length, true);
  end.setUint16(10, files.length, true);
  end.setUint32(12, directorySize, true);
  end.setUint32(16, offset, true);
  const all = [...chunks, ...directory, new Uint8Array(end.buffer)];
  const result = new Uint8Array(all.reduce((size, chunk) => size + chunk.length, 0));
  let position = 0;
  for (const chunk of all) {
    result.set(chunk, position);
    position += chunk.length;
  }
  return result;
}

/** The bytes of a .xlsx with the sheets, in order. */
export async function buildXlsx(
  sheets: readonly XlsxSheet[],
  colors: XlsxColors,
): Promise<Uint8Array<ArrayBuffer>> {
  return zip(partsOf(sheets, colors).map(([name, xml]) => [name, encoder.encode(xml)]));
}

/** The media type of a .xlsx, for the file the browser saves. */
export const xlsxType = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
