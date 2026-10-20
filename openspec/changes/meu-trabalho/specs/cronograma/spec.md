## ADDED Requirements

### Requirement: Endereço do detalhe

O endereço do cronograma com o item (`?item=`) MUST abrir os detalhes da tarefa ou do marco na gaveta, como a Lista; fechar a gaveta MUST tirar o item do endereço, e um item que não está mais no cronograma MUST deixar a aba como sem item escolhido (ADR-0081).

#### Scenario: Detalhe pelo endereço
- **WHEN** a pessoa abre o cronograma do projeto com o endereço de uma tarefa
- **THEN** os detalhes da tarefa abrem na gaveta

#### Scenario: Item que saiu do cronograma
- **WHEN** a pessoa abre o endereço de uma tarefa já excluída
- **THEN** a aba abre sem a gaveta
