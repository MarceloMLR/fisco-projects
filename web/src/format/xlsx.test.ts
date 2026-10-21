import { describe, expect, it } from 'vitest';
import { unzip } from '../test/unzip';
import { buildXlsx, columnName, crc32, xmlText, zip } from './xlsx';

const colors = { headerFill: 'f3f5f8', headerText: '1b1f24', border: 'dde2e8' };
const encoder = new TextEncoder();

// Design: relatorio-da-auditoria (ADR-0099), the writer of spreadsheets.
describe('Planilha .xlsx', () => {
  it('Nomes das colunas', () => {
    expect([0, 1, 25, 26, 27, 51, 52, 701, 702].map(columnName)).toEqual([
      'A',
      'B',
      'Z',
      'AA',
      'AB',
      'AZ',
      'BA',
      'ZZ',
      'AAA',
    ]);
  });

  it('Texto escapado no XML', () => {
    expect(xmlText('Gerente & líder <técnico> "sim"\u0007')).toBe(
      'Gerente &amp; líder &lt;técnico&gt; &quot;sim&quot;',
    );
  });

  it('CRC-32 do .zip', () => {
    expect(crc32(encoder.encode('123456789')).toString(16)).toBe('cbf43926');
  });

  it('Arquivos do .zip', async () => {
    const long = 'auditoria '.repeat(200);

    const files = await unzip(
      await zip([
        ['curto.txt', encoder.encode('oi')],
        ['pasta/longo.xml', encoder.encode(long)],
      ]),
    );

    expect([...files.entries()]).toEqual([
      ['curto.txt', 'oi'],
      ['pasta/longo.xml', long],
    ]);
  });

  it('Abas, cabeçalho, datas e textos', async () => {
    const files = await unzip(
      await buildXlsx(
        [
          {
            name: 'Auditoria',
            widths: [18, 30],
            header: true,
            rows: [
              ['Data e hora', 'O que'],
              [{ dateTime: 46315.5 }, 'Trocou um papel\nUsuário → Gerente'],
              [{ dateTime: 46316.25 }, 'Entrou & saiu'],
            ],
          },
          { name: 'Filtros', widths: [20, 40], header: false, rows: [[{ label: 'Registros' }, 2]] },
        ],
        colors,
      ),
    );

    expect([...files.keys()]).toEqual([
      '[Content_Types].xml',
      '_rels/.rels',
      'xl/workbook.xml',
      'xl/_rels/workbook.xml.rels',
      'xl/styles.xml',
      'xl/worksheets/sheet1.xml',
      'xl/worksheets/sheet2.xml',
    ]);
    const workbook = files.get('xl/workbook.xml') ?? '';
    expect(workbook).toContain('<sheet name="Auditoria" sheetId="1" r:id="rId1"/>');
    expect(workbook).toContain('<sheet name="Filtros" sheetId="2" r:id="rId2"/>');
    expect(workbook).toContain("'Auditoria'!$A$1:$B$3");
    const audit = files.get('xl/worksheets/sheet1.xml') ?? '';
    expect(audit).toContain('state="frozen"');
    expect(audit).toContain('<autoFilter ref="A1:B3"/>');
    expect(audit).toContain('<col min="1" max="1" width="18" customWidth="1"/>');
    expect(audit).toContain(
      '<c r="A1" t="inlineStr" s="1"><is><t xml:space="preserve">Data e hora</t></is></c>',
    );
    expect(audit).toContain('<c r="A2" s="3"><v>46315.5</v></c>');
    expect(audit).toContain('Trocou um papel\nUsuário → Gerente');
    expect(audit).toContain('Entrou &amp; saiu');
    const filters = files.get('xl/worksheets/sheet2.xml') ?? '';
    expect(filters).not.toContain('autoFilter');
    expect(filters).toContain(
      '<c r="A1" t="inlineStr" s="5"><is><t xml:space="preserve">Registros</t></is></c>',
    );
    expect(filters).toContain('<c r="B1" s="4"><v>2</v></c>');
    const styles = files.get('xl/styles.xml') ?? '';
    expect(styles).toContain('<fgColor rgb="FFF3F5F8"/>');
    expect(styles).toContain('formatCode="dd/mm/yyyy hh:mm"');
  });
});
