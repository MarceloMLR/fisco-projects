import Box from '@mui/material/Box';
import Drawer from '@mui/material/Drawer';
import Popover from '@mui/material/Popover';
import useMediaQuery from '@mui/material/useMediaQuery';
import {
  type ChangeEvent,
  type KeyboardEvent,
  type PointerEvent,
  useEffect,
  useId,
  useRef,
  useState,
} from 'react';
import { useTranslation } from 'react-i18next';
import {
  type ColorFormat,
  formatColor,
  hexOfHsv,
  type Hsv,
  hsvOf,
  hueGradient,
  parseColor,
} from '../theme/colorFormats';
import { Button } from './Button';
import { SegmentedControl } from './SegmentedControl';

// The white and the black of the area of saturation and value, and of the rings of the thumbs: any color goes under
// them, so they are no color of a theme.
const white = hexOfHsv({ h: 0, s: 0, v: 1 });
const black = hexOfHsv({ h: 0, s: 0, v: 0 });

const clamp = (value: number) => Math.min(1, Math.max(0, value));

const thumbSx = {
  position: 'absolute',
  width: 18,
  height: 18,
  borderRadius: 'var(--radius-full)',
  border: `2px solid ${white}`,
  boxShadow: `0 0 0 1px ${black}`,
  transform: 'translate(-50%, -50%)',
  pointerEvents: 'none',
  boxSizing: 'border-box',
} as const;

const focusSx = {
  outline: 'none',
  '&:focus-visible': { boxShadow: '0 0 0 2px var(--bg-elevated), 0 0 0 4px var(--focus-ring)' },
} as const;

/** The position of the pointer in a box, from 0 to 1 on each side. */
function positionIn(event: PointerEvent<HTMLElement>): { x: number; y: number } {
  const box = event.currentTarget.getBoundingClientRect();
  return {
    x: box.width === 0 ? 0 : clamp((event.clientX - box.left) / box.width),
    y: box.height === 0 ? 0 : clamp((event.clientY - box.top) / box.height),
  };
}

/** Follows the pointer from the press to the release, also outside the box (by the capture of the pointer). */
function useDragging(onMove: (event: PointerEvent<HTMLElement>) => void) {
  const pressed = useRef(false);
  const release = () => {
    pressed.current = false;
  };
  return {
    onPointerDown: (event: PointerEvent<HTMLElement>) => {
      pressed.current = true;
      // Some environments, as the one of the tests, have no capture of the pointer.
      (event.currentTarget as Partial<Pick<HTMLElement, 'setPointerCapture'>>).setPointerCapture?.(
        event.pointerId,
      );
      onMove(event);
    },
    onPointerMove: (event: PointerEvent<HTMLElement>) => {
      if (pressed.current) {
        onMove(event);
      }
    },
    onPointerUp: release,
    onPointerCancel: release,
  };
}

/** A color beside its name, as the current and the new one. */
function Swatch({ color, text, testId }: { color: string; text: string; testId: string }) {
  return (
    <>
      <Box
        data-testid={testId}
        sx={{
          width: 40,
          height: 24,
          borderRadius: 'var(--radius-sm)',
          border: '1px solid var(--border-strong)',
          backgroundColor: color,
        }}
      />
      <span>{text}</span>
    </>
  );
}

/**
 * What the picker shows: the area of saturation and value and the hue bar, by the pointer and by the arrows (Shift
 * for bigger steps), the current color beside the new one, the format and the value in it, which takes the color
 * typed or pasted in any format, and "Cancelar" and "Usar esta cor".
 */
function PickerBody({
  label,
  initial,
  onChange,
  onUse,
  onCancel,
}: {
  label: string;
  initial: string;
  onChange: (hex: string) => void;
  onUse: (hex: string) => void;
  onCancel: () => void;
}) {
  const { t } = useTranslation();
  const titleId = useId();
  const valueId = useId();
  const hintId = useId();
  const [hsv, setHsv] = useState<Hsv>(() => hsvOf(initial));
  const [format, setFormat] = useState<ColorFormat>('hex');
  // The value as typed, until it is a color; then it follows the area and the hue.
  const [typed, setTyped] = useState<string | undefined>(undefined);
  const hex = hexOfHsv(hsv);
  const typedInvalid = typed !== undefined && parseColor(typed) === undefined;
  const area = useRef<HTMLDivElement>(null);

  useEffect(() => {
    area.current?.focus();
  }, []);

  const change = (next: Hsv) => {
    setHsv(next);
    setTyped(undefined);
    onChange(hexOfHsv(next));
  };

  const areaDragging = useDragging((event) => {
    const { x, y } = positionIn(event);
    change({ ...hsv, s: x, v: 1 - y });
  });
  const hueDragging = useDragging((event) => {
    change({ ...hsv, h: positionIn(event).x * 359.9 });
  });

  const areaKeys = (event: KeyboardEvent<HTMLElement>) => {
    const step = event.shiftKey ? 0.1 : 0.01;
    const moves: Record<string, Partial<Hsv>> = {
      ArrowLeft: { s: clamp(hsv.s - step) },
      ArrowRight: { s: clamp(hsv.s + step) },
      ArrowUp: { v: clamp(hsv.v + step) },
      ArrowDown: { v: clamp(hsv.v - step) },
    };
    const move = moves[event.key];
    if (move !== undefined) {
      event.preventDefault();
      change({ ...hsv, ...move });
    }
  };
  const hueKeys = (event: KeyboardEvent<HTMLElement>) => {
    const step = event.shiftKey ? 10 : 1;
    const direction =
      event.key === 'ArrowLeft' || event.key === 'ArrowDown'
        ? -1
        : event.key === 'ArrowRight' || event.key === 'ArrowUp'
          ? 1
          : 0;
    if (direction !== 0) {
      event.preventDefault();
      change({ ...hsv, h: (hsv.h + direction * step + 360) % 360 });
    }
  };

  const saturation = Math.round(hsv.s * 100);
  const value = Math.round(hsv.v * 100);

  return (
    <Box
      sx={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}
      onKeyDown={(event) => {
        if (event.key === 'Enter' && event.target instanceof HTMLInputElement && !typedInvalid) {
          event.preventDefault();
          onUse(hex);
        }
      }}
    >
      <Box id={titleId} sx={{ font: 'inherit', fontWeight: 500 }}>
        {label}
      </Box>
      <Box
        ref={area}
        role="slider"
        tabIndex={0}
        aria-label={t('colors.picker.area')}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={saturation}
        aria-valuetext={t('colors.picker.areaValue', { saturation, value })}
        onKeyDown={areaKeys}
        {...areaDragging}
        sx={{
          position: 'relative',
          height: { xs: 200, sm: 170 },
          borderRadius: 'var(--radius-md)',
          cursor: 'crosshair',
          touchAction: 'none',
          backgroundColor: hexOfHsv({ h: hsv.h, s: 1, v: 1 }),
          backgroundImage: `linear-gradient(to top, ${black}, transparent), linear-gradient(to right, ${white}, transparent)`,
          ...focusSx,
        }}
      >
        <Box
          sx={{
            ...thumbSx,
            left: `${String(hsv.s * 100)}%`,
            top: `${String((1 - hsv.v) * 100)}%`,
            backgroundColor: hex,
          }}
        />
      </Box>
      <Box
        role="slider"
        tabIndex={0}
        aria-label={t('colors.picker.hue')}
        aria-valuemin={0}
        aria-valuemax={360}
        aria-valuenow={Math.round(hsv.h)}
        onKeyDown={hueKeys}
        {...hueDragging}
        sx={{
          position: 'relative',
          height: 14,
          borderRadius: 'var(--radius-full)',
          cursor: 'pointer',
          touchAction: 'none',
          backgroundImage: hueGradient(),
          ...focusSx,
        }}
      >
        <Box
          sx={{
            ...thumbSx,
            top: '50%',
            left: `${String((hsv.h / 360) * 100)}%`,
            backgroundColor: hexOfHsv({ h: hsv.h, s: 1, v: 1 }),
          }}
        />
      </Box>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 'var(--space-2)',
          fontSize: 12,
          lineHeight: '16px',
          color: 'var(--text-secondary)',
        }}
      >
        <Swatch color={initial} text={t('colors.picker.current')} testId="picker-current" />
        <Swatch color={hex} text={t('colors.picker.new')} testId="picker-new" />
      </Box>
      <SegmentedControl
        label={t('colors.picker.format')}
        value={format}
        options={[
          { value: 'hex', label: 'HEX' },
          { value: 'rgb', label: 'RGB' },
          { value: 'hsl', label: 'HSL' },
        ]}
        onChange={(next) => {
          setFormat(next);
          setTyped(undefined);
        }}
      />
      <Box
        component="input"
        id={valueId}
        aria-label={t('colors.picker.value')}
        aria-describedby={hintId}
        aria-invalid={typedInvalid ? true : undefined}
        autoComplete="off"
        spellCheck={false}
        value={typed ?? formatColor(hex, format)}
        onChange={(event: ChangeEvent<HTMLInputElement>) => {
          const text = event.target.value;
          setTyped(text);
          const parsed = parseColor(text);
          if (parsed !== undefined) {
            setHsv(hsvOf(parsed));
            onChange(parsed);
          }
        }}
        onBlur={() => {
          if (!typedInvalid) {
            setTyped(undefined);
          }
        }}
        sx={{
          height: 36,
          boxSizing: 'border-box',
          width: '100%',
          padding: '0 10px',
          border: '1px solid var(--border-control)',
          borderRadius: 'var(--radius-md)',
          backgroundColor: 'var(--bg-surface)',
          color: 'var(--text)',
          font: 'inherit',
          fontFamily: 'var(--font-mono)',
          fontSize: 13,
          '&[aria-invalid="true"]': { borderColor: 'var(--danger)' },
          '&:focus-visible': { outline: '2px solid var(--focus-ring)', outlineOffset: 1 },
        }}
      />
      <Box
        id={hintId}
        sx={{
          fontSize: 12,
          lineHeight: '16px',
          color: typedInvalid ? 'var(--status-danger-text)' : 'var(--text-secondary)',
        }}
      >
        {typedInvalid ? t('colors.invalid') : t('colors.picker.hint')}
      </Box>
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 'var(--space-2)' }}>
        <Button variant="outlined" onClick={onCancel}>
          {t('colors.picker.cancel')}
        </Button>
        <Button
          variant="contained"
          disabled={typedInvalid}
          onClick={() => {
            onUse(hex);
          }}
        >
          {t('colors.picker.use')}
        </Button>
      </Box>
    </Box>
  );
}

/**
 * The color picker of FiscoProjects (ADR-0146): a popover under the swatch on the computer and a sheet that rises from
 * the foot of the screen on narrow screens. Esc, a click outside and "Cancelar" give back the color of before; the
 * colors chosen on the way go to onChange, for the preview.
 */
export function ColorPicker({
  anchor,
  label,
  initial,
  onChange,
  onUse,
  onCancel,
}: {
  /** The swatch that opens the picker; null while it is closed. */
  anchor: HTMLElement | null;
  label: string;
  /** The color the picker starts from, #RRGGBB. */
  initial: string;
  onChange: (hex: string) => void;
  onUse: (hex: string) => void;
  onCancel: () => void;
}) {
  const narrow = useMediaQuery('(max-width: 600px)', { noSsr: true });
  const open = anchor !== null;
  const body = open ? (
    <PickerBody
      key={initial}
      label={label}
      initial={initial}
      onChange={onChange}
      onUse={onUse}
      onCancel={onCancel}
    />
  ) : null;
  const paper = { role: 'dialog', 'aria-label': label } as const;

  if (narrow) {
    return (
      <Drawer
        anchor="bottom"
        open={open}
        onClose={onCancel}
        slotProps={{
          paper: {
            ...paper,
            sx: {
              boxSizing: 'border-box',
              padding: 'var(--space-4)',
              paddingLeft: 'max(var(--space-4), env(safe-area-inset-left, 0px))',
              paddingRight: 'max(var(--space-4), env(safe-area-inset-right, 0px))',
              paddingBottom: 'calc(var(--space-4) + env(safe-area-inset-bottom, 0px))',
              borderRadius: 'var(--radius-xl) var(--radius-xl) 0 0',
              backgroundColor: 'var(--bg-elevated)',
              backgroundImage: 'none',
            },
          },
          backdrop: { sx: { backgroundColor: 'var(--overlay)' } },
        }}
      >
        {body}
      </Drawer>
    );
  }
  return (
    <Popover
      open={open}
      anchorEl={anchor}
      onClose={onCancel}
      anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
      slotProps={{
        paper: {
          ...paper,
          sx: {
            marginTop: 'var(--space-1)',
            width: 300,
            maxWidth: 'calc(100vw - 32px)',
            boxSizing: 'border-box',
            padding: 'var(--space-4)',
            borderRadius: 'var(--radius-lg)',
          },
        },
      }}
    >
      {body}
    </Popover>
  );
}
