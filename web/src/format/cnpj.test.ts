import { describe, expect, it } from 'vitest';
import { cleanCnpj, cnpjProblem, formatCnpj, isValidCnpj, maskTypedCnpj } from './cnpj';

// Spec: clientes, requirement "CNPJ do cliente" (screen side, the same cases as the Projects module).
describe('CNPJ do cliente', () => {
  it('CNPJ com e sem pontuação', () => {
    expect(cleanCnpj('11.222.333/0001-81')).toBe('11222333000181');
    expect(cleanCnpj(' 11 222 333 0001 81 ')).toBe('11222333000181');
    expect(isValidCnpj('11222333000181')).toBe(true);
    expect(isValidCnpj('77555888000153')).toBe(true);
  });

  it('CNPJ alfanumérico', () => {
    expect(cleanCnpj('12.abc.345/01de-35')).toBe('12ABC34501DE35');
    expect(isValidCnpj('12ABC34501DE35')).toBe(true);
  });

  it('CNPJ inválido', () => {
    for (const cnpj of [
      '11222333000180',
      '1122233300011',
      '11111111111111',
      '00000000000000',
      '12ABC34501DE36',
      '12ABC34501DE3A',
      '1234567890123456',
    ]) {
      expect(isValidCnpj(cnpj), cnpj).toBe(false);
    }
  });

  it('Motivo do CNPJ inválido', () => {
    expect(cnpjProblem('11222333000181')).toBeNull();
    expect(cnpjProblem('12ABC34501DE35')).toBeNull();
    expect(cnpjProblem('1122233300011')).toBe('incomplete');
    expect(cnpjProblem('1234567890123456')).toBe('incomplete');
    expect(cnpjProblem('11111111111111')).toBe('repeated');
    expect(cnpjProblem('00000000000000')).toBe('repeated');
    expect(cnpjProblem('11222333000180')).toBe('digits');
    expect(cnpjProblem('12ABC34501DE3A')).toBe('digits');
  });

  it('CNPJ formatado', () => {
    expect(formatCnpj('11222333000181')).toBe('11.222.333/0001-81');
    expect(formatCnpj('12ABC34501DE35')).toBe('12.ABC.345/01DE-35');
  });

  it('Máscara do CNPJ ao digitar', () => {
    expect(maskTypedCnpj('11')).toBe('11');
    expect(maskTypedCnpj('112')).toBe('11.2');
    expect(maskTypedCnpj('11222333')).toBe('11.222.333');
    expect(maskTypedCnpj('112223330')).toBe('11.222.333/0');
    expect(maskTypedCnpj('1122233300018')).toBe('11.222.333/0001-8');
    expect(maskTypedCnpj('12abc34501de35')).toBe('12.ABC.345/01DE-35');
    expect(maskTypedCnpj('11.222.333/0001-81999')).toBe('11.222.333/0001-81');
  });
});
