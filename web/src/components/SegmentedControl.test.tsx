import LightModeOutlined from '@mui/icons-material/LightModeOutlined';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { useState } from 'react';
import { describe, expect, it } from 'vitest';
import { AppThemeProvider } from '../theme/AppThemeProvider';
import { SegmentedControl } from './SegmentedControl';

function Choice({ iconsOnly = false }: { iconsOnly?: boolean }) {
  const [value, setValue] = useState<'a' | 'b' | 'c'>('a');
  return (
    <AppThemeProvider>
      <SegmentedControl
        label="Density"
        iconsOnly={iconsOnly}
        value={value}
        options={[
          { value: 'a', label: 'Comfortable', icon: LightModeOutlined },
          { value: 'b', label: 'Compact', icon: LightModeOutlined },
          { value: 'c', label: 'Dense', icon: LightModeOutlined },
        ]}
        onChange={setValue}
      />
    </AppThemeProvider>
  );
}

function checked() {
  return within(screen.getByRole('radiogroup', { name: 'Density' }))
    .getAllByRole('radio')
    .filter((option) => option.getAttribute('aria-checked') === 'true')
    .map((option) => option.getAttribute('aria-label') ?? option.textContent);
}

describe('Controle segmentado', () => {
  it('Escolher pelo clique e pelas setas', () => {
    render(<Choice />);

    fireEvent.click(screen.getByRole('radio', { name: 'Compact' }));
    expect(checked()).toEqual(['Compact']);

    // The arrows move the choice, and around at the ends; only the chosen option is in the tab order.
    fireEvent.keyDown(screen.getByRole('radio', { name: 'Compact' }), { key: 'ArrowRight' });
    expect(checked()).toEqual(['Dense']);
    expect(screen.getByRole('radio', { name: 'Dense' })).toHaveFocus();
    fireEvent.keyDown(screen.getByRole('radio', { name: 'Dense' }), { key: 'ArrowRight' });
    expect(checked()).toEqual(['Comfortable']);
    fireEvent.keyDown(screen.getByRole('radio', { name: 'Comfortable' }), { key: 'ArrowLeft' });
    expect(checked()).toEqual(['Dense']);
    expect(
      within(screen.getByRole('radiogroup'))
        .getAllByRole('radio')
        .map((option) => option.tabIndex),
    ).toEqual([-1, -1, 0]);
  });

  it('Só com ícones', () => {
    render(<Choice iconsOnly />);

    const option = screen.getByRole('radio', { name: 'Compact' });

    expect(option).toHaveAttribute('title', 'Compact');
    expect(option).toHaveTextContent('');
    expect(option.querySelector('svg')).not.toBeNull();
  });
});
