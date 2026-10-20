import { loadDemo } from './load.ts';

/**
 * `npm run demo:data [endereço da API]`: loads the demonstration data into a development installation (ADR-0089). The
 * API must be running, with the local sign-in on (ADR-0078).
 */
const baseUrl = process.argv[2] ?? 'http://localhost:5175';

try {
  console.log(`Carregando os dados de demonstração pela API em ${baseUrl}...`);
  await loadDemo(baseUrl, (line) => {
    console.log(line);
  });
} catch (error) {
  const reason =
    error instanceof TypeError
      ? `não foi possível falar com a API em ${baseUrl}: ela está rodando?`
      : String(error instanceof Error ? error.message : error);
  console.error(`Os dados de demonstração não foram carregados: ${reason}`);
  process.exitCode = 1;
}
